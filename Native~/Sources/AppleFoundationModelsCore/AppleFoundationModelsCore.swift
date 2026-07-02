import Foundation

#if canImport(FoundationModels)
import FoundationModels
#endif

actor AppleFoundationModelsCore {
    typealias EventSink = @Sendable (AFMEvent) -> Void

    private var tasks: [String: Task<Void, Never>] = [:]
    private var cancelledBeforeStart: Set<String> = []

    func getAvailability(requestId: String, emit: @escaping EventSink) {
        start(requestId: requestId, emit: emit) {
            emit(Self.readAvailability(requestId: requestId))
        }
    }

    func generateText(
        requestId: String,
        prompt: String,
        options: AFMGenerationOptions,
        emit: @escaping EventSink
    ) {
        start(requestId: requestId, emit: emit) {
#if canImport(FoundationModels)
            guard #available(iOS 26.0, macOS 26.0, *) else {
                emit(.failure(
                    requestId: requestId,
                    status: "UnsupportedOSVersion",
                    code: "unsupportedOSVersion",
                    message: "Apple Foundation Models require a newer operating system."
                ))
                return
            }

            let session = Self.makeSession(options: options)
            let generationOptions = try Self.makeGenerationOptions(options)
            let response = try await session.respond(
                to: prompt,
                options: generationOptions
            )
            try Task.checkCancellation()
            emit(.text(requestId: requestId, content: response.content))
#else
            emit(.failure(
                requestId: requestId,
                status: "NativeFrameworkUnavailable",
                code: "frameworkUnavailable",
                message: "The Foundation Models framework is unavailable in this SDK."
            ))
#endif
        }
    }

    func streamText(
        requestId: String,
        prompt: String,
        options: AFMGenerationOptions,
        emit: @escaping EventSink
    ) {
        start(requestId: requestId, emit: emit) {
#if canImport(FoundationModels)
            guard #available(iOS 26.0, macOS 26.0, *) else {
                emit(.failure(
                    requestId: requestId,
                    status: "UnsupportedOSVersion",
                    code: "unsupportedOSVersion",
                    message: "Apple Foundation Models require a newer operating system."
                ))
                return
            }

            let session = Self.makeSession(options: options)
            let generationOptions = try Self.makeGenerationOptions(options)
            let stream = session.streamResponse(
                to: prompt,
                options: generationOptions
            )
            var previous = ""

            for try await snapshot in stream {
                try Task.checkCancellation()
                let current = snapshot.content
                guard current.hasPrefix(previous) else {
                    throw AFMBridgeError.nonMonotonicStream
                }

                let delta = String(current.dropFirst(previous.count))
                if !delta.isEmpty {
                    emit(.streamDelta(requestId: requestId, content: delta))
                }
                previous = current
            }

            try Task.checkCancellation()
            emit(.complete(requestId: requestId, content: previous))
#else
            emit(.failure(
                requestId: requestId,
                status: "NativeFrameworkUnavailable",
                code: "frameworkUnavailable",
                message: "The Foundation Models framework is unavailable in this SDK."
            ))
#endif
        }
    }

    func cancel(requestId: String) {
        if let task = tasks.removeValue(forKey: requestId) {
            task.cancel()
            return
        }

        cancelledBeforeStart.insert(requestId)
    }

    private func start(
        requestId: String,
        emit: @escaping EventSink,
        operation: @escaping @Sendable () async throws -> Void
    ) {
        if cancelledBeforeStart.remove(requestId) != nil {
            return
        }

        guard tasks[requestId] == nil else {
            let mapped = AFMErrorMapper.map(AFMBridgeError.duplicateRequest)
            emit(.failure(
                requestId: requestId,
                status: mapped.status,
                code: mapped.code,
                message: mapped.message
            ))
            return
        }

        tasks[requestId] = Task { [weak self] in
            do {
                try await operation()
            } catch is CancellationError {
                // C# owns the cancellation terminal state and ignores late callbacks.
            } catch {
                let mapped = AFMErrorMapper.map(error)
                emit(.failure(
                    requestId: requestId,
                    status: mapped.status,
                    code: mapped.code,
                    message: mapped.message
                ))
            }

            await self?.finish(requestId: requestId)
        }
    }

    private func finish(requestId: String) {
        tasks.removeValue(forKey: requestId)
    }

    private static func readAvailability(requestId: String) -> AFMEvent {
#if canImport(FoundationModels)
        guard #available(iOS 26.0, macOS 26.0, *) else {
            return .availability(
                requestId: requestId,
                status: "UnsupportedOSVersion",
                message: "Apple Foundation Models require a newer operating system."
            )
        }

        switch SystemLanguageModel.default.availability {
        case .available:
            return .availability(
                requestId: requestId,
                status: "Available",
                message: "Apple Foundation Models are available."
            )
        case .unavailable(.deviceNotEligible):
            return .availability(
                requestId: requestId,
                status: "UnsupportedDevice",
                message: "This device does not support Apple Intelligence."
            )
        case .unavailable(.appleIntelligenceNotEnabled):
            return .availability(
                requestId: requestId,
                status: "AppleIntelligenceDisabled",
                message: "Apple Intelligence is disabled in system settings."
            )
        case .unavailable(.modelNotReady):
            return .availability(
                requestId: requestId,
                status: "ModelNotReady",
                message: "The on-device model is not ready yet."
            )
        case .unavailable:
            return .availability(
                requestId: requestId,
                status: "Unknown",
                message: "Apple Foundation Models are currently unavailable."
            )
        }
#else
        return .availability(
            requestId: requestId,
            status: "NativeFrameworkUnavailable",
            message: "The Foundation Models framework is unavailable in this SDK."
        )
#endif
    }

#if canImport(FoundationModels)
    @available(iOS 26.0, macOS 26.0, *)
    private static func makeSession(
        options: AFMGenerationOptions
    ) -> LanguageModelSession {
        if options.instructions.trimmingCharacters(in: .whitespacesAndNewlines).isEmpty {
            return LanguageModelSession()
        }

        return LanguageModelSession(instructions: options.instructions)
    }

    @available(iOS 26.0, macOS 26.0, *)
    private static func makeGenerationOptions(
        _ options: AFMGenerationOptions
    ) throws -> GenerationOptions {
        if options.hasTemperature && !(0...1).contains(options.temperature) {
            throw AFMBridgeError.invalidOptions
        }
        if options.hasMaxOutputTokens && options.maxOutputTokens <= 0 {
            throw AFMBridgeError.invalidOptions
        }

        return GenerationOptions(
            temperature: options.hasTemperature ? Double(options.temperature) : nil,
            maximumResponseTokens: options.hasMaxOutputTokens
                ? options.maxOutputTokens
                : nil
        )
    }
#endif
}
