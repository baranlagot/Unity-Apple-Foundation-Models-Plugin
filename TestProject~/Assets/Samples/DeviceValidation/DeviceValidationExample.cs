namespace Baran.AppleFoundationModels.Samples
{
    public sealed class DeviceValidationExample : DiagnosticSampleBehaviourBase
    {
        protected override ISampleDiagnosticPresenter CreatePresenter(
            IAppleFoundationModelsClient client,
            IDiagnosticShellView view)
        {
            return new DeviceValidationPresenter(
                view,
                new DeviceValidationRunner(
                    client,
                    new DefaultDeviceValidationEnvironment()),
                new SystemClipboard());
        }
    }
}
