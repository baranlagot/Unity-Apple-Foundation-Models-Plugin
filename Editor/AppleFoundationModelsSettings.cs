using UnityEditor;
using UnityEngine;

namespace Baran.AppleFoundationModels.Editor
{
    [FilePath(
        "ProjectSettings/AppleFoundationModelsSettings.asset",
        FilePathAttribute.Location.ProjectFolder)]
    internal sealed class AppleFoundationModelsSettings :
        ScriptableSingleton<AppleFoundationModelsSettings>
    {
        internal const int MinimumTimeoutSeconds = 1;
        internal const int MaximumTimeoutSeconds = 3600;

        [SerializeField]
        private bool useMockProviderInEditor = true;

        [SerializeField]
        private bool enableNativeDebugLogs;

        [SerializeField]
        private int defaultTimeoutSeconds =
            AppleFoundationModelsConfiguration.DefaultTimeoutSecondsValue;

        [SerializeField]
        private bool enableFallbackProvider;

        public bool UseMockProviderInEditor => useMockProviderInEditor;

        public bool EnableNativeDebugLogs => enableNativeDebugLogs;

        public int DefaultTimeoutSeconds => defaultTimeoutSeconds;

        public bool EnableFallbackProvider => enableFallbackProvider;

        internal AppleFoundationModelsConfiguration ToRuntimeConfiguration()
        {
            return new AppleFoundationModelsConfiguration(
                useMockProviderInEditor,
                enableNativeDebugLogs,
                Mathf.Clamp(
                    defaultTimeoutSeconds,
                    MinimumTimeoutSeconds,
                    MaximumTimeoutSeconds),
                enableFallbackProvider);
        }

        internal void UpdateValues(
            bool useMock,
            bool nativeDebugLogs,
            int timeoutSeconds,
            bool fallbackProvider)
        {
            useMockProviderInEditor = useMock;
            enableNativeDebugLogs = nativeDebugLogs;
            defaultTimeoutSeconds = Mathf.Clamp(
                timeoutSeconds,
                MinimumTimeoutSeconds,
                MaximumTimeoutSeconds);
            enableFallbackProvider = fallbackProvider;
        }

        internal void SaveSettings()
        {
            Save(saveAsText: true);
        }

        private void OnValidate()
        {
            defaultTimeoutSeconds = Mathf.Clamp(
                defaultTimeoutSeconds,
                MinimumTimeoutSeconds,
                MaximumTimeoutSeconds);
        }
    }
}
