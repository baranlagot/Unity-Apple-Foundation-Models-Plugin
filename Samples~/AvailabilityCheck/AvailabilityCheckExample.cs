using UnityEngine;

namespace Baran.AppleFoundationModels.Samples
{
    public sealed class AvailabilityCheckExample : MonoBehaviour
    {
        public async void CheckAvailability()
        {
            var availability = await AppleFoundationModels.GetAvailabilityAsync();
            Debug.Log($"Foundation Models: {availability.Status} — {availability.Message}");
        }
    }
}
