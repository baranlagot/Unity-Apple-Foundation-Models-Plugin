# Native Build Notes

The implemented dependency direction is:

```text
Unity public API -> native provider -> C ABI -> platform bridge -> shared Swift core
```

The shared Swift actor owns Foundation Models sessions and tasks. The C ABI translates UTF-8 inputs and JSON events without exposing Swift types. C# assigns request IDs, captures callback scheduling, and uses a single registry for completion, streaming order, timeout, cancellation, late-event rejection, and cleanup.

The iOS postprocessor copies the shared Swift sources into `UnityFramework`, weak-links `FoundationModels.framework`, configures Swift modules, and enforces a minimum iOS 26 deployment target. It is idempotent across repeated exports and is exercised by the local exported-project validation command.

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

Local validation commands:

```text
./scripts/validate_swift_bridge.sh
./scripts/run_unity_editmode_tests.sh <unity-editor-path> default
./scripts/run_unity_editmode_tests.sh <unity-editor-path> ios
./scripts/validate_exported_ios_project.sh <unity-editor-path>
```

The Swift command type-checks with `-warnings-as-errors` and runs a native harness. The exported-project command drives a Unity iOS export, checks that the shared Swift files are added once to `UnityFramework`, verifies the weak framework link, and builds the generated `UnityFramework` scheme with `xcodebuild`.
