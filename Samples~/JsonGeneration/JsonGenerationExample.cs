namespace Baran.AppleFoundationModels.Samples
{
    public sealed class JsonGenerationExample : DiagnosticSampleBehaviourBase
    {
        [UnityEngine.SerializeField]
        private string prompt =
            "Generate a short cozy fetch quest for a pet game.";

        protected override ISampleDiagnosticPresenter CreatePresenter(
            IAppleFoundationModelsClient client,
            IDiagnosticShellView view)
        {
            return new SampleRequestPresenter(
                view,
                "Apple Foundation Models - JSON Quest Generation",
                "Generates structured JSON through the client helper and formats it in the reusable diagnostic shell.",
                prompt,
                "Generate Quest",
                async (currentPrompt, cancellationToken) =>
                {
                    var quest = await client.GenerateJsonAsync<QuestData>(
                        currentPrompt,
                        cancellationToken: cancellationToken);
                    return
                        "Title: " + quest.title + "\n" +
                        "NPC: " + quest.npcName + "\n" +
                        "Objective: " + quest.objective + "\n" +
                        "Reward: " + quest.rewardCoins + " coins";
                });
        }
    }

    [Serializable]
    public sealed class QuestData
    {
        public string title;
        public string objective;
        public int rewardCoins;
        public string npcName;
    }
}
