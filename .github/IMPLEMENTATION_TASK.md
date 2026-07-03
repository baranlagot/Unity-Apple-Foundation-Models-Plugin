# TASK-050 — Complete v0.1 iOS release hardening

## Goal

Ship `0.1.0` with a verified native iOS path, repeatable release checks, Unity 2022.3 and Unity 6 compatibility evidence, and device evidence from both eligible and unavailable configurations. Preserve the provider abstractions, deterministic Editor mock, custom-provider path, and unsupported-platform behavior.

## Current status

Verified locally on 2026-07-03 at `4b8bac4` with Unity `6000.0.61f1`, Xcode `26.3`, Swift `6.2.4`, and the iOS `26.2` SDK.

### Repository state

- `main` is three commits ahead of `origin/main`: native bridge hardening, reusable diagnostic/device-validation flows, and release validation automation.
- This task file is not yet committed.
- `package.json`, runtime release metadata, and `CHANGELOG.md` all declare `0.1.0`; no `v0.1.0` tag exists.
- The changelog currently presents `0.1.0` as released even though the release acceptance criteria are not complete.

### Verified passing

- Strict Swift type-checking succeeds with complete concurrency checks and warnings treated as errors.
- The native Swift harness (now compiled and executed as a real `@main` executable) passes event encoding, the full bridge-owned stable error-code set, snapshot-to-delta conversion, invalid options, duplicate request IDs, and cancel-before-start behavior.
- A clean Unity 6 iOS export succeeds without manual Xcode edits.
- The generated `UnityFramework` project contains one copy of each shared Swift core file, weak-links `FoundationModels.framework`, and builds and links with code signing disabled.
- Release metadata validation passes when no expected Git tag is supplied.
- All shell scripts pass Bash syntax validation.

### Recently fixed (2026-07-03)

- Unity 6 normal Editor target: **46/46 pass, 0 fail**.
- Unity 6 iOS target: **48/48 pass, 0 fail**.
- The three former failures are resolved:
  - `SampleRequestPresenter_DisablesPrimaryActionUntilPromptIsSet`: `SampleDiagnosticPresenterBase.Initialize` now computes primary-action availability before the first render, so an empty prompt renders disabled;
  - `StreamingPresenter_WhenCancelled_RendersWarningState`: `CancelActiveRequest` clears the field before cancelling/disposing, so the synchronous cancellation continuation cannot double-dispose the `CancellationTokenSource`;
  - `RunAsync_WhenMockProviderIsActive_ExercisesFunctionalScenarios`: the JSON fake now deserializes a JSON payload into the runner-requested generic type via `JsonUtility.FromJson<T>` instead of casting a test-private type.
- `run_unity_editmode_tests.sh` no longer passes `-quit`, selects an explicit build target for both `default` (StandaloneOSX) and `ios`, and delegates result validation to `assert_unity_test_results.sh`, which rejects missing, empty, inconclusive, or non-passing result XML and requires exactly one `<test-run>` root with a positive total.

### Section 2 — automated behavior coverage (2026-07-03)

- Unity 6 normal Editor target: **60/60 pass, 0 fail**; iOS target: **62/62 pass, 0 fail**.
- Presenter coverage now spans the required matrix: initial disabled state, busy-state re-entry (primary action ignored while a run is in flight), unavailable and provider-error states for availability, provider-error state for one-shot requests, ordered streaming with exactly-once completion, and streaming cancellation.
- Device-runner coverage now spans availability failure, outer-token cancellation, single-scenario partial failure, the exactly-once streaming-completion guard, lifecycle observation (focus round-trip vs. not-run), report formatting (clipboard header vs. display body), and privacy-sensitive field exclusion.
- Fixed a re-entrant `CancellationTokenSource` double-dispose in `DeviceValidationPresenter.CancelCurrentRun` (same class of bug as the streaming presenter).
- Privacy hardening: the JSON scenario no longer embeds the generated quest title, and the repeated-request scenario now verifies distinct outputs to match its report claim.
- Native harness now covers the full bridge-owned stable error-code set (`duplicateRequest`, `invalidOptions`, `nonMonotonicStream`, `cancelled`, and the `nativeFailure` fallback) and snapshot-to-delta conversion (ordered deltas, empty-delta suppression, non-monotonic rejection) via an extracted, hardware-independent `AFMStreamAccumulator`.
- **Fixed a false-success defect in `validate_swift_bridge.sh`:** it passed the harness as a non-primary file to the `swift` interpreter, which silently ignored the `@main` entry point and reported success without running a single assertion. The script now compiles a real executable with `swiftc -parse-as-library -warnings-as-errors` and runs it, and the previously latent `events` data race in the harness is fixed with a lock-guarded `Sendable` collector.

