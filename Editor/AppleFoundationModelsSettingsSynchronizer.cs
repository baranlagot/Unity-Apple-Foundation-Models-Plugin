using UnityEditor;

namespace Baran.AppleFoundationModels.Editor
{
    [InitializeOnLoad]
    internal static class AppleFoundationModelsSettingsSynchronizer
    {
        static AppleFoundationModelsSettingsSynchronizer()
        {
            Apply();
        }

        internal static void Apply()
        {
            global::Baran.AppleFoundationModels.AppleFoundationModels.ApplyConfiguration(
                AppleFoundationModelsSettings.instance.ToRuntimeConfiguration());
        }
    }
}
