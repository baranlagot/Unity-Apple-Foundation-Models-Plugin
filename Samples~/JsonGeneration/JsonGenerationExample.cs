using System;
using UnityEngine;

namespace Baran.AppleFoundationModels.Samples
{
    public sealed class JsonGenerationExample : MonoBehaviour
    {
        public async void GenerateQuest()
        {
            try
            {
                var quest = await AppleFoundationModels.GenerateJsonAsync<QuestData>(
                    "Generate a short cozy fetch quest for a pet game.");
                Debug.Log($"{quest.title}: {quest.objective} ({quest.rewardCoins} coins)");
            }
            catch (Exception exception)
            {
                Debug.LogError(exception.Message);
            }
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
