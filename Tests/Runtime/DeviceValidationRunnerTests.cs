using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Baran.AppleFoundationModels.Samples.Tests
{
    public sealed class DeviceValidationRunnerTests
    {
        [Test]
        public async Task RunAsync_WhenProviderIsUnavailable_MarksGenerativeScenariosNotRun()
        {
            var runner = new DeviceValidationRunner(
                new FakeValidationClient
                {
                    Availability = AppleFoundationModelsAvailability.Unavailable(
                        AppleFoundationModelsAvailabilityStatus.UnsupportedDevice,
                        "This device is not eligible.")
                },
                new FakeEnvironment(runGenerativeScenarios: false));

            var report = await runner.RunAsync(CancellationToken.None);
            var statuses = report.Scenarios.ToDictionary(item => item.Name, item => item.Outcome);

            Assert.That(statuses["Availability"], Is.EqualTo(DeviceValidationScenarioOutcome.Passed));
            Assert.That(statuses["OneShotText"], Is.EqualTo(DeviceValidationScenarioOutcome.NotRun));
            Assert.That(statuses["Streaming"], Is.EqualTo(DeviceValidationScenarioOutcome.NotRun));
        }

        [Test]
        public async Task RunAsync_WhenMockProviderIsActive_ExercisesFunctionalScenarios()
        {
            var runner = new DeviceValidationRunner(
                new FakeValidationClient
                {
                    Availability = AppleFoundationModelsAvailability.Available(
                        "The deterministic mock provider is active."),
                    StreamDelay = TimeSpan.FromMilliseconds(5)
                },
                new FakeEnvironment(runGenerativeScenarios: true));
            runner.RecordSceneReload();
            runner.RecordApplicationPause(true);
            runner.RecordApplicationPause(false);

            var report = await runner.RunAsync(CancellationToken.None);
            var statuses = report.Scenarios.ToDictionary(item => item.Name, item => item.Outcome);

            Assert.That(statuses["OneShotText"], Is.EqualTo(DeviceValidationScenarioOutcome.Passed));
            Assert.That(statuses["Streaming"], Is.EqualTo(DeviceValidationScenarioOutcome.Passed));
            Assert.That(statuses["Cancellation"], Is.EqualTo(DeviceValidationScenarioOutcome.Passed));
            Assert.That(statuses["Json"], Is.EqualTo(DeviceValidationScenarioOutcome.Passed));
            Assert.That(statuses["RepeatedRequests"], Is.EqualTo(DeviceValidationScenarioOutcome.Passed));
            Assert.That(statuses["ConcurrentRequests"], Is.EqualTo(DeviceValidationScenarioOutcome.Passed));
            Assert.That(statuses["LateCancel"], Is.EqualTo(DeviceValidationScenarioOutcome.Passed));
            Assert.That(statuses["SceneReload"], Is.EqualTo(DeviceValidationScenarioOutcome.Passed));
            Assert.That(statuses["BackgroundForeground"], Is.EqualTo(DeviceValidationScenarioOutcome.Passed));
            StringAssert.Contains("PackageVersion", report.ToClipboardText());
        }

        private sealed class FakeEnvironment : IDeviceValidationEnvironment
        {
            private readonly bool _runGenerativeScenarios;

            public FakeEnvironment(bool runGenerativeScenarios)
            {
                _runGenerativeScenarios = runGenerativeScenarios;
            }

            public DeviceValidationEnvironmentInfo GetInfo()
            {
                return new DeviceValidationEnvironmentInfo
                {
                    PackageVersion = "0.1.0",
                    PackageRevision = "test",
                    UnityVersion = "6000.0.61f1",
                    XcodeVersion = "Xcode 26.3",
                    OperatingSystem = "iOS 26.0",
                    DeviceModel = "Mock Device",
                    RunMode = "Test"
                };
            }

            public bool ShouldRunGenerationScenarios(
                AppleFoundationModelsAvailability availability)
            {
                return _runGenerativeScenarios;
            }

            public Task<DeviceValidationScenarioResult> RunTimeoutScenarioAsync(
                IAppleFoundationModelsClient client,
                CancellationToken cancellationToken)
            {
                return Task.FromResult(new DeviceValidationScenarioResult(
                    "Timeout",
                    DeviceValidationScenarioOutcome.NotRun,
                    "Timeout probe not configured for this test."));
            }
        }

        private sealed class FakeValidationClient : IAppleFoundationModelsClient
        {
            public AppleFoundationModelsAvailability Availability { get; set; }

            public TimeSpan StreamDelay { get; set; }

            public Task<AppleFoundationModelsAvailability> GetAvailabilityAsync()
            {
                return Task.FromResult(Availability);
            }

            public Task<AppleFoundationModelsResult> GenerateTextAsync(
                string prompt,
                AppleFoundationModelsOptions options = null,
                CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return Task.FromResult(AppleFoundationModelsResult.Success(
                    "[Validation] " + prompt));
            }

            public Task<T> GenerateJsonAsync<T>(
                string prompt,
                AppleFoundationModelsOptions options = null,
                CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                object quest = new ValidationQuestData
                {
                    title = "Quest",
                    objective = "Objective",
                    rewardCoins = 10,
                    npcName = "Mittens"
                };
                return Task.FromResult((T)quest);
            }

            public async Task StreamTextAsync(
                string prompt,
                Action<string> onToken,
                Action<AppleFoundationModelsResult> onComplete,
                Action<Exception> onError = null,
                AppleFoundationModelsOptions options = null,
                CancellationToken cancellationToken = default)
            {
                try
                {
                    foreach (var chunk in new[] { "one ", "two" })
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        if (StreamDelay > TimeSpan.Zero)
                        {
                            await Task.Delay(StreamDelay, cancellationToken);
                        }

                        onToken(chunk);
                    }

                    cancellationToken.ThrowIfCancellationRequested();
                    onComplete(AppleFoundationModelsResult.Success("one two"));
                }
                catch (Exception exception)
                {
                    if (onError != null && !(exception is OperationCanceledException))
                    {
                        onError(exception);
                        return;
                    }

                    throw;
                }
            }

            [Serializable]
            private sealed class ValidationQuestData
            {
                public string title;
                public string objective;
                public int rewardCoins;
                public string npcName;
            }
        }
    }
}
