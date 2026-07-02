# Native Build Notes

The native bridge is the next implementation milestone.

The planned dependency direction is:

```text
Unity public API -> native provider -> C ABI -> platform bridge -> shared Swift core
```

The shared Swift core will own Foundation Models behavior. Platform bridge code will only translate C-compatible requests and callbacks. C# will assign request IDs and a single registry will own completion, streaming order, timeout, cancellation, and cleanup.

Apple SDK availability checks and exact Foundation Models signatures must be verified against the targeted Xcode SDK before native symbols are committed.
