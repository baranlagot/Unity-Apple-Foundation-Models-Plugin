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

        public static void ExportIOSValidationProject()
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
