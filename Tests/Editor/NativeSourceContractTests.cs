using System.Collections.Generic;
using System.IO;
using Baran.AppleFoundationModels.Native;
using NUnit.Framework;
using UnityEditor.PackageManager;

namespace Baran.AppleFoundationModels.Editor.Tests
{
    public sealed class NativeSourceContractTests
    {
        private static readonly string[] AbiSymbols =
        {
            "AFM_SetEventCallback",
            "AFM_SetDebugLogging",
            "AFM_GetAvailability",
            "AFM_GenerateText",
            "AFM_StreamText",
            "AFM_CancelRequest"
        };

        [Test]
        public void NativeSourceLayout_ContainsEveryRequiredBridgeFile()
        {
            var packageRoot = GetPackageRoot();
            var files = new[]
            {
                "Native~/Sources/AppleFoundationModelsCore/AppleFoundationModelsCore.swift",
                "Native~/Sources/AppleFoundationModelsCore/AppleFoundationModelsModels.swift",
                "Native~/Sources/AppleFoundationModelsCore/AppleFoundationModelsErrorMapper.swift",
                "Plugins/iOS/AppleFoundationModelsBridge.h",
                "Plugins/iOS/AppleFoundationModelsBridge.mm",
                "Plugins/iOS/AppleFoundationModelsBridge.swift",
                "Tests/Native/AppleFoundationModelsNativeHarness.swift",
                "scripts/validate_swift_bridge.sh"
            };

            foreach (var relativePath in files)
            {
                Assert.That(
                    File.Exists(Path.Combine(packageRoot, relativePath)),
                    Is.True,
                    $"Missing native bridge source: {relativePath}");
            }
        }

        [Test]
        public void NativeAbi_HeaderSwiftAndManagedTransportExposeSameSymbols()
        {
            var packageRoot = GetPackageRoot();
            var header = File.ReadAllText(Path.Combine(
                packageRoot,
                "Plugins/iOS/AppleFoundationModelsBridge.h"));
            var swift = File.ReadAllText(Path.Combine(
                packageRoot,
                "Plugins/iOS/AppleFoundationModelsBridge.swift"));
            var managed = File.ReadAllText(Path.Combine(
                packageRoot,
                "Runtime/Native/AppleFoundationModelsNativeTransport.cs"));

            foreach (var symbol in AbiSymbols)
            {
                StringAssert.Contains(symbol, header, $"Header is missing {symbol}.");
                StringAssert.Contains(
                    $"@_cdecl(\"{symbol}\")",
                    swift,
                    $"Swift bridge is missing {symbol}.");
                StringAssert.Contains(symbol, managed, $"Managed transport is missing {symbol}.");
            }
        }

        [Test]
        public void NativeProtocol_SwiftEmitsEveryManagedEventType()
        {
            var models = File.ReadAllText(Path.Combine(
                GetPackageRoot(),
                "Native~/Sources/AppleFoundationModelsCore/AppleFoundationModelsModels.swift"));
            var eventTypes = new HashSet<string>
            {
                NativeEventTypes.Availability,
                NativeEventTypes.Text,
                NativeEventTypes.StreamDelta,
                NativeEventTypes.Complete,
                NativeEventTypes.Error
            };

            foreach (var eventType in eventTypes)
            {
                StringAssert.Contains(
                    $"type: \"{eventType}\"",
                    models,
                    $"Swift event model is missing '{eventType}'.");
            }
        }

        [Test]
        public void StreamingCore_ConvertsMonotonicSnapshotsIntoDeltas()
        {
            var packageRoot = GetPackageRoot();
            var core = File.ReadAllText(Path.Combine(
                packageRoot,
                "Native~/Sources/AppleFoundationModelsCore/AppleFoundationModelsCore.swift"));
            var models = File.ReadAllText(Path.Combine(
                packageRoot,
                "Native~/Sources/AppleFoundationModelsCore/AppleFoundationModelsModels.swift"));

            // The streaming core delegates snapshot-to-delta conversion to the reusable,
            // hardware-independent accumulator and still guards cancellation per snapshot.
            StringAssert.Contains("AFMStreamAccumulator()", core);
            StringAssert.Contains("accumulator.delta(for: snapshot.content)", core);
            StringAssert.Contains("Task.checkCancellation()", core);

            // The accumulator owns the monotonic-prefix contract and delta extraction.
            StringAssert.Contains("snapshot.hasPrefix(accumulated)", models);
            StringAssert.Contains("snapshot.dropFirst(accumulated.count)", models);
            StringAssert.Contains("AFMBridgeError.nonMonotonicStream", models);
        }

        private static string GetPackageRoot()
        {
            var package = PackageInfo.FindForAssembly(
                typeof(global::Baran.AppleFoundationModels.AppleFoundationModels).Assembly);
            Assert.That(package, Is.Not.Null, "Could not resolve the package root.");
            return package.resolvedPath;
        }
    }
}
