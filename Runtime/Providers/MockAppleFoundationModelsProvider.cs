using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Baran.AppleFoundationModels.Providers
{
    public sealed class MockAppleFoundationModelsProvider : IAppleFoundationModelsProvider
    {
        private const string Prefix = "[Mock Apple Foundation Models] Generated response for: ";
        private const string MockJson = "{\"title\":\"A Cozy Mock Quest\",\"objective\":\"Find three warm blankets\",\"rewardCoins\":25,\"npcName\":\"Mittens\"}";

        private readonly TimeSpan _streamDelay;

        public MockAppleFoundationModelsProvider()
            : this(TimeSpan.FromMilliseconds(25))
        {
        }

        public MockAppleFoundationModelsProvider(TimeSpan streamDelay)
        {
            if (streamDelay < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(streamDelay));
            }

            _streamDelay = streamDelay;
        }

        public Task<AppleFoundationModelsAvailability> GetAvailabilityAsync()
        {
            return Task.FromResult(AppleFoundationModelsAvailability.Available(
                "The deterministic mock provider is active. No Apple model is being used."));
        }

        public Task<AppleFoundationModelsResult> GenerateTextAsync(
            string prompt,
            AppleFoundationModelsOptions options,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var text = options != null && options.PreferStructuredOutput
                ? MockJson
                : Prefix + prompt;
            return Task.FromResult(AppleFoundationModelsResult.Success(text));
        }

        public async Task StreamTextAsync(
            string prompt,
            Action<string> onToken,
            Action<AppleFoundationModelsResult> onComplete,
            Action<Exception> onError,
            AppleFoundationModelsOptions options,
            CancellationToken cancellationToken)
        {
            try
            {
                var result = await GenerateTextAsync(prompt, options, cancellationToken);
                foreach (var chunk in SplitIntoChunks(result.Text))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (_streamDelay > TimeSpan.Zero)
                    {
                        await Task.Delay(_streamDelay, cancellationToken);
                    }

                    onToken(chunk);
                }

                onComplete(result);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                if (onError == null)
                {
                    throw;
                }

                onError(exception);
            }
        }

        private static IEnumerable<string> SplitIntoChunks(string text)
        {
            var words = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            for (var index = 0; index < words.Length; index++)
            {
                yield return index == words.Length - 1 ? words[index] : words[index] + " ";
            }
        }
    }
}
