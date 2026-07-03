using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Baran.AppleFoundationModels.Samples.Tests
{
    public sealed class SampleDiagnosticPresenterTests
    {
        [Test]
        public void AvailabilityPresenter_WhenAvailabilitySucceeds_RendersSuccessState()
        {
            var view = new FakeView();
            var presenter = new AvailabilityPresenter(
                new FakeClient
                {
                    AvailabilityAsync = () => Task.FromResult(
                        AppleFoundationModelsAvailability.Available(
                            "The deterministic mock provider is active."))
                },
                view);

            presenter.Initialize();
            presenter.OnPrimaryActionRequested();

            Assert.That(
                SpinWait.SpinUntil(
                    () => view.LastState != null &&
                          view.LastState.StatusTone == SampleDiagnosticStatusTone.Success,
                    1000),
                Is.True);
            StringAssert.Contains("mock provider", view.LastState.Output);
        }

        [Test]
        public void SampleRequestPresenter_DisablesPrimaryActionUntilPromptIsSet()
        {
            var view = new FakeView();
            var presenter = new SampleRequestPresenter(
                view,
                "Title",
                "Subtitle",
                string.Empty,
                "Generate",
                (prompt, token) => Task.FromResult(prompt.ToUpperInvariant()));

            presenter.Initialize();
            Assert.That(view.LastState.PrimaryActionEnabled, Is.False);

            presenter.OnPromptChanged("hello");
            Assert.That(view.LastState.PrimaryActionEnabled, Is.True);

            presenter.OnPrimaryActionRequested();
            Assert.That(
                SpinWait.SpinUntil(
                    () => view.LastState.StatusTone == SampleDiagnosticStatusTone.Success,
                    1000),
                Is.True);
            Assert.That(view.LastState.Output, Is.EqualTo("HELLO"));
        }

        [Test]
        public void StreamingPresenter_WhenCancelled_RendersWarningState()
        {
            var view = new FakeView();
            var presenter = new StreamingSamplePresenter(
                new FakeClient
                {
                    StreamAsync = async (
                        prompt,
                        onToken,
                        onComplete,
                        onError,
                        options,
                        token) =>
                    {
                        onToken("one ");
                        await Task.Delay(50, token);
                        onToken("two");
                        onComplete(AppleFoundationModelsResult.Success("one two"));
                    }
                },
                view,
                "hello");

            presenter.Initialize();
            presenter.OnPrimaryActionRequested();
            Assert.That(
                SpinWait.SpinUntil(
                    () => view.LastState.StatusTone == SampleDiagnosticStatusTone.InProgress,
                    1000),
                Is.True);

            presenter.OnSecondaryActionRequested();

            Assert.That(
                SpinWait.SpinUntil(
                    () => view.LastState.StatusTone == SampleDiagnosticStatusTone.Warning,
                    1000),
                Is.True);
            StringAssert.Contains("cancelled", view.LastState.Status.ToLowerInvariant());
        }

        [Test]
        public void AvailabilityPresenter_WhenUnavailable_RendersWarningState()
        {
            var view = new FakeView();
            var presenter = new AvailabilityPresenter(
                new FakeClient
                {
                    AvailabilityAsync = () => Task.FromResult(
                        AppleFoundationModelsAvailability.Unavailable(
                            AppleFoundationModelsAvailabilityStatus.AppleIntelligenceDisabled,
                            "Apple Intelligence is turned off in Settings."))
                },
                view);

            presenter.Initialize();
            presenter.OnPrimaryActionRequested();

            Assert.That(
                SpinWait.SpinUntil(
                    () => view.LastState != null &&
                          view.LastState.StatusTone == SampleDiagnosticStatusTone.Warning,
                    1000),
                Is.True);
            StringAssert.Contains("AppleIntelligenceDisabled", view.LastState.Output);
        }

        [Test]
        public void AvailabilityPresenter_WhenClientThrows_RendersErrorState()
        {
            var view = new FakeView();
            var presenter = new AvailabilityPresenter(
                new FakeClient
                {
                    AvailabilityAsync = () =>
                        Task.FromException<AppleFoundationModelsAvailability>(
                            new InvalidOperationException("bridge offline"))
                },
                view);

            presenter.Initialize();
            presenter.OnPrimaryActionRequested();

            Assert.That(
                SpinWait.SpinUntil(
                    () => view.LastState != null &&
                          view.LastState.StatusTone == SampleDiagnosticStatusTone.Error,
                    1000),
                Is.True);
            StringAssert.Contains("bridge offline", view.LastState.Output);
        }

        [Test]
        public void SampleRequestPresenter_WhenBusy_IgnoresReentrantPrimaryAction()
        {
            var view = new FakeView();
            var gate = new TaskCompletionSource<string>();
            var invocations = 0;
            var presenter = new SampleRequestPresenter(
                view,
                "Title",
                "Subtitle",
                "prompt",
                "Generate",
                (prompt, token) =>
                {
                    Interlocked.Increment(ref invocations);
                    return gate.Task;
                });

            presenter.Initialize();
            presenter.OnPrimaryActionRequested();
            Assert.That(
                SpinWait.SpinUntil(
                    () => view.LastState.StatusTone == SampleDiagnosticStatusTone.InProgress,
                    1000),
                Is.True);

            // Re-entrant requests while a run is in flight must be ignored.
            presenter.OnPrimaryActionRequested();
            presenter.OnPrimaryActionRequested();
            Assert.That(view.LastState.PrimaryActionEnabled, Is.False);

            gate.SetResult("done");
            Assert.That(
                SpinWait.SpinUntil(
                    () => view.LastState.StatusTone == SampleDiagnosticStatusTone.Success,
                    1000),
                Is.True);
            Assert.That(invocations, Is.EqualTo(1));
        }

        [Test]
        public void SampleRequestPresenter_WhenExecuteThrows_RendersErrorState()
        {
            var view = new FakeView();
            var presenter = new SampleRequestPresenter(
                view,
                "Title",
                "Subtitle",
                "prompt",
                "Generate",
                (prompt, token) =>
                    Task.FromException<string>(new InvalidOperationException("provider failed")));

            presenter.Initialize();
            presenter.OnPrimaryActionRequested();

            Assert.That(
                SpinWait.SpinUntil(
                    () => view.LastState.StatusTone == SampleDiagnosticStatusTone.Error,
                    1000),
                Is.True);
            StringAssert.Contains("provider failed", view.LastState.Output);
            // The action re-enables once the failed run settles.
            Assert.That(view.LastState.PrimaryActionEnabled, Is.True);
        }

        [Test]
        public void StreamingPresenter_StreamsOrderedTokens_AndCompletesOnce()
        {
            var view = new FakeView();
            var completionCount = 0;
            var presenter = new StreamingSamplePresenter(
                new FakeClient
                {
                    StreamAsync = (prompt, onToken, onComplete, onError, options, token) =>
                    {
                        onToken("one ");
                        onToken("two ");
                        onToken("three");
                        Interlocked.Increment(ref completionCount);
                        onComplete(AppleFoundationModelsResult.Success("one two three"));
                        return Task.CompletedTask;
                    }
                },
                view,
                "hello");

            presenter.Initialize();
            presenter.OnPrimaryActionRequested();

            Assert.That(
                SpinWait.SpinUntil(
                    () => view.LastState.StatusTone == SampleDiagnosticStatusTone.Success,
                    1000),
                Is.True);
            Assert.That(view.LastState.Output, Is.EqualTo("one two three"));
            Assert.That(completionCount, Is.EqualTo(1));

            // Streamed outputs must have grown monotonically in token order.
            var streamedOutputs = view.RenderedOutputs
                .Where(output => output.StartsWith("one", StringComparison.Ordinal))
                .ToList();
            CollectionAssert.Contains(streamedOutputs, "one ");
            CollectionAssert.Contains(streamedOutputs, "one two ");
        }

        [Test]
        public void StreamingPresenter_WhenBusy_IgnoresReentrantPrimaryAction()
        {
            var view = new FakeView();
            var streamStarts = 0;
            var release = new TaskCompletionSource<bool>();
            var presenter = new StreamingSamplePresenter(
                new FakeClient
                {
                    StreamAsync = async (prompt, onToken, onComplete, onError, options, token) =>
                    {
                        Interlocked.Increment(ref streamStarts);
                        onToken("partial");
                        await release.Task;
                        onComplete(AppleFoundationModelsResult.Success("partial"));
                    }
                },
                view,
                "hello");

            presenter.Initialize();
            presenter.OnPrimaryActionRequested();
            Assert.That(
                SpinWait.SpinUntil(
                    () => view.LastState.StatusTone == SampleDiagnosticStatusTone.InProgress,
                    1000),
                Is.True);

            presenter.OnPrimaryActionRequested();
            presenter.OnPrimaryActionRequested();

            release.SetResult(true);
            Assert.That(
                SpinWait.SpinUntil(
                    () => view.LastState.StatusTone == SampleDiagnosticStatusTone.Success,
                    1000),
                Is.True);
            Assert.That(streamStarts, Is.EqualTo(1));
        }

        private sealed class FakeView : IDiagnosticShellView
        {
            private readonly object _gate = new object();
            private readonly List<SampleDiagnosticState> _rendered = new List<SampleDiagnosticState>();

            public event Action PrimaryActionRequested
            {
                add { }
                remove { }
            }

            public event Action SecondaryActionRequested
            {
                add { }
                remove { }
            }

            public event Action CopyActionRequested
            {
                add { }
                remove { }
            }

            public event Action<string> PromptChanged
            {
                add { }
                remove { }
            }

            public string Prompt => LastState == null ? string.Empty : LastState.Prompt;

            public SampleDiagnosticState LastState
            {
                get
                {
                    lock (_gate)
                    {
                        return _rendered.Count == 0 ? null : _rendered[_rendered.Count - 1];
                    }
                }
            }

            public IReadOnlyList<string> RenderedOutputs
            {
                get
                {
                    lock (_gate)
                    {
                        return _rendered.Select(state => state.Output).ToList();
                    }
                }
            }

            public void Render(SampleDiagnosticState state)
            {
                lock (_gate)
                {
                    _rendered.Add(state);
                }
            }
        }

        private sealed class FakeClient : IAppleFoundationModelsClient
        {
            public Func<Task<AppleFoundationModelsAvailability>> AvailabilityAsync { get; set; }

            public Func<string, AppleFoundationModelsOptions, CancellationToken, Task<AppleFoundationModelsResult>> GenerateAsync { get; set; }

            public Func<string, AppleFoundationModelsOptions, CancellationToken, Task<object>> GenerateJsonAsyncDelegate { get; set; }

            public Func<string, Action<string>, Action<AppleFoundationModelsResult>, Action<Exception>, AppleFoundationModelsOptions, CancellationToken, Task> StreamAsync { get; set; }

            public Task<AppleFoundationModelsAvailability> GetAvailabilityAsync()
            {
                return AvailabilityAsync();
            }

            public Task<AppleFoundationModelsResult> GenerateTextAsync(
                string prompt,
                AppleFoundationModelsOptions options = null,
                CancellationToken cancellationToken = default)
            {
                if (GenerateAsync != null)
                {
                    return GenerateAsync(prompt, options, cancellationToken);
                }

                return Task.FromResult(AppleFoundationModelsResult.Success(prompt));
            }

            public async Task<T> GenerateJsonAsync<T>(
                string prompt,
                AppleFoundationModelsOptions options = null,
                CancellationToken cancellationToken = default)
            {
                if (GenerateJsonAsyncDelegate != null)
                {
                    var value = await GenerateJsonAsyncDelegate(prompt, options, cancellationToken);
                    return (T)value;
                }

                return default(T);
            }

            public Task StreamTextAsync(
                string prompt,
                Action<string> onToken,
                Action<AppleFoundationModelsResult> onComplete,
                Action<Exception> onError = null,
                AppleFoundationModelsOptions options = null,
                CancellationToken cancellationToken = default)
            {
                return StreamAsync(
                    prompt,
                    onToken,
                    onComplete,
                    onError,
                    options,
                    cancellationToken);
            }
        }
    }
}
