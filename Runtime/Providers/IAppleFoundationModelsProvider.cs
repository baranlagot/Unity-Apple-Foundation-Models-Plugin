using System;
using System.Threading;
using System.Threading.Tasks;

namespace Baran.AppleFoundationModels.Providers
{
    public interface IAppleFoundationModelsProvider
    {
        Task<AppleFoundationModelsAvailability> GetAvailabilityAsync();

        Task<AppleFoundationModelsResult> GenerateTextAsync(
            string prompt,
            AppleFoundationModelsOptions options,
            CancellationToken cancellationToken);

        Task StreamTextAsync(
            string prompt,
            Action<string> onToken,
            Action<AppleFoundationModelsResult> onComplete,
            Action<Exception> onError,
            AppleFoundationModelsOptions options,
            CancellationToken cancellationToken);
    }
}
