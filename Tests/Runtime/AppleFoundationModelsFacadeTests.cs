using System;
using System.Threading;
using System.Threading.Tasks;
using Baran.AppleFoundationModels.Providers;
using NUnit.Framework;

namespace Baran.AppleFoundationModels.Tests
{
    public sealed class AppleFoundationModelsFacadeTests
    {
        [TearDown]
        public void TearDown()
        {
            AppleFoundationModels.ResetProvider();
        }

        [Test]
        public async Task SetProvider_ReplacesDefaultProvider()
        {
            AppleFoundationModels.SetProvider(new StubProvider());

            var result = await AppleFoundationModels.GenerateTextAsync("hello");

            Assert.That(result.Text, Is.EqualTo("custom"));
        }

        [Test]
        public void SetProvider_WhenNull_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => AppleFoundationModels.SetProvider(null));
        }

        [Test]
        public async Task ResetProvider_InEditor_RestoresMockProvider()
        {
            AppleFoundationModels.SetProvider(new StubProvider());
            AppleFoundationModels.ResetProvider();

            var result = await AppleFoundationModels.GenerateTextAsync("hello");

            StringAssert.StartsWith("[Mock Apple Foundation Models]", result.Text);
        }

        private sealed class StubProvider : IAppleFoundationModelsProvider
        {
            public Task<AppleFoundationModelsAvailability> GetAvailabilityAsync()
            {
                return Task.FromResult(AppleFoundationModelsAvailability.Available());
            }

            public Task<AppleFoundationModelsResult> GenerateTextAsync(
                string prompt,
                AppleFoundationModelsOptions options,
                CancellationToken cancellationToken)
            {
                return Task.FromResult(AppleFoundationModelsResult.Success("custom"));
            }

            public Task StreamTextAsync(
                string prompt,
                Action<string> onToken,
                Action<AppleFoundationModelsResult> onComplete,
                Action<Exception> onError,
                AppleFoundationModelsOptions options,
                CancellationToken cancellationToken)
            {
                onToken("custom");
                onComplete(AppleFoundationModelsResult.Success("custom"));
                return Task.CompletedTask;
            }
        }
    }
}
