using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Baran.AppleFoundationModels.Vision
{
    public enum AppleVisionRequestKind
    {
        /// <summary>General image classification (scene/object labels with confidence).</summary>
        Classify,

        /// <summary>Text recognition (OCR): reads printed text out of the image.</summary>
        RecognizeText
    }

    public sealed class AppleVisionResult
    {
        public AppleVisionResult(
            AppleVisionRequestKind kind,
            IReadOnlyList<string> items)
        {
            Kind = kind;
            Items = items ?? Array.Empty<string>();
        }

        public AppleVisionRequestKind Kind { get; }

        /// <summary>Classification labels or recognized text lines, most relevant first.</summary>
        public IReadOnlyList<string> Items { get; }
    }

    /// <summary>
    /// On-device image understanding through Apple's Vision framework. Unlike Foundation
    /// Models, Vision does not require Apple Intelligence and runs on any supported device
    /// and the Simulator.
    /// </summary>
    public static class AppleVision
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void VisionCallback(IntPtr eventJson);

        private static readonly VisionCallback Callback = OnNativeEvent;
        private static readonly object Gate = new object();
        private static readonly Dictionary<string, TaskCompletionSource<AppleVisionResult>> Pending =
            new Dictionary<string, TaskCompletionSource<AppleVisionResult>>();

        private static SynchronizationContext _context;
        private static bool _initialized;

        /// <summary>True on platforms where the native Vision transport is available.</summary>
        public static bool IsSupported =>
#if (UNITY_IOS || UNITY_STANDALONE_OSX) && !UNITY_EDITOR
            true;
#else
            false;
#endif

        public static Task<AppleVisionResult> AnalyzeAsync(
            byte[] imageBytes,
            AppleVisionRequestKind kind)
        {
            if (imageBytes == null || imageBytes.Length == 0)
            {
                throw new ArgumentException("Image bytes are required.", nameof(imageBytes));
            }

            var tcs = new TaskCompletionSource<AppleVisionResult>();

            if (!IsSupported)
            {
                tcs.SetException(new PlatformNotSupportedException(
                    "Apple Vision is only available in an iOS player or Simulator."));
                return tcs.Task;
            }

            Initialize();

            var requestId = Guid.NewGuid().ToString("N");
            lock (Gate)
            {
                Pending[requestId] = tcs;
            }

            var kindToken = kind == AppleVisionRequestKind.RecognizeText
                ? "recognizeText"
                : "classify";

#if (UNITY_IOS || UNITY_STANDALONE_OSX) && !UNITY_EDITOR
            AFMVision_Analyze(requestId, kindToken, imageBytes, imageBytes.Length);
#endif
            return tcs.Task;
        }

        private static void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            _context = SynchronizationContext.Current;
#if (UNITY_IOS || UNITY_STANDALONE_OSX) && !UNITY_EDITOR
            AFMVision_SetCallback(Callback);
#endif
        }

        [AOT.MonoPInvokeCallback(typeof(VisionCallback))]
        private static void OnNativeEvent(IntPtr eventJson)
        {
            var json = ReadUtf8String(eventJson);
            if (_context != null)
            {
                _context.Post(_ => Dispatch(json), null);
            }
            else
            {
                Dispatch(json);
            }
        }

        private static void Dispatch(string json)
        {
            VisionEvent message;
            try
            {
                message = JsonUtility.FromJson<VisionEvent>(json);
            }
            catch (Exception)
            {
                return;
            }

            if (message == null || string.IsNullOrEmpty(message.requestId))
            {
                return;
            }

            TaskCompletionSource<AppleVisionResult> tcs;
            lock (Gate)
            {
                if (!Pending.TryGetValue(message.requestId, out tcs))
                {
                    return;
                }

                Pending.Remove(message.requestId);
            }

            if (message.type == "error")
            {
                tcs.SetException(new InvalidOperationException(
                    string.IsNullOrEmpty(message.errorMessage)
                        ? "Vision analysis failed."
                        : message.errorMessage));
                return;
            }

            var kind = message.kind == "recognizeText"
                ? AppleVisionRequestKind.RecognizeText
                : AppleVisionRequestKind.Classify;
            tcs.SetResult(new AppleVisionResult(kind, message.items ?? Array.Empty<string>()));
        }

        private static string ReadUtf8String(IntPtr pointer)
        {
            if (pointer == IntPtr.Zero)
            {
                return string.Empty;
            }

            var length = 0;
            while (Marshal.ReadByte(pointer, length) != 0)
            {
                length++;
            }

            var bytes = new byte[length];
            Marshal.Copy(pointer, bytes, 0, length);
            return Encoding.UTF8.GetString(bytes);
        }

        [Serializable]
        private sealed class VisionEvent
        {
            public string requestId;
            public string type;
            public string kind;
            public string[] items;
            public string errorCode;
            public string errorMessage;
        }

#if (UNITY_IOS || UNITY_STANDALONE_OSX) && !UNITY_EDITOR
#if UNITY_IOS
        private const string NativeLibrary = "__Internal";
#else
        private const string NativeLibrary = "AppleFoundationModelsMac";
#endif

        [DllImport(NativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        private static extern void AFMVision_SetCallback(VisionCallback callback);

        [DllImport(NativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        private static extern void AFMVision_Analyze(
            string requestId,
            string kind,
            byte[] imageBytes,
            int length);
#endif
    }
}
