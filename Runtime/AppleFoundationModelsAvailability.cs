namespace Baran.AppleFoundationModels
{
    public enum AppleFoundationModelsAvailabilityStatus
    {
        Available,
        UnsupportedPlatform,
        UnsupportedOSVersion,
        UnsupportedDevice,
        AppleIntelligenceDisabled,
        ModelNotReady,
        NativeFrameworkUnavailable,
        Unknown
    }

    public sealed class AppleFoundationModelsAvailability
    {
        public AppleFoundationModelsAvailability(
            AppleFoundationModelsAvailabilityStatus status,
            string message)
        {
            Status = status;
            Message = message ?? string.Empty;
        }

        public AppleFoundationModelsAvailabilityStatus Status { get; }

        public string Message { get; }

        public bool IsAvailable => Status == AppleFoundationModelsAvailabilityStatus.Available;

        public static AppleFoundationModelsAvailability Available(string message = null)
        {
            return new AppleFoundationModelsAvailability(
                AppleFoundationModelsAvailabilityStatus.Available,
                message ?? "Apple Foundation Models are available.");
        }

        public static AppleFoundationModelsAvailability Unavailable(
            AppleFoundationModelsAvailabilityStatus status,
            string message)
        {
            return new AppleFoundationModelsAvailability(status, message);
        }
    }
}
