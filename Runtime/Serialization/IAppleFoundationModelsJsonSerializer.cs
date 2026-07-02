namespace Baran.AppleFoundationModels.Serialization
{
    public interface IAppleFoundationModelsJsonSerializer
    {
        T Deserialize<T>(string json);
    }
}
