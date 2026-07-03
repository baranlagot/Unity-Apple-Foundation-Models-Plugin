import Foundation

typealias AFMEventCallback = @convention(c) (
    UnsafePointer<CChar>?
) -> Void

private final class AFMCallbackStore: @unchecked Sendable {
    private let lock = NSLock()
    private var callback: AFMEventCallback?
    private var debugLoggingEnabled = false

    func setCallback(_ callback: AFMEventCallback?) {
        lock.lock()
        self.callback = callback
        lock.unlock()
    }

    func setDebugLogging(enabled: Bool) {
        lock.lock()
        debugLoggingEnabled = enabled
        lock.unlock()
    }

    func emit(_ event: AFMEvent) {
        let encoder = JSONEncoder()
        guard
            let data = try? encoder.encode(event),
            let json = String(data: data, encoding: .utf8)
        else {
            log("Failed to encode native event for request \(event.requestId).")
            return
        }

        lock.lock()
        let currentCallback = callback
        lock.unlock()
        guard let currentCallback else {
            log("No Unity event callback is registered.")
            return
        }

        json.withCString { pointer in
            currentCallback(pointer)
        }
    }

    func log(_ message: String) {
        lock.lock()
        let enabled = debugLoggingEnabled
        lock.unlock()
        if enabled {
            print("[AppleFoundationModels] \(message)")
        }
    }
}

private final class AFMBridgeRuntime: @unchecked Sendable {
    static let shared = AFMBridgeRuntime()

    private let callbackStore = AFMCallbackStore()
    private let core = AppleFoundationModelsCore()

    func setCallback(_ callback: AFMEventCallback?) {
        callbackStore.setCallback(callback)
    }

    func setDebugLogging(enabled: Bool) {
        callbackStore.setDebugLogging(enabled: enabled)
    }

    func getAvailability(requestIdPointer: UnsafePointer<CChar>?) {
        guard let requestId = readRequiredString(requestIdPointer, name: "requestId") else {
            return
        }

        let emit = makeEventSink()
        Task {
            await core.getAvailability(
                requestId: requestId,
                emit: emit
            )
        }
    }

    func generateText(
        requestIdPointer: UnsafePointer<CChar>?,
        promptPointer: UnsafePointer<CChar>?,
        optionsPointer: UnsafePointer<CChar>?
    ) {
        guard let request = readRequest(
            requestIdPointer: requestIdPointer,
            promptPointer: promptPointer,
            optionsPointer: optionsPointer
        ) else {
            return
        }

        let emit = makeEventSink()
        Task {
            await core.generateText(
                requestId: request.requestId,
                prompt: request.prompt,
                options: request.options,
                emit: emit
            )
        }
    }

    func streamText(
        requestIdPointer: UnsafePointer<CChar>?,
        promptPointer: UnsafePointer<CChar>?,
        optionsPointer: UnsafePointer<CChar>?
    ) {
        guard let request = readRequest(
            requestIdPointer: requestIdPointer,
            promptPointer: promptPointer,
            optionsPointer: optionsPointer
        ) else {
            return
        }

        let emit = makeEventSink()
        Task {
            await core.streamText(
                requestId: request.requestId,
                prompt: request.prompt,
                options: request.options,
                emit: emit
            )
        }
    }

    func cancel(requestIdPointer: UnsafePointer<CChar>?) {
        guard let requestId = readRequiredString(requestIdPointer, name: "requestId") else {
            return
        }

        Task {
            await core.cancel(requestId: requestId)
        }
    }

    private func readRequest(
        requestIdPointer: UnsafePointer<CChar>?,
        promptPointer: UnsafePointer<CChar>?,
        optionsPointer: UnsafePointer<CChar>?
    ) -> (requestId: String, prompt: String, options: AFMGenerationOptions)? {
        guard
            let requestId = readRequiredString(requestIdPointer, name: "requestId"),
            let prompt = readRequiredString(promptPointer, name: "prompt")
        else {
            return nil
        }

        let options: AFMGenerationOptions
        if let optionsPointer {
            let optionsJson = String(cString: optionsPointer)
            do {
                options = try JSONDecoder().decode(
                    AFMGenerationOptions.self,
                    from: Data(optionsJson.utf8)
                )
            } catch {
                callbackStore.emit(.failure(
                    requestId: requestId,
                    code: "invalidOptions",
                    message: "The native bridge could not decode generation options."
                ))
                callbackStore.log("Options decode failed: \(error)")
                return nil
            }
        } else {
            options = .default
        }

        return (requestId, prompt, options)
    }

    private func readRequiredString(
        _ pointer: UnsafePointer<CChar>?,
        name: String
    ) -> String? {
        guard let pointer else {
            callbackStore.log("Missing required C string: \(name).")
            return nil
        }

        return String(cString: pointer)
    }

    private func makeEventSink() -> AppleFoundationModelsCore.EventSink {
        let callbackStore = callbackStore
        return { event in
            callbackStore.emit(event)
        }
    }
}

@_cdecl("AFM_SetEventCallback")
func AFM_SetEventCallback(_ callback: AFMEventCallback?) {
    AFMBridgeRuntime.shared.setCallback(callback)
}

@_cdecl("AFM_SetDebugLogging")
func AFM_SetDebugLogging(_ enabled: UInt8) {
    AFMBridgeRuntime.shared.setDebugLogging(enabled: enabled != 0)
}

@_cdecl("AFM_GetAvailability")
func AFM_GetAvailability(_ requestId: UnsafePointer<CChar>?) {
    AFMBridgeRuntime.shared.getAvailability(requestIdPointer: requestId)
}

@_cdecl("AFM_GenerateText")
func AFM_GenerateText(
    _ requestId: UnsafePointer<CChar>?,
    _ prompt: UnsafePointer<CChar>?,
    _ optionsJson: UnsafePointer<CChar>?
) {
    AFMBridgeRuntime.shared.generateText(
        requestIdPointer: requestId,
        promptPointer: prompt,
        optionsPointer: optionsJson
    )
}

@_cdecl("AFM_StreamText")
func AFM_StreamText(
    _ requestId: UnsafePointer<CChar>?,
    _ prompt: UnsafePointer<CChar>?,
    _ optionsJson: UnsafePointer<CChar>?
) {
    AFMBridgeRuntime.shared.streamText(
        requestIdPointer: requestId,
        promptPointer: prompt,
        optionsPointer: optionsJson
    )
}

@_cdecl("AFM_CancelRequest")
func AFM_CancelRequest(_ requestId: UnsafePointer<CChar>?) {
    AFMBridgeRuntime.shared.cancel(requestIdPointer: requestId)
}
