using System;
using UnityEngine;

namespace Baran.AppleFoundationModels.Samples
{
    public sealed class JsonGenerationExample : MonoBehaviour
    {
        private IAppleFoundationModelsClient _client;
        private string _result = "Generated quest fields appear here.";
        private bool _isBusy;

        private void Awake()
        {
            _client = AppleFoundationModels.DefaultClient;
        }

        public async void GenerateQuest()
        {
            if (_isBusy)
            {
                return;
            }

            _isBusy = true;
            _result = "Generating quest…";
            try
            {
                var quest = await _client.GenerateJsonAsync<QuestData>(
                    "Generate a short cozy fetch quest for a pet game.");
                _result =
                    $"Title: {quest.title}\n" +
                    $"NPC: {quest.npcName}\n" +
                    $"Objective: {quest.objective}\n" +
                    $"Reward: {quest.rewardCoins} coins";
            }
            catch (Exception exception)
            {
                _result = exception.Message;
            }
            finally
            {
                _isBusy = false;
            }
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(24, 24, 620, 340), GUI.skin.box);
            GUILayout.Label("Apple Foundation Models — JSON Quest Generation");
            GUI.enabled = !_isBusy;
            if (GUILayout.Button("Generate Cozy Quest", GUILayout.Height(36)))
            {
                GenerateQuest();
            }
            GUI.enabled = true;
            GUILayout.Space(12);
            GUILayout.TextArea(_result, GUILayout.ExpandHeight(true));
            GUILayout.EndArea();
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
