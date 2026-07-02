# Platform Support

| Platform | Current behavior |
| --- | --- |
| Unity Editor | Deterministic mock provider |
| iOS player | Compiles; native provider planned |
| macOS player | Compiles; native provider planned |
| Windows, Android, Linux, WebGL | Unsupported or custom provider |

Native availability will depend on the Apple operating-system version, device capability, Apple Intelligence settings, model readiness, and framework presence. The future native provider will report these as distinct `AppleFoundationModelsAvailabilityStatus` values.

Unsupported targets must remain compilable. They return a clear unavailable state and may use any application-defined custom provider.
