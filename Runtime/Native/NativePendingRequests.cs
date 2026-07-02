using System;
using System.Threading.Tasks;

namespace Baran.AppleFoundationModels.Native
{
    internal interface INativePendingRequest
    {
        string RequestId { get; }

        bool IsTerminal { get; }

        void AttachTerminalHandler(Action<INativePendingRequest> handler);

        void Handle(NativeEventMessage message);

        bool TryFail(Exception exception);

        bool TryCancel();
    }

    internal abstract class NativePendingRequest : INativePendingRequest
    {
        private readonly object _sync = new object();
        private Action<INativePendingRequest> _terminalHandler;
        private bool _isTerminal;

        protected NativePendingRequest(string requestId, ICallbackScheduler scheduler)
        {
            RequestId = string.IsNullOrWhiteSpace(requestId)
                ? throw new ArgumentException("Request ID is required.", nameof(requestId))
                : requestId;
            Scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
        }

        public string RequestId { get; }

        public bool IsTerminal
        {
            get
            {
                lock (_sync)
                {
                    return _isTerminal;
                }
            }
        }

        protected ICallbackScheduler Scheduler { get; }

        public void AttachTerminalHandler(Action<INativePendingRequest> handler)
        {
            lock (_sync)
            {
                if (_terminalHandler != null)
                {
                    throw new InvalidOperationException(
                        $"Native request {RequestId} already has a terminal handler.");
                }

                _terminalHandler = handler ?? throw new ArgumentNullException(nameof(handler));
            }
        }

        public abstract void Handle(NativeEventMessage message);

        public abstract bool TryFail(Exception exception);

        public abstract bool TryCancel();

        protected bool TrySchedule(Action callback)
        {
            lock (_sync)
            {
                if (_isTerminal)
                {
                    return false;
                }

                Scheduler.Schedule(callback);
                return true;
            }
        }

        protected bool TryTerminate(Action callback)
        {
            Action<INativePendingRequest> terminalHandler;
            lock (_sync)
            {
                if (_isTerminal)
                {
                    return false;
                }

                _isTerminal = true;
                terminalHandler = _terminalHandler;
            }

            terminalHandler?.Invoke(this);
            Scheduler.Schedule(callback);
            return true;
        }

        protected AppleFoundationModelsException CreateNativeException(
            NativeEventMessage message)
        {
            var status = ParseStatus(message.status);
            var userMessage = string.IsNullOrWhiteSpace(message.errorMessage)
                ? "Apple Foundation Models could not complete the request."
                : message.errorMessage;
            return new AppleFoundationModelsException(userMessage, status);
        }

        protected AppleFoundationModelsException CreateUnexpectedEventException(
            NativeEventMessage message)
        {
            return new AppleFoundationModelsException(
                $"Native request {RequestId} returned unexpected event type '{message.type}'.");
        }

        private static AppleFoundationModelsAvailabilityStatus ParseStatus(string value)
        {
            return Enum.TryParse(value, ignoreCase: true, out AppleFoundationModelsAvailabilityStatus status)
                ? status
                : AppleFoundationModelsAvailabilityStatus.Unknown;
        }
    }

    internal sealed class PendingAvailabilityRequest : NativePendingRequest
    {
        private readonly TaskCompletionSource<AppleFoundationModelsAvailability> _completion =
            new TaskCompletionSource<AppleFoundationModelsAvailability>(
                TaskCreationOptions.RunContinuationsAsynchronously);

        public PendingAvailabilityRequest(string requestId, ICallbackScheduler scheduler)
            : base(requestId, scheduler)
        {
        }

        public Task<AppleFoundationModelsAvailability> Task => _completion.Task;

