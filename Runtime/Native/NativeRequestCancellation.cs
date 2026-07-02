using System;
using System.Threading;

namespace Baran.AppleFoundationModels.Native
{
    internal sealed class NativeRequestCancellation : IDisposable
    {
        private readonly string _requestId;
        private readonly NativeRequestRegistry _registry;
        private readonly INativeFoundationModelsTransport _transport;
        private readonly CancellationTokenRegistration _callerRegistration;
        private readonly CancellationTokenRegistration _timeoutRegistration;
        private readonly CancellationTokenSource _timeoutSource;

        public NativeRequestCancellation(
            string requestId,
            NativeRequestRegistry registry,
            INativeFoundationModelsTransport transport,
            CancellationToken callerCancellation,
            TimeSpan timeout)
        {
            _requestId = requestId;
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _transport = transport ?? throw new ArgumentNullException(nameof(transport));

            _callerRegistration = callerCancellation.CanBeCanceled
                ? callerCancellation.Register(CancelFromCaller)
                : default;

            _timeoutSource = new CancellationTokenSource();
            _timeoutRegistration = _timeoutSource.Token.Register(CancelFromTimeout);
            _timeoutSource.CancelAfter(timeout);
        }

        public void Dispose()
        {
            _callerRegistration.Dispose();
            _timeoutRegistration.Dispose();
            _timeoutSource.Dispose();
        }

        private void CancelFromCaller()
        {
            if (_registry.TryCancel(_requestId))
            {
                _transport.CancelRequest(_requestId);
            }
        }

        private void CancelFromTimeout()
        {
            var exception = new TimeoutException(
                $"Apple Foundation Models request {_requestId} timed out.");
            if (_registry.TryFail(_requestId, exception))
            {
                _transport.CancelRequest(_requestId);
            }
        }
    }
}
