# Changelog

All notable LHash release-line changes are documented in this file.

The historical upstream fHash release log is intentionally not duplicated here.
This changelog tracks the maintained LHash release line that starts at `1.10.0`.

## 1.10.0 - 2026-03-30

Initial maintained LHash release line.

### Release and delivery

- reset the maintained product version to `1.10.0`
- added a formal GitHub release path for `v*` tags
- added branch-based release rehearsal bundles and release manifests
- kept optional Authenticode signing support in CI

### Security and hardening

- hardened command-line and `WM_COPYDATA` input handling in the Windows desktop path
- hardened shell integration and release packaging flow
- removed Windows-side third-party hash submission entry points from the maintained release line

### Architecture

- introduced `HashRequest`, `HashResult`, `ProgressEvent`, `HashProgressSink`, and `HashExecutionContext`
- moved the core execution path onto `HashResult`-driven contracts
- routed WinUI native builds through `fHashNativeCore`
- reduced legacy `ResultData` compatibility layers to compatibility shims

### Validation

- added an independent xUnit unit-test framework
- added native C++ runtime tests
- gated Windows native builds on unit tests, security regression, and native runtime tests
- exercised the publish-release chain in CI before the formal tag release
