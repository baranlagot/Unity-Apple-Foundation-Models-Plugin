using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Baran.AppleFoundationModels.Native
{
    internal sealed class AppleFoundationModelsNativeTransport :
        INativeFoundationModelsTransport
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void NativeEventCallback(IntPtr eventJson);

        private static readonly NativeEventCallback Callback = OnNativeEvent;
        private static Action<string> _eventHandler;

        public void Initialize(Action<string> eventHandler, bool debugLoggingEnabled)
        {
            _eventHandler = eventHandler ?? throw new ArgumentNullException(nameof(eventHandler));
#if UNITY_IOS && !UNITY_EDITOR
            AFM_SetEventCallback(Callback);
            AFM_SetDebugLogging(debugLoggingEnabled);
#endif
        }

        public void GetAvailability(string requestId)
        {
#if UNITY_IOS && !UNITY_EDITOR
            AFM_GetAvailability(requestId);
#else
            throw CreateUnsupportedException();
#endif
        }

        public void GenerateText(string requestId, string prompt, string optionsJson)
        {
#if UNITY_IOS && !UNITY_EDITOR
            AFM_GenerateText(requestId, prompt, optionsJson);
#else
            throw CreateUnsupportedException();
#endif
        }

        public void StreamText(string requestId, string prompt, string optionsJson)
        {
#if UNITY_IOS && !UNITY_EDITOR
            AFM_StreamText(requestId, prompt, optionsJson);
#else
            throw CreateUnsupportedException();
#endif
        }

        public void CancelRequest(string requestId)
        {
#if UNITY_IOS && !UNITY_EDITOR
            AFM_CancelRequest(requestId);
#endif
        }

        [AOT.MonoPInvokeCallback(typeof(NativeEventCallback))]
        private static void OnNativeEvent(IntPtr eventJson)
        {
            _eventHandler?.Invoke(ReadUtf8String(eventJson));
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

        private static PlatformNotSupportedException CreateUnsupportedException()
        {
            return new PlatformNotSupportedException(
                "The native Apple Foundation Models transport is only available in an iOS player.");
        }

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal", CallingConvention = CallingConvention.Cdecl)]
        private static extern void AFM_SetEventCallback(NativeEventCallback callback);

        [DllImport("__Internal", CallingConvention = CallingConvention.Cdecl)]
        private static extern void AFM_SetDebugLogging([MarshalAs(UnmanagedType.I1)] bool enabled);

        [DllImport("__Internal", CallingConvention = CallingConvention.Cdecl)]
        private static extern void AFM_GetAvailability(string requestId);

        [DllImport("__Internal", CallingConvention = CallingConvention.Cdecl)]
        private static extern void AFM_GenerateText(
            string requestId,
            string prompt,
            string optionsJson);

        [DllImport("__Internal", CallingConvention = CallingConvention.Cdecl)]
        private static extern void AFM_StreamText(
            string requestId,
            string prompt,
            string optionsJson);

        [DllImport("__Internal", CallingConvention = CallingConvention.Cdecl)]
        private static extern void AFM_CancelRequest(string requestId);
#endif
    }
}
