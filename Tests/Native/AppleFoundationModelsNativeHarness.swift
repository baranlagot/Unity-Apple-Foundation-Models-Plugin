import Foundation

@main
struct AppleFoundationModelsNativeHarness {
    static func main() async {
        do {
            try testEventEncoding()
            try testErrorMapping()
            try testStableErrorCodes()
            try testStreamAccumulatorOrderedDeltas()
            try testStreamAccumulatorSuppressesEmptyDeltas()
            try testStreamAccumulatorRejectsNonMonotonic()
            try await testInvalidOptions()
            try await testDuplicateRequest()
            try await testCancelledBeforeStart()
            print("AppleFoundationModels native harness passed.")
        } catch {
            fputs("Native harness failure: \(error)\n", stderr)
            exit(1)
        }
    }

    static func testEventEncoding() throws {
        let event = AFMEvent.availability(
            requestId: "availability-request",
            status: "Available",
            message: "Available."
        )
        let encoded = try JSONEncoder().encode(event)
        let json = try unwrap(String(data: encoded, encoding: .utf8))
        try expect(json.contains("\"type\":\"availability\""), "Expected availability event JSON.")
        try expect(json.contains("\"status\":\"Available\""), "Expected status in event JSON.")
    }

    static func testErrorMapping() throws {
        let mapped = AFMErrorMapper.map(AFMBridgeError.invalidOptions)
        try expect(mapped.code == "invalidOptions", "Expected invalidOptions error code.")
        try expect(mapped.message.contains("invalid"), "Expected invalid options message.")
    }

    static func testStableErrorCodes() throws {
        // Every bridge-owned error must map to a stable, hardware-independent code and a
        // non-empty message. These are the codes C# depends on regardless of the SDK.
        let expected: [(Error, String)] = [
            (AFMBridgeError.duplicateRequest, "duplicateRequest"),
            (AFMBridgeError.invalidOptions, "invalidOptions"),
            (AFMBridgeError.nonMonotonicStream, "nonMonotonicStream"),
            (CancellationError(), "cancelled"),
            (NSError(domain: "AFMTest", code: 42), "nativeFailure")
        ]

        for (error, code) in expected {
            let mapped = AFMErrorMapper.map(error)
            try expect(mapped.code == code, "Expected error code \(code), got \(mapped.code).")
            try expect(!mapped.message.isEmpty, "Expected a non-empty message for \(code).")
            try expect(!mapped.status.isEmpty, "Expected a non-empty status for \(code).")
        }
    }

    static func testStreamAccumulatorOrderedDeltas() throws {
        var accumulator = AFMStreamAccumulator()
        try expect(try accumulator.delta(for: "He") == "He", "Expected first delta 'He'.")
        try expect(try accumulator.delta(for: "Hello") == "llo", "Expected delta 'llo'.")
        try expect(try accumulator.delta(for: "Hello World") == " World", "Expected delta ' World'.")
        try expect(accumulator.accumulated == "Hello World", "Expected accumulated snapshot.")
    }

    static func testStreamAccumulatorSuppressesEmptyDeltas() throws {
        var accumulator = AFMStreamAccumulator()
        _ = try accumulator.delta(for: "Hi")
        try expect(try accumulator.delta(for: "Hi") == "", "Repeated snapshots must yield an empty delta.")
        try expect(accumulator.accumulated == "Hi", "Accumulated content must be unchanged.")
    }

    static func testStreamAccumulatorRejectsNonMonotonic() throws {
        var accumulator = AFMStreamAccumulator()
        _ = try accumulator.delta(for: "Hello")
        do {
            _ = try accumulator.delta(for: "Help")
            throw HarnessError.assertion("Expected a non-monotonic snapshot to throw.")
        } catch AFMBridgeError.nonMonotonicStream {
            // Expected.
        }
    }

    static func testInvalidOptions() async throws {
        let core = AppleFoundationModelsCore()
        let events = EventCollector()
        let options = AFMGenerationOptions(
            instructions: "",
            hasTemperature: true,
            temperature: 1.5,
            hasMaxOutputTokens: false,
            maxOutputTokens: 0,
            sessionId: "",
            preferStructuredOutput: false
        )

        await core.generateText(
            requestId: "invalid-options",
            prompt: "hello",
            options: options,
            emit: { events.append($0) }
        )

        try await waitFor { !events.snapshot.isEmpty }
        let observed = events.snapshot
        try expect(observed[0].type == "error", "Expected invalid options to produce an error event.")
        try expect(observed[0].errorCode == "invalidOptions", "Expected invalidOptions error code.")
    }

    static func testDuplicateRequest() async throws {
        let core = AppleFoundationModelsCore()
        let events = EventCollector()

        await core.streamText(
            requestId: "duplicate-request",
            prompt: "hello",
            options: .default,
            emit: { events.append($0) }
        )
        await core.streamText(
            requestId: "duplicate-request",
            prompt: "hello",
            options: .default,
            emit: { events.append($0) }
        )

        try await waitFor {
            events.snapshot.contains(where: { $0.errorCode == "duplicateRequest" })
        }
    }

    static func testCancelledBeforeStart() async throws {
        let core = AppleFoundationModelsCore()
        let events = EventCollector()

        await core.cancel(requestId: "cancel-before-start")
        await core.getAvailability(
            requestId: "cancel-before-start",
            emit: { events.append($0) }
        )

        try await Task.sleep(nanoseconds: 50_000_000)
        try expect(events.snapshot.isEmpty, "Cancelled-before-start requests should not emit events.")
    }

    static func waitFor(
        timeoutNanoseconds: UInt64 = 500_000_000,
        condition: @escaping @Sendable () -> Bool
    ) async throws {
        let deadline = DispatchTime.now().uptimeNanoseconds + timeoutNanoseconds
        while !condition() {
            if DispatchTime.now().uptimeNanoseconds >= deadline {
                throw HarnessError.timeout
            }

            try await Task.sleep(nanoseconds: 10_000_000)
        }
    }

    static func expect(_ condition: Bool, _ message: String) throws {
        if !condition {
            throw HarnessError.assertion(message)
        }
    }

    static func unwrap<T>(_ value: T?) throws -> T {
        guard let value else {
            throw HarnessError.assertion("Expected value to be present.")
        }

        return value
    }
}

enum HarnessError: Error {
    case assertion(String)
    case timeout
}

/// Thread-safe collector for events emitted from the actor's `@Sendable` sink. The sink
/// fires on the actor's executor while the test task observes progress, so the shared
/// buffer must be synchronized to stay data-race free under complete concurrency checking.
final class EventCollector: @unchecked Sendable {
    private let lock = NSLock()
    private var events: [AFMEvent] = []

    func append(_ event: AFMEvent) {
        lock.lock()
        defer { lock.unlock() }
        events.append(event)
    }

    var snapshot: [AFMEvent] {
        lock.lock()
        defer { lock.unlock() }
        return events
    }
}
