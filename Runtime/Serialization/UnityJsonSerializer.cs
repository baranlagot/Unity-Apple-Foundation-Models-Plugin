using UnityEngine;

namespace Baran.AppleFoundationModels.Serialization
{
    public sealed class UnityJsonSerializer : IAppleFoundationModelsJsonSerializer
    {
        public T Deserialize<T>(string json)
        {
            return JsonUtility.FromJson<T>(json);
        }
    }
}
