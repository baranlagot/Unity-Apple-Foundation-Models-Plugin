using System;
using NUnit.Framework;

namespace Baran.AppleFoundationModels.Tests
{
    public sealed class AppleFoundationModelsConfigurationTests
    {
        [Test]
        public void Default_HasSafeProjectDefaults()
        {
            var configuration = AppleFoundationModelsConfiguration.Default;

            Assert.That(configuration.UseMockProviderInEditor, Is.True);
            Assert.That(configuration.EnableNativeDebugLogs, Is.False);
            Assert.That(configuration.DefaultTimeoutSeconds,
                Is.EqualTo(AppleFoundationModelsConfiguration.DefaultTimeoutSecondsValue));
            Assert.That(configuration.EnableFallbackProvider, Is.False);
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Constructor_WhenTimeoutIsNotPositive_Throws(int timeoutSeconds)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new AppleFoundationModelsConfiguration(
                    useMockProviderInEditor: true,
                    enableNativeDebugLogs: false,
                    defaultTimeoutSeconds: timeoutSeconds,
                    enableFallbackProvider: false));
        }
    }
}
