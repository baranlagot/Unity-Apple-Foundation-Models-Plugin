using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Baran.AppleFoundationModels.Editor.Tests
{
    public sealed class AppleFoundationModelsSettingsTests
    {
        private AppleFoundationModelsSettings _settings;
        private AppleFoundationModelsConfiguration _originalConfiguration;

        [SetUp]
        public void SetUp()
        {
            _settings = AppleFoundationModelsSettings.instance;
            _originalConfiguration = _settings.ToRuntimeConfiguration();
            global::Baran.AppleFoundationModels.AppleFoundationModels.ResetProvider();
        }

        [TearDown]
        public void TearDown()
        {
            _settings.UpdateValues(
                _originalConfiguration.UseMockProviderInEditor,
                _originalConfiguration.EnableNativeDebugLogs,
                _originalConfiguration.DefaultTimeoutSeconds,
                _originalConfiguration.EnableFallbackProvider);
            _settings.SaveSettings();
            AppleFoundationModelsSettingsSynchronizer.Apply();
            global::Baran.AppleFoundationModels.AppleFoundationModels.ResetProvider();
        }

        [Test]
        public void SaveSettings_CreatesProjectSettingsAsset()
        {
            _settings.SaveSettings();

            var settingsPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "ProjectSettings",
                "AppleFoundationModelsSettings.asset");
            Assert.That(File.Exists(settingsPath), Is.True);
        }

        [Test]
        public void UpdateValues_ClampsTimeoutAndMapsEverySetting()
        {
            _settings.UpdateValues(
                useMock: false,
                nativeDebugLogs: true,
                timeoutSeconds: 0,
                fallbackProvider: true);

            var configuration = _settings.ToRuntimeConfiguration();

            Assert.That(configuration.UseMockProviderInEditor, Is.False);
            Assert.That(configuration.EnableNativeDebugLogs, Is.True);
            Assert.That(configuration.DefaultTimeoutSeconds,
                Is.EqualTo(AppleFoundationModelsSettings.MinimumTimeoutSeconds));
            Assert.That(configuration.EnableFallbackProvider, Is.True);
        }

        [Test]
        public async Task Apply_WhenMockSettingChanges_RecomposesDefaultProvider()
        {
            _settings.UpdateValues(
                useMock: false,
                nativeDebugLogs: false,
                timeoutSeconds: 30,
                fallbackProvider: false);
            AppleFoundationModelsSettingsSynchronizer.Apply();

            var availability = await global::Baran.AppleFoundationModels
                .AppleFoundationModels.GetAvailabilityAsync();

            Assert.That(availability.Status,
                Is.EqualTo(AppleFoundationModelsAvailabilityStatus.UnsupportedPlatform));
        }

        [Test]
        public void CreateProvider_ReturnsProjectScopedSettingsProvider()
        {
            var provider = AppleFoundationModelsSettingsProvider.CreateProvider();

            Assert.That(provider.settingsPath,
                Is.EqualTo("Project/Apple Foundation Models"));
        }
    }
}
