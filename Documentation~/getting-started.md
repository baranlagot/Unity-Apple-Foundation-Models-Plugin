# Getting Started

## Install locally

1. In Unity 2022.3+, open **Window > Package Manager**.
2. Select **Add package from disk**.
3. Select this repository's `package.json`.
4. Import one of the samples from the package details pane.

## Run in the Editor

The default editor provider is deterministic and clearly labels its output as mock data. Call `GetAvailabilityAsync`, `GenerateTextAsync`, or `StreamTextAsync` from a MonoBehaviour. No Apple hardware is required.

## Use a custom provider

Implement `IAppleFoundationModelsProvider`, register it with `AppleFoundationModels.SetProvider`, and call `ResetProvider` to restore the platform default.

## Build targets

Native iOS and macOS integrations are not part of the current managed-core snapshot. Unsupported players return `UnsupportedPlatform` and can use a custom provider. Native setup instructions will be expanded with the iOS bridge milestone.

## Troubleshooting

- **Mock output appears on an Apple development machine:** the Unity Editor intentionally uses the mock provider.
- **Unsupported platform:** register a custom provider or run the code in the Editor for mock behavior.
- **JSON parsing failed:** use a `[Serializable]` class with Unity-serializable fields. `JsonUtility` does not support arbitrary dictionaries or top-level arrays.
