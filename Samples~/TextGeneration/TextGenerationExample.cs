using System;
using UnityEngine;

namespace Baran.AppleFoundationModels.Samples
{
    public sealed class TextGenerationExample : MonoBehaviour
    {
        [SerializeField]
        private string prompt = "Generate a short funny NPC line for a cozy cat cafe game.";

        private IAppleFoundationModelsClient _client;
        private string _result = "Generated text appears here.";
        private bool _isBusy;

        private void Awake()
        {
            _client = AppleFoundationModels.DefaultClient;
        }

        public async void Generate()
        {
            if (_isBusy)
            {
                return;
            }

            _isBusy = true;
            _result = "Generating…";
            try
            {
                var result = await _client.GenerateTextAsync(prompt);
                _result = result.Text;
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
            GUILayout.BeginArea(new Rect(24, 24, 620, 360), GUI.skin.box);
            GUILayout.Label("Apple Foundation Models — Text Generation");
            GUILayout.Label("Prompt");
            prompt = GUILayout.TextArea(prompt, GUILayout.Height(90));
            GUI.enabled = !_isBusy && !string.IsNullOrWhiteSpace(prompt);
            if (GUILayout.Button("Generate", GUILayout.Height(36)))
            {
                Generate();
            }
            GUI.enabled = true;
            GUILayout.Space(12);
            GUILayout.Label("Response");
            GUILayout.TextArea(_result, GUILayout.ExpandHeight(true));
            GUILayout.EndArea();
        }
    }
}
