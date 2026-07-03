using System;

namespace Baran.AppleFoundationModels.Samples
{
    public sealed class AvailabilityPresenter : SampleDiagnosticPresenterBase
    {
        private readonly IAppleFoundationModelsClient _client;
        private bool _isBusy;

        public AvailabilityPresenter(
            IAppleFoundationModelsClient client,
            IDiagnosticShellView view)
            : base(view, new SampleDiagnosticState
            {
                Title = "Apple Foundation Models - Availability",
                Subtitle = "Checks the current device or mock configuration without issuing a generation request.",
                ShowPrompt = false,
                Status = "Select Check Availability to begin.",
                Output = "Availability details will appear here.",
                PrimaryActionLabel = "Check Availability"
            })
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public override void OnPrimaryActionRequested()
        {
            if (_isBusy)
            {
                return;
            }

            _ = RunAsync();
        }

        protected override bool CanRunPrimaryAction()
        {
            return !_isBusy;
        }

        private async System.Threading.Tasks.Task RunAsync()
        {
            _isBusy = true;
            SetBusyState(true);
            SetStatus("Checking availability...", SampleDiagnosticStatusTone.InProgress);
            SetOutput("Waiting for the current provider to respond.");
            Render();

            try
            {
                var availability = await _client.GetAvailabilityAsync();
                SetStatus(
                    availability.IsAvailable
                        ? "Availability check completed."
                        : "Availability check completed with an unavailable result.",
                    availability.IsAvailable
                        ? SampleDiagnosticStatusTone.Success
                        : SampleDiagnosticStatusTone.Warning);
                SetOutput(
                    "Status: " + availability.Status + Environment.NewLine +
                    "Message: " + availability.Message);
            }
            catch (Exception exception)
            {
                SetStatus("Availability check failed.", SampleDiagnosticStatusTone.Error);
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
