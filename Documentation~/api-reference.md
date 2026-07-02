# API Reference

## `AppleFoundationModels`

Static convenience facade for the default client. It exposes availability, text generation, streaming, JSON generation, `SetProvider`, and `ResetProvider`.

## `IAppleFoundationModelsClient` and `AppleFoundationModelsClient`

The injectable use-case boundary and its default implementation. Construct a client directly when a component should avoid global provider state or when writing tests.

## `IAppleFoundationModelsProvider`

Platform and fallback adapter contract. Providers implement availability, one-shot generation, and streaming. Provider implementations must honor cancellation, preserve streaming order, and invoke at most one terminal callback.

## `AppleFoundationModelsOptions`

Per-request options: instructions, temperature, maximum output tokens, session ID, structured-output preference, and editor mock preference. The client snapshots options before passing them to a provider.

## Availability, result, and exception types

`AppleFoundationModelsAvailability` carries a typed status and user-facing message. `AppleFoundationModelsResult` represents successful or failed generation. `AppleFoundationModelsException` carries an availability status and optional inner exception.

## Errors and cancellation

Blank prompts, missing required callbacks, and invalid option values are rejected before provider invocation. One-shot methods propagate cancellation through their returned task. Streaming providers receive the same cancellation token.
