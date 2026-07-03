namespace Baran.AppleFoundationModels.Samples
{
    public sealed class AvailabilityCheckExample : DiagnosticSampleBehaviourBase
    {
        protected override ISampleDiagnosticPresenter CreatePresenter(
            IAppleFoundationModelsClient client,
            IDiagnosticShellView view)
        {
            return new AvailabilityPresenter(client, view);
        }
    }
}
