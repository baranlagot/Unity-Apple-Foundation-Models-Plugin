using System;
using System.Threading;
using System.Threading.Tasks;

namespace Baran.AppleFoundationModels.Providers
{
    internal sealed class EditorAppleFoundationModelsProvider : IAppleFoundationModelsProvider
    {
        private readonly bool _mockEnabledByDefault;
        private readonly IAppleFoundationModelsProvider _mockProvider;
        private readonly IAppleFoundationModelsProvider _unsupportedProvider;

        public EditorAppleFoundationModelsProvider(
            bool mockEnabledByDefault,
            IAppleFoundationModelsProvider mockProvider,
            IAppleFoundationModelsProvider unsupportedProvider)
        {
            _mockEnabledByDefault = mockEnabledByDefault;
            _mockProvider = mockProvider ?? throw new ArgumentNullException(nameof(mockProvider));
            _unsupportedProvider = unsupportedProvider ?? throw new ArgumentNullException(nameof(unsupportedProvider));
        }

        public Task<AppleFoundationModelsAvailability> GetAvailabilityAsync()
        {
            return DefaultProvider.GetAvailabilityAsync();
        }

        public Task<AppleFoundationModelsResult> GenerateTextAsync(
            string prompt,
            AppleFoundationModelsOptions options,
            CancellationToken cancellationToken)
        {
            return SelectProvider(options).GenerateTextAsync(
                prompt,
                options,
                cancellationToken);
        }

        public Task StreamTextAsync(
            string prompt,
            Action<string> onToken,
            Action<AppleFoundationModelsResult> onComplete,
            Action<Exception> onError,
            AppleFoundationModelsOptions options,
            CancellationToken cancellationToken)
        {
            return SelectProvider(options).StreamTextAsync(
                prompt,
                onToken,
                onComplete,
                onError,
                options,
                cancellationToken);
        }

        private IAppleFoundationModelsProvider DefaultProvider =>
            _mockEnabledByDefault ? _mockProvider : _unsupportedProvider;

        private IAppleFoundationModelsProvider SelectProvider(AppleFoundationModelsOptions options)
        {
            return _mockEnabledByDefault && (options == null || options.UseMockInEditor)
                ? _mockProvider
                : _unsupportedProvider;
        }
    }
}
