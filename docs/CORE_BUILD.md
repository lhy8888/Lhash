# Core Build Entry Point

## Scope

M2 introduces a core-only cross-platform build entry point for LHash.

This entry point is intentionally narrower than the maintained Windows MFC release line:

- Windows x64 and native Windows arm64: core build + baseline smoke run + engine link smoke + real-file runtime tests
- macOS arm64 (Apple Silicon M-series): the same core tests + Darwin security regression

It does not replace the MFC Windows mainline or add release-path GUI bridge dependencies.

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

The current CMake core uses the OpenSSL stub; setting
`LHASH_ENABLE_OPENSSL_VENDOR=ON` reports that integration is not implemented.
The standalone provider tests link the actual OpenSSL 3.5.9 static library on
each supported test platform. See [Testing](TESTING.md) for the separate coverage.

## What it does not build

The core entry point does not include:

- MFC UI targets
- Linux targets
- macOS GUI targets
- OpenSSL 4.0 migration work

## How to build

### Windows x64

```powershell
cmake -S . -B build/core/windows-x64 -G "Visual Studio 17 2022" -A x64 -DLHASH_BUILD_M2_SMOKE=ON -DLHASH_BUILD_RUNTIME_TESTS=ON
cmake --build build/core/windows-x64 --config Release --parallel
ctest --test-dir build/core/windows-x64 --output-on-failure --no-tests=error -C Release
```

### Windows arm64

The `windows-11-arm` CI image uses Visual Studio 2026. The following commands
require Visual Studio 2026 and CMake 4.2 or newer. For a local Visual Studio 2022
installation, use the `Visual Studio 17 2022` generator instead.

```powershell
cmake -S . -B build/core/windows-arm64 -G "Visual Studio 18 2026" -A ARM64 -DLHASH_BUILD_M2_SMOKE=ON -DLHASH_BUILD_RUNTIME_TESTS=ON
cmake --build build/core/windows-arm64 --config Release --parallel
ctest --test-dir build/core/windows-arm64 --output-on-failure --no-tests=error -C Release
```

CI executes the smoke and real-file runtime tests on the native ARM64 runner.
For local execution, use an ARM64 Windows machine; cross-compilation alone does
not exercise ARM64 runtime behavior.

### macOS arm64

```powershell
cmake -S . -B build/core/macos-arm64 -G Xcode -DCMAKE_OSX_ARCHITECTURES=arm64 -DLHASH_BUILD_M2_SMOKE=ON -DLHASH_BUILD_RUNTIME_TESTS=ON
cmake --build build/core/macos-arm64 --config Release --parallel
ctest --test-dir build/core/macos-arm64 --output-on-failure --no-tests=error -C Release
```

Use native arm64 development tools on an Apple Silicon Mac. All M-series
generations use this generic arm64 target; no per-model build is required.

## Real-file runtime coverage

`LHASH_BUILD_RUNTIME_TESTS=ON` adds `lhash_core_runtime`: 43 cases covering actual
file reads, fixed digest vectors, buffer boundaries, Unicode paths, error
continuation, cancellation and concurrent requests. The tests exercise AUTO,
single-threaded and parallel execution. Details are in
[the runtime suite README](../tests/core_runtime/README.md).

The standalone OpenSSL provider suite and Windows native suite provide separate
SHA256 coverage; passing core tests with the stub does not establish that the
OpenSSL backend works.

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
validation artifact containing `lhash`, `README.txt`, `BUILD_INFO.txt`, and
`SHA256.txt`. It is an engine validation artifact, not a feature-complete end-user CLI, and it is separate from the core matrix workflow and the Windows MFC release line.

