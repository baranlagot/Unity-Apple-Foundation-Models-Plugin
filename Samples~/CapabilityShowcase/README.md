# Capability Showcase

A single, self-contained scene that demonstrates every capability of the package on a
real device or the iOS Simulator. It uses IMGUI so it renders reliably without any UI
theme, prefab, or font asset.

Open `CapabilityShowcase.unity` and press Play (Editor uses the deterministic mock), or
build it to a device / the Simulator / a native macOS app.

## What it demonstrates

- **Foundation Models** (requires Apple Intelligence): availability, one-shot text
  generation, token streaming with cancellation, structured JSON output, and a full
  device-validation run. Options for system instructions, temperature, and output length.
- **Vision** (no Apple Intelligence required; works in the Simulator): image
  classification and text recognition (OCR) of a captured screen.
- **Image Playground** (requires Apple Intelligence): on-device image generation from a
  text prompt, displayed inline.

## Layout

Availability → prompt and presets → capability buttons → **Output** (where results and
streamed tokens appear) → options → event log.

## Notes

- Vision and Image Playground run on iOS device/Simulator and native macOS players.
- On a macOS standalone build, the native provider is active, so a Mac with Apple
  Intelligence runs the real on-device model. The Unity Editor keeps the mock.
- This sample depends on the `com.unity.modules.screencapture` and
  `com.unity.modules.imageconversion` modules for the Vision and image-display features.
