# Development Plan

This plan turns the task backlog into dependency-ordered increments. The design uses a small static facade as a composition root only; behavior lives in injected clients, providers, serializers, validators, and native adapters.

## Architecture principles

- Depend on abstractions at integration boundaries: native Apple APIs, JSON serialization, and fallback providers.
- Keep the public facade thin. `AppleFoundationModelsClient` owns use-case orchestration and is independently testable.
- Use immutable result and availability value objects. Copy mutable request options at the API boundary.
- Keep platform checks and native imports outside domain-facing code.
- Give every native request a unique ID and make one component responsible for its lifecycle.
- Add features vertically: API, implementation, tests, sample, and documentation land together.

## Milestone 1 — Package and editor-first core

Tasks: TASK-001–010, TASK-024, TASK-034, TASK-037, TASK-039, TASK-044, TASK-046.

1. Establish the UPM layout, assembly definitions, manifest, license, and contributor files.
2. Implement immutable availability/result/error models and validated options snapshots.
3. Implement an injectable client and provider abstraction behind the static facade.
4. Add deterministic mock and unsupported-platform providers.
5. Add JSON deserialization behind an adapter and actionable parse errors.
6. Cover orchestration, validation, streaming, cancellation, JSON, and provider replacement with Unity tests.

Exit condition: the package imports in Unity 2022.3, editor tests pass, and public APIs work without Apple hardware.

`TestProject~` is a small local validation host for running package tests without turning the package root into a Unity project. Unity ignores tilde-suffixed folders when importing the package.

## Milestone 2 — Samples and settings

Tasks: TASK-019, TASK-030–035, TASK-047–049.

1. Add project-scoped settings and a settings-backed default provider policy.
2. Build script-first sample components, then scenes for availability, text, streaming, and JSON.
3. Keep UI code dependent on the public client contract so sample behavior is testable.
4. Add editor tests for settings persistence and default provider resolution.

Exit condition: all samples run in the Unity Editor with deterministic mock output.

## Milestone 3 — iOS native vertical slice

Tasks: TASK-011–014, TASK-017, TASK-020–023, TASK-036, TASK-041, TASK-045.

1. Confirm the supported Apple SDK/Xcode deployment matrix before locking native API signatures.
2. Put Foundation Models calls in a shared Swift core; keep the Objective-C++ layer limited to C ABI conversion.
3. Add a C# native provider, request registry, callback dispatcher, cancellation, and timeouts.
4. Make callback completion idempotent and remove request state on every terminal path.
5. Add post-process build configuration and a device integration scene.

Exit condition: availability, one-shot generation, streaming, cancellation, and JSON work in an exported iOS project with no manual Xcode edits.

## Milestone 4 — v0.1 hardening and release

Tasks: TASK-002, TASK-038–043, TASK-050.

1. Complete API/native documentation and troubleshooting guides.
2. Add structural, manifest, formatting, and Unity test CI checks.
3. Validate unsupported targets compile and return clear availability states.
4. Run an iOS device test matrix, update the changelog, tag, and publish v0.1.0.

Exit condition: the TASK-050 acceptance criteria are met and reproducible from a clean Unity project.

## Milestone 5 — v0.2 extensions

Tasks: TASK-015–016, TASK-018, TASK-025–029, TASK-051–054.

Add macOS parity first, then C#-managed sessions. Research tool calling before exposing a public contract. Keep cloud fallback implementations in samples so the core remains provider-neutral.

## First implementation slice

The current slice implements the package skeleton and editor-first runtime core (TASK-001, TASK-003–010, the initial TASK-024 helper, and focused TASK-034/TASK-044 coverage). Native code deliberately follows after the managed contracts are executable and tested.

## Progress

- Completed: package scaffold, public managed API, provider abstraction, mock/unsupported providers, JSON helper, custom provider hook, and runtime tests.
- Completed: TASK-019 Project Settings persistence/UI and settings-aware Editor provider selection.
- Planned next: managed native request infrastructure with a fake transport, followed by the Swift/iOS bridge and executable sample scenes.

## v0.1 execution plan

