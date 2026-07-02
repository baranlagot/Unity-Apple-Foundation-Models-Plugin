using System;
using System.Threading;
using System.Threading.Tasks;

namespace Baran.AppleFoundationModels
{
    public interface IAppleFoundationModelsClient
    {
        Task<AppleFoundationModelsAvailability> GetAvailabilityAsync();

        Task<AppleFoundationModelsResult> GenerateTextAsync(
            string prompt,
            AppleFoundationModelsOptions options = null,
            CancellationToken cancellationToken = default);

        Task<T> GenerateJsonAsync<T>(
            string prompt,
            AppleFoundationModelsOptions options = null,
            CancellationToken cancellationToken = default);

        Task StreamTextAsync(
            string prompt,
            Action<string> onToken,
            Action<AppleFoundationModelsResult> onComplete,
            Action<Exception> onError = null,
            AppleFoundationModelsOptions options = null,
            CancellationToken cancellationToken = default);
    }
}
