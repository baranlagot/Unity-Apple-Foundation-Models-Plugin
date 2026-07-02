namespace Baran.AppleFoundationModels.Native
{
    internal interface INativeMessageCodec
    {
        NativeEventMessage DecodeEvent(string eventJson);

        string EncodeOptions(AppleFoundationModelsOptions options);
    }
}
