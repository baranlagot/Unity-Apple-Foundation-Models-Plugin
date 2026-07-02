# Fallback Providers

Fallbacks are application policy, not a core-package dependency. Implement `IAppleFoundationModelsProvider` and register it explicitly. A fallback can wrap another provider, inspect its availability, and delegate when needed.

Document whether a fallback sends prompts off-device. Obtain appropriate user consent and account for data handling, credentials, latency, availability, and usage cost.
