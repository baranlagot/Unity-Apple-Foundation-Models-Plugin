using System;
using System.Threading;

namespace Baran.AppleFoundationModels.Native
{
    internal sealed class SynchronizationContextCallbackSchedulerFactory :
        ICallbackSchedulerFactory
    {
        public ICallbackScheduler CaptureCurrent()
        {
            return new SynchronizationContextCallbackScheduler(
                SynchronizationContext.Current);
        }
    }

    internal sealed class SynchronizationContextCallbackScheduler : ICallbackScheduler
    {
        private readonly SynchronizationContext _context;

        public SynchronizationContextCallbackScheduler(
            SynchronizationContext context)
        {
            _context = context;
        }

        public void Schedule(Action callback)
        {
            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            if (_context == null || ReferenceEquals(_context, SynchronizationContext.Current))
            {
                callback();
                return;
            }

            _context.Post(_ => callback(), null);
        }
    }
}
