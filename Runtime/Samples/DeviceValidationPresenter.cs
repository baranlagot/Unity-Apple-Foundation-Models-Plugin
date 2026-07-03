using System;
using System.Threading;

namespace Baran.AppleFoundationModels.Samples
{
    public sealed class DeviceValidationPresenter : SampleDiagnosticPresenterBase
    {
        private static bool _hasInitialized;

        private readonly DeviceValidationRunner _runner;
        private readonly ISampleClipboard _clipboard;
        private CancellationTokenSource _cancellation;
        private DeviceValidationReport _latestReport;
        private bool _isBusy;

        public DeviceValidationPresenter(
            IDiagnosticShellView view,
            DeviceValidationRunner runner,
            ISampleClipboard clipboard)
            : base(view, new SampleDiagnosticState
            {
                Title = "Apple Foundation Models - Device Validation",
                Subtitle = "Runs repeatable release-hardening checks and produces a privacy-safe report for local mock or device testing.",
                ShowPrompt = false,
                Status = "Run validation to collect a local or device report.",
                Output = "Validation results will appear here.",
                PrimaryActionLabel = "Run Validation",
                SecondaryActionLabel = "Cancel",
                ShowSecondaryAction = true,
                ShowCopyAction = true,
                CopyActionEnabled = false
            })
        {
            _runner = runner ?? throw new ArgumentNullException(nameof(runner));
            _clipboard = clipboard ?? throw new ArgumentNullException(nameof(clipboard));

            if (_hasInitialized)
            {
                _runner.RecordSceneReload();
            }

            _hasInitialized = true;
        }

        public override void Dispose()
        {
            CancelCurrentRun();
        }

        public override void OnPrimaryActionRequested()
        {
            if (_isBusy)
            {
                return;
            }

            _ = RunAsync();
        }

        public override void OnSecondaryActionRequested()
        {
            CancelCurrentRun();
        }

        public override void OnCopyActionRequested()
        {
            if (_latestReport == null)
            {
                return;
            }

            _clipboard.Copy(_latestReport.ToClipboardText());
            SetStatus("Validation report copied.", SampleDiagnosticStatusTone.Success);
            SetCopyAction(true, true, "Copy Report");
            Render();
        }

        public override void OnApplicationPause(bool paused)
        {
            _runner.RecordApplicationPause(paused);
        }

        public override void OnApplicationFocus(bool hasFocus)
        {
            _runner.RecordApplicationFocus(hasFocus);
        }

        protected override bool CanRunPrimaryAction()
        {
            return !_isBusy;
        }

        private async System.Threading.Tasks.Task RunAsync()
        {
            _latestReport = null;
            CancelCurrentRun();
            _cancellation = new CancellationTokenSource();
            _isBusy = true;
            SetBusyState(true);
            SetCopyAction(true, false, "Copy Report");
            SetStatus("Running validation scenarios...", SampleDiagnosticStatusTone.InProgress);
            SetOutput("Collecting availability, generation, and lifecycle evidence.");
            Render();

            try
            {
                _latestReport = await _runner.RunAsync(_cancellation.Token);
                SetOutput(_latestReport.ToDisplayText());
                SetStatus("Validation run completed.", SampleDiagnosticStatusTone.Success);
                SetCopyAction(true, true, "Copy Report");
            }
            catch (OperationCanceledException)
            {
                SetStatus("Validation run cancelled.", SampleDiagnosticStatusTone.Warning);
                SetOutput("The validation run was cancelled before completion.");
            }
            catch (Exception exception)
            {
                SetStatus("Validation run failed.", SampleDiagnosticStatusTone.Error);
                SetOutput(exception.Message);
            }
            finally
            {
                _isBusy = false;
                CancelCurrentRun(disposeOnly: true);
                SetBusyState(false);
                Render();
            }
        }

        private void CancelCurrentRun(bool disposeOnly = false)
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
