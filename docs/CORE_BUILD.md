# Core Build Entry Point

## Scope

M2 introduces a core-only cross-platform build entry point for LHash.

This entry point is intentionally narrower than the maintained Windows MFC release line:

- Windows x64: core build + baseline smoke run + engine link smoke
- Windows arm64: core build verification only
- macOS arm64: core build + baseline smoke run + engine link smoke + Darwin security regression

It does not replace the MFC Windows mainline and it does not bring back WinUI or the CLR bridge as release-path dependencies.

M3 extends this entry point so macOS arm64 is not just buildable, but also has
an explicit Darwin path-security contract and a macOS-specific security
regression check.

## What it builds

The core build target is centered around:

- shared engine and runtime code
- baseline algorithms
- portable third-party hash implementations
- platform-specific OS shims for Windows and Darwin

The M2 entry point intentionally keeps OpenSSL optional so the core can still build even when the vendor backend is not enabled.

For M3, the macOS bring-up remains baseline-only. OpenSSL extension backends are
still optional and are not required for the macOS support contract.

## What it does not build

The core entry point does not include:

- WinUI
- `fHashClrBridge`
- MFC UI targets
- Linux targets
- macOS GUI targets
- OpenSSL 4.0 migration work

## How to build

### Windows x64

```powershell
cmake -S . -B build/core/windows-x64 -G "Visual Studio 17 2022" -A x64 -DLHASH_BUILD_M2_SMOKE=ON
cmake --build build/core/windows-x64 --config Release --target lhash_core_smoke lhash_engine_link_smoke --parallel
ctest --test-dir build/core/windows-x64 --output-on-failure -C Release -R "^(lhash_core_smoke|lhash_engine_link_smoke)$"
```

### Windows arm64

```powershell
cmake -S . -B build/core/windows-arm64 -G "Visual Studio 17 2022" -A ARM64 -DLHASH_BUILD_M2_SMOKE=ON
cmake --build build/core/windows-arm64 --config Release --target lhash_core_smoke --parallel
```

This path is build-verification only in CI: the smoke target is compiled on
Windows arm64, but it is not executed there yet.

### macOS arm64

```powershell
cmake -S . -B build/core/macos-arm64 -G Xcode -DCMAKE_OSX_ARCHITECTURES=arm64 -DLHASH_BUILD_M2_SMOKE=ON
cmake --build build/core/macos-arm64 --config Release --target lhash_core_smoke lhash_engine_link_smoke --parallel
ctest --test-dir build/core/macos-arm64 --output-on-failure -C Release -R "^(lhash_core_smoke|lhash_engine_link_smoke)$"
```

## Smoke coverage

The `lhash_core_smoke` target verifies a minimal fixed-input set for:

- MD5
- SHA-1
- BLAKE3

That smoke target is intentionally baseline-only for M2. It proves the new
core entry point can build and run the portable baseline algorithms without
pulling the Windows MFC release line back into the core build.

The `lhash_engine_link_smoke` target is the companion linkability check for the
same core entry point. It verifies that `RunHashRequest()` remains linkable and
that an empty request can traverse the public engine entry point without
reintroducing the Windows UI mainline.

On Apple arm64, `lhash_core_smoke` still exercises the NEON implementation path
while checking the same canonical digest values. The additional provider
algorithms remain built in the core target, but they are not part of the M2
smoke contract.

## M3 macOS support

M3 adds the macOS-specific security regression target:

- `lhash_darwin_security`

This target verifies the Darwin hashing contract around symbolic-link rejection,
regular-file acceptance, and descriptor-based validation. It is intentionally
smaller than the Windows security regression surface and exists to prove that
the macOS core support is real rather than merely buildable.

## Related macOS CLI MVP

The macOS arm64 CLI MVP workflow, [`macOS CLI MVP Build workflow`](../.github/workflows/macos-cli-build.yml),
builds `lhash_cli` on top of the core entry point and publishes a `tar.gz`
validation artifact. It is separate from the core matrix workflow and is not
part of the Windows MFC release line.
