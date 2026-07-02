using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Baran.AppleFoundationModels.Native;
using NUnit.Framework;
using UnityEngine;

namespace Baran.AppleFoundationModels.Tests
{
    public sealed class NativeAppleFoundationModelsProviderTests
    {
        private FakeNativeTransport _transport;
        private NativeRequestRegistry _registry;
        private NativeAppleFoundationModelsProvider _provider;

        [SetUp]
        public void SetUp()
        {
            _transport = new FakeNativeTransport();
            _registry = new NativeRequestRegistry();
            _provider = CreateProvider(_transport, _registry);
        }

        [Test]
        public async Task Availability_MapsNativeStatusAndMessage()
        {
            var task = _provider.GetAvailabilityAsync();

            _transport.Emit(new NativeEventMessage
            {
                requestId = _transport.LastRequestId,
                type = NativeEventTypes.Availability,
                status = nameof(AppleFoundationModelsAvailabilityStatus.ModelNotReady),
                payload = "The model is downloading."
            });
            var availability = await task;

            Assert.That(availability.Status,
                Is.EqualTo(AppleFoundationModelsAvailabilityStatus.ModelNotReady));
            Assert.That(availability.Message, Is.EqualTo("The model is downloading."));
            Assert.That(_registry.Count, Is.Zero);
        }

        [Test]
        public async Task GenerateText_EncodesOptionsAndCompletesRequest()
        {
            var options = new AppleFoundationModelsOptions
            {
                Instructions = "Be concise.",
                Temperature = 0.5f,
                MaxOutputTokens = 42
            };
            var task = _provider.GenerateTextAsync(
                "hello",
                options,
                CancellationToken.None);

            StringAssert.Contains("\"hasTemperature\":true", _transport.LastOptionsJson);
            StringAssert.Contains("\"temperature\":0.5", _transport.LastOptionsJson);
            StringAssert.Contains("\"maxOutputTokens\":42", _transport.LastOptionsJson);
            _transport.Emit(new NativeEventMessage
            {
                requestId = _transport.LastRequestId,
                type = NativeEventTypes.Text,
                payload = "native result"
            });

            var result = await task;
            Assert.That(result.Text, Is.EqualTo("native result"));
            Assert.That(_registry.Count, Is.Zero);
        }

        [Test]
        public async Task StreamText_EmitsOrderedDeltasAndCompletesOnce()
        {
            var deltas = new List<string>();
            var completionCount = 0;
            var task = _provider.StreamTextAsync(
                "hello",
                deltas.Add,
                _ => completionCount++,
                null,
                new AppleFoundationModelsOptions(),
                CancellationToken.None);
            var requestId = _transport.LastRequestId;

            _transport.Emit(StreamEvent(requestId, NativeEventTypes.StreamDelta, "one "));
            _transport.Emit(StreamEvent(requestId, NativeEventTypes.StreamDelta, "two"));
            _transport.Emit(StreamEvent(requestId, NativeEventTypes.Complete, "one two"));
            _transport.Emit(StreamEvent(requestId, NativeEventTypes.Complete, "ignored"));
            await task;

            Assert.That(string.Concat(deltas), Is.EqualTo("one two"));
            Assert.That(completionCount, Is.EqualTo(1));
            Assert.That(_registry.Count, Is.Zero);
        }

        [Test]
        public void GenerateText_WhenCancelled_CancelsNativeExactlyOnceAndIgnoresLateEvents()
        {
            using (var cancellation = new CancellationTokenSource())
            {
                var task = _provider.GenerateTextAsync(
                    "hello",
                    new AppleFoundationModelsOptions(),
                    cancellation.Token);
                var requestId = _transport.LastRequestId;

                cancellation.Cancel();
                cancellation.Cancel();

                Assert.CatchAsync<OperationCanceledException>(async () => await task);
                Assert.That(_transport.CancelledRequestIds,
                    Is.EqualTo(new[] { requestId }));
                Assert.That(_registry.Count, Is.Zero);

                Assert.DoesNotThrow(() => _transport.Emit(new NativeEventMessage
                {
                    requestId = requestId,
                    type = NativeEventTypes.Text,
                    payload = "late"
                }));
            }
        }

        [Test]
        public void GenerateText_WhenNativeReturnsError_MapsTypedException()
        {
            var task = _provider.GenerateTextAsync(
                "hello",
                new AppleFoundationModelsOptions(),
                CancellationToken.None);

            _transport.Emit(new NativeEventMessage
            {
                requestId = _transport.LastRequestId,
                type = NativeEventTypes.Error,
                status = nameof(AppleFoundationModelsAvailabilityStatus.UnsupportedDevice),
                errorCode = "deviceNotEligible",
                errorMessage = "Apple Foundation Models are not available on this device."
            });

            var exception = Assert.ThrowsAsync<AppleFoundationModelsException>(
                async () => await task);
            Assert.That(exception.Status,
                Is.EqualTo(AppleFoundationModelsAvailabilityStatus.UnsupportedDevice));
            StringAssert.Contains("not available", exception.Message);
        }

