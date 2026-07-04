using Baran.AppleFoundationModels.Native;
using Baran.AppleFoundationModels.Providers;

namespace Baran.AppleFoundationModels.Internal
{
    internal static class DefaultProviderFactory
    {
        public static IAppleFoundationModelsProvider Create(
            AppleFoundationModelsConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new System.ArgumentNullException(nameof(configuration));
            }

#if UNITY_EDITOR
            return new EditorAppleFoundationModelsProvider(
                configuration.UseMockProviderInEditor,
                new MockAppleFoundationModelsProvider(),
                new UnsupportedAppleFoundationModelsProvider());
#elif UNITY_IOS || UNITY_STANDALONE_OSX
            return new NativeAppleFoundationModelsProvider(
                new AppleFoundationModelsNativeTransport(),
                new UnityNativeMessageCodec(),
                new NativeRequestRegistry(),
                new SynchronizationContextCallbackSchedulerFactory(),
                configuration);
#else
            // Other platforms have no native Apple Foundation Models transport.
            return new UnsupportedAppleFoundationModelsProvider();
#endif
        }
    }
}
