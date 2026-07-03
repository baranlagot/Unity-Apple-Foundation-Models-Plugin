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

        [Test]
        public async Task RunAsync_WhenAvailabilityThrows_MarksAvailabilityFailedAndScenariosNotRun()
        {
            var runner = new DeviceValidationRunner(
                new FakeValidationClient
                {
                    AvailabilityError = new InvalidOperationException("bridge offline")
                },
                new FakeEnvironment(runGenerativeScenarios: true));

            var report = await runner.RunAsync(CancellationToken.None);
            var statuses = report.Scenarios.ToDictionary(item => item.Name, item => item.Outcome);

            Assert.That(statuses["Availability"], Is.EqualTo(DeviceValidationScenarioOutcome.Failed));
            Assert.That(statuses["OneShotText"], Is.EqualTo(DeviceValidationScenarioOutcome.NotRun));
            Assert.That(statuses["Json"], Is.EqualTo(DeviceValidationScenarioOutcome.NotRun));
        }

        [Test]
        public async Task RunAsync_WhenOuterTokenCancelled_MarksGenerationScenariosFailed()
        {
            var runner = new DeviceValidationRunner(
                new FakeValidationClient
                {
                    Availability = AppleFoundationModelsAvailability.Available("mock active")
                },
                new FakeEnvironment(runGenerativeScenarios: true));

            using (var cancelled = new CancellationTokenSource())
            {
                cancelled.Cancel();
                var report = await runner.RunAsync(cancelled.Token);
                var statuses = report.Scenarios.ToDictionary(item => item.Name, item => item.Outcome);

                Assert.That(statuses["Availability"], Is.EqualTo(DeviceValidationScenarioOutcome.Passed));
                Assert.That(statuses["OneShotText"], Is.EqualTo(DeviceValidationScenarioOutcome.Failed));
                Assert.That(statuses["Streaming"], Is.EqualTo(DeviceValidationScenarioOutcome.Failed));
                Assert.That(statuses["RepeatedRequests"], Is.EqualTo(DeviceValidationScenarioOutcome.Failed));
            }
        }

        [Test]
        public async Task RunAsync_WhenOneScenarioFails_RecordsPartialFailure()
        {
            var runner = new DeviceValidationRunner(
                new FakeValidationClient
                {
                    Availability = AppleFoundationModelsAvailability.Available("mock active"),
                    TextFactory = prompt => prompt.Contains("release-hardening")
                        ? string.Empty
                        : "token-" + prompt.GetHashCode()
                },
                new FakeEnvironment(runGenerativeScenarios: true));

            var report = await runner.RunAsync(CancellationToken.None);
            var statuses = report.Scenarios.ToDictionary(item => item.Name, item => item.Outcome);

            Assert.That(statuses["OneShotText"], Is.EqualTo(DeviceValidationScenarioOutcome.Failed));
            Assert.That(statuses["RepeatedRequests"], Is.EqualTo(DeviceValidationScenarioOutcome.Passed));
            Assert.That(statuses["ConcurrentRequests"], Is.EqualTo(DeviceValidationScenarioOutcome.Passed));
        }

        [Test]
        public async Task RunAsync_WhenStreamCompletesTwice_MarksStreamingFailed()
        {
            var runner = new DeviceValidationRunner(
                new FakeValidationClient
                {
                    Availability = AppleFoundationModelsAvailability.Available("mock active"),
                    StreamCompletionCount = 2
                },
                new FakeEnvironment(runGenerativeScenarios: true));

            var report = await runner.RunAsync(CancellationToken.None);
            var statuses = report.Scenarios.ToDictionary(item => item.Name, item => item.Outcome);

            Assert.That(statuses["Streaming"], Is.EqualTo(DeviceValidationScenarioOutcome.Failed));
            Assert.That(statuses["OneShotText"], Is.EqualTo(DeviceValidationScenarioOutcome.Passed));
        }

        [Test]
        public async Task RunAsync_WhenFocusRoundTripObserved_MarksBackgroundForegroundPassed()
        {
            var runner = new DeviceValidationRunner(
                new FakeValidationClient
                {
                    Availability = AppleFoundationModelsAvailability.Unavailable(
                        AppleFoundationModelsAvailabilityStatus.UnsupportedDevice,
                        "not eligible")
                },
                new FakeEnvironment(runGenerativeScenarios: false));
            runner.RecordApplicationFocus(false);
            runner.RecordApplicationFocus(true);

            var report = await runner.RunAsync(CancellationToken.None);
            var statuses = report.Scenarios.ToDictionary(item => item.Name, item => item.Outcome);

            Assert.That(statuses["BackgroundForeground"], Is.EqualTo(DeviceValidationScenarioOutcome.Passed));
            Assert.That(statuses["SceneReload"], Is.EqualTo(DeviceValidationScenarioOutcome.NotRun));
        }

        [Test]
        public async Task RunAsync_WhenNoLifecycleObserved_MarksLifecycleScenariosNotRun()
        {
            var runner = new DeviceValidationRunner(
                new FakeValidationClient
                {
                    Availability = AppleFoundationModelsAvailability.Unavailable(
                        AppleFoundationModelsAvailabilityStatus.UnsupportedDevice,
                        "not eligible")
                },
                new FakeEnvironment(runGenerativeScenarios: false));

            var report = await runner.RunAsync(CancellationToken.None);
            var statuses = report.Scenarios.ToDictionary(item => item.Name, item => item.Outcome);

            Assert.That(statuses["BackgroundForeground"], Is.EqualTo(DeviceValidationScenarioOutcome.NotRun));
            Assert.That(statuses["SceneReload"], Is.EqualTo(DeviceValidationScenarioOutcome.NotRun));
        }

        [Test]
        public async Task Report_ClipboardIncludesHeader_DisplayOmitsHeader()
        {
            var runner = new DeviceValidationRunner(
                new FakeValidationClient
                {
                    Availability = AppleFoundationModelsAvailability.Available("mock active")
                },
                new FakeEnvironment(runGenerativeScenarios: true));

            var report = await runner.RunAsync(CancellationToken.None);
            var clipboard = report.ToClipboardText();
            var display = report.ToDisplayText();

            StringAssert.Contains("Apple Foundation Models Validation Report", clipboard);
            StringAssert.Contains("GeneratedAtUtc:", clipboard);
            StringAssert.DoesNotContain("Validation Report", display);
            StringAssert.Contains("PackageVersion: 0.1.0", clipboard);
            StringAssert.Contains("PackageVersion: 0.1.0", display);
        }

        [Test]
        public async Task Report_ExcludesPromptsAndGeneratedContent()
        {
            var runner = new DeviceValidationRunner(
                new FakeValidationClient
                {
                    Availability = AppleFoundationModelsAvailability.Available("mock active"),
                    TextFactory = prompt => "SECRET_MODEL_OUTPUT_" + prompt.GetHashCode(),
                    JsonTitle = "SECRET_JSON_TITLE"
                },
                new FakeEnvironment(runGenerativeScenarios: true));

            var report = await runner.RunAsync(CancellationToken.None);
            var clipboard = report.ToClipboardText();

            // No model-generated text, structured field content, or prompt wording may appear.
            StringAssert.DoesNotContain("SECRET_MODEL_OUTPUT", clipboard);
            StringAssert.DoesNotContain("SECRET_JSON_TITLE", clipboard);
            StringAssert.DoesNotContain("release-hardening", clipboard);
            StringAssert.DoesNotContain("orange cat", clipboard);
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

            public Exception AvailabilityError { get; set; }

            public TimeSpan StreamDelay { get; set; }

            // Produces the text returned for a prompt. Distinct per prompt by default so
            // repeated/concurrent scenarios observe non-duplicate output.
            public Func<string, string> TextFactory { get; set; }

            // Number of times a stream invokes onComplete. Anything other than one exercises
            // the exactly-once completion guard in the runner.
            public int StreamCompletionCount { get; set; } = 1;

            // Distinctive marker embedded in the generated JSON title, used to prove the
            // report never leaks model-generated content.
            public string JsonTitle { get; set; } = "GeneratedQuestTitle";

            public Task<AppleFoundationModelsAvailability> GetAvailabilityAsync()
            {
                if (AvailabilityError != null)
                {
                    return Task.FromException<AppleFoundationModelsAvailability>(AvailabilityError);
                }

                return Task.FromResult(Availability);
            }

            public Task<AppleFoundationModelsResult> GenerateTextAsync(
                string prompt,
                AppleFoundationModelsOptions options = null,
                CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var text = TextFactory != null
                    ? TextFactory(prompt)
                    : "[Validation:" + prompt.GetHashCode() + "]";
                return Task.FromResult(AppleFoundationModelsResult.Success(text));
            }

            public Task<T> GenerateJsonAsync<T>(
                string prompt,
                AppleFoundationModelsOptions options = null,
                CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Honor the real generic contract: deserialize a JSON payload into the
                // caller-requested type rather than casting from a test-private type.
                var json =
                    "{\"title\":\"" + JsonTitle +
                    "\",\"objective\":\"Objective\",\"rewardCoins\":10,\"npcName\":\"Mittens\"}";
                return Task.FromResult(UnityEngine.JsonUtility.FromJson<T>(json));
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
                    for (var completion = 0; completion < StreamCompletionCount; completion++)
                    {
                        onComplete(AppleFoundationModelsResult.Success("one two"));
                    }
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
        }
    }
}
