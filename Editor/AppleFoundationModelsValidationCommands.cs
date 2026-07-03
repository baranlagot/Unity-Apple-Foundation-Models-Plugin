#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Baran.AppleFoundationModels.Editor
{
    internal static class AppleFoundationModelsValidationCommands
    {
        private const string TemporaryScenePath = "Assets/__AFMValidationScene.unity";
        private const string DeviceValidationScenePath =
            "Assets/Samples/DeviceValidation/DeviceValidation.unity";
        private const string DeviceValidationBundleId =
            "com.baran.applefoundationmodels.devicevalidation";

        /// <summary>
        /// Builds the all-capabilities Device Validation sample scene into an iOS Xcode
        /// project ready for on-device testing. Signing team and the physical device are
        /// configured in Xcode after export.
        /// </summary>
        public static void ExportIOSDeviceValidationApp()
        {
            var outputPath = ResolveExportPath();

            var scenePath = DeviceValidationScenePath;
            if (!File.Exists(scenePath))
            {
                var matches = AssetDatabase.FindAssets("DeviceValidation t:Scene");
                scenePath = matches.Length > 0
                    ? AssetDatabase.GUIDToAssetPath(matches[0])
                    : null;
            }

            if (string.IsNullOrWhiteSpace(scenePath) || !File.Exists(scenePath))
            {
                throw new InvalidOperationException(
                    "Could not locate the DeviceValidation sample scene. Expected it at " +
                    DeviceValidationScenePath + ".");
            }

            PlayerSettings.SetApplicationIdentifier(
                BuildTargetGroup.iOS,
                DeviceValidationBundleId);
            PlayerSettings.SetScriptingBackend(
                NamedBuildTarget.iOS,
                ScriptingImplementation.IL2CPP);
            PlayerSettings.iOS.sdkVersion = iOSSdkVersion.DeviceSDK;
            PlayerSettings.iOS.targetOSVersionString = "26.0";
            PlayerSettings.iOS.targetDevice = iOSTargetDevice.iPhoneAndiPad;
            PlayerSettings.productName = "AFM Device Validation";

            var report = BuildPipeline.BuildPlayer(
                new BuildPlayerOptions
                {
                    scenes = new[] { scenePath },
                    target = BuildTarget.iOS,
                    targetGroup = BuildTargetGroup.iOS,
                    locationPathName = outputPath
                });
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException(
                    "iOS device-validation build failed with result " +
                    report.summary.result + ".");
            }

            Debug.Log(
                "Apple Foundation Models exported the Device Validation iOS app to " +
                outputPath + ".");
        }

        private static string ResolveExportPath()
        {
            var outputPath = GetArgumentValue("afmExportPath");
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                outputPath = Environment.GetEnvironmentVariable("AFM_IOS_EXPORT_PATH");
            }
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                throw new InvalidOperationException(
                    "Set AFM_IOS_EXPORT_PATH or pass -afmExportPath <path>.");
            }

            outputPath = Path.GetFullPath(outputPath);
            if (Directory.Exists(outputPath))
            {
                Directory.Delete(outputPath, recursive: true);
            }

            return outputPath;
        }

        public static void ExportIOSValidationProject()
        {
            var outputPath = ResolveExportPath();

            var originalScenes = EditorBuildSettings.scenes;
            try
            {
                var scene = EditorSceneManager.NewScene(
                    NewSceneSetup.EmptyScene,
                    NewSceneMode.Single);
                var root = new GameObject("Apple Foundation Models Validation");
                SceneManager.MoveGameObjectToScene(root, scene);
                EditorSceneManager.SaveScene(scene, TemporaryScenePath);

                EditorBuildSettings.scenes = new[]
                {
                    new EditorBuildSettingsScene(TemporaryScenePath, enabled: true)
                };

                var report = BuildPipeline.BuildPlayer(
                    new BuildPlayerOptions
                    {
                        scenes = new[] { TemporaryScenePath },
                        target = BuildTarget.iOS,
                        locationPathName = outputPath
                    });
                if (report.summary.result != BuildResult.Succeeded)
                {
                    throw new BuildFailedException(
                        "iOS validation export failed with result " +
                        report.summary.result + ".");
                }
            }
            finally
            {
                EditorBuildSettings.scenes = originalScenes;
                if (AssetDatabase.DeleteAsset(TemporaryScenePath))
                {
                    AssetDatabase.SaveAssets();
                }
            }

            Debug.Log(
                "Apple Foundation Models exported an iOS validation project to " +
                outputPath + ".");
        }

        private static string GetArgumentValue(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (var index = 0; index < args.Length - 1; index++)
            {
                if (args[index] == "-" + name)
                {
                    return args[index + 1];
                }
            }

            return null;
        }
    }
}
#endif
