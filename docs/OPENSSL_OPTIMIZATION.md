# OpenSSL assembly builds

The vendored OpenSSL 3.5.9 source stays pristine. Production builds enable its
upstream architecture-specific assembly by default, including SHA256/SHA512 CPU
dispatch. They do not add `-march=native`, force a particular CPU feature, or
disable runtime capability detection.

| Platform | Optimized Configure target | Assembler |
| --- | --- | --- |
| Windows x64 | `VC-WIN64A` | NASM 2.09 or newer, as required by OpenSSL |
| Windows ARM64 | `VC-WIN64-CLANGASM-ARM` | LLVM `clang-cl.exe`; C remains compiled by MSVC `cl` |
| macOS Apple Silicon | `darwin64-arm64-cc` | Apple toolchain |
| Linux x64 test backend | `linux-x86_64` | System compiler/assembler |

The Windows ARM64 assembly target is provided by the vendored upstream
`Configurations/50-win-clang-cl.conf` and documented in `NOTES-WINDOWS.md`.
The old `VC-WIN64-ARM` target lacks its assembly configuration. Merely removing
`no-asm` while keeping that old target would not enable ARM digest assembly.
No local assembly code or OpenSSL patches are needed.

```powershell
# Optimized is the default; missing assemblers produce an actionable error.
pwsh -NoProfile -File trunk/build_openssl_vendor.ps1 -Platform x64
pwsh -NoProfile -File trunk/build_openssl_vendor.ps1 -Platform ARM64

# Explicit compatibility / diagnosis build, in a separate default install root.
pwsh -NoProfile -File trunk/build_openssl_vendor.ps1 -Platform ARM64 -AssemblyMode portable
```

The Windows script searches PATH and the usual NASM, Visual Studio LLVM and
Program Files LLVM locations. Both x64-to-ARM64 cross builds and native ARM64
builds retain the appropriate MSVC environment. LLVM only assembles upstream
Windows ARM64 sources; it does not change the application's compiler or CRT.

```sh
bash tools/build_openssl_vendor_posix.sh "$PWD/build/openssl-optimized"
bash tools/build_openssl_vendor_posix.sh "$PWD/build/openssl-portable" portable
```

`portable` passes `no-asm`, disabling OpenSSL digest assembly; ordinary compiler
optimizations remain enabled. It does not imply cross-architecture binaries.
Optimized builds never silently fall back to portable when a tool is missing.

Both scripts run `tools/verify_openssl_build_config.pl` against the actual
generated `configdata.pm` before compiling. It checks the requested target,
the `asm` disable state and the SHA256/SHA512 assembly definitions. A successful
install includes `lhash-openssl-build-config.json` with those facts and
`lhash-openssl-build-identity.txt` with the target, mode and script/verifier hashes.
The Windows reuse check validates both files; an interrupted build has no valid
completion identity. POSIX builds rebuild when invoked. CI cache keys include
the mode and relevant script hashes, and restored metadata is verified before use.
Use separate install roots when comparing modes, including separate sanitizer
and normal builds; do not mix their libraries or generated headers.

Configuration checks can run on Linux x64 or Apple Silicon:

```sh
python3 tests/openssl-build/test_configuration.py
```

They generate real upstream configurations for both modes and test rejection of
wrong-mode and wrong-target metadata. These checks do not execute Darwin code on
Linux. Native Windows x64/ARM64 and macOS ARM64 CI must additionally build and
run the existing digest known-answer, boundary, concurrent engine and application
tests with optimized libraries. The portable option has its own regression path.

Assembly can improve CPU-bound hashing but does not promise a fixed end-to-end
speedup: storage throughput, file sizes and UI overhead also matter. Measure the
same version and input on the intended hardware before publishing a speed ratio.
