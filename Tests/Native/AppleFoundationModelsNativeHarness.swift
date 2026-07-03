import Foundation

@main
struct AppleFoundationModelsNativeHarness {
    static func main() async {
        do {
            try testEventEncoding()
            try testErrorMapping()
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

    static func testInvalidOptions() async throws {
        let core = AppleFoundationModelsCore()
        var events = [AFMEvent]()
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

        try await waitFor { !events.isEmpty }
        try expect(events[0].type == "error", "Expected invalid options to produce an error event.")
        try expect(events[0].errorCode == "invalidOptions", "Expected invalidOptions error code.")
    }

    static func testDuplicateRequest() async throws {
        let core = AppleFoundationModelsCore()
        var events = [AFMEvent]()

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
            events.contains(where: { $0.errorCode == "duplicateRequest" })
        }
    }

    static func testCancelledBeforeStart() async throws {
        let core = AppleFoundationModelsCore()
        var events = [AFMEvent]()

        await core.cancel(requestId: "cancel-before-start")
        await core.getAvailability(
            requestId: "cancel-before-start",
            emit: { events.append($0) }
        )

        try await Task.sleep(nanoseconds: 50_000_000)
        try expect(events.isEmpty, "Cancelled-before-start requests should not emit events.")
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
