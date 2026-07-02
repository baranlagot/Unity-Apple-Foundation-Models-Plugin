using System;
using System.Text;
using System.Threading;
using UnityEngine;

namespace Baran.AppleFoundationModels.Samples
{
    public sealed class StreamingChatExample : MonoBehaviour
    {
        private CancellationTokenSource _cancellation;

        public async void Stream(string prompt)
        {
            Cancel();
            _cancellation = new CancellationTokenSource();
            var response = new StringBuilder();

            try
            {
                await AppleFoundationModels.StreamTextAsync(
                    prompt,
                    chunk =>
                    {
                        response.Append(chunk);
                        Debug.Log(response.ToString());
                    },
                    result => Debug.Log($"Completed: {result.Text}"),
                    exception => Debug.LogError(exception.Message),
                    cancellationToken: _cancellation.Token);
            }
            catch (OperationCanceledException)
            {
                // Cancellation is an expected terminal state for this sample.
            }
        }

        public void Cancel()
        {
            _cancellation?.Cancel();
            _cancellation?.Dispose();
            _cancellation = null;
        }

        private void OnDestroy()
        {
            Cancel();
        }
    }
}
