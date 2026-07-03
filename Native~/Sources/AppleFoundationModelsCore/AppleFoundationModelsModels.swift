import Foundation

struct AFMGenerationOptions: Decodable, Sendable {
    let instructions: String
    let hasTemperature: Bool
    let temperature: Float
    let hasMaxOutputTokens: Bool
    let maxOutputTokens: Int
    let sessionId: String
    let preferStructuredOutput: Bool

    static let `default` = AFMGenerationOptions(
        instructions: "",
        hasTemperature: false,
        temperature: 0,
        hasMaxOutputTokens: false,
        maxOutputTokens: 0,
        sessionId: "",
        preferStructuredOutput: false
    )
}

struct AFMEvent: Encodable, Sendable {
    let requestId: String
    let type: String
    let payload: String
    let status: String
    let errorCode: String
    let errorMessage: String

    static func availability(
        requestId: String,
        status: String,
        message: String
    ) -> AFMEvent {
        AFMEvent(
            requestId: requestId,
            type: "availability",
            payload: message,
            status: status,
            errorCode: "",
            errorMessage: ""
        )
    }

    static func text(requestId: String, content: String) -> AFMEvent {
        AFMEvent(
            requestId: requestId,
            type: "text",
            payload: content,
            status: "",
            errorCode: "",
            errorMessage: ""
        )
    }

    static func streamDelta(requestId: String, content: String) -> AFMEvent {
        AFMEvent(
            requestId: requestId,
            type: "streamDelta",
            payload: content,
            status: "",
            errorCode: "",
            errorMessage: ""
        )
    }

    static func complete(requestId: String, content: String) -> AFMEvent {
        AFMEvent(
            requestId: requestId,
            type: "complete",
            payload: content,
            status: "",
            errorCode: "",
            errorMessage: ""
        )
    }

    static func failure(
        requestId: String,
        status: String = "Unknown",
        code: String,
        message: String
    ) -> AFMEvent {
        AFMEvent(
            requestId: requestId,
            type: "error",
            payload: "",
            status: status,
            errorCode: code,
            errorMessage: message
        )
    }
}

enum AFMBridgeError: Error {
    case duplicateRequest
    case invalidOptions
    case nonMonotonicStream
}

/// Converts the model's cumulative streaming snapshots into ordered, non-overlapping
/// deltas. Kept free of any FoundationModels dependency so the conversion contract can
/// be validated without Apple Intelligence hardware.
struct AFMStreamAccumulator: Sendable {
    private(set) var accumulated: String = ""

    /// Returns the newly appended text for a cumulative snapshot. The returned delta may
    /// be empty when a snapshot repeats the previous content. Throws
    /// `AFMBridgeError.nonMonotonicStream` when a snapshot is not an extension of the
    /// previously observed content.
    mutating func delta(for snapshot: String) throws -> String {
        guard snapshot.hasPrefix(accumulated) else {
            throw AFMBridgeError.nonMonotonicStream
        }

        let delta = String(snapshot.dropFirst(accumulated.count))
        accumulated = snapshot
        return delta
    }
}
