using System;
using UnityEngine;

namespace Baran.AppleFoundationModels.Samples
{
    public sealed class TextGenerationExample : MonoBehaviour
    {
        [SerializeField]
        private string prompt = "Generate a short funny NPC line for a cozy cat cafe game.";

        public async void Generate()
        {
            try
            {
                var result = await AppleFoundationModels.GenerateTextAsync(prompt);
                Debug.Log(result.Text);
            }
            catch (Exception exception)
            {
                Debug.LogError(exception.Message);
            }
        }
    }
}
