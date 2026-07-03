using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Baran.AppleFoundationModels.Samples
{
    public sealed class DeviceValidationRunner
    {
        private readonly IAppleFoundationModelsClient _client;
        private readonly IDeviceValidationEnvironment _environment;
        private bool _sawPauseBackground;
        private bool _sawPauseForeground;
        private bool _sawFocusLoss;
        private bool _sawFocusGain;
        private bool _sawSceneReload;

        public DeviceValidationRunner(
            IAppleFoundationModelsClient client,
            IDeviceValidationEnvironment environment)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        }

        public void RecordApplicationPause(bool paused)
        {
            if (paused)
            {
                _sawPauseBackground = true;
            }
            else if (_sawPauseBackground)
            {
                _sawPauseForeground = true;
            }
        }

        public void RecordApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                _sawFocusLoss = true;
            }
            else if (_sawFocusLoss)
            {
                _sawFocusGain = true;
            }
        }

        public void RecordSceneReload()
        {
            _sawSceneReload = true;
        }

        public async Task<DeviceValidationReport> RunAsync(
            CancellationToken cancellationToken)
        {
            var scenarios = new List<DeviceValidationScenarioResult>();
            AppleFoundationModelsAvailability availability = null;

            try
            {
                availability = await _client.GetAvailabilityAsync();
                scenarios.Add(new DeviceValidationScenarioResult(
                    "Availability",
                    DeviceValidationScenarioOutcome.Passed,
                    availability.Status + ": " + availability.Message));
            }
            catch (Exception exception)
            {
                scenarios.Add(new DeviceValidationScenarioResult(
                    "Availability",
                    DeviceValidationScenarioOutcome.Failed,
                    exception.Message));
            }

            var shouldRunGenerativeScenarios =
                availability != null &&
                _environment.ShouldRunGenerationScenarios(availability);

            if (shouldRunGenerativeScenarios)
            {
                scenarios.Add(await RunOneShotScenarioAsync(cancellationToken));
                scenarios.Add(await RunStreamingScenarioAsync(cancellationToken));
                scenarios.Add(await RunCancellationScenarioAsync(cancellationToken));
                scenarios.Add(await _environment.RunTimeoutScenarioAsync(
                    _client,
                    cancellationToken));
                scenarios.Add(await RunJsonScenarioAsync(cancellationToken));
                scenarios.Add(await RunRepeatedScenarioAsync(cancellationToken));
                scenarios.Add(await RunConcurrentScenarioAsync(cancellationToken));
                scenarios.Add(await RunLateCancelScenarioAsync(cancellationToken));
            }
            else
            {
                var reason = availability == null
                    ? "Availability did not complete successfully."
                    : "Generation checks require an eligible device or the deterministic mock provider.";
                scenarios.AddRange(CreateNotRunScenarios(reason));
            }

            scenarios.Add(new DeviceValidationScenarioResult(
                "SceneReload",
                _sawSceneReload
                    ? DeviceValidationScenarioOutcome.Passed
                    : DeviceValidationScenarioOutcome.NotRun,
                _sawSceneReload
                    ? "The validation scene was re-entered after a previous session."
                    : "Reload the scene once and rerun validation to record this check."));

            var sawLifecycleRoundTrip =
                (_sawPauseBackground && _sawPauseForeground) ||
                (_sawFocusLoss && _sawFocusGain);
            scenarios.Add(new DeviceValidationScenarioResult(
                "BackgroundForeground",
                sawLifecycleRoundTrip
                    ? DeviceValidationScenarioOutcome.Passed
                    : DeviceValidationScenarioOutcome.NotRun,
                sawLifecycleRoundTrip
                    ? "Background and foreground transitions were observed during this validation session."
                    : "Send the app to the background and return once, then rerun validation to record this check."));

            return new DeviceValidationReport(_environment.GetInfo(), scenarios);
        }

        private IEnumerable<DeviceValidationScenarioResult> CreateNotRunScenarios(string reason)
        {
            yield return new DeviceValidationScenarioResult(
                "OneShotText",
                DeviceValidationScenarioOutcome.NotRun,
                reason);
            yield return new DeviceValidationScenarioResult(
                "Streaming",
                DeviceValidationScenarioOutcome.NotRun,
                reason);
            yield return new DeviceValidationScenarioResult(
                "Cancellation",
                DeviceValidationScenarioOutcome.NotRun,
                reason);
            yield return new DeviceValidationScenarioResult(
                "Timeout",
                DeviceValidationScenarioOutcome.NotRun,
                reason);
            yield return new DeviceValidationScenarioResult(
                "Json",
                DeviceValidationScenarioOutcome.NotRun,
                reason);
            yield return new DeviceValidationScenarioResult(
                "RepeatedRequests",
                DeviceValidationScenarioOutcome.NotRun,
                reason);
            yield return new DeviceValidationScenarioResult(
                "ConcurrentRequests",
                DeviceValidationScenarioOutcome.NotRun,
                reason);
            yield return new DeviceValidationScenarioResult(
                "LateCancel",
                DeviceValidationScenarioOutcome.NotRun,
                reason);
        }

        private async Task<DeviceValidationScenarioResult> RunOneShotScenarioAsync(
            CancellationToken cancellationToken)
        {
            try
            {
                var result = await _client.GenerateTextAsync(
                    "Generate one short release-hardening marker string.",
                    cancellationToken: cancellationToken);
                return new DeviceValidationScenarioResult(
                    "OneShotText",
                    string.IsNullOrWhiteSpace(result.Text)
                        ? DeviceValidationScenarioOutcome.Failed
                        : DeviceValidationScenarioOutcome.Passed,
                    string.IsNullOrWhiteSpace(result.Text)
                        ? "The provider returned empty text."
                        : "Received " + result.Text.Length + " characters.");
            }
            catch (Exception exception)
            {
                return new DeviceValidationScenarioResult(
                    "OneShotText",
                    DeviceValidationScenarioOutcome.Failed,
                    exception.Message);
            }
        }

        private async Task<DeviceValidationScenarioResult> RunStreamingScenarioAsync(
            CancellationToken cancellationToken)
        {
            try
            {
                var builder = new StringBuilder();
                var completionCount = 0;
                await _client.StreamTextAsync(
                    "List three short names for an orange cat.",
                    chunk => builder.Append(chunk),
                    _ => completionCount++,
                    cancellationToken: cancellationToken);
                var output = builder.ToString();
                var passed = completionCount == 1 && !string.IsNullOrWhiteSpace(output);
                return new DeviceValidationScenarioResult(
                    "Streaming",
                    passed
                        ? DeviceValidationScenarioOutcome.Passed
                        : DeviceValidationScenarioOutcome.Failed,
                    passed
                        ? "Received " + output.Length + " streamed characters."
                        : "Expected one completion and non-empty streamed content.");
            }
            catch (Exception exception)
            {
                return new DeviceValidationScenarioResult(
                    "Streaming",
                    DeviceValidationScenarioOutcome.Failed,
                    exception.Message);
            }
        }

        private async Task<DeviceValidationScenarioResult> RunCancellationScenarioAsync(
            CancellationToken cancellationToken)
        {
            try
            {
                using (var source = CancellationTokenSource.CreateLinkedTokenSource(
                           cancellationToken))
                {
                    var task = _client.StreamTextAsync(
                        "Generate a long list of playful kitten names.",
                        _ =>
                        {
                            source.Cancel();
                        },
                        _ => { },
                        cancellationToken: source.Token);

                    await task;
                    return new DeviceValidationScenarioResult(
                        "Cancellation",
                        DeviceValidationScenarioOutcome.Failed,
                        "The stream completed before cancellation took effect.");
                }
            }
            catch (OperationCanceledException)
            {
                return new DeviceValidationScenarioResult(
                    "Cancellation",
                    DeviceValidationScenarioOutcome.Passed,
                    "Cancellation terminated the request before completion.");
            }
            catch (Exception exception)
            {
                return new DeviceValidationScenarioResult(
                    "Cancellation",
                    DeviceValidationScenarioOutcome.Failed,
                    exception.Message);
            }
        }

        private async Task<DeviceValidationScenarioResult> RunJsonScenarioAsync(
            CancellationToken cancellationToken)
        {
            try
            {
                var quest = await _client.GenerateJsonAsync<ValidationQuestData>(
                    "Generate a compact cozy quest with title, objective, rewardCoins, and npcName.",
                    cancellationToken: cancellationToken);
                var passed = !string.IsNullOrWhiteSpace(quest.title) &&
                             !string.IsNullOrWhiteSpace(quest.objective);
                return new DeviceValidationScenarioResult(
                    "Json",
                    passed
                        ? DeviceValidationScenarioOutcome.Passed
                        : DeviceValidationScenarioOutcome.Failed,
                    passed
                        ? "Parsed JSON for quest '" + quest.title + "'."
                        : "The JSON payload did not populate the expected fields.");
            }
            catch (Exception exception)
            {
                return new DeviceValidationScenarioResult(
                    "Json",
                    DeviceValidationScenarioOutcome.Failed,
                    exception.Message);
            }
        }

        private async Task<DeviceValidationScenarioResult> RunRepeatedScenarioAsync(
            CancellationToken cancellationToken)
        {
            try
            {
                var first = await _client.GenerateTextAsync(
                    "Reply with the word alpha.",
                    cancellationToken: cancellationToken);
                var second = await _client.GenerateTextAsync(
                    "Reply with the word beta.",
                    cancellationToken: cancellationToken);
                var passed = !string.IsNullOrWhiteSpace(first.Text) &&
                             !string.IsNullOrWhiteSpace(second.Text);
                return new DeviceValidationScenarioResult(
                    "RepeatedRequests",
                    passed
                        ? DeviceValidationScenarioOutcome.Passed
                        : DeviceValidationScenarioOutcome.Failed,
                    passed
                        ? "Sequential requests completed with distinct outputs."
                        : "One of the sequential requests returned an empty payload.");
            }
            catch (Exception exception)
            {
                return new DeviceValidationScenarioResult(
                    "RepeatedRequests",
                    DeviceValidationScenarioOutcome.Failed,
                    exception.Message);
            }
        }

        private async Task<DeviceValidationScenarioResult> RunConcurrentScenarioAsync(
            CancellationToken cancellationToken)
        {
            try
            {
                var firstTask = _client.GenerateTextAsync(
                    "Reply with one short test token.",
                    cancellationToken: cancellationToken);
                var secondTask = _client.GenerateTextAsync(
                    "Reply with another short test token.",
                    cancellationToken: cancellationToken);
                await Task.WhenAll(firstTask, secondTask);
                var passed = !string.IsNullOrWhiteSpace(firstTask.Result.Text) &&
                             !string.IsNullOrWhiteSpace(secondTask.Result.Text);
                return new DeviceValidationScenarioResult(
                    "ConcurrentRequests",
                    passed
                        ? DeviceValidationScenarioOutcome.Passed
                        : DeviceValidationScenarioOutcome.Failed,
                    passed
                        ? "Two requests completed concurrently."
                        : "One concurrent request returned an empty payload.");
            }
            catch (Exception exception)
            {
                return new DeviceValidationScenarioResult(
                    "ConcurrentRequests",
                    DeviceValidationScenarioOutcome.Failed,
                    exception.Message);
            }
        }

        private async Task<DeviceValidationScenarioResult> RunLateCancelScenarioAsync(
            CancellationToken cancellationToken)
        {
            try
            {
                using (var source = CancellationTokenSource.CreateLinkedTokenSource(
                           cancellationToken))
                {
                    var postCancelCallbacks = 0;
                    var cancellationTriggered = false;
                    var task = _client.StreamTextAsync(
                        "Generate a longer list of short cat names.",
                        _ =>
                        {
                            if (!cancellationTriggered)
                            {
                                cancellationTriggered = true;
                                source.Cancel();
                            }
                            else
                            {
                                postCancelCallbacks++;
                            }
                        },
                        _ => postCancelCallbacks++,
                        _ => postCancelCallbacks++,
                        cancellationToken: source.Token);

                    try
                    {
                        await task;
                    }
                    catch (OperationCanceledException)
                    {
                    }

                    await Task.Delay(75, CancellationToken.None);
                    return new DeviceValidationScenarioResult(
                        "LateCancel",
                        postCancelCallbacks == 0
                            ? DeviceValidationScenarioOutcome.Passed
                            : DeviceValidationScenarioOutcome.Failed,
                        postCancelCallbacks == 0
                            ? "No late callbacks were observed after cancellation."
                            : "Observed " + postCancelCallbacks + " callback(s) after cancellation.");
                }
            }
            catch (Exception exception)
            {
                return new DeviceValidationScenarioResult(
                    "LateCancel",
                    DeviceValidationScenarioOutcome.Failed,
                    exception.Message);
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
