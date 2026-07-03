using System;

namespace Baran.AppleFoundationModels.Samples
{
    public interface IDiagnosticShellView
    {
        event Action PrimaryActionRequested;

        event Action SecondaryActionRequested;

        event Action CopyActionRequested;

        event Action<string> PromptChanged;

        string Prompt { get; }

        void Render(SampleDiagnosticState state);
    }
}
