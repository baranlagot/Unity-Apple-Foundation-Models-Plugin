# Getting Started

## Install locally

1. In Unity 6 (6000.0) or newer, open **Window > Package Manager**.
2. Select **Add package from disk**.
3. Select this repository's `package.json`.
4. Import one of the samples from the package details pane. The package now ships availability, text, streaming, JSON, and device-validation samples.

## Run in the Editor

The default editor provider is deterministic and clearly labels its output as mock data. The sample scenes now use a shared UI Toolkit diagnostic shell and presenter-based logic, but the public runtime API is unchanged. No Apple hardware is required to exercise the core requests locally.

Configure project defaults under **Edit > Project Settings > Apple Foundation Models**. Disabling the Editor mock makes availability return `UnsupportedPlatform` unless a custom provider is registered.

## Use a custom provider

Implement `IAppleFoundationModelsProvider`, register it with `AppleFoundationModels.SetProvider`, and call `ResetProvider` to restore the platform default.

## Build to iOS

1. Switch the Unity build target to iOS.
2. Build the Unity project normally.
3. The postprocessor copies the shared Swift core, links `FoundationModels.framework` weakly, configures Swift, and raises deployment targets below iOS 26.
4. Validate the generated project locally with `./scripts/validate_exported_ios_project.sh <unity-editor-path>`.
5. Open the generated project in a compatible Xcode version on macOS and build for an eligible device when collecting final release evidence.
6. Call `GetAvailabilityAsync` before generation because Apple Intelligence can be disabled or the model may still be downloading.

Native macOS integration is planned for v0.2. Other unsupported players return `UnsupportedPlatform` and can use a custom provider.

## Troubleshooting

- **Mock output appears on an Apple development machine:** the Unity Editor intentionally uses the mock provider.
- **Unsupported platform:** register a custom provider or run the code in the Editor for mock behavior.
- **Need a local release-hardening pass:** run the scripts from the repository root in this order: Swift bridge, Unity Edit Mode default, Unity Edit Mode iOS, exported iOS project, release metadata.
- **JSON parsing failed:** use a `[Serializable]` class with Unity-serializable fields. `JsonUtility` does not support arbitrary dictionaries or top-level arrays.
