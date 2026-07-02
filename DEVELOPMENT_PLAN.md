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
- In progress next: sample scenes and UI behavior for TASK-030–033, followed by the iOS native vertical slice.
