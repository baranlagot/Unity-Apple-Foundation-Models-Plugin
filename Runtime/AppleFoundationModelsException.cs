using System;

namespace Baran.AppleFoundationModels
{
    public sealed class AppleFoundationModelsException : Exception
    {
        public AppleFoundationModelsException(
            string message,
            AppleFoundationModelsAvailabilityStatus status = AppleFoundationModelsAvailabilityStatus.Unknown,
            Exception innerException = null)
            : base(message, innerException)
        {
            Status = status;
        }

        public AppleFoundationModelsAvailabilityStatus Status { get; }
    }
}
