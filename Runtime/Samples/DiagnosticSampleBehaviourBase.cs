using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Baran.AppleFoundationModels.Samples
{
    public abstract class DiagnosticSampleBehaviourBase : MonoBehaviour
    {
        private UIDocument _document;
        private SampleDiagnosticView _view;
        private ISampleDiagnosticPresenter _presenter;
        private bool _ownsPanelSettings;

        protected virtual IAppleFoundationModelsClient ResolveClient()
        {
            return AppleFoundationModels.DefaultClient;
        }

        protected abstract ISampleDiagnosticPresenter CreatePresenter(
            IAppleFoundationModelsClient client,
            IDiagnosticShellView view);

        protected virtual void Awake()
        {
            _document = GetComponent<UIDocument>();
            if (_document == null)
            {
                _document = gameObject.AddComponent<UIDocument>();
            }

            if (_document.panelSettings == null)
            {
                _document.panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
                _document.panelSettings.scaleMode = PanelScaleMode.ConstantPixelSize;
                _ownsPanelSettings = true;
            }

            var root = _document.rootVisualElement;
            root.style.flexGrow = 1f;
            root.Clear();

            _view = new SampleDiagnosticView(root);
            _presenter = CreatePresenter(ResolveClient(), _view);
            _view.PrimaryActionRequested += HandlePrimaryActionRequested;
            _view.SecondaryActionRequested += HandleSecondaryActionRequested;
            _view.CopyActionRequested += HandleCopyActionRequested;
            _view.PromptChanged += HandlePromptChanged;
            _presenter.Initialize();
        }

        protected virtual void OnDestroy()
        {
            if (_view != null)
            {
                _view.PrimaryActionRequested -= HandlePrimaryActionRequested;
                _view.SecondaryActionRequested -= HandleSecondaryActionRequested;
                _view.CopyActionRequested -= HandleCopyActionRequested;
                _view.PromptChanged -= HandlePromptChanged;
            }

            _presenter?.Dispose();
            _presenter = null;

            if (_ownsPanelSettings && _document != null && _document.panelSettings != null)
            {
                Destroy(_document.panelSettings);
                _document.panelSettings = null;
            }
        }

        protected virtual void OnApplicationPause(bool pauseStatus)
        {
            _presenter?.OnApplicationPause(pauseStatus);
        }

        protected virtual void OnApplicationFocus(bool hasFocus)
        {
            _presenter?.OnApplicationFocus(hasFocus);
        }

        private void HandlePrimaryActionRequested()
        {
            _presenter?.OnPrimaryActionRequested();
        }

        private void HandleSecondaryActionRequested()
        {
            _presenter?.OnSecondaryActionRequested();
        }

        private void HandleCopyActionRequested()
        {
            _presenter?.OnCopyActionRequested();
        }

        private void HandlePromptChanged(string prompt)
        {
            _presenter?.OnPromptChanged(prompt);
        }
    }
}
