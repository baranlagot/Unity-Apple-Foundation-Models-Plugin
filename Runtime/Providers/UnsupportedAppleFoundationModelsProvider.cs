using System;
using System.Threading;
using System.Threading.Tasks;

namespace Baran.AppleFoundationModels.Providers
{
    public sealed class UnsupportedAppleFoundationModelsProvider : IAppleFoundationModelsProvider
    {
        private const string Message = "This platform only supports the mock or a custom provider.";

        public Task<AppleFoundationModelsAvailability> GetAvailabilityAsync()
        {
            return Task.FromResult(AppleFoundationModelsAvailability.Unavailable(
                AppleFoundationModelsAvailabilityStatus.UnsupportedPlatform,
                Message));
        }

        public Task<AppleFoundationModelsResult> GenerateTextAsync(
            string prompt,
            AppleFoundationModelsOptions options,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            throw CreateException();
        }

        public Task StreamTextAsync(
            string prompt,
            Action<string> onToken,
            Action<AppleFoundationModelsResult> onComplete,
            Action<Exception> onError,
            AppleFoundationModelsOptions options,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var exception = CreateException();
            if (onError == null)
            {
                throw exception;
            }

            onError(exception);
            return Task.CompletedTask;
        }

        private static AppleFoundationModelsException CreateException()
        {
            return new AppleFoundationModelsException(
                Message,
                AppleFoundationModelsAvailabilityStatus.UnsupportedPlatform);
        }
    }
}
