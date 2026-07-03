# Device Validation

Runs a reusable diagnostic validation suite against the active `IAppleFoundationModelsClient`.

This scene is intended for release hardening. It:

- records availability details;
- exercises text, streaming, cancellation, JSON, repeated, concurrent, and late-cancel checks when the provider is eligible or mocked;
- keeps lifecycle checks as `NotRun` until you reload the scene or background/foreground the app;
- produces a privacy-safe report that omits prompt contents and device identifiers.

In the Unity Editor the deterministic mock provider can complete the functional checks locally. On real hardware, rerun the scene after a background/foreground cycle and after reloading the scene once so those observational checks are recorded in the report.
