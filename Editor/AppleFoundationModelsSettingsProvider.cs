using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Baran.AppleFoundationModels.Editor
{
    internal sealed class AppleFoundationModelsSettingsProvider : SettingsProvider
    {
        private static readonly GUIContent UseMockLabel = new GUIContent(
            "Use Mock Provider In Editor",
            "Use deterministic local output while running inside the Unity Editor.");

        private static readonly GUIContent NativeDebugLabel = new GUIContent(
            "Enable Native Debug Logs",
            "Include request IDs and native bridge diagnostics. Disabled by default.");

        private static readonly GUIContent TimeoutLabel = new GUIContent(
            "Default Timeout Seconds",
            "Maximum time allowed for a native generation request.");

        private static readonly GUIContent FallbackLabel = new GUIContent(
            "Enable Fallback Provider",
            "Allow an application-configured fallback provider when native models are unavailable.");

        private SerializedObject _serializedSettings;
        private SerializedProperty _useMock;
        private SerializedProperty _nativeDebugLogs;
        private SerializedProperty _timeoutSeconds;
        private SerializedProperty _fallbackProvider;

        private AppleFoundationModelsSettingsProvider()
            : base("Project/Apple Foundation Models", SettingsScope.Project)
        {
            keywords = new HashSet<string>(new[]
            {
                "Apple",
                "Foundation Models",
                "Mock",
                "Native",
                "Timeout",
                "Fallback"
            });
        }

        [SettingsProvider]
        public static SettingsProvider CreateProvider()
        {
            return new AppleFoundationModelsSettingsProvider();
        }

        public override void OnActivate(string searchContext, UnityEngine.UIElements.VisualElement rootElement)
        {
            _serializedSettings = new SerializedObject(AppleFoundationModelsSettings.instance);
            _useMock = _serializedSettings.FindProperty("useMockProviderInEditor");
            _nativeDebugLogs = _serializedSettings.FindProperty("enableNativeDebugLogs");
            _timeoutSeconds = _serializedSettings.FindProperty("defaultTimeoutSeconds");
            _fallbackProvider = _serializedSettings.FindProperty("enableFallbackProvider");
        }

        public override void OnGUI(string searchContext)
        {
            _serializedSettings.Update();

            EditorGUILayout.LabelField("Provider Defaults", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_useMock, UseMockLabel);
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Native Requests", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_nativeDebugLogs, NativeDebugLabel);
            EditorGUILayout.PropertyField(_timeoutSeconds, TimeoutLabel);
            _timeoutSeconds.intValue = Mathf.Clamp(
                _timeoutSeconds.intValue,
                AppleFoundationModelsSettings.MinimumTimeoutSeconds,
                AppleFoundationModelsSettings.MaximumTimeoutSeconds);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Fallback Policy", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_fallbackProvider, FallbackLabel);

            if (_fallbackProvider.boolValue)
            {
                EditorGUILayout.HelpBox(
                    "A fallback must still be registered by application code. " +
                    "The core package does not send prompts to a cloud provider.",
                    MessageType.Info);
            }

            if (_serializedSettings.ApplyModifiedProperties())
            {
                AppleFoundationModelsSettings.instance.SaveSettings();
                AppleFoundationModelsSettingsSynchronizer.Apply();
            }
        }
    }
}
