# Native Build Notes

The implemented dependency direction is:

```text
Unity public API -> native provider -> C ABI -> platform bridge -> shared Swift core
```

The shared Swift actor owns Foundation Models sessions and tasks. The C ABI translates UTF-8 inputs and JSON events without exposing Swift types. C# assigns request IDs, captures callback scheduling, and uses a single registry for completion, streaming order, timeout, cancellation, late-event rejection, and cleanup.

The iOS postprocessor copies the shared Swift sources into `UnityFramework`, weak-links `FoundationModels.framework`, configures Swift modules, and enforces a minimum iOS 26 deployment target. It is idempotent across repeated exports.

The exported C symbols are:

```text
AFM_SetEventCallback
AFM_SetDebugLogging
AFM_GetAvailability
AFM_GenerateText
AFM_StreamText
AFM_CancelRequest
```

Apple streams cumulative response snapshots. The Swift core validates that snapshots are monotonic and emits only the appended delta to C#.

Unity-side ABI and iOS-target postprocessor tests run on Windows with iOS Build Support installed. Final Swift compilation, Xcode linking, and eligible-device behavior must be validated on macOS before v0.1 release.
