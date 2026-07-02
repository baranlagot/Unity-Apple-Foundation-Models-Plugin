using System;
using System.Threading;
using System.Threading.Tasks;
using Baran.AppleFoundationModels.Internal;
using Baran.AppleFoundationModels.Providers;
using Baran.AppleFoundationModels.Serialization;

namespace Baran.AppleFoundationModels
{
    public sealed class AppleFoundationModelsClient : IAppleFoundationModelsClient
    {
        private const string JsonInstruction = "Return only one valid JSON object. Do not use Markdown fences or add commentary.";

        private readonly IAppleFoundationModelsProvider _provider;
        private readonly IAppleFoundationModelsJsonSerializer _jsonSerializer;

        public AppleFoundationModelsClient(
            IAppleFoundationModelsProvider provider,
            IAppleFoundationModelsJsonSerializer jsonSerializer)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _jsonSerializer = jsonSerializer ?? throw new ArgumentNullException(nameof(jsonSerializer));
        }

        public Task<AppleFoundationModelsAvailability> GetAvailabilityAsync()
        {
            return _provider.GetAvailabilityAsync();
        }

        public Task<AppleFoundationModelsResult> GenerateTextAsync(
            string prompt,
            AppleFoundationModelsOptions options = null,
            CancellationToken cancellationToken = default)
        {
            Guard.Prompt(prompt);
            cancellationToken.ThrowIfCancellationRequested();

            var snapshot = AppleFoundationModelsOptions.Snapshot(options);
            Guard.Options(snapshot);
            return _provider.GenerateTextAsync(prompt, snapshot, cancellationToken);
        }

        public async Task<T> GenerateJsonAsync<T>(
            string prompt,
            AppleFoundationModelsOptions options = null,
            CancellationToken cancellationToken = default)
        {
            Guard.Prompt(prompt);

            var snapshot = AppleFoundationModelsOptions.Snapshot(options);
            snapshot.PreferStructuredOutput = true;
            snapshot.Instructions = string.IsNullOrWhiteSpace(snapshot.Instructions)
                ? JsonInstruction
                : snapshot.Instructions + "\n" + JsonInstruction;

            var result = await GenerateTextAsync(prompt, snapshot, cancellationToken);
            if (!result.IsSuccess)
            {
                throw new AppleFoundationModelsException(
                    string.IsNullOrWhiteSpace(result.ErrorMessage)
                        ? "Apple Foundation Models could not generate JSON."
                        : result.ErrorMessage);
            }

            var json = JsonResponseSanitizer.StripMarkdownFence(result.Text);
            try
            {
                var value = _jsonSerializer.Deserialize<T>(json);
                if (ReferenceEquals(value, null))
                {
                    throw new InvalidOperationException("The JSON deserializer returned null.");
                }

                return value;
            }
            catch (Exception exception) when (!(exception is AppleFoundationModelsException))
            {
                throw new AppleFoundationModelsException(
                    $"The generated response was not valid JSON for {typeof(T).Name}.",
                    AppleFoundationModelsAvailabilityStatus.Unknown,
                    exception);
            }
        }

        public async Task StreamTextAsync(
            string prompt,
            Action<string> onToken,
            Action<AppleFoundationModelsResult> onComplete,
            Action<Exception> onError = null,
            AppleFoundationModelsOptions options = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                Guard.Prompt(prompt);
                Guard.Callback(onToken, nameof(onToken));
                Guard.Callback(onComplete, nameof(onComplete));
                cancellationToken.ThrowIfCancellationRequested();

                var snapshot = AppleFoundationModelsOptions.Snapshot(options);
                Guard.Options(snapshot);
                await _provider.StreamTextAsync(
                    prompt,
                    onToken,
                    onComplete,
                    onError,
                    snapshot,
                    cancellationToken);
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
    }
}
