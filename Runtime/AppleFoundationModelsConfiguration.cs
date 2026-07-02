using System;

namespace Baran.AppleFoundationModels
{
    public sealed class AppleFoundationModelsConfiguration
    {
        public const int DefaultTimeoutSecondsValue = 30;

        public AppleFoundationModelsConfiguration(
            bool useMockProviderInEditor,
            bool enableNativeDebugLogs,
            int defaultTimeoutSeconds,
            bool enableFallbackProvider)
        {
            if (defaultTimeoutSeconds <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(defaultTimeoutSeconds),
                    "Default timeout must be greater than zero seconds.");
            }

            UseMockProviderInEditor = useMockProviderInEditor;
            EnableNativeDebugLogs = enableNativeDebugLogs;
            DefaultTimeoutSeconds = defaultTimeoutSeconds;
            EnableFallbackProvider = enableFallbackProvider;
        }

        public bool UseMockProviderInEditor { get; }

        public bool EnableNativeDebugLogs { get; }

        public int DefaultTimeoutSeconds { get; }

        public bool EnableFallbackProvider { get; }

        public static AppleFoundationModelsConfiguration Default =>
            new AppleFoundationModelsConfiguration(
                useMockProviderInEditor: true,
                enableNativeDebugLogs: false,
                defaultTimeoutSeconds: DefaultTimeoutSecondsValue,
                enableFallbackProvider: false);
    }
}
