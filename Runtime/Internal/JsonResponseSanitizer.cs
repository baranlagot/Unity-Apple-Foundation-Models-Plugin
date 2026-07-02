using System;

namespace Baran.AppleFoundationModels.Internal
{
    internal static class JsonResponseSanitizer
    {
        public static string StripMarkdownFence(string response)
        {
            var value = (response ?? string.Empty).Trim();
            if (!value.StartsWith("```", StringComparison.Ordinal))
            {
                return value;
            }

            var firstLineEnd = value.IndexOf('\n');
            var closingFence = value.LastIndexOf("```", StringComparison.Ordinal);
            if (firstLineEnd < 0 || closingFence <= firstLineEnd)
            {
                return value;
            }

            return value.Substring(firstLineEnd + 1, closingFence - firstLineEnd - 1).Trim();
        }
    }
}
