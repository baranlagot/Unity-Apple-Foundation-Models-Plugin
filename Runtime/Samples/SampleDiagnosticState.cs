namespace Baran.AppleFoundationModels.Samples
{
    public enum SampleDiagnosticStatusTone
    {
        Neutral,
        InProgress,
        Success,
        Warning,
        Error
    }

    public sealed class SampleDiagnosticState
    {
        public string Title = string.Empty;
        public string Subtitle = string.Empty;
        public string PromptLabel = "Prompt";
        public string Prompt = string.Empty;
        public string PrimaryActionLabel = "Run";
        public string SecondaryActionLabel = "Cancel";
        public string CopyActionLabel = "Copy Report";
        public string Status = string.Empty;
        public string Output = string.Empty;
        public bool ShowPrompt = true;
        public bool ShowSecondaryAction;
        public bool ShowCopyAction;
        public bool PrimaryActionEnabled = true;
        public bool SecondaryActionEnabled;
        public bool CopyActionEnabled;
        public bool PromptEnabled = true;
        public SampleDiagnosticStatusTone StatusTone = SampleDiagnosticStatusTone.Neutral;

        public SampleDiagnosticState Clone()
        {
            return new SampleDiagnosticState
            {
                Title = Title,
                Subtitle = Subtitle,
                PromptLabel = PromptLabel,
                Prompt = Prompt,
                PrimaryActionLabel = PrimaryActionLabel,
                SecondaryActionLabel = SecondaryActionLabel,
                CopyActionLabel = CopyActionLabel,
                Status = Status,
                Output = Output,
                ShowPrompt = ShowPrompt,
                ShowSecondaryAction = ShowSecondaryAction,
                ShowCopyAction = ShowCopyAction,
                PrimaryActionEnabled = PrimaryActionEnabled,
                SecondaryActionEnabled = SecondaryActionEnabled,
                CopyActionEnabled = CopyActionEnabled,
                PromptEnabled = PromptEnabled,
                StatusTone = StatusTone
            };
        }
    }
}
