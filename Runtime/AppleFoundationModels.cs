using System;
using System.Threading;
using System.Threading.Tasks;
using Baran.AppleFoundationModels.Internal;
using Baran.AppleFoundationModels.Providers;
using Baran.AppleFoundationModels.Serialization;

namespace Baran.AppleFoundationModels
{
    public static class AppleFoundationModels
    {
        private static readonly object Sync = new object();
        private static IAppleFoundationModelsClient _client = CreateDefaultClient();

        public static bool IsSupportedPlatform
        {
            get
            {
#if (UNITY_IOS || UNITY_STANDALONE_OSX) && !UNITY_EDITOR
                return true;
#else
                return false;
#endif
            }
        }

        public static Task<AppleFoundationModelsAvailability> GetAvailabilityAsync()
        {
            return CurrentClient.GetAvailabilityAsync();
        }

        public static Task<AppleFoundationModelsResult> GenerateTextAsync(
            string prompt,
            AppleFoundationModelsOptions options = null,
            CancellationToken cancellationToken = default)
        {
            return CurrentClient.GenerateTextAsync(prompt, options, cancellationToken);
        }

        public static Task<T> GenerateJsonAsync<T>(
            string prompt,
            AppleFoundationModelsOptions options = null,
            CancellationToken cancellationToken = default)
        {
            return CurrentClient.GenerateJsonAsync<T>(prompt, options, cancellationToken);
        }

        public static Task StreamTextAsync(
            string prompt,
            Action<string> onToken,
            Action<AppleFoundationModelsResult> onComplete,
            Action<Exception> onError = null,
            AppleFoundationModelsOptions options = null,
            CancellationToken cancellationToken = default)
        {
            return CurrentClient.StreamTextAsync(
                prompt,
                onToken,
                onComplete,
                onError,
                options,
                cancellationToken);
        }

        public static void SetProvider(IAppleFoundationModelsProvider provider)
        {
            if (provider == null)
            {
                throw new ArgumentNullException(nameof(provider));
            }

            lock (Sync)
            {
                _client = new AppleFoundationModelsClient(provider, new UnityJsonSerializer());
            }
        }

        public static void ResetProvider()
        {
            lock (Sync)
            {
                _client = CreateDefaultClient();
            }
        }

        private static IAppleFoundationModelsClient CurrentClient
        {
            get
            {
                lock (Sync)
                {
                    return _client;
                }
            }
        }

        private static IAppleFoundationModelsClient CreateDefaultClient()
        {
            return new AppleFoundationModelsClient(
                DefaultProviderFactory.Create(),
                new UnityJsonSerializer());
        }
    }
}
