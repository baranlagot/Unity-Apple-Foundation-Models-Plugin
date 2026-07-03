using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Baran.AppleFoundationModels.Samples
{
    public enum DeviceValidationScenarioOutcome
    {
        Passed,
        Failed,
        NotRun
    }

    public sealed class DeviceValidationScenarioResult
    {
        public DeviceValidationScenarioResult(
            string name,
            DeviceValidationScenarioOutcome outcome,
            string detail)
        {
            Name = name ?? string.Empty;
            Outcome = outcome;
            Detail = detail ?? string.Empty;
        }

        public string Name { get; }

        public DeviceValidationScenarioOutcome Outcome { get; }

        public string Detail { get; }
    }

    public sealed class DeviceValidationEnvironmentInfo
    {
        public string PackageVersion = AppleFoundationModelsReleaseMetadata.PackageVersion;
        public string PackageRevision = AppleFoundationModelsReleaseMetadata.PackageRevision;
        public string UnityVersion = Application.unityVersion;
        public string XcodeVersion = "Unavailable";
        public string OperatingSystem = SystemInfo.operatingSystem;
        public string DeviceModel = SystemInfo.deviceModel;
        public string RunMode = Application.isEditor ? "Editor" : "Device";
    }

    public sealed class DeviceValidationReport
    {
        public DeviceValidationReport(
            DeviceValidationEnvironmentInfo environment,
            IList<DeviceValidationScenarioResult> scenarios)
        {
            Environment = environment ?? throw new ArgumentNullException(nameof(environment));
            Scenarios = new List<DeviceValidationScenarioResult>(
                scenarios ?? throw new ArgumentNullException(nameof(scenarios)));
            GeneratedAtUtc = DateTime.UtcNow;
        }

        public DeviceValidationEnvironmentInfo Environment { get; }

        public IList<DeviceValidationScenarioResult> Scenarios { get; }

        public DateTime GeneratedAtUtc { get; }

        public string ToDisplayText()
        {
            return BuildText(includeHeader: false);
        }

        public string ToClipboardText()
        {
            return BuildText(includeHeader: true);
        }

        private string BuildText(bool includeHeader)
        {
            var builder = new StringBuilder();
            if (includeHeader)
            {
                builder.AppendLine("Apple Foundation Models Validation Report");
                builder.AppendLine("GeneratedAtUtc: " + GeneratedAtUtc.ToString("O"));
            }

            builder.AppendLine("PackageVersion: " + Environment.PackageVersion);
            builder.AppendLine("PackageRevision: " + Environment.PackageRevision);
            builder.AppendLine("UnityVersion: " + Environment.UnityVersion);
            builder.AppendLine("XcodeVersion: " + Environment.XcodeVersion);
            builder.AppendLine("OperatingSystem: " + Environment.OperatingSystem);
            builder.AppendLine("DeviceModel: " + Environment.DeviceModel);
            builder.AppendLine("RunMode: " + Environment.RunMode);
            builder.AppendLine();

            foreach (var scenario in Scenarios)
            {
                builder.AppendLine(
                    scenario.Name + ": " + scenario.Outcome + " - " + scenario.Detail);
            }

            return builder.ToString().TrimEnd();
        }
    }

    public interface IDeviceValidationEnvironment
    {
        DeviceValidationEnvironmentInfo GetInfo();

        bool ShouldRunGenerationScenarios(AppleFoundationModelsAvailability availability);

        Task<DeviceValidationScenarioResult> RunTimeoutScenarioAsync(
            IAppleFoundationModelsClient client,
            CancellationToken cancellationToken);
    }

    public sealed class DefaultDeviceValidationEnvironment :
        IDeviceValidationEnvironment
    {
        public DeviceValidationEnvironmentInfo GetInfo()
        {
            var info = new DeviceValidationEnvironmentInfo();
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            info.XcodeVersion = ReadXcodeVersion();
#endif
            return info;
        }

        public bool ShouldRunGenerationScenarios(
            AppleFoundationModelsAvailability availability)
        {
            if (availability == null)
            {
                return false;
            }

            return availability.IsAvailable ||
                   availability.Message.ToLowerInvariant().Contains("mock");
        }

        public Task<DeviceValidationScenarioResult> RunTimeoutScenarioAsync(
            IAppleFoundationModelsClient client,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new DeviceValidationScenarioResult(
                "Timeout",
                DeviceValidationScenarioOutcome.NotRun,
                "Requires a provider-specific timeout probe to verify native timeout behavior."));
        }

#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        private static string ReadXcodeVersion()
        {
            try
            {
                using (var process = new Process())
                {
                    process.StartInfo.FileName = "/usr/bin/xcodebuild";
                    process.StartInfo.Arguments = "-version";
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;
                    process.Start();
                    var output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit(1000);
                    if (!string.IsNullOrWhiteSpace(output))
                    {
                        return output.Trim().Replace(Environment.NewLine, " | ");
                    }
                }
            }
            catch
            {
            }

            return "Unavailable";
        }
#endif
    }
}
