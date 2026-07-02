using System;

namespace Baran.AppleFoundationModels.Native
{
    internal interface INativeFoundationModelsTransport
    {
        void Initialize(Action<string> eventHandler, bool debugLoggingEnabled);

        void GetAvailability(string requestId);

        void GenerateText(string requestId, string prompt, string optionsJson);

        void StreamText(string requestId, string prompt, string optionsJson);

        void CancelRequest(string requestId);
    }
}
