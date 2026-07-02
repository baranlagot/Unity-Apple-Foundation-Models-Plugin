using System;

namespace Baran.AppleFoundationModels.Native
{
    internal interface ICallbackScheduler
    {
        void Schedule(Action callback);
    }

    internal interface ICallbackSchedulerFactory
    {
        ICallbackScheduler CaptureCurrent();
    }
}
