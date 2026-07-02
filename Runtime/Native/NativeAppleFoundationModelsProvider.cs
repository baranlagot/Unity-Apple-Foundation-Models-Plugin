using System;
using System.Threading;
using System.Threading.Tasks;
using Baran.AppleFoundationModels.Providers;
using UnityEngine;

namespace Baran.AppleFoundationModels.Native
{
    internal sealed class NativeAppleFoundationModelsProvider :
        IAppleFoundationModelsProvider
    {
        private readonly INativeFoundationModelsTransport _transport;
        private readonly INativeMessageCodec _codec;
        private readonly NativeRequestRegistry _registry;
        private readonly ICallbackSchedulerFactory _schedulerFactory;
        private readonly TimeSpan _timeout;
        private readonly bool _debugLogging;

        public NativeAppleFoundationModelsProvider(
            INativeFoundationModelsTransport transport,
            INativeMessageCodec codec,
            NativeRequestRegistry registry,
            ICallbackSchedulerFactory schedulerFactory,
            AppleFoundationModelsConfiguration configuration)
        {
            _transport = transport ?? throw new ArgumentNullException(nameof(transport));
            _codec = codec ?? throw new ArgumentNullException(nameof(codec));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _schedulerFactory = schedulerFactory ?? throw new ArgumentNullException(nameof(schedulerFactory));
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            _timeout = TimeSpan.FromSeconds(configuration.DefaultTimeoutSeconds);
            _debugLogging = configuration.EnableNativeDebugLogs;
            _transport.Initialize(HandleNativeEvent, _debugLogging);
        }

        public async Task<AppleFoundationModelsAvailability> GetAvailabilityAsync()
        {
            var request = new PendingAvailabilityRequest(
                CreateRequestId(),
                _schedulerFactory.CaptureCurrent());
            _registry.Register(request);

            try
            {
                _transport.GetAvailability(request.RequestId);
            }
            catch (Exception exception)
            {
                _registry.TryFail(request.RequestId, exception);
            }

            using (new NativeRequestCancellation(
                       request.RequestId,
                       _registry,
                       _transport,
                       CancellationToken.None,
                       _timeout))
            {
                return await request.Task.ConfigureAwait(false);
            }
        }

        public async Task<AppleFoundationModelsResult> GenerateTextAsync(
            string prompt,
            AppleFoundationModelsOptions options,
            CancellationToken cancellationToken)
        {
            var request = new PendingTextRequest(
                CreateRequestId(),
                _schedulerFactory.CaptureCurrent());
            _registry.Register(request);

            try
            {
                _transport.GenerateText(
                    request.RequestId,
                    prompt,
                    _codec.EncodeOptions(options));
            }
            catch (Exception exception)
            {
                _registry.TryFail(request.RequestId, exception);
            }

            using (new NativeRequestCancellation(
                       request.RequestId,
                       _registry,
                       _transport,
                       cancellationToken,
                       _timeout))
            {
                return await request.Task.ConfigureAwait(false);
            }
        }

        public async Task StreamTextAsync(
            string prompt,
            Action<string> onToken,
            Action<AppleFoundationModelsResult> onComplete,
            Action<Exception> onError,
            AppleFoundationModelsOptions options,
            CancellationToken cancellationToken)
        {
            var request = new PendingStreamRequest(
                CreateRequestId(),
                _schedulerFactory.CaptureCurrent(),
                onToken,
                onComplete,
                onError);
            _registry.Register(request);

            try
            {
                _transport.StreamText(
                    request.RequestId,
                    prompt,
                    _codec.EncodeOptions(options));
            }
            catch (Exception exception)
            {
                _registry.TryFail(request.RequestId, exception);
            }

            using (new NativeRequestCancellation(
                       request.RequestId,
                       _registry,
                       _transport,
                       cancellationToken,
                       _timeout))
            {
                await request.Task.ConfigureAwait(false);
            }
        }

        private static string CreateRequestId()
        {
            return Guid.NewGuid().ToString("N");
        }

        private void HandleNativeEvent(string eventJson)
        {
            try
            {
                var message = _codec.DecodeEvent(eventJson);
                if (!_registry.Dispatch(message) && _debugLogging)
                {
                    Debug.LogWarning(
                        $"Ignoring native event for unknown request {message.requestId}.");
                }
            }
            catch (NativeEventDecodingException exception)
            {
                if (!string.IsNullOrWhiteSpace(exception.RequestId))
                {
                    _registry.TryFail(
                        exception.RequestId,
                        new AppleFoundationModelsException(
                            exception.Message,
                            AppleFoundationModelsAvailabilityStatus.Unknown,
                            exception));
                }

                if (_debugLogging)
                {
                    Debug.LogWarning(
                        $"Ignoring malformed Apple Foundation Models event: {exception.Message}");
                }
            }
            catch (Exception exception)
            {
                if (_debugLogging)
                {
                    Debug.LogWarning(
                        $"Ignoring malformed Apple Foundation Models event: {exception.Message}");
                }
            }
        }
    }
}
