#if UNITY_IOS
using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace Baran.AppleFoundationModels.Editor.Tests
{
    public sealed class IOSBuildIntegrationTests
    {
        [Test]
        public void Integrate_CopiesSourcesAndConfiguresProjectIdempotently()
        {
            var fileSystem = new FakeFileSystem();
            var project = new FakeXcodeProject();
            project.BuildProperties[(project.UnityFrameworkTargetGuid, "IPHONEOS_DEPLOYMENT_TARGET")] = "15.0";
            project.BuildProperties[(project.MainTargetGuid, "IPHONEOS_DEPLOYMENT_TARGET")] = "27.0";
            var integrator = new AppleFoundationModelsIOSBuildIntegrator(fileSystem);

            integrator.Integrate("package", "xcode", project);
            integrator.Integrate("package", "xcode", project);

            Assert.That(fileSystem.EnsuredDirectories.Count, Is.EqualTo(2));
            Assert.That(fileSystem.Copies.Count, Is.EqualTo(6));
            Assert.That(project.SourceFiles.Count, Is.EqualTo(3));
            Assert.That(project.Frameworks,
                Does.Contain(AppleFoundationModelsIOSBuildIntegrator.FrameworkName));
            Assert.That(project.WeakFrameworks,
                Does.Contain(AppleFoundationModelsIOSBuildIntegrator.FrameworkName));
            Assert.That(project.BuildProperties[(project.UnityFrameworkTargetGuid, "SWIFT_VERSION")],
                Is.EqualTo(AppleFoundationModelsIOSBuildIntegrator.SwiftVersion));
            Assert.That(project.BuildProperties[(project.UnityFrameworkTargetGuid, "IPHONEOS_DEPLOYMENT_TARGET")],
                Is.EqualTo(AppleFoundationModelsIOSBuildIntegrator.MinimumIOSVersion));
            Assert.That(project.BuildProperties[(project.MainTargetGuid, "IPHONEOS_DEPLOYMENT_TARGET")],
                Is.EqualTo("27.0"));
        }

        [Test]
        public void Integrate_WhenSourceIsMissing_PropagatesContextualFailure()
        {
            var fileSystem = new FakeFileSystem { ThrowOnCopy = true };
            var integrator = new AppleFoundationModelsIOSBuildIntegrator(fileSystem);

            Assert.Throws<FileNotFoundException>(() =>
                integrator.Integrate("package", "xcode", new FakeXcodeProject()));
        }

        private sealed class FakeFileSystem : IAppleFoundationModelsFileSystem
        {
            public bool ThrowOnCopy { get; set; }

            public List<string> EnsuredDirectories { get; } = new List<string>();

            public List<(string Source, string Destination)> Copies { get; } =
                new List<(string Source, string Destination)>();

            public void EnsureDirectory(string path)
            {
                EnsuredDirectories.Add(path);
            }

            public void CopyFile(string sourcePath, string destinationPath)
            {
                if (ThrowOnCopy)
                {
                    throw new FileNotFoundException("Missing source.", sourcePath);
                }

                Copies.Add((sourcePath, destinationPath));
            }
        }

        private sealed class FakeXcodeProject : IAppleFoundationModelsXcodeProject
        {
            public string UnityFrameworkTargetGuid => "framework-target";

            public string MainTargetGuid => "main-target";

            public HashSet<string> SourceFiles { get; } = new HashSet<string>();

            public HashSet<string> Frameworks { get; } = new HashSet<string>();

            public HashSet<string> WeakFrameworks { get; } = new HashSet<string>();

            public Dictionary<(string Target, string Property), string> BuildProperties { get; } =
                new Dictionary<(string Target, string Property), string>();

            public bool ContainsFile(string projectPath)
            {
                return SourceFiles.Contains(projectPath);
            }

            public void AddSourceFile(string projectPath, string targetGuid)
            {
                SourceFiles.Add(projectPath);
            }

            public void AddFramework(string targetGuid, string frameworkName, bool weak)
            {
                Frameworks.Add(frameworkName);
                if (weak)
                {
                    WeakFrameworks.Add(frameworkName);
                }
            }

            public string GetBuildProperty(string targetGuid, string propertyName)
            {
                return BuildProperties.TryGetValue((targetGuid, propertyName), out var value)
                    ? value
                    : null;
            }

            public void SetBuildProperty(string targetGuid, string propertyName, string value)
            {
                BuildProperties[(targetGuid, propertyName)] = value;
            }
        }
    }
}
#endif
