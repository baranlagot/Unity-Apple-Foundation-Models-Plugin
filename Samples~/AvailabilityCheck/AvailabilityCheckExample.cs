using System;
using UnityEngine;

namespace Baran.AppleFoundationModels.Samples
{
    public sealed class AvailabilityCheckExample : MonoBehaviour
    {
        private IAppleFoundationModelsClient _client;
        private string _status = "Select Check Availability to begin.";
        private bool _isBusy;

        private void Awake()
        {
            _client = AppleFoundationModels.DefaultClient;
        }

        public async void CheckAvailability()
        {
            if (_isBusy)
            {
                return;
            }

            _isBusy = true;
            _status = "Checking availability…";
            try
            {
                var availability = await _client.GetAvailabilityAsync();
                _status = $"{availability.Status}\n{availability.Message}";
            }
            catch (Exception exception)
            {
                _status = exception.Message;
            }
            finally
            {
                _isBusy = false;
            }
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(24, 24, 520, 220), GUI.skin.box);
            GUILayout.Label("Apple Foundation Models — Availability");
            GUI.enabled = !_isBusy;
            if (GUILayout.Button("Check Availability", GUILayout.Height(36)))
            {
                CheckAvailability();
            }
            GUI.enabled = true;
            GUILayout.Space(12);
            GUILayout.TextArea(_status, GUILayout.ExpandHeight(true));
            GUILayout.EndArea();
        }
    }
}
