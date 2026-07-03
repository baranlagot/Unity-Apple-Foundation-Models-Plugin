import Foundation
import ImageIO
import CoreGraphics
import UniformTypeIdentifiers

#if canImport(ImagePlayground)
import ImagePlayground
#endif

// A self-contained bridge over Image Playground's on-device image generation (ImageCreator).
// This requires Apple Intelligence and generally only produces images on a capable device.
// The generated image is returned to Unity as a base64-encoded PNG inside a JSON string,
// mirroring the other native bridges' event contract.

typealias AFMImageCallback = @convention(c) (UnsafePointer<CChar>?) -> Void

private final class AFMImageRuntime: @unchecked Sendable {
    static let shared = AFMImageRuntime()

    private let lock = NSLock()
    private var callback: AFMImageCallback?

    func setCallback(_ callback: AFMImageCallback?) {
        lock.lock()
        self.callback = callback
        lock.unlock()
    }

    func generate(requestId: String, prompt: String) {
#if canImport(ImagePlayground)
        guard #available(iOS 18.4, macOS 15.4, *) else {
            emitError(
                requestId: requestId,
                code: "unsupportedOSVersion",
                message: "Image Playground requires a newer operating system."
            )
            return
        }

        Task {
            do {
                let creator = try await ImageCreator()
                let style = creator.availableStyles.first ?? .animation
                let images = creator.images(
                    for: [.text(prompt)],
                    style: style,
                    limit: 1
                )

                for try await created in images {
                    guard let data = Self.pngData(from: created.cgImage) else {
                        emitError(
                            requestId: requestId,
                            code: "encodeFailed",
                            message: "The generated image could not be encoded."
                        )
                        return
                    }

                    emitImage(requestId: requestId, data: data)
                    return
                }

                emitError(
                    requestId: requestId,
                    code: "noImage",
                    message: "Image Playground produced no image."
                )
            } catch {
                emitError(
                    requestId: requestId,
                    code: "generationFailed",
                    message: error.localizedDescription
                )
            }
        }
#else
        emitError(
            requestId: requestId,
            code: "frameworkUnavailable",
            message: "Image Playground is unavailable in this SDK."
        )
#endif
    }

    private func emitImage(requestId: String, data: Data) {
        emit([
            "requestId": requestId,
            "type": "imageResult",
            "image": data.base64EncodedString()
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

    private static func pngData(from image: CGImage) -> Data? {
        let target = NSMutableData()
        guard let destination = CGImageDestinationCreateWithData(
            target as CFMutableData,
            UTType.png.identifier as CFString,
            1,
            nil
        ) else {
            return nil
        }

        CGImageDestinationAddImage(destination, image, nil)
        guard CGImageDestinationFinalize(destination) else {
            return nil
        }

        return target as Data
    }
}

@_cdecl("AFMImage_SetCallback")
func AFMImage_SetCallback(_ callback: AFMImageCallback?) {
    AFMImageRuntime.shared.setCallback(callback)
}

@_cdecl("AFMImage_Generate")
func AFMImage_Generate(
    _ requestId: UnsafePointer<CChar>?,
    _ prompt: UnsafePointer<CChar>?
) {
    guard let requestId, let prompt else {
        return
    }

    AFMImageRuntime.shared.generate(
        requestId: String(cString: requestId),
        prompt: String(cString: prompt)
    )
}
