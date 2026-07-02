# Apple Foundation Models for Unity

The missing Unity bridge for Apple's on-device Foundation Models.

Apple Foundation Models for Unity is an experimental, open-source Unity package that provides a C# API for availability checks, text generation, streaming, structured JSON, and replaceable fallback providers.

> This is an independent community project. It is not an official Apple package and it does not expose every Apple Intelligence system feature.

## Current status

The editor-first managed core is implemented. It includes the public API, deterministic mock provider, custom provider injection, Project Settings, validation, streaming, and JSON parsing. The native iOS and macOS providers are planned and are not yet available in this development snapshot.

## Requirements and platform support

- Unity 2022.3 or newer.
- Unity Editor: deterministic mock provider.
- iOS/macOS: managed API currently compiles; native Foundation Models integration is the next milestone.
- Windows, Android, Linux, and WebGL: custom provider support; no native Apple model access.

## Install

Once this repository is published, add its Git URL in Unity Package Manager using **Add package from git URL**. During local development, use **Add package from disk** and select `package.json`.

## Use

```csharp
using Baran.AppleFoundationModels;

var availability = await AppleFoundationModels.GetAvailabilityAsync();
if (!availability.IsAvailable)
{
    UnityEngine.Debug.LogWarning(availability.Message);
    return;
}

var result = await AppleFoundationModels.GenerateTextAsync(
    "Generate one short funny NPC line for a cozy cat cafe game.");
UnityEngine.Debug.Log(result.Text);
```

Streaming uses token, completion, and error callbacks:

```csharp
await AppleFoundationModels.StreamTextAsync(
    "Name ten playful orange cats.",
    chunk => output.text += chunk,
    result => UnityEngine.Debug.Log("Complete"),
    error => UnityEngine.Debug.LogException(error));
```

Simple Unity-serializable classes can be populated from generated JSON:

```csharp
[System.Serializable]
public sealed class QuestData
{
    public string title;
    public string objective;
    public int rewardCoins;
}

QuestData quest = await AppleFoundationModels.GenerateJsonAsync<QuestData>(
    "Generate a short cozy fetch quest.");
```

## Custom providers

Implement `IAppleFoundationModelsProvider` and register it at application startup:

```csharp
AppleFoundationModels.SetProvider(myProvider);
// Later: AppleFoundationModels.ResetProvider();
```

This hook supports deterministic tests and optional local or cloud fallbacks without coupling the core package to any vendor. Review the privacy and cost behavior of any fallback before shipping it.

## Project Settings

Open **Edit > Project Settings > Apple Foundation Models** to configure the Editor mock provider, native debug logging, default native timeout, and fallback policy. Settings are stored with the Unity project and exposed at runtime through the immutable `AppleFoundationModels.Configuration` snapshot.

## Project docs

- [Development plan](DEVELOPMENT_PLAN.md)
- [Getting started](Documentation~/getting-started.md)
- [Platform support](Documentation~/platform-support.md)
- [API reference](Documentation~/api-reference.md)
- [Native build notes](Documentation~/native-build-notes.md)

Licensed under the [MIT License](LICENSE).
