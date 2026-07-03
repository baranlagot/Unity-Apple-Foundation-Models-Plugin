# Changelog

All notable changes to this project are documented here.

## [Unreleased]

## [0.1.0] - 2026-07-03

### Added

- Public managed API with immutable availability/result models, provider injection, JSON helpers, deterministic mock behavior, and unsupported-platform handling.
- Shared Swift Foundation Models core, iOS C ABI bridge, native request lifecycle, cancellation, timeout plumbing, and idempotent Xcode postprocessor integration.
- Reusable UI Toolkit diagnostic shell, presenter-based availability/text/streaming/JSON samples, and a device validation sample with privacy-safe report copying.
- Local validation scripts for Swift bridge checks, Unity Edit Mode suites, exported iOS project validation, and release metadata consistency.
- GitHub Actions validation for Swift bridge checks, Unity Edit Mode suites, and release metadata verification.

### Changed

- Sample scenes now use thin `MonoBehaviour` bindings over shared presenters instead of one-off IMGUI controllers.
- Native bridge validation now includes a compiled Swift harness and strict-concurrency type-checking with warnings treated as failures.
