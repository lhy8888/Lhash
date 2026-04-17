# Changelog

All notable LHash release-line changes are documented in this file.

The historical upstream fHash release log is intentionally not duplicated here.
This changelog tracks the maintained LHash release line that starts at `1.10.0`.

## 1.12.2 - 2026-04-17

Follow-up patch release focused on safer defaults, cleaner algorithm UX, and continued runtime hardening.

### Algorithm defaults and UX

- enabled `SHA3-256` by default alongside the maintained OpenSSL `SHA-256` and `SHA-512` defaults
- replaced the one-shot settings submenu for algorithm selection with a dedicated multi-toggle dialog, so multiple algorithms can be changed before closing settings
- fully removed the legacy built-in `SHA256` / `SHA512` implementations from the active code path and kept OpenSSL `SHA-256` / `SHA-512` as the only maintained SHA-2 variants

### Runtime and safety hardening

- made OpenSSL EVP update failures sticky and explicit, so digest update/finalize errors no longer degrade into empty or misleading digest output
- removed the legacy `CSHA1::HashFile()` file-I/O helper that bypassed the hardened `OsFile` path
- cleaned up remaining global scratch and registry snapshot edge cases in digest/result access seams
- tightened Windows legacy helpers by removing stale OS-version detection code and hardening shell/DLL helper behavior

### Input and platform robustness

- unified the per-session file-count limit handling across dialog, drag-and-drop, folder recursion, and `WM_COPYDATA` inputs with explicit user-visible outcomes
- strengthened Win32 long-path handling and continued reducing TOCTOU-style drift in metadata and file-open paths
- hardened the POSIX/Darwin string and file helpers around conversion, `O_CREAT`, and metadata lookup behavior

## 1.12.1 - 2026-04-11

Focused patch release for safety, compatibility, and runtime correctness after the OpenSSL 3 EVP expansion.

### Safety and compatibility

- hardened legacy shell-launch helpers so child processes no longer inherit parent handles by default
- strengthened legacy DLL loading fallback paths so older-system compatibility no longer drops back to the weakest `LoadLibrary` behavior
- fixed ARM64 detection in legacy Windows architecture checks used by shell-extension lookup
- corrected context-menu removal result handling so registry-delete failures cannot be folded into false success states

### Runtime correctness

- completed the hash algorithm descriptor registry snapshot cleanup so descriptor enumeration no longer leaks shared live views across threads
- fixed remaining registry initialization edge cases that could surface during native runtime tests
- removed stale duplicated macro residue from the legacy `sha256.cpp` implementation

### Delivery and CI

- kept the maintained native desktop release path fast by reusing cached OpenSSL vendor outputs across routine builds
- preserved WinUI preview availability without putting preview bridge jobs back onto every ordinary `push`

## 1.12.0 - 2026-04-10

Introduced a fixed-version OpenSSL 3 EVP algorithm family alongside deeper runtime hardening and repository cleanup.

### Algorithms

- added fixed-version vendored `OpenSSL 3 EVP` integration
- added distinct `SHA-256`, `SHA-384`, and `SHA-512` descriptors without changing the legacy `SHA256` / `SHA512` ids
- added `SHA3-256`, `SHA3-384`, `SHA3-512`, `BLAKE2b-512`, `BLAKE2s-256`, `SHAKE128-256`, and `SHAKE256-512`
- kept the new OpenSSL-backed family isolated behind separate provider and vendor seams under `third_party/openssl`

### Build and licensing

- added a dedicated OpenSSL vendor build step for maintained native builds
- added `GPL-2.0-only` OpenSSL linking exception documentation
- kept the original built-in SHA-2 implementations untouched so either family can be retired later without rewriting the old code

### Security and maintenance

- hardened Windows version metadata extraction against malformed PE version resources
- tightened `WM_COPYDATA` sender validation and command payload parsing
- removed stale duplicated macro remnants from the legacy `sha256.cpp` source
- archived old UWP, WAP, macOS, and deprecated shell/bridge platform trees out of the live repository surface

### CI and delivery

- trimmed normal `push` builds down to the maintained native desktop path
- cached the vendored OpenSSL build output so routine Actions runs stay fast

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
