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
        private static AppleFoundationModelsConfiguration _configuration =
            AppleFoundationModelsConfiguration.Default;
        private static IAppleFoundationModelsClient _client = CreateDefaultClient(_configuration);
        private static bool _usesCustomProvider;

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

        public static AppleFoundationModelsConfiguration Configuration
        {
            get
            {
                lock (Sync)
                {
                    return _configuration;
                }
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
                _usesCustomProvider = true;
            }
        }

        public static void ResetProvider()
        {
            lock (Sync)
            {
                _usesCustomProvider = false;
                _client = CreateDefaultClient(_configuration);
            }
        }

        internal static void ApplyConfiguration(
            AppleFoundationModelsConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            lock (Sync)
            {
                _configuration = configuration;
                if (!_usesCustomProvider)
                {
                    _client = CreateDefaultClient(configuration);
                }
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

        private static IAppleFoundationModelsClient CreateDefaultClient(
            AppleFoundationModelsConfiguration configuration)
        {
            return new AppleFoundationModelsClient(
                DefaultProviderFactory.Create(configuration),
                new UnityJsonSerializer());
        }
    }
}
