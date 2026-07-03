using System;
using System.Threading;
using System.Threading.Tasks;

namespace Baran.AppleFoundationModels.Samples
{
    public sealed class SampleRequestPresenter : SampleDiagnosticPresenterBase
    {
        private readonly Func<string, CancellationToken, Task<string>> _executeAsync;
        private bool _isBusy;

        public SampleRequestPresenter(
            IDiagnosticShellView view,
            string title,
            string subtitle,
            string initialPrompt,
            string primaryActionLabel,
            Func<string, CancellationToken, Task<string>> executeAsync)
            : base(view, new SampleDiagnosticState
            {
                Title = title ?? string.Empty,
                Subtitle = subtitle ?? string.Empty,
                Prompt = initialPrompt ?? string.Empty,
                Status = "Ready.",
                Output = "Generated output will appear here.",
                PrimaryActionLabel = primaryActionLabel ?? "Run",
                PromptLabel = "Prompt"
            })
        {
            _executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
        }

        public override void OnPrimaryActionRequested()
        {
            if (_isBusy || !CanRunPrimaryAction())
            {
                return;
            }

            _ = RunAsync();
        }

        protected override bool CanRunPrimaryAction()
        {
            return !_isBusy && !string.IsNullOrWhiteSpace(State.Prompt);
        }

        private async Task RunAsync()
        {
            _isBusy = true;
            SetBusyState(true);
            SetStatus("Running request...", SampleDiagnosticStatusTone.InProgress);
            SetOutput("Waiting for the provider to finish.");
            Render();

            try
            {
                var output = await _executeAsync(State.Prompt, CancellationToken.None);
                SetStatus("Request completed.", SampleDiagnosticStatusTone.Success);
                SetOutput(output);
            }
            catch (Exception exception)
            {
                SetStatus("Request failed.", SampleDiagnosticStatusTone.Error);
                SetOutput(exception.Message);
            }
            finally
            {
                _isBusy = false;
                SetBusyState(false);
                Render();
            }
        }
    }
}