### Failing or not yet proven

- Unity `2022.3` is not installed locally, so the advertised minimum version has not been revalidated.
- The GitHub Actions workflow has not run for the local commits. Its Unity jobs require a valid license, and the `swift-and-release` job targets `arm64-apple-macosx26.0` on a `macos-15` runner, so the Xcode/SDK toolchain still needs to be pinned to one that provides the macOS/iOS 26 SDK.
- No eligible-device or unavailable-device report has been recorded.
- The device runner always reports the native timeout scenario as `NotRun` with its default environment.
- On an iOS device the report cannot currently obtain the Xcode version, and `PackageRevision` remains the constant `local-working-copy`.
- Active native cancellation of a running generation is not covered by the harness because it requires an on-device model session; it remains device-evidence work.
- The native harness does not yet exercise the availability status mapping, which reads `SystemLanguageModel.default.availability` directly and would require a testable seam or device state.
- `package.json` still describes native macOS functionality even though native macOS support is planned for v0.2.

## Remaining implementation plan

### 1. Restore trustworthy local test gates — P0

- Fix the initial presenter action state and the streaming cancellation/disposal race.
- Correct the device-validation JSON fake so it tests the generic contract rather than a mismatched private type.
- Remove the premature Unity `-quit`, select an explicit normal target for `default`, and require a result XML file whose root result is passing.
- Make the wrapper propagate Unity test failures and reject missing, empty, or inconclusive result files.
- Rerun Unity 6 normal and iOS Edit Mode suites to zero failures and record the exact totals.

Exit gate: a false-success Unity invocation is impossible, and both Unity 6 suites pass from a clean validation project.

### 2. Complete automated behavior coverage — P0

- Add presenter tests for initial state, busy-state re-entry, unavailable state, provider errors, ordered streaming, exactly-once completion, cancellation, and mock labeling.
- Add device-runner tests for every scenario outcome, outer cancellation, availability failure, partial failures, lifecycle observation, report formatting, and privacy-sensitive field exclusion.
- Expand the Swift harness to cover all availability mappings that do not require hardware, active cancellation, snapshot-to-delta conversion, duplicate terminal behavior, and every documented stable error code.
- Keep all tests runnable with fakes or the deterministic local mock.

Exit gate: the stated acceptance behavior is executable locally without Apple Intelligence hardware.

### 3. Finish device validation evidence capture — P1

- Implement a deterministic native-timeout probe instead of returning `NotRun` on device.
- Decide and implement how build-time Xcode and package revision values enter the device report; do not infer Git or Xcode metadata at runtime.
- Tighten repeated-request assertions so the result matches the report claim, and verify no callback is accepted after cancellation or lifecycle changes.
- Run and save a privacy-safe report on one unavailable configuration.
- Run and save a passing report on an eligible Apple Intelligence device at the minimum supported iOS version.
- Exercise scene reload, background/foreground, repeated requests, concurrent requests, late cancellation, timeout, JSON, streaming, and one-shot generation.

Exit gate: both required device reports exist and contain no device identifiers, prompts, or generated model content.

### 4. Validate compatibility and CI — P1

- Install or provision the minimum supported Unity `2022.3` editor and iOS module.
- Run normal and iOS-targeted Edit Mode suites on Unity `2022.3` and Unity `6000.0.61f1`.
- Confirm unsupported targets compile and cannot invoke native imports.
- Run the workflow and verify that every matrix entry actually executes tests and uploads readable results.
- Pin or explicitly select a CI Xcode toolchain that contains the required iOS 26 SDK.

