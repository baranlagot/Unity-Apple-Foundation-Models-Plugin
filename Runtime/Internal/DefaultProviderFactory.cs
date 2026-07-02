using Baran.AppleFoundationModels.Providers;

namespace Baran.AppleFoundationModels.Internal
{
    internal static class DefaultProviderFactory
    {
        public static IAppleFoundationModelsProvider Create()
        {
#if UNITY_EDITOR
            return new MockAppleFoundationModelsProvider();
#else
            // Native providers are introduced by the iOS/macOS native vertical slice.
            return new UnsupportedAppleFoundationModelsProvider();
#endif
        }
    }
}
