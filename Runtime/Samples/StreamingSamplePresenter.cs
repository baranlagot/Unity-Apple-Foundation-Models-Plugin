using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Baran.AppleFoundationModels.Samples
{
    public sealed class StreamingSamplePresenter : SampleDiagnosticPresenterBase
    {
        private readonly IAppleFoundationModelsClient _client;
        private CancellationTokenSource _cancellation;
        private bool _isBusy;

        public StreamingSamplePresenter(
            IAppleFoundationModelsClient client,
            IDiagnosticShellView view,
            string initialPrompt)
            : base(view, new SampleDiagnosticState
            {
                Title = "Apple Foundation Models - Streaming Chat",
                Subtitle = "Streams ordered text deltas and keeps cancellation on the same reusable shell.",
                Prompt = initialPrompt ?? string.Empty,
                Status = "Ready.",
                Output = "Streaming output will appear here.",
                PrimaryActionLabel = "Stream",
                SecondaryActionLabel = "Cancel",
                ShowSecondaryAction = true
            })
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public override void Dispose()
        {
            CancelActiveRequest();
        }

        public override void OnPrimaryActionRequested()
        {
            if (_isBusy || !CanRunPrimaryAction())
            {
                return;
            }

            _ = RunAsync();
        }

        public override void OnSecondaryActionRequested()
        {
            CancelActiveRequest();
        }

        protected override bool CanRunPrimaryAction()
        {
            return !_isBusy && !string.IsNullOrWhiteSpace(State.Prompt);
        }

        private async Task RunAsync()
        {
            CancelActiveRequest();
            _cancellation = new CancellationTokenSource();
            _isBusy = true;
            SetBusyState(true);
            SetStatus("Streaming response...", SampleDiagnosticStatusTone.InProgress);
            SetOutput(string.Empty);
            Render();

            var response = new StringBuilder();
            Exception streamedError = null;

            try
            {
                await _client.StreamTextAsync(
                    State.Prompt,
                    chunk =>
                    {
                        response.Append(chunk);
                        SetOutput(response.ToString());
                        Render();
                    },
                    result =>
                    {
                        SetOutput(result.Text);
                        SetStatus("Streaming completed.", SampleDiagnosticStatusTone.Success);
                        Render();
                    },
                    exception => streamedError = exception,
                    cancellationToken: _cancellation.Token);

                if (streamedError != null)
                {
                    SetStatus("Streaming failed.", SampleDiagnosticStatusTone.Error);
                    SetOutput(streamedError.Message);
                }
            }
            catch (OperationCanceledException)
            {
                SetStatus("Streaming cancelled.", SampleDiagnosticStatusTone.Warning);
                if (string.IsNullOrWhiteSpace(State.Output))
                {
                    SetOutput("The request was cancelled before completion.");
                }
            }
            finally
            {
                _isBusy = false;
                CancelActiveRequest(disposeOnly: true);
                SetBusyState(false);
                Render();
            }
        }

        private void CancelActiveRequest(bool disposeOnly = false)
        {
            var cancellation = _cancellation;
            if (cancellation == null)
            {
                return;
            }

            // Clear the field before cancelling so a re-entrant call triggered by the
            // synchronous cancellation continuation observes null and cannot double-dispose.
            _cancellation = null;

            if (!disposeOnly)
            {
                cancellation.Cancel();
            }

            cancellation.Dispose();
        }
    }
}
