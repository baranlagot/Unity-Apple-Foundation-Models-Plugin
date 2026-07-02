using System;
using UnityEngine;

namespace Baran.AppleFoundationModels.Native
{
    internal sealed class NativeEventDecodingException : Exception
    {
        public NativeEventDecodingException(
            string message,
            string requestId = null,
            Exception innerException = null)
            : base(message, innerException)
        {
            RequestId = requestId;
        }

        public string RequestId { get; }
    }

    internal sealed class UnityNativeMessageCodec : INativeMessageCodec
    {
        public NativeEventMessage DecodeEvent(string eventJson)
        {
            if (string.IsNullOrWhiteSpace(eventJson))
            {
                throw new NativeEventDecodingException(
                    "The native bridge returned an empty event.");
            }

            NativeEventMessage message;
            try
            {
                message = JsonUtility.FromJson<NativeEventMessage>(eventJson);
            }
            catch (Exception exception)
            {
                throw new NativeEventDecodingException(
                    "The native bridge returned an invalid event.",
                    innerException: exception);
            }

            if (message == null || string.IsNullOrWhiteSpace(message.requestId))
            {
                throw new NativeEventDecodingException(
                    "The native bridge returned an event without a request ID.");
            }

            if (string.IsNullOrWhiteSpace(message.type))
            {
                throw new NativeEventDecodingException(
                    $"Native request {message.requestId} returned an event without a type.",
                    message.requestId);
            }

            return message;
        }

        public string EncodeOptions(AppleFoundationModelsOptions options)
        {
            var snapshot = AppleFoundationModelsOptions.Snapshot(options);
            var message = new NativeGenerationOptionsMessage
            {
                instructions = snapshot.Instructions ?? string.Empty,
                hasTemperature = snapshot.Temperature.HasValue,
                temperature = snapshot.Temperature.GetValueOrDefault(),
                hasMaxOutputTokens = snapshot.MaxOutputTokens.HasValue,
                maxOutputTokens = snapshot.MaxOutputTokens.GetValueOrDefault(),
                sessionId = snapshot.SessionId ?? string.Empty,
                preferStructuredOutput = snapshot.PreferStructuredOutput
            };
            return JsonUtility.ToJson(message);
        }

        [Serializable]
        private sealed class NativeGenerationOptionsMessage
        {
            public string instructions;
            public bool hasTemperature;
            public float temperature;
            public bool hasMaxOutputTokens;
            public int maxOutputTokens;
            public string sessionId;
            public bool preferStructuredOutput;
        }
    }
}
