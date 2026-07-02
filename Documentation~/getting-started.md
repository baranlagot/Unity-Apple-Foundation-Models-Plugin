# Getting Started

## Install locally

1. In Unity 2022.3+, open **Window > Package Manager**.
2. Select **Add package from disk**.
3. Select this repository's `package.json`.
4. Import one of the samples from the package details pane.

## Run in the Editor

The default editor provider is deterministic and clearly labels its output as mock data. Call `GetAvailabilityAsync`, `GenerateTextAsync`, or `StreamTextAsync` from a MonoBehaviour. No Apple hardware is required.

Configure project defaults under **Edit > Project Settings > Apple Foundation Models**. Disabling the Editor mock makes availability return `UnsupportedPlatform` unless a custom provider is registered.

## Use a custom provider

Implement `IAppleFoundationModelsProvider`, register it with `AppleFoundationModels.SetProvider`, and call `ResetProvider` to restore the platform default.

## Build to iOS

1. Switch the Unity build target to iOS.
2. Build the Unity project normally.
3. The postprocessor copies the shared Swift core, links `FoundationModels.framework`, configures Swift, and raises deployment targets below iOS 26.
4. Open the generated project in a compatible Xcode version on macOS and build for an eligible device.
5. Call `GetAvailabilityAsync` before generation because Apple Intelligence can be disabled or the model may still be downloading.

Final Swift/Xcode and device validation is still required for this development snapshot. Native macOS integration is planned for v0.2. Other unsupported players return `UnsupportedPlatform` and can use a custom provider.

## Troubleshooting

- **Mock output appears on an Apple development machine:** the Unity Editor intentionally uses the mock provider.
- **Unsupported platform:** register a custom provider or run the code in the Editor for mock behavior.
- **JSON parsing failed:** use a `[Serializable]` class with Unity-serializable fields. `JsonUtility` does not support arbitrary dictionaries or top-level arrays.
