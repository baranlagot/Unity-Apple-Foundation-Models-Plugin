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
        private const string ShowcaseScenePath = "Assets/__AFMShowcaseScene.unity";
        private const string ShowcaseTypeName =
            "Baran.AppleFoundationModels.Samples.CapabilityShowcaseExample";
        private const string ShowcaseBundleId =
            "com.baran.applefoundationmodels.showcase";

        /// <summary>
        /// Builds the capability-showcase sample into an iOS Xcode project ready for
        /// on-device testing. The scene is generated in code with a camera and the IMGUI
        /// showcase behaviour so it renders reliably on device without any UI theme asset.
        /// Signing team and the physical device are configured in Xcode after export.
        /// </summary>
        public static void ExportIOSCapabilityShowcaseApp()
        {
            var outputPath = ResolveExportPath();

            // Set AFM_IOS_SDK=simulator to target the iOS Simulator (which uses the host
            // Mac's Apple Intelligence); anything else builds for a physical device.
            var useSimulator = string.Equals(
                Environment.GetEnvironmentVariable("AFM_IOS_SDK"),
                "simulator",
                StringComparison.OrdinalIgnoreCase);

            PlayerSettings.SetApplicationIdentifier(
                BuildTargetGroup.iOS,
                ShowcaseBundleId);
            PlayerSettings.SetScriptingBackend(
                NamedBuildTarget.iOS,
                ScriptingImplementation.IL2CPP);
            PlayerSettings.iOS.sdkVersion = useSimulator
                ? iOSSdkVersion.SimulatorSDK
                : iOSSdkVersion.DeviceSDK;
            PlayerSettings.iOS.targetOSVersionString = "26.0";
            PlayerSettings.iOS.targetDevice = iOSTargetDevice.iPhoneAndiPad;
            PlayerSettings.productName = "AFM Showcase";

            BuildShowcase(BuildTarget.iOS, BuildTargetGroup.iOS, outputPath, "iOS");
        }

        /// <summary>
        /// Builds the capability-showcase sample into a native macOS .app. On a macOS
        /// standalone build the native provider is active, so a Mac with Apple Intelligence
        /// runs the real on-device model. The build script drops the native dylib into the
        /// app bundle after this export.
        /// </summary>
        public static void ExportMacShowcaseApp()
        {
            var outputPath = ResolveExportPath();

            PlayerSettings.SetApplicationIdentifier(
                BuildTargetGroup.Standalone,
                ShowcaseBundleId);
            PlayerSettings.productName = "AFM Showcase";

            BuildShowcase(
                BuildTarget.StandaloneOSX,
                BuildTargetGroup.Standalone,
                outputPath,
                "macOS");
        }

        private static void BuildShowcase(
            BuildTarget target,
            BuildTargetGroup group,
            string outputPath,
            string label)
        {
            var showcaseType = ResolveShowcaseType();
            var originalScenes = EditorBuildSettings.scenes;
            try
            {
                var scene = EditorSceneManager.NewScene(
                    NewSceneSetup.EmptyScene,
                    NewSceneMode.Single);

                var cameraObject = new GameObject("Main Camera", typeof(Camera));
                cameraObject.tag = "MainCamera";
                var camera = cameraObject.GetComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.09f, 0.10f, 0.13f);
                SceneManager.MoveGameObjectToScene(cameraObject, scene);

                var showcaseObject = new GameObject(
                    "AFM Capability Showcase",
                    showcaseType);
                SceneManager.MoveGameObjectToScene(showcaseObject, scene);

                EditorSceneManager.SaveScene(scene, ShowcaseScenePath);
                EditorBuildSettings.scenes = new[]
                {
                    new EditorBuildSettingsScene(ShowcaseScenePath, enabled: true)
                };

                var report = BuildPipeline.BuildPlayer(
                    new BuildPlayerOptions
                    {
                        scenes = new[] { ShowcaseScenePath },
                        target = target,
                        targetGroup = group,
                        locationPathName = outputPath
                    });
                if (report.summary.result != BuildResult.Succeeded)
                {
                    throw new BuildFailedException(
                        label + " capability-showcase build failed with result " +
                        report.summary.result + ".");
                }
            }
            finally
            {
                EditorBuildSettings.scenes = originalScenes;
                if (AssetDatabase.DeleteAsset(ShowcaseScenePath))
                {
                    AssetDatabase.SaveAssets();
                }
            }

            Debug.Log(
                "Apple Foundation Models exported the capability-showcase " + label +
                " app to " + outputPath + ".");
        }

        private static Type ResolveShowcaseType()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(ShowcaseTypeName);
                if (type != null)
                {
                    return type;
                }
            }

            throw new InvalidOperationException(
                "Could not find " + ShowcaseTypeName +
                ". Ensure the showcase sample compiled into the project.");
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
