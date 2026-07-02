using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor.PackageManager;

namespace Baran.AppleFoundationModels.Editor.Tests
{
    public sealed class SampleSourceContractTests
    {
        private static readonly (string Folder, string Script, string Scene)[] Samples =
        {
            ("AvailabilityCheck", "AvailabilityCheckExample.cs", "AvailabilityCheck.unity"),
            ("TextGeneration", "TextGenerationExample.cs", "TextGeneration.unity"),
            ("StreamingChat", "StreamingChatExample.cs", "StreamingChat.unity"),
            ("JsonGeneration", "JsonGenerationExample.cs", "JsonGeneration.unity")
        };

        [Test]
        public void EveryDeclaredSample_HasExecutableSceneAndDocumentation()
        {
            var packageRoot = GetPackageRoot();
            foreach (var sample in Samples)
            {
                var root = Path.Combine(packageRoot, "Samples~", sample.Folder);
                Assert.That(File.Exists(Path.Combine(root, sample.Script)), Is.True);
                Assert.That(File.Exists(Path.Combine(root, sample.Script + ".meta")), Is.True);
                Assert.That(File.Exists(Path.Combine(root, sample.Scene)), Is.True);
                Assert.That(File.Exists(Path.Combine(root, sample.Scene + ".meta")), Is.True);
                Assert.That(File.Exists(Path.Combine(root, "README.md")), Is.True);
            }
        }

        [Test]
        public void EverySampleScene_ReferencesItsControllerScript()
        {
            var packageRoot = GetPackageRoot();
            foreach (var sample in Samples)
            {
                var root = Path.Combine(packageRoot, "Samples~", sample.Folder);
                var meta = File.ReadAllText(Path.Combine(root, sample.Script + ".meta"));
                var match = Regex.Match(meta, @"(?m)^guid:\s*(?<guid>[a-f0-9]+)$");
                Assert.That(match.Success, Is.True, $"No script GUID found for {sample.Folder}.");

                var scene = File.ReadAllText(Path.Combine(root, sample.Scene));
                StringAssert.Contains(
                    $"guid: {match.Groups["guid"].Value}",
                    scene,
                    $"{sample.Scene} does not reference {sample.Script}.");
            }
        }

        [Test]
        public void EverySampleController_UsesInjectableDefaultClientAndBuiltInView()
        {
            var packageRoot = GetPackageRoot();
            foreach (var sample in Samples)
            {
                var source = File.ReadAllText(Path.Combine(
                    packageRoot,
                    "Samples~",
                    sample.Folder,
                    sample.Script));
                StringAssert.Contains("IAppleFoundationModelsClient", source);
                StringAssert.Contains("AppleFoundationModels.DefaultClient", source);
                StringAssert.Contains("OnGUI", source);
            }
        }

        private static string GetPackageRoot()
        {
            var package = PackageInfo.FindForAssembly(
                typeof(global::Baran.AppleFoundationModels.AppleFoundationModels).Assembly);
            Assert.That(package, Is.Not.Null, "Could not resolve package root.");
            return package.resolvedPath;
        }
    }
}
