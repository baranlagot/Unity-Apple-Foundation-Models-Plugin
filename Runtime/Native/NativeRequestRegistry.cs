using System;
using System.Collections.Concurrent;

namespace Baran.AppleFoundationModels.Native
{
    internal sealed class NativeRequestRegistry
    {
        private readonly ConcurrentDictionary<string, INativePendingRequest> _requests =
            new ConcurrentDictionary<string, INativePendingRequest>();

        public int Count => _requests.Count;

        public void Register(INativePendingRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            request.AttachTerminalHandler(Remove);
            if (!_requests.TryAdd(request.RequestId, request))
            {
                throw new InvalidOperationException(
                    $"Native request {request.RequestId} is already registered.");
            }
        }

        public bool Dispatch(NativeEventMessage message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            if (!_requests.TryGetValue(message.requestId, out var request))
            {
                return false;
            }

            request.Handle(message);
            return true;
        }

        public bool TryFail(string requestId, Exception exception)
        {
            return _requests.TryGetValue(requestId, out var request) &&
                   request.TryFail(exception);
        }

        public bool TryCancel(string requestId)
        {
            return _requests.TryGetValue(requestId, out var request) &&
                   request.TryCancel();
        }

        private void Remove(INativePendingRequest request)
        {
            _requests.TryRemove(request.RequestId, out _);
        }
    }
}