        public override void Handle(NativeEventMessage message)
        {
            if (message.type == NativeEventTypes.Error)
            {
                TryFail(CreateNativeException(message));
                return;
            }

            if (message.type != NativeEventTypes.Availability)
            {
                TryFail(CreateUnexpectedEventException(message));
                return;
            }

            var status = Enum.TryParse(
                message.status,
                ignoreCase: true,
                out AppleFoundationModelsAvailabilityStatus parsed)
                ? parsed
                : AppleFoundationModelsAvailabilityStatus.Unknown;
            var availability = new AppleFoundationModelsAvailability(
                status,
                message.payload ?? string.Empty);
            TryTerminate(() => _completion.TrySetResult(availability));
        }

        public override bool TryFail(Exception exception)
        {
            return TryTerminate(() => _completion.TrySetException(exception));
        }

        public override bool TryCancel()
        {
            return TryTerminate(() => _completion.TrySetCanceled());
        }
    }

    internal sealed class PendingTextRequest : NativePendingRequest
    {
        private readonly TaskCompletionSource<AppleFoundationModelsResult> _completion =
            new TaskCompletionSource<AppleFoundationModelsResult>(
                TaskCreationOptions.RunContinuationsAsynchronously);

        public PendingTextRequest(string requestId, ICallbackScheduler scheduler)
            : base(requestId, scheduler)
        {
        }

        public Task<AppleFoundationModelsResult> Task => _completion.Task;

        public override void Handle(NativeEventMessage message)
        {
            if (message.type == NativeEventTypes.Error)
            {
                TryFail(CreateNativeException(message));
                return;
            }

            if (message.type != NativeEventTypes.Text)
            {
                TryFail(CreateUnexpectedEventException(message));
                return;
            }

            var result = AppleFoundationModelsResult.Success(
                message.payload ?? string.Empty);
            TryTerminate(() => _completion.TrySetResult(result));
        }

        public override bool TryFail(Exception exception)
        {
            return TryTerminate(() => _completion.TrySetException(exception));
        }

        public override bool TryCancel()
        {
            return TryTerminate(() => _completion.TrySetCanceled());
        }
    }

    internal sealed class PendingStreamRequest : NativePendingRequest
    {
        private readonly Action<string> _onToken;
        private readonly Action<AppleFoundationModelsResult> _onComplete;
        private readonly Action<Exception> _onError;
        private readonly TaskCompletionSource<bool> _completion =
            new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        public PendingStreamRequest(
            string requestId,
            ICallbackScheduler scheduler,
            Action<string> onToken,
            Action<AppleFoundationModelsResult> onComplete,
            Action<Exception> onError)
            : base(requestId, scheduler)
        {
            _onToken = onToken ?? throw new ArgumentNullException(nameof(onToken));
            _onComplete = onComplete ?? throw new ArgumentNullException(nameof(onComplete));
            _onError = onError;
        }

        public Task Task => _completion.Task;

        public override void Handle(NativeEventMessage message)
        {
            switch (message.type)
            {
                case NativeEventTypes.StreamDelta:
                    TrySchedule(() =>
                    {
                        try
                        {
                            _onToken(message.payload ?? string.Empty);
                        }
                        catch (Exception exception)
                        {
                            TryFail(exception);
                        }
                    });
                    break;
                case NativeEventTypes.Complete:
                    var result = AppleFoundationModelsResult.Success(
                        message.payload ?? string.Empty);
                    TryTerminate(() =>
                    {
                        try
                        {
                            _onComplete(result);
                            _completion.TrySetResult(true);
                        }
                        catch (Exception exception)
                        {
                            _completion.TrySetException(exception);
                        }
                    });
                    break;
                case NativeEventTypes.Error:
                    TryFail(CreateNativeException(message));
                    break;
                default:
                    TryFail(CreateUnexpectedEventException(message));
                    break;
            }
        }

        public override bool TryFail(Exception exception)
        {
            return TryTerminate(() =>
            {
                if (_onError == null)
                {
                    _completion.TrySetException(exception);
                    return;
                }

                try
                {
                    _onError(exception);
                    _completion.TrySetResult(true);
                }
                catch (Exception callbackException)
                {
                    _completion.TrySetException(callbackException);
                }
            });
        }

        public override bool TryCancel()
        {
            return TryTerminate(() => _completion.TrySetCanceled());
        }
    }
}
