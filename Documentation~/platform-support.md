# Platform Support

| Platform | Current behavior |
| --- | --- |
| Unity Editor | Deterministic mock provider, reusable diagnostic shell, and local validation sample |
| iOS player | Native bridge for iOS 26+, exported-Xcode validation, device validation sample, eligible-device evidence still required |
| macOS player | Compiles; native provider planned for v0.2 |
| Windows, Android, Linux, WebGL | Unsupported or custom provider |

Native availability depends on the Apple operating-system version, device capability, Apple Intelligence settings, model readiness, and framework presence. The provider reports these as distinct `AppleFoundationModelsAvailabilityStatus` values.

Unsupported targets must remain compilable. They return a clear unavailable state and may use any application-defined custom provider.