Plan baseline: 2026-07-02.

### Scope decisions

- v0.1 targets the stable iOS 26-era Foundation Models API and Unity 2022.3+ with IL2CPP. Newer beta-only Foundation Models APIs stay out of the first release.
- Native macOS parity, tool calling, native session persistence, schema reflection, and a cloud fallback implementation remain v0.2 work.
- v0.1 structured JSON remains provider-neutral prompt-plus-parse behavior. Swift `Generable` types cannot be synthesized from arbitrary C# types at runtime.
- Each stateless request owns a separate Swift `LanguageModelSession`. The bridge never shares one session between concurrent calls because a session only supports one active response.
- The native ABI uses a registered C callback rather than `UnitySendMessage`. This avoids scene objects, survives scene changes, and keeps the bridge testable outside Unity UI.
- Apple streaming is snapshot-based. The Swift core converts cumulative snapshots into ordered text deltas before crossing the ABI.

Official SDK references used for this baseline:

- [Checking model availability](https://developer.apple.com/documentation/foundationmodels/generating-content-and-performing-tasks-with-foundation-models)
- [LanguageModelSession](https://developer.apple.com/documentation/foundationmodels/languagemodelsession)
- [GenerationOptions](https://developer.apple.com/documentation/foundationmodels/generationoptions)
- [Foundation Models framework overview](https://developer.apple.com/documentation/foundationmodels)

### Slice 1 — Managed native request infrastructure

Tasks: TASK-013, TASK-014, TASK-022, TASK-023, TASK-044, TASK-046.

Add these runtime boundaries:

```text
NativeAppleFoundationModelsProvider
  -> INativeFoundationModelsTransport
      -> AppleFoundationModelsNativeTransport
  -> NativeRequestRegistry
      -> PendingAvailabilityRequest
      -> PendingTextRequest
      -> PendingStreamRequest
  -> ICallbackScheduler
```

Implementation steps:

1. Define an internal native event envelope with `requestId`, event type, payload, status, error code, and user-facing error message.
2. Implement an injectable transport interface for availability, text, streaming, cancellation, and debug logging.
3. Implement a thread-safe request registry with exactly-once terminal transitions.
4. Capture the caller's Unity synchronization context and marshal tokens/completion back to it in order.
5. Link caller cancellation and configured timeout to the same terminal cleanup path and native cancel call.
6. Ignore late, duplicate, and unknown native callbacks safely; log request IDs only when debug logging is enabled.
7. Keep all `DllImport` calls behind `UNITY_IOS && !UNITY_EDITOR` guards.

Required tests:

- availability/text/stream success through a fake transport;
- ordered streaming deltas and exactly one completion;
- caller cancellation and timeout both call native cancel once;
- late callbacks after cancellation are ignored;
- duplicate terminal callbacks do not re-complete tasks;
- malformed/unknown native events produce contextual errors without leaking internals;
- unsupported targets never touch native imports;
- settings changes do not replace a custom provider.

Exit gate: the complete C# native lifecycle passes Editor tests using only a fake transport.

### Slice 2 — Shared Swift core and iOS C ABI

Tasks: TASK-011, TASK-012, TASK-020, TASK-021.

Native layout:

```text
Native~/Sources/AppleFoundationModelsCore/
  AppleFoundationModelsCore.swift
  AppleFoundationModelsModels.swift
  AppleFoundationModelsErrorMapper.swift
Plugins/iOS/
  AppleFoundationModelsBridge.h
  AppleFoundationModelsBridge.mm
  AppleFoundationModelsBridge.swift
```

Stable C ABI:

```c
typedef void (*AFM_EventCallback)(const char* eventJson);

void AFM_SetEventCallback(AFM_EventCallback callback);
void AFM_SetDebugLogging(bool enabled);
void AFM_GetAvailability(const char* requestId);
void AFM_GenerateText(const char* requestId, const char* prompt, const char* optionsJson);
void AFM_StreamText(const char* requestId, const char* prompt, const char* optionsJson);
void AFM_CancelRequest(const char* requestId);
```

Implementation steps:

1. Map `SystemLanguageModel.default.availability` to `Available`, `UnsupportedDevice`, `AppleIntelligenceDisabled`, `ModelNotReady`, or `Unknown`.
2. Translate C# temperature to Apple's valid `0...1` range and `MaxOutputTokens` to `maximumResponseTokens`; reject invalid options before native invocation.
3. Create one Swift `Task` and one `LanguageModelSession` per generation request, tracked by request ID in an actor-isolated store.
4. Convert `respond(to:options:)` output into a text completion event.
5. Convert `streamResponse(to:options:)` cumulative snapshots into deltas, followed by one completion event containing the full response.
6. Cancel and remove Swift tasks idempotently. Never emit callbacks after a request reaches a terminal state.
7. Map Foundation Models errors to stable package error codes; put technical details behind the debug setting.
8. Specify callback memory ownership: native owns event memory for the duration of the callback and C# copies it synchronously.

Exit gate: a small native harness compiles against the selected stable Xcode SDK and verifies availability, response, stream delta conversion, cancellation, and error mapping.

### Slice 3 — Unity iOS build integration

Tasks: TASK-017 and TASK-045.

1. Implement `AppleFoundationModelsPostProcessBuild` with a single-purpose Xcode project configurator.
2. Copy shared Swift sources into the generated project, add them to the UnityFramework target, and set the required Swift version.
3. Link the Foundation Models framework weakly and preserve runtime OS availability checks.
4. Apply the minimum supported iOS version only when lower than the package requirement; log every changed build setting.
5. Add Editor validation that reports missing source files or incompatible build settings before export.
6. Keep the postprocessor idempotent so repeated exports do not duplicate files, phases, or framework entries.

Exit gate: a clean Unity iOS export builds in Xcode without manual project edits.

### Slice 4 — Complete executable samples

Tasks: TASK-030–033 and TASK-047–049.

1. Use runtime UI Toolkit to avoid adding a uGUI dependency to the core package.
2. Separate each sample's presenter/controller from its `MonoBehaviour` and inject `IAppleFoundationModelsClient` for tests.
3. Add importable scenes for availability, one-shot text, streaming/cancel, and JSON quest generation.
4. Disable submit actions while a request is active and show friendly unavailable/error states.
5. Keep mock output visibly marked and make every sample work before native hardware is involved.

Exit gate: all four imported sample scenes run in the Editor and their controllers pass Edit Mode tests.

### Slice 5 — Device integration and hardening

Tasks: TASK-036, TASK-038–041, and remaining TASK-044–046 work.

1. Add a device integration scene that runs availability, text, streaming, cancellation, timeout, and JSON checks with a copyable report.
2. Test on at least one eligible device and one unavailable configuration.
3. Verify background/foreground transitions, scene changes, repeated requests, concurrent request IDs, and late cancellation.
4. Finish native troubleshooting, error mapping, platform support, API, and getting-started documentation.
5. Record the exact Unity, Xcode, iOS, device, and Foundation Models API versions used for release validation.

Exit gate: the TASK-050 user journey works from a clean clone and a clean Unity iOS export.

### Slice 6 — CI and v0.1 release

Tasks: TASK-042, TASK-043, TASK-050.

1. Add package/JSON/assembly-definition validation and Unity Edit Mode tests on pull requests.
2. Add a macOS CI job that compiles the Swift core and C ABI against the pinned stable SDK without requiring model hardware.
3. Add release validation for package version, changelog entry, Git tag, and UPM sample paths.
4. Publish `v0.1.0` only after the device test report and clean-install test pass.

### Planned commit boundaries

```text
feat: add native request protocol and lifecycle
test: cover native callback, cancellation, and timeout races
feat: add shared Foundation Models Swift core
feat: add iOS C ABI bridge
build: configure exported iOS projects
feat: add executable sample scenes
test: add iOS device integration harness
ci: validate Unity package and native bridge
docs: complete v0.1 native and release documentation
```

Every commit must compile on unsupported Unity targets. Native implementation commits must not weaken the mock/custom provider paths.
