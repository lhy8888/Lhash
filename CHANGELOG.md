# Changelog

All notable LHash release-line changes are documented in this file.

The historical upstream fHash release log is intentionally not duplicated here.
This changelog tracks the maintained LHash release line that starts at `1.10.0`.

## Unreleased

Introduced a fixed-version OpenSSL 3 EVP algorithm family that coexists with the existing built-in SHA-2 implementations.

### Algorithms

- added fixed-version vendored `OpenSSL 3 EVP` integration
- added distinct `SHA-256`, `SHA-384`, and `SHA-512` descriptors without changing the legacy `SHA256` / `SHA512` ids
- added `SHA3-256`, `SHA3-384`, `SHA3-512`, `BLAKE2b-512`, `BLAKE2s-256`, `SHAKE128-256`, and `SHAKE256-512`
- kept the new OpenSSL-backed family isolated behind separate provider and vendor seams under `third_party/openssl`

### Build and licensing

- added a dedicated OpenSSL vendor build step for maintained native builds
- added `GPL-2.0-only` OpenSSL linking exception documentation
- kept the original built-in SHA-2 implementations untouched so either family can be retired later without rewriting the old code

## 1.11.0 - 2026-04-09

Introduced the first maintained release with built-in BLAKE3 variants and deeper runtime hardening coverage.

### Algorithms

- added fixed-version official BLAKE3 C integration
- added `BLAKE3-256`, `BLAKE3-512`, and `BLAKE3 XOF` descriptor variants
- added fixed-version official `XXH3-64`, `XXH3-128`, and `CRC32C` integrations
- benchmark-backed BLAKE3 SIMD decisions now keep:
  - `x64`: `SSE2`, `SSE4.1`, `AVX2`, `AVX512`
  - `Win32`: `SSE2`, `SSE4.1`, `AVX2`
  - `ARM64`: `NEON`

### Runtime and architecture

- strengthened `HashExecutionContext` so the progress sink is modeled as a non-owning observer seam with a null fallback
- expanded native runtime coverage for BLAKE3 uppercase behavior, unknown-id handling, ordering, and concurrent stability
- added native runtime coverage for official `XXH3` and `CRC32C` vectors, unknown-id handling, ordering, and concurrent stability
- kept the maintained native desktop release line lightweight and portable

### Validation

- extended unit, refactor-baseline, and security-regression gates for BLAKE3 behavior and runtime-seam expectations
- extended unit, refactor-baseline, and security-regression gates for fixed-version `XXH3` and `CRC32C` provider coverage
- added dedicated native benchmark coverage for `x64`, `Win32`, and `ARM64`
- current benchmark evidence shows large-file `BLAKE3-256` uplift of roughly:
  - `x64`: `481 MiB/s -> 1641 MiB/s`
  - `Win32`: `390 MiB/s -> 1369 MiB/s`
  - `ARM64`: `532 MiB/s -> 901 MiB/s`

## 1.10.1 - 2026-04-09

Refined the maintained Windows release line after the 1.10.0 reset.

### Desktop experience

- tightened the shipped native desktop layout and command density
- kept the result area stable while preserving task history in the task pane
- fixed export so the visible hash results are written correctly to UTF-8 text files
- moved algorithm controls under settings and renamed the settings entry to `Algorithm Selection`

### Release metadata

- bumped maintained product metadata to `1.10.1`
- aligned legacy, WinUI preview, and UWP preview version resources to `1.10.1.0`

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
