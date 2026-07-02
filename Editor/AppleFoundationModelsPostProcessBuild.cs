#if UNITY_IOS
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

namespace Baran.AppleFoundationModels.Editor
{
    internal interface IAppleFoundationModelsFileSystem
    {
        void EnsureDirectory(string path);

        void CopyFile(string sourcePath, string destinationPath);
    }

    internal interface IAppleFoundationModelsXcodeProject
    {
        string UnityFrameworkTargetGuid { get; }

        string MainTargetGuid { get; }

        bool ContainsFile(string projectPath);

        void AddSourceFile(string projectPath, string targetGuid);

        void AddFramework(string targetGuid, string frameworkName, bool weak);

        string GetBuildProperty(string targetGuid, string propertyName);

        void SetBuildProperty(string targetGuid, string propertyName, string value);
    }

    internal sealed class AppleFoundationModelsIOSBuildIntegrator
    {
        internal const string MinimumIOSVersion = "26.0";
        internal const string SwiftVersion = "5.0";
        internal const string FrameworkName = "FoundationModels.framework";
        internal const string NativeProjectRoot =
            "Libraries/com.baran.apple-foundation-models/Native/AppleFoundationModelsCore";

        private static readonly string[] CoreSourceFiles =
        {
            "AppleFoundationModelsCore.swift",
            "AppleFoundationModelsModels.swift",
            "AppleFoundationModelsErrorMapper.swift"
        };

        private readonly IAppleFoundationModelsFileSystem _fileSystem;

        public AppleFoundationModelsIOSBuildIntegrator(
            IAppleFoundationModelsFileSystem fileSystem)
        {
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        }

        public void Integrate(
            string packageRoot,
            string xcodeProjectRoot,
            IAppleFoundationModelsXcodeProject project)
        {
            if (string.IsNullOrWhiteSpace(packageRoot))
            {
                throw new ArgumentException("Package root is required.", nameof(packageRoot));
            }
            if (string.IsNullOrWhiteSpace(xcodeProjectRoot))
            {
                throw new ArgumentException("Xcode project root is required.", nameof(xcodeProjectRoot));
            }
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            var sourceRoot = Path.Combine(
                packageRoot,
                "Native~",
                "Sources",
                "AppleFoundationModelsCore");
            var destinationRoot = Path.Combine(
                xcodeProjectRoot,
                NativeProjectRoot.Replace('/', Path.DirectorySeparatorChar));
            _fileSystem.EnsureDirectory(destinationRoot);

            foreach (var fileName in CoreSourceFiles)
            {
                var sourcePath = Path.Combine(sourceRoot, fileName);
                var destinationPath = Path.Combine(destinationRoot, fileName);
                _fileSystem.CopyFile(sourcePath, destinationPath);

                var projectPath = $"{NativeProjectRoot}/{fileName}";
                if (!project.ContainsFile(projectPath))
                {
                    project.AddSourceFile(
                        projectPath,
                        project.UnityFrameworkTargetGuid);
                }
            }

            project.AddFramework(
                project.UnityFrameworkTargetGuid,
                FrameworkName,
                weak: true);
            project.SetBuildProperty(
                project.UnityFrameworkTargetGuid,
                "SWIFT_VERSION",
                SwiftVersion);
            project.SetBuildProperty(
                project.UnityFrameworkTargetGuid,
                "CLANG_ENABLE_MODULES",
                "YES");
            project.SetBuildProperty(
                project.MainTargetGuid,
                "ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES",
                "YES");

            EnsureMinimumDeploymentTarget(project, project.UnityFrameworkTargetGuid);
            EnsureMinimumDeploymentTarget(project, project.MainTargetGuid);
        }

        private static void EnsureMinimumDeploymentTarget(
            IAppleFoundationModelsXcodeProject project,
            string targetGuid)
        {
            var current = project.GetBuildProperty(
                targetGuid,
                "IPHONEOS_DEPLOYMENT_TARGET");
            if (!Version.TryParse(current, out var currentVersion) ||
                currentVersion < Version.Parse(MinimumIOSVersion))
            {
                project.SetBuildProperty(
                    targetGuid,
                    "IPHONEOS_DEPLOYMENT_TARGET",
                    MinimumIOSVersion);
            }
        }
    }

    internal sealed class SystemAppleFoundationModelsFileSystem :
        IAppleFoundationModelsFileSystem
    {
        public void EnsureDirectory(string path)
        {
            Directory.CreateDirectory(path);
        }

        public void CopyFile(string sourcePath, string destinationPath)
        {
            if (!File.Exists(sourcePath))
            {
                throw new FileNotFoundException(
                    "Required Apple Foundation Models native source was not found.",
                    sourcePath);
            }

            File.Copy(sourcePath, destinationPath, overwrite: true);
        }
    }

    internal sealed class UnityAppleFoundationModelsXcodeProject :
        IAppleFoundationModelsXcodeProject
    {
        private readonly PBXProject _project;

        public UnityAppleFoundationModelsXcodeProject(PBXProject project)
        {
            _project = project ?? throw new ArgumentNullException(nameof(project));
        }

        public string UnityFrameworkTargetGuid => _project.GetUnityFrameworkTargetGuid();

        public string MainTargetGuid => _project.GetUnityMainTargetGuid();

        public bool ContainsFile(string projectPath)
        {
            return _project.ContainsFileByProjectPath(projectPath);
        }

        public void AddSourceFile(string projectPath, string targetGuid)
        {
            var fileGuid = _project.AddFile(
                projectPath,
                projectPath,
                PBXSourceTree.Source);
            _project.AddFileToBuild(targetGuid, fileGuid);
        }

        public void AddFramework(string targetGuid, string frameworkName, bool weak)
        {
            _project.AddFrameworkToProject(targetGuid, frameworkName, weak);
        }

        public string GetBuildProperty(string targetGuid, string propertyName)
        {
            return _project.GetBuildPropertyForAnyConfig(targetGuid, propertyName);
        }

        public void SetBuildProperty(string targetGuid, string propertyName, string value)
        {
            _project.SetBuildProperty(targetGuid, propertyName, value);
        }
    }

    internal static class AppleFoundationModelsPostProcessBuild
    {
        [PostProcessBuild(900)]
        public static void ConfigureXcodeProject(BuildTarget target, string buildPath)
        {
            if (target != BuildTarget.iOS)
            {
                return;
            }

            var package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(
                typeof(global::Baran.AppleFoundationModels.AppleFoundationModels).Assembly);
            if (package == null)
            {
                throw new BuildFailedException(
                    "Could not locate the Apple Foundation Models package.");
            }

            var projectPath = PBXProject.GetPBXProjectPath(buildPath);
            var project = new PBXProject();
            project.ReadFromFile(projectPath);

            var integrator = new AppleFoundationModelsIOSBuildIntegrator(
                new SystemAppleFoundationModelsFileSystem());
            integrator.Integrate(
                package.resolvedPath,
                buildPath,
                new UnityAppleFoundationModelsXcodeProject(project));

            project.WriteToFile(projectPath);
            UnityEngine.Debug.Log(
                "Apple Foundation Models configured the generated iOS Xcode project.");
        }
    }
}
#endif
