using System;

namespace Baran.AppleFoundationModels.Native
{
    [Serializable]
    internal sealed class NativeEventMessage
    {
        public string requestId;
        public string type;
        public string payload;
        public string status;
        public string errorCode;
        public string errorMessage;
    }

    internal static class NativeEventTypes
    {
        public const string Availability = "availability";
        public const string Text = "text";
        public const string StreamDelta = "streamDelta";
        public const string Complete = "complete";
        public const string Error = "error";
    }
}
