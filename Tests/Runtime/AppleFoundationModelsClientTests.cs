using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Baran.AppleFoundationModels.Providers;
using Baran.AppleFoundationModels.Serialization;
using NUnit.Framework;

namespace Baran.AppleFoundationModels.Tests
{
    public sealed class AppleFoundationModelsClientTests
    {
        private AppleFoundationModelsClient _client;

        [SetUp]
        public void SetUp()
        {
            _client = new AppleFoundationModelsClient(
                new MockAppleFoundationModelsProvider(TimeSpan.Zero),
                new UnityJsonSerializer());
        }

        [Test]
        public async Task Availability_WhenUsingMock_IsAvailableAndClearlyMarked()
        {
            var availability = await _client.GetAvailabilityAsync();

            Assert.That(availability.IsAvailable, Is.True);
            StringAssert.Contains("mock", availability.Message.ToLowerInvariant());
        }

        [Test]
        public async Task GenerateText_ReturnsDeterministicMockResult()
        {
            var result = await _client.GenerateTextAsync("hello");

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Text, Is.EqualTo(
                "[Mock Apple Foundation Models] Generated response for: hello"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void GenerateText_WhenPromptIsBlank_RejectsBeforeProvider(string prompt)
        {
            Assert.ThrowsAsync<ArgumentException>(async () =>
                await _client.GenerateTextAsync(prompt));
        }

        [Test]
        public void GenerateText_WhenOptionsAreInvalid_ThrowsClearError()
        {
            var options = new AppleFoundationModelsOptions { MaxOutputTokens = 0 };

            Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
                await _client.GenerateTextAsync("hello", options));
        }

        [Test]
        public async Task GenerateJson_ParsesMockDataWithoutMutatingCallerOptions()
        {
            var options = new AppleFoundationModelsOptions { Instructions = "Keep it cozy." };

            var quest = await _client.GenerateJsonAsync<QuestData>("Create a quest", options);

            Assert.That(quest.title, Is.EqualTo("A Cozy Mock Quest"));
            Assert.That(quest.rewardCoins, Is.EqualTo(25));
            Assert.That(options.PreferStructuredOutput, Is.False);
            Assert.That(options.Instructions, Is.EqualTo("Keep it cozy."));
        }

        [Test]
        public void GenerateJson_WhenDeserializerFails_WrapsErrorWithTargetType()
        {
            var client = new AppleFoundationModelsClient(
                new MockAppleFoundationModelsProvider(TimeSpan.Zero),
                new ThrowingSerializer());

            var exception = Assert.ThrowsAsync<AppleFoundationModelsException>(async () =>
                await client.GenerateJsonAsync<QuestData>("Create a quest"));

            StringAssert.Contains(nameof(QuestData), exception.Message);
            Assert.That(exception.InnerException, Is.TypeOf<FormatException>());
        }

        [Test]
        public async Task StreamText_EmitsOrderedChunksAndCompletesExactlyOnce()
        {
            var chunks = new List<string>();
            var completionCount = 0;

            await _client.StreamTextAsync(
                "hello",
                chunks.Add,
                _ => completionCount++);

            Assert.That(string.Concat(chunks), Is.EqualTo(
                "[Mock Apple Foundation Models] Generated response for: hello"));
            Assert.That(completionCount, Is.EqualTo(1));
        }

        [Test]
        public void GenerateText_WhenAlreadyCancelled_CompletesAsCancelled()
        {
            using (var source = new CancellationTokenSource())
            {
                source.Cancel();

                Assert.CatchAsync<OperationCanceledException>(async () =>
                    await _client.GenerateTextAsync("hello", cancellationToken: source.Token));
            }
        }

        [Test]
        public void StreamText_WhenAlreadyCancelled_CompletesAsCancelledWithoutCallbacks()
        {
            using (var source = new CancellationTokenSource())
            {
                source.Cancel();
                var callbackCount = 0;

                Assert.CatchAsync<OperationCanceledException>(async () =>
                    await _client.StreamTextAsync(
                        "hello",
                        _ => callbackCount++,
                        _ => callbackCount++,
                        cancellationToken: source.Token));

                Assert.That(callbackCount, Is.Zero);
            }
        }

        [Serializable]
        private sealed class QuestData
        {
            public string title;
            public string objective;
            public int rewardCoins;
            public string npcName;
        }

        private sealed class ThrowingSerializer : IAppleFoundationModelsJsonSerializer
        {
            public T Deserialize<T>(string json)
            {
                throw new FormatException("Invalid JSON for test.");
            }
        }
    }
}
