using System;
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

        private sealed class FakeView : IDiagnosticShellView
        {
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

            public SampleDiagnosticState LastState { get; private set; }

            public void Render(SampleDiagnosticState state)
            {
                LastState = state;
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
