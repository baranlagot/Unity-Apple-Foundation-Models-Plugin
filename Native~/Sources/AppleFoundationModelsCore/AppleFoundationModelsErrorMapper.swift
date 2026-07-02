import Foundation

#if canImport(FoundationModels)
import FoundationModels
#endif

struct AFMMappedError: Sendable {
    let status: String
    let code: String
    let message: String
}

enum AFMErrorMapper {
    static func map(_ error: Error) -> AFMMappedError {
        if error is CancellationError {
            return AFMMappedError(
                status: "Unknown",
                code: "cancelled",
                message: "The Apple Foundation Models request was cancelled."
            )
        }

        if let bridgeError = error as? AFMBridgeError {
            switch bridgeError {
            case .duplicateRequest:
                return AFMMappedError(
                    status: "Unknown",
                    code: "duplicateRequest",
                    message: "A native request with the same ID is already active."
                )
            case .invalidOptions:
                return AFMMappedError(
                    status: "Unknown",
                    code: "invalidOptions",
                    message: "The generation options were invalid."
                )
            case .nonMonotonicStream:
                return AFMMappedError(
                    status: "Unknown",
                    code: "nonMonotonicStream",
                    message: "The model returned an unsupported streaming snapshot."
                )
            }
        }

#if canImport(FoundationModels)
        if #available(iOS 26.0, macOS 26.0, *),
           let generationError = error as? LanguageModelSession.GenerationError {
            switch generationError {
            case .assetsUnavailable:
                return AFMMappedError(
                    status: "ModelNotReady",
                    code: "assetsUnavailable",
                    message: "The on-device model assets are not available."
                )
            case .exceededContextWindowSize:
                return AFMMappedError(
                    status: "Unknown",
                    code: "contextWindowExceeded",
                    message: "The request exceeded the model's context window."
                )
            case .guardrailViolation:
                return AFMMappedError(
                    status: "Unknown",
                    code: "guardrailViolation",
                    message: "The request or response was blocked by model safety protections."
                )
            case .rateLimited:
                return AFMMappedError(
                    status: "Unknown",
                    code: "rateLimited",
                    message: "The on-device model is temporarily rate limited."
                )
            case .refusal:
                return AFMMappedError(
                    status: "Unknown",
                    code: "refusal",
                    message: "The on-device model declined the request."
                )
            case .concurrentRequests:
                return AFMMappedError(
                    status: "Unknown",
                    code: "concurrentSessionRequest",
                    message: "The model session is already processing a request."
                )
            case .unsupportedLanguageOrLocale:
                return AFMMappedError(
                    status: "Unknown",
                    code: "unsupportedLanguageOrLocale",
                    message: "The requested language or locale is not supported."
                )
            case .unsupportedGuide:
                return AFMMappedError(
                    status: "Unknown",
                    code: "unsupportedGuide",
                    message: "The requested structured generation guide is not supported."
                )
            case .decodingFailure:
                return AFMMappedError(
                    status: "Unknown",
                    code: "decodingFailure",
                    message: "The model response could not be decoded."
                )
            @unknown default:
                break
            }
        }
#endif

        return AFMMappedError(
            status: "Unknown",
            code: "nativeFailure",
            message: "Apple Foundation Models could not complete the request."
        )
    }
}
