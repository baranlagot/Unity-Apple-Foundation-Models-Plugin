using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Baran.AppleFoundationModels.ImagePlayground
{
    /// <summary>
    /// On-device image generation through Apple's Image Playground (ImageCreator). This
    /// requires Apple Intelligence and generally only produces images on a capable device,
    /// not the Simulator.
    /// </summary>
    public static class AppleImagePlayground
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void ImageCallback(IntPtr eventJson);

        private static readonly ImageCallback Callback = OnNativeEvent;
        private static readonly object Gate = new object();
        private static readonly Dictionary<string, TaskCompletionSource<byte[]>> Pending =
            new Dictionary<string, TaskCompletionSource<byte[]>>();

        private static SynchronizationContext _context;
        private static bool _initialized;

        /// <summary>True on platforms where the native Image Playground transport exists.</summary>
        public static bool IsSupported =>
#if UNITY_IOS && !UNITY_EDITOR
            true;
#else
            false;
#endif

        /// <summary>
        /// Generates an image for the prompt and returns it as PNG-encoded bytes. Load the
        /// bytes into a <see cref="Texture2D"/> with <c>ImageConversion.LoadImage</c>.
        /// </summary>
        public static Task<byte[]> GenerateAsync(string prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt))
            {
                throw new ArgumentException("A prompt is required.", nameof(prompt));
            }

            var tcs = new TaskCompletionSource<byte[]>();

            if (!IsSupported)
            {
                tcs.SetException(new PlatformNotSupportedException(
                    "Image Playground is only available in an iOS player on an Apple " +
                    "Intelligence device."));
                return tcs.Task;
            }

            Initialize();

            var requestId = Guid.NewGuid().ToString("N");
            lock (Gate)
            {
                Pending[requestId] = tcs;
            }

#if UNITY_IOS && !UNITY_EDITOR
            AFMImage_Generate(requestId, prompt);
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
#if UNITY_IOS && !UNITY_EDITOR
            AFMImage_SetCallback(Callback);
#endif
        }

        [AOT.MonoPInvokeCallback(typeof(ImageCallback))]
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
            ImageEvent message;
            try
            {
                message = JsonUtility.FromJson<ImageEvent>(json);
            }
            catch (Exception)
            {
                return;
            }

            if (message == null || string.IsNullOrEmpty(message.requestId))
            {
                return;
            }

            TaskCompletionSource<byte[]> tcs;
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
                        ? "Image generation failed."
                        : message.errorMessage));
                return;
            }

            try
            {
                tcs.SetResult(Convert.FromBase64String(message.image ?? string.Empty));
            }
            catch (FormatException exception)
            {
                tcs.SetException(new InvalidOperationException(
                    "The generated image payload was not valid base64.", exception));
            }
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
        private sealed class ImageEvent
        {
            public string requestId;
            public string type;
            public string image;
            public string errorCode;
            public string errorMessage;
        }

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal", CallingConvention = CallingConvention.Cdecl)]
        private static extern void AFMImage_SetCallback(ImageCallback callback);

        [DllImport("__Internal", CallingConvention = CallingConvention.Cdecl)]
        private static extern void AFMImage_Generate(string requestId, string prompt);
#endif
    }
}
