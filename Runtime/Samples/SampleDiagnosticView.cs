using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Baran.AppleFoundationModels.Samples
{
    public sealed class SampleDiagnosticView : IDiagnosticShellView
    {
        private readonly VisualElement _root;
        private readonly Label _titleLabel;
        private readonly Label _subtitleLabel;
        private readonly VisualElement _statusContainer;
        private readonly Label _statusLabel;
        private readonly VisualElement _promptContainer;
        private readonly Label _promptLabel;
        private readonly TextField _promptField;
        private readonly Button _primaryButton;
        private readonly Button _secondaryButton;
        private readonly Button _copyButton;
        private readonly TextField _outputField;
        private bool _suppressPromptChanged;

        public SampleDiagnosticView(VisualElement parent)
        {
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            _root = new VisualElement();
            _root.style.flexGrow = 1f;
            _root.style.paddingLeft = 18f;
            _root.style.paddingRight = 18f;
            _root.style.paddingTop = 18f;
            _root.style.paddingBottom = 18f;
            _root.style.backgroundColor = new Color(0.11f, 0.13f, 0.16f);
            _root.style.unityFontStyleAndWeight = FontStyle.Normal;

            _titleLabel = new Label();
            _titleLabel.style.fontSize = 20f;
            _titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _titleLabel.style.color = new Color(0.96f, 0.97f, 0.99f);
            _titleLabel.style.marginBottom = 4f;
            _root.Add(_titleLabel);

            _subtitleLabel = new Label();
            _subtitleLabel.style.fontSize = 12f;
            _subtitleLabel.style.color = new Color(0.76f, 0.8f, 0.86f);
            _subtitleLabel.style.whiteSpace = WhiteSpace.Normal;
            _subtitleLabel.style.marginBottom = 12f;
            _root.Add(_subtitleLabel);

            _statusContainer = new VisualElement();
            _statusContainer.style.marginBottom = 12f;
            _statusContainer.style.paddingLeft = 12f;
            _statusContainer.style.paddingRight = 12f;
            _statusContainer.style.paddingTop = 10f;
            _statusContainer.style.paddingBottom = 10f;
            _statusContainer.style.borderBottomLeftRadius = 8f;
            _statusContainer.style.borderBottomRightRadius = 8f;
            _statusContainer.style.borderTopLeftRadius = 8f;
            _statusContainer.style.borderTopRightRadius = 8f;
            _statusLabel = new Label();
            _statusLabel.style.whiteSpace = WhiteSpace.Normal;
            _statusLabel.style.color = new Color(0.94f, 0.95f, 0.97f);
            _statusContainer.Add(_statusLabel);
            _root.Add(_statusContainer);

            _promptContainer = new VisualElement();
            _promptContainer.style.marginBottom = 12f;
            _promptLabel = new Label();
            _promptLabel.style.fontSize = 12f;
            _promptLabel.style.color = new Color(0.82f, 0.85f, 0.9f);
            _promptLabel.style.marginBottom = 6f;
            _promptContainer.Add(_promptLabel);
            _promptField = new TextField();
            _promptField.multiline = true;
            _promptField.style.minHeight = 110f;
            _promptField.style.whiteSpace = WhiteSpace.Normal;
            _promptField.style.backgroundColor = new Color(0.16f, 0.18f, 0.22f);
            _promptField.style.color = new Color(0.96f, 0.97f, 0.99f);
            _promptField.style.borderBottomLeftRadius = 8f;
            _promptField.style.borderBottomRightRadius = 8f;
            _promptField.style.borderTopLeftRadius = 8f;
            _promptField.style.borderTopRightRadius = 8f;
            _promptField.style.paddingBottom = 6f;
            _promptField.style.paddingTop = 6f;
            _promptField.RegisterValueChangedCallback(OnPromptValueChanged);
            _promptContainer.Add(_promptField);
            _root.Add(_promptContainer);

            var actions = new VisualElement();
            actions.style.flexDirection = FlexDirection.Row;
            actions.style.flexWrap = Wrap.Wrap;
            actions.style.marginBottom = 12f;

            _primaryButton = CreateActionButton();
            _primaryButton.clicked += () => PrimaryActionRequested?.Invoke();
            actions.Add(_primaryButton);

            _secondaryButton = CreateActionButton();
            _secondaryButton.clicked += () => SecondaryActionRequested?.Invoke();
            actions.Add(_secondaryButton);

            _copyButton = CreateActionButton();
            _copyButton.clicked += () => CopyActionRequested?.Invoke();
            actions.Add(_copyButton);
            _root.Add(actions);

            var outputLabel = new Label("Output");
            outputLabel.style.fontSize = 12f;
            outputLabel.style.color = new Color(0.82f, 0.85f, 0.9f);
            outputLabel.style.marginBottom = 6f;
            _root.Add(outputLabel);

            _outputField = new TextField();
            _outputField.multiline = true;
            _outputField.isReadOnly = true;
            _outputField.style.flexGrow = 1f;
            _outputField.style.minHeight = 180f;
            _outputField.style.whiteSpace = WhiteSpace.Normal;
            _outputField.style.backgroundColor = new Color(0.14f, 0.16f, 0.19f);
            _outputField.style.color = new Color(0.95f, 0.96f, 0.98f);
            _outputField.style.borderBottomLeftRadius = 8f;
            _outputField.style.borderBottomRightRadius = 8f;
            _outputField.style.borderTopLeftRadius = 8f;
            _outputField.style.borderTopRightRadius = 8f;
            _outputField.style.paddingBottom = 6f;
            _outputField.style.paddingTop = 6f;
            _root.Add(_outputField);

            parent.style.flexGrow = 1f;
            parent.Add(_root);
        }

        public event Action PrimaryActionRequested;

        public event Action SecondaryActionRequested;

        public event Action CopyActionRequested;

        public event Action<string> PromptChanged;

        public string Prompt => _promptField.value ?? string.Empty;

        public void Render(SampleDiagnosticState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            _titleLabel.text = state.Title;
            _subtitleLabel.text = state.Subtitle;
            _statusLabel.text = state.Status;
            _promptLabel.text = state.PromptLabel;

            _suppressPromptChanged = true;
            _promptField.value = state.Prompt ?? string.Empty;
            _suppressPromptChanged = false;

            _promptField.SetEnabled(state.PromptEnabled);
            _outputField.value = state.Output ?? string.Empty;

            _primaryButton.text = state.PrimaryActionLabel;
            _primaryButton.SetEnabled(state.PrimaryActionEnabled);
            _secondaryButton.text = state.SecondaryActionLabel;
            _secondaryButton.SetEnabled(state.SecondaryActionEnabled);
            _copyButton.text = state.CopyActionLabel;
            _copyButton.SetEnabled(state.CopyActionEnabled);

            _promptContainer.style.display = state.ShowPrompt
                ? DisplayStyle.Flex
                : DisplayStyle.None;
            _secondaryButton.style.display = state.ShowSecondaryAction
                ? DisplayStyle.Flex
                : DisplayStyle.None;
            _copyButton.style.display = state.ShowCopyAction
                ? DisplayStyle.Flex
                : DisplayStyle.None;

            ApplyStatusTone(state.StatusTone);
        }

        private static Button CreateActionButton()
        {
            var button = new Button();
            button.style.height = 34f;
            button.style.marginRight = 8f;
            button.style.marginBottom = 8f;
            button.style.paddingLeft = 12f;
            button.style.paddingRight = 12f;
            button.style.backgroundColor = new Color(0.23f, 0.44f, 0.76f);
            button.style.color = new Color(0.98f, 0.99f, 1f);
            return button;
        }

        private void ApplyStatusTone(SampleDiagnosticStatusTone tone)
        {
            switch (tone)
            {
                case SampleDiagnosticStatusTone.InProgress:
                    _statusContainer.style.backgroundColor = new Color(0.18f, 0.3f, 0.47f);
                    break;
                case SampleDiagnosticStatusTone.Success:
                    _statusContainer.style.backgroundColor = new Color(0.16f, 0.38f, 0.27f);
                    break;
                case SampleDiagnosticStatusTone.Warning:
                    _statusContainer.style.backgroundColor = new Color(0.46f, 0.33f, 0.12f);
                    break;
                case SampleDiagnosticStatusTone.Error:
                    _statusContainer.style.backgroundColor = new Color(0.47f, 0.17f, 0.19f);
                    break;
                default:
                    _statusContainer.style.backgroundColor = new Color(0.2f, 0.23f, 0.27f);
                    break;
            }
        }

        private void OnPromptValueChanged(ChangeEvent<string> changeEvent)
        {
            if (_suppressPromptChanged)
            {
                return;
            }

            PromptChanged?.Invoke(changeEvent.newValue ?? string.Empty);
        }
    }
}
