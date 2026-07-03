using System;
using System.Threading;

namespace Baran.AppleFoundationModels.Samples
{
    public interface ISampleDiagnosticPresenter : IDisposable
    {
        void Initialize();

        void OnPrimaryActionRequested();

        void OnSecondaryActionRequested();

        void OnCopyActionRequested();

        void OnPromptChanged(string prompt);

        void OnApplicationPause(bool paused);

        void OnApplicationFocus(bool hasFocus);
    }

    public abstract class SampleDiagnosticPresenterBase : ISampleDiagnosticPresenter
    {
        private readonly IDiagnosticShellView _view;
        private readonly SampleDiagnosticState _state;

        protected SampleDiagnosticPresenterBase(
            IDiagnosticShellView view,
            SampleDiagnosticState initialState)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _state = initialState == null
                ? throw new ArgumentNullException(nameof(initialState))
                : initialState.Clone();
        }

        protected SampleDiagnosticState State => _state;

        protected IDiagnosticShellView View => _view;

        public virtual void Initialize()
        {
            Render();
        }

        public abstract void OnPrimaryActionRequested();

        public virtual void OnSecondaryActionRequested()
        {
        }

        public virtual void OnCopyActionRequested()
        {
        }

        public virtual void OnPromptChanged(string prompt)
        {
            _state.Prompt = prompt ?? string.Empty;
            UpdatePrimaryActionAvailability();
            Render();
        }

        public virtual void OnApplicationPause(bool paused)
        {
        }

        public virtual void OnApplicationFocus(bool hasFocus)
        {
        }

        public virtual void Dispose()
        {
        }

        protected void SetBusyState(bool isBusy)
        {
            _state.PrimaryActionEnabled = !isBusy && CanRunPrimaryAction();
            _state.PromptEnabled = !isBusy;
            if (_state.ShowSecondaryAction)
            {
                _state.SecondaryActionEnabled = isBusy;
            }
        }

        protected void SetStatus(
            string status,
            SampleDiagnosticStatusTone tone)
        {
            _state.Status = status ?? string.Empty;
            _state.StatusTone = tone;
        }

        protected void SetOutput(string output)
        {
            _state.Output = output ?? string.Empty;
        }

        protected void SetCopyAction(bool visible, bool enabled, string label)
        {
            _state.ShowCopyAction = visible;
            _state.CopyActionEnabled = enabled;
            if (!string.IsNullOrWhiteSpace(label))
            {
                _state.CopyActionLabel = label;
            }
        }

        protected void SetSecondaryAction(bool visible, string label)
        {
            _state.ShowSecondaryAction = visible;
            if (!string.IsNullOrWhiteSpace(label))
            {
                _state.SecondaryActionLabel = label;
            }
        }

        protected void Render()
        {
            _view.Render(_state.Clone());
        }

        protected void FireAndForget(Func<CancellationToken, System.Threading.Tasks.Task> operation)
        {
            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            _ = operation(CancellationToken.None);
        }

        protected void UpdatePrimaryActionAvailability()
        {
            _state.PrimaryActionEnabled = CanRunPrimaryAction();
        }

        protected virtual bool CanRunPrimaryAction()
        {
            return true;
        }
    }
}
