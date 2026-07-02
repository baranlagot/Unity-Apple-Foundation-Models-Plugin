using System;

namespace Baran.AppleFoundationModels
{
    [Serializable]
    public sealed class AppleFoundationModelsOptions
    {
        public string Instructions;
        public float? Temperature;
        public int? MaxOutputTokens;
        public string SessionId;
        public bool PreferStructuredOutput;
        public bool UseMockInEditor = true;

        internal AppleFoundationModelsOptions Copy()
        {
            return new AppleFoundationModelsOptions
            {
                Instructions = Instructions,
                Temperature = Temperature,
                MaxOutputTokens = MaxOutputTokens,
                SessionId = SessionId,
                PreferStructuredOutput = PreferStructuredOutput,
                UseMockInEditor = UseMockInEditor
            };
        }

        internal static AppleFoundationModelsOptions Snapshot(AppleFoundationModelsOptions options)
        {
            return options == null ? new AppleFoundationModelsOptions() : options.Copy();
        }
    }
}
