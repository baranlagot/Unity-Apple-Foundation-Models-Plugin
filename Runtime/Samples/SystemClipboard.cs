using UnityEngine;

namespace Baran.AppleFoundationModels.Samples
{
    public sealed class SystemClipboard : ISampleClipboard
    {
        public void Copy(string text)
        {
            GUIUtility.systemCopyBuffer = text ?? string.Empty;
        }
    }
}
