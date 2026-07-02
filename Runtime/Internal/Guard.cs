using System;

namespace Baran.AppleFoundationModels.Internal
{
    internal static class Guard
    {
        public static void Prompt(string prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt))
            {
                throw new ArgumentException("Prompt cannot be null, empty, or whitespace.", nameof(prompt));
            }
        }

        public static void Callback(Delegate callback, string parameterName)
        {
            if (callback == null)
            {
                throw new ArgumentNullException(parameterName);
            }
        }

        public static void Options(AppleFoundationModelsOptions options)
        {
            if (options.Temperature.HasValue &&
                (options.Temperature.Value < 0f || options.Temperature.Value > 1f))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(options.Temperature),
                    "Temperature must be between zero and one inclusive.");
            }

            if (options.MaxOutputTokens.HasValue && options.MaxOutputTokens.Value <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(options.MaxOutputTokens),
                    "MaxOutputTokens must be greater than zero.");
            }
        }
    }
}