        [Test]
        public void GenerateText_WhenEventTypeIsUnexpected_FailsAndCleansUp()
        {
            var task = _provider.GenerateTextAsync(
                "hello",
                new AppleFoundationModelsOptions(),
                CancellationToken.None);

            _transport.Emit(new NativeEventMessage
            {
                requestId = _transport.LastRequestId,
                type = NativeEventTypes.Availability,
                status = nameof(AppleFoundationModelsAvailabilityStatus.Available)
            });

            var exception = Assert.ThrowsAsync<AppleFoundationModelsException>(
                async () => await task);
            StringAssert.Contains("unexpected event type", exception.Message);
            Assert.That(_registry.Count, Is.Zero);
        }

        [Test]
        public void GenerateText_WhenEventHasNoType_FailsAssociatedRequest()
        {
            var task = _provider.GenerateTextAsync(
                "hello",
                new AppleFoundationModelsOptions(),
                CancellationToken.None);

            _transport.EmitRaw(
                $"{{\"requestId\":\"{_transport.LastRequestId}\",\"payload\":\"bad\"}}");

            var exception = Assert.ThrowsAsync<AppleFoundationModelsException>(
                async () => await task);
            StringAssert.Contains("without a type", exception.Message);
            Assert.That(_registry.Count, Is.Zero);
        }

        [Test]
        public void Timeout_FailsRequestAndCancelsNativeExactlyOnce()
        {
            var request = new PendingTextRequest("timeout-request", new ImmediateScheduler());
            _registry.Register(request);

            using (new NativeRequestCancellation(
                       request.RequestId,
                       _registry,
                       _transport,
                       CancellationToken.None,
                       TimeSpan.FromMilliseconds(25)))
            {
                Assert.That(
                    SpinWait.SpinUntil(() => request.Task.IsCompleted, 1000),
                    Is.True,
                    "The timeout did not complete the pending request.");
            }

            Assert.That(request.Task.IsFaulted, Is.True);
            Assert.That(request.Task.Exception?.InnerException,
                Is.TypeOf<TimeoutException>());
            Assert.That(_transport.CancelledRequestIds,
                Is.EqualTo(new[] { request.RequestId }));
            Assert.That(_registry.Count, Is.Zero);
        }

        [Test]
        public void NativeTransport_InEditor_NeverInvokesDllImport()
        {
            var transport = new AppleFoundationModelsNativeTransport();
            transport.Initialize(_ => { }, debugLoggingEnabled: false);

            Assert.Throws<PlatformNotSupportedException>(() =>
                transport.GetAvailability("request"));
        }

        private static NativeAppleFoundationModelsProvider CreateProvider(
            INativeFoundationModelsTransport transport,
            NativeRequestRegistry registry)
        {
            return new NativeAppleFoundationModelsProvider(
                transport,
                new UnityNativeMessageCodec(),
                registry,
                new ImmediateSchedulerFactory(),
                new AppleFoundationModelsConfiguration(
                    useMockProviderInEditor: true,
                    enableNativeDebugLogs: false,
                    defaultTimeoutSeconds: 1,
                    enableFallbackProvider: false));
        }

        private static NativeEventMessage StreamEvent(
            string requestId,
            string type,
            string payload)
        {
            return new NativeEventMessage
            {
                requestId = requestId,
                type = type,
                payload = payload
            };
        }

        private sealed class ImmediateSchedulerFactory : ICallbackSchedulerFactory
        {
            public ICallbackScheduler CaptureCurrent()
            {
                return new ImmediateScheduler();
            }
        }

        private sealed class ImmediateScheduler : ICallbackScheduler
        {
            public void Schedule(Action callback)
            {
                callback();
            }
        }

        private sealed class FakeNativeTransport : INativeFoundationModelsTransport
        {
            private Action<string> _eventHandler;

            public string LastRequestId { get; private set; }

            public string LastOptionsJson { get; private set; }

            public List<string> CancelledRequestIds { get; } = new List<string>();

            public void Initialize(Action<string> eventHandler, bool debugLoggingEnabled)
            {
                _eventHandler = eventHandler;
            }

            public void GetAvailability(string requestId)
            {
                LastRequestId = requestId;
            }

            public void GenerateText(string requestId, string prompt, string optionsJson)
            {
                LastRequestId = requestId;
                LastOptionsJson = optionsJson;
            }

            public void StreamText(string requestId, string prompt, string optionsJson)
            {
                LastRequestId = requestId;
                LastOptionsJson = optionsJson;
            }

            public void CancelRequest(string requestId)
            {
                CancelledRequestIds.Add(requestId);
            }

            public void Emit(NativeEventMessage message)
            {
                EmitRaw(JsonUtility.ToJson(message));
            }

            public void EmitRaw(string message)
            {
                _eventHandler(message);
            }
        }
    }
}
