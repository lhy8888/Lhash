# Core Build Entry Point

## Scope

M2 introduces a core-only cross-platform build entry point for LHash.

This entry point is intentionally narrower than the maintained Windows MFC release line:

- Windows x64: core build + baseline smoke run
- Windows arm64: core build verification
- macOS arm64: core build + baseline smoke run

It does not replace the MFC Windows mainline and it does not bring back WinUI or the CLR bridge as release-path dependencies.

## What it builds

The core build target is centered around:

- shared engine and runtime code
- baseline algorithms
- portable third-party hash implementations
- platform-specific OS shims for Windows and Darwin

The M2 entry point intentionally keeps OpenSSL optional so the core can still build even when the vendor backend is not enabled.

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
cmake --build build/core/windows-x64 --config Release --target lhash_core_smoke --parallel
ctest --test-dir build/core/windows-x64 --output-on-failure -C Release -R lhash_core_smoke
```

### Windows arm64

```powershell
cmake -S . -B build/core/windows-arm64 -G "Visual Studio 17 2022" -A ARM64 -DLHASH_BUILD_M2_SMOKE=ON
cmake --build build/core/windows-arm64 --config Release --target lhash_core_smoke --parallel
```

### macOS arm64

```powershell
cmake -S . -B build/core/macos-arm64 -G Xcode -DCMAKE_OSX_ARCHITECTURES=arm64 -DLHASH_BUILD_M2_SMOKE=ON
cmake --build build/core/macos-arm64 --config Release --target lhash_core_smoke --parallel
ctest --test-dir build/core/macos-arm64 --output-on-failure -C Release -R lhash_core_smoke
```

## Smoke coverage

The `lhash_core_smoke` target verifies a minimal fixed-input set for:

- MD5
- SHA-1

That smoke target is intentionally baseline-only for M2. It proves the new
core entry point can build and run the portable baseline algorithms without
pulling the Windows MFC release line back into the core build. The additional
provider algorithms remain built in the core target, but they are not part of
the M2 smoke contract.
