using System;
using System.Text;
using System.Threading;
using UnityEngine;

namespace Baran.AppleFoundationModels.Samples
{
    public sealed class StreamingChatExample : MonoBehaviour
    {
        [SerializeField]
        private string prompt = "Generate three cute pet names for an orange cat.";

        private IAppleFoundationModelsClient _client;
        private CancellationTokenSource _cancellation;
        private string _response = "Streaming text appears here.";
        private bool _isBusy;

        private void Awake()
        {
            _client = AppleFoundationModels.DefaultClient;
        }

        public async void Stream(string prompt)
        {
            Cancel();
            _cancellation = new CancellationTokenSource();
            var response = new StringBuilder();
            _response = string.Empty;
            _isBusy = true;

            try
            {
                await _client.StreamTextAsync(
                    prompt,
                    chunk =>
                    {
                        response.Append(chunk);
                        _response = response.ToString();
                    },
                    result => _response = result.Text,
                    exception => _response = exception.Message,
                    cancellationToken: _cancellation.Token);
            }
            catch (OperationCanceledException)
            {
                // Cancellation is an expected terminal state for this sample.
                _response = "Cancelled.";
            }
            finally
            {
                _isBusy = false;
            }
        }

        public void Cancel()
        {
            _cancellation?.Cancel();
            _cancellation?.Dispose();
            _cancellation = null;
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(24, 24, 620, 400), GUI.skin.box);
            GUILayout.Label("Apple Foundation Models — Streaming Chat");
            GUILayout.Label("Prompt");
            prompt = GUILayout.TextArea(prompt, GUILayout.Height(80));

            GUILayout.BeginHorizontal();
            GUI.enabled = !_isBusy && !string.IsNullOrWhiteSpace(prompt);
            if (GUILayout.Button("Stream", GUILayout.Height(36)))
            {
                Stream(prompt);
            }
            GUI.enabled = _isBusy;
            if (GUILayout.Button("Cancel", GUILayout.Height(36)))
            {
                Cancel();
            }
            GUI.enabled = true;
            GUILayout.EndHorizontal();

            GUILayout.Space(12);
            GUILayout.Label("Response");
            GUILayout.TextArea(_response, GUILayout.ExpandHeight(true));
            GUILayout.EndArea();
        }

        private void OnDestroy()
        {
            Cancel();
        }
    }
}
