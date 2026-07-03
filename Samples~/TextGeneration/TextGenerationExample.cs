namespace Baran.AppleFoundationModels.Samples
{
    public sealed class TextGenerationExample : DiagnosticSampleBehaviourBase
    {
        [UnityEngine.SerializeField]
        private string prompt =
            "Generate a short funny NPC line for a cozy cat cafe game.";

        protected override ISampleDiagnosticPresenter CreatePresenter(
            IAppleFoundationModelsClient client,
            IDiagnosticShellView view)
        {
            return new SampleRequestPresenter(
                view,
                "Apple Foundation Models - Text Generation",
                "Runs a one-shot text request on the active client and keeps the prompt editable in the reusable diagnostic shell.",
                prompt,
                "Generate",
                async (currentPrompt, cancellationToken) =>
                {
                    var result = await client.GenerateTextAsync(
                        currentPrompt,
                        cancellationToken: cancellationToken);
                    return result.Text;
                });
        }
    }
}
