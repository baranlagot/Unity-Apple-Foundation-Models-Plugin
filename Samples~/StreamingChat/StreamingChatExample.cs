namespace Baran.AppleFoundationModels.Samples
{
    public sealed class StreamingChatExample : DiagnosticSampleBehaviourBase
    {
        [UnityEngine.SerializeField]
        private string prompt =
            "Generate three cute pet names for an orange cat.";

        protected override ISampleDiagnosticPresenter CreatePresenter(
            IAppleFoundationModelsClient client,
            IDiagnosticShellView view)
        {
            return new StreamingSamplePresenter(client, view, prompt);
        }
    }
}