CI topology is an owner decision before further workflow changes:

1. **Hosted standalone tests plus a Unity-capable macOS iOS job (recommended):** accurately exercises the iOS target and Xcode toolchain, but requires macOS capacity and Unity licensing.
2. **Local Unity compatibility matrix with reduced hosted CI:** keep Swift and metadata checks in hosted CI until a macOS Unity runner is available; avoids a misleading iOS job but provides less automatic coverage.

Exit gate: all four Unity version/target combinations pass, and CI has no skipped or false-success test jobs.

### 5. Close release metadata and documentation — P1

- Correct the package description so it does not advertise native macOS support in v0.1.
- Keep release notes under `Unreleased` until device, compatibility, and CI gates pass; assign the final date only at release time.
- Separate ordinary metadata validation from tag-time validation, and require `v0.1.0` for the release command.
- Record exact validated Unity, Xcode, SDK, iOS, and device-model versions in the release documentation.
- Commit the task update, push the three implementation commits, obtain passing CI, then create `v0.1.0` only after every acceptance item is complete.

Exit gate: package version, release metadata, changelog, documentation, and Git tag all agree.

## Acceptance status

| Criterion | Status |
| --- | --- |
| Strict Swift type-checking with zero warnings | Complete locally |
| Clean Unity iOS export compiles and links | Complete locally on Unity 6 |
| Unity 2022.3 and Unity 6, normal and iOS suites | Unity 6 normal 46/46 and iOS 48/48 pass; Unity 2022.3 unavailable locally |
| Presenter behavior tests and deterministic local mock | Partial |
| Privacy-safe unavailable and eligible device reports | Open |
| Full functional and lifecycle device matrix | Open |
| Unsupported platforms and custom providers unaffected | Partial; automated managed coverage passes, compatibility matrix remains |
| Local validation and agreed CI subset | Partial; local Unity wrapper is defective and CI is unproven |
| Version, changelog, docs, and `v0.1.0` tag agree | Open |

## Separate refactoring plan

Refactoring is recommended because sample presentation code currently compiles into the main runtime assembly, `DeviceValidationRunner` owns eight scenarios plus orchestration and lifecycle state, visual tokens are embedded directly in the view, and request cancellation ownership is repeated across presenters. Keep this work separate from the P0 correctness fixes so behavior changes remain reviewable.

Choose the refactoring scope before implementation:

1. **Targeted pre-release refactor (recommended):** isolate sample diagnostics in their own runtime assembly, split validation scenarios behind a small interface, centralize the existing visual tokens without changing the chosen diagnostic appearance, and introduce one focused request-lifetime helper. Improves API boundaries and testability now, but adds assembly and migration work before release.
2. **Defer structural refactoring to v0.2:** apply only the P0 correctness fixes for v0.1 and record the assembly/scenario split as the first v0.2 task. Reduces immediate release churn, but ships sample-only UI and validation types in the core runtime assembly.

If option 1 is approved:

1. Add a `Baran.AppleFoundationModels.Samples` runtime assembly referencing only the core client contract; update test and imported-sample assembly references.
2. Extract `IDeviceValidationScenario` implementations for availability, generation, streaming, cancellation, timeout, JSON, repeated, concurrent, and late-cancel checks. Keep the runner responsible only for ordering, cancellation, and report aggregation.
3. Extract lifecycle observation and report metadata provision behind injected interfaces.
4. Add a small request-lifetime object that owns cancellation and disposal exactly once; use it in streaming and device presenters.
5. Centralize the current diagnostic color, spacing, and typography values in one theme/configuration module with no visual changes.
6. Run all local mock, presenter, runtime, Swift, and exported-Xcode gates after the assembly move.

Refactor exit gate: the core assembly exposes no sample UI or device-report types, each validation scenario is independently testable, and rendered behavior remains unchanged.

## Out of scope

- Native macOS support.
- Tool calling, persistent or conversational sessions, schema reflection, and Swift `Generable` synthesis from arbitrary C# types.
- A production cloud fallback, authentication, analytics, payments, or remote configuration.
- API expansion unrelated to a defect found during release validation.
