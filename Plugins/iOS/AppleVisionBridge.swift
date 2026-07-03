import Foundation
import ImageIO
import Vision

// A small, self-contained bridge over the on-device Vision framework. It is independent of
// the Foundation Models core: Vision does not require Apple Intelligence and runs on any
// supported device and the Simulator. Results are returned to Unity as a JSON string via a
// registered C callback, mirroring the Foundation Models event contract.

typealias AFMVisionCallback = @convention(c) (UnsafePointer<CChar>?) -> Void

private final class AFMVisionRuntime: @unchecked Sendable {
    static let shared = AFMVisionRuntime()

    private let lock = NSLock()
    private var callback: AFMVisionCallback?

    func setCallback(_ callback: AFMVisionCallback?) {
        lock.lock()
        self.callback = callback
        lock.unlock()
    }

    func analyze(requestId: String, kind: String, data: Data) {
        guard let image = Self.decodeImage(data) else {
            emitError(
                requestId: requestId,
                code: "invalidImage",
                message: "The image data could not be decoded."
            )
            return
        }

        let handler = VNImageRequestHandler(cgImage: image, options: [:])
        do {
            if kind == "recognizeText" {
                let request = VNRecognizeTextRequest()
                request.recognitionLevel = .accurate
                request.usesLanguageCorrection = true
                try handler.perform([request])
                let lines = (request.results ?? []).compactMap {
                    $0.topCandidates(1).first?.string
                }
                emitResult(requestId: requestId, kind: "recognizeText", items: lines)
            } else {
                let request = VNClassifyImageRequest()
                try handler.perform([request])
                let items = (request.results ?? [])
                    .filter { $0.confidence >= 0.1 }
                    .prefix(6)
                    .map { "\($0.identifier) (\(Int(($0.confidence * 100).rounded()))%)" }
                emitResult(requestId: requestId, kind: "classify", items: Array(items))
            }
        } catch {
            emitError(
                requestId: requestId,
                code: "visionFailure",
                message: error.localizedDescription
            )
        }
    }

    private func emitResult(requestId: String, kind: String, items: [String]) {
        emit([
            "requestId": requestId,
            "type": "visionResult",
            "kind": kind,
            "items": items
        ])
    }

    private func emitError(requestId: String, code: String, message: String) {
        emit([
            "requestId": requestId,
            "type": "error",
            "errorCode": code,
            "errorMessage": message
        ])
    }

    private func emit(_ payload: [String: Any]) {
        guard
            let data = try? JSONSerialization.data(withJSONObject: payload),
            let json = String(data: data, encoding: .utf8)
        else {
            return
        }

        lock.lock()
        let currentCallback = callback
        lock.unlock()
        guard let currentCallback else {
            return
        }

        json.withCString { pointer in
            currentCallback(pointer)
        }
    }

    private static func decodeImage(_ data: Data) -> CGImage? {
        guard let source = CGImageSourceCreateWithData(data as CFData, nil) else {
            return nil
        }

        return CGImageSourceCreateImageAtIndex(source, 0, nil)
    }
}

@_cdecl("AFMVision_SetCallback")
func AFMVision_SetCallback(_ callback: AFMVisionCallback?) {
    AFMVisionRuntime.shared.setCallback(callback)
}

@_cdecl("AFMVision_Analyze")
func AFMVision_Analyze(
    _ requestId: UnsafePointer<CChar>?,
    _ kind: UnsafePointer<CChar>?,
    _ bytes: UnsafePointer<UInt8>?,
    _ length: Int32
) {
    guard
        let requestId,
        let kind,
        let bytes,
        length > 0
    else {
        return
    }

    let requestString = String(cString: requestId)
    let kindString = String(cString: kind)
    let data = Data(bytes: bytes, count: Int(length))

    DispatchQueue.global(qos: .userInitiated).async {
        AFMVisionRuntime.shared.analyze(
            requestId: requestString,
            kind: kindString,
            data: data
        )
    }
}
