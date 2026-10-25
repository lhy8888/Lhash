# Windows native runtime tests

This executable runs the real Windows file backend and hash engine, including
the MFC adapter types and the vendored OpenSSL provider. Build and execute it on
both Windows x64 and native Windows ARM64; an ARM64 cross-compile alone does not
verify runtime behavior.

With the OpenSSL vendor enabled, the executable registers 49 tests. Coverage
includes known-answer digest vectors, multiple files, missing and locked files,
directory junction rejection, algorithm selection, failure propagation,
concurrent requests, and cancellation. The SHA-256 boundary test checks 17 file
lengths in AUTO, SINGLE_THREADED, and PARALLEL modes (51 subcases). Additional
tests cover Unicode paths and cancellation without publishing partial digests,
file-handle release, and a subsequent successful request. Multi-block fixtures
use the engine's actual buffer size.

After preparing the OpenSSL install tree with `trunk/build_openssl_vendor.ps1`
or downloading the matching CI artifact, run these commands from the repository
root in a Visual Studio developer shell. Set `$openSslRoot` to that install tree.

```powershell
# Visual Studio 2022, x64 host and executable
msbuild native-runtime-tests/LHash.NativeRuntimeTests/LHash.NativeRuntimeTests.vcxproj /m /p:Configuration=Release /p:Platform=x64 /p:PlatformToolset=v143 /p:BuildProjectReferences=true /p:LHashOpenSslInstallRoot=$openSslRoot
if ($LASTEXITCODE -ne 0) { throw 'Native test build failed.' }
& native-runtime-tests/LHash.NativeRuntimeTests/x64/Release/LHash.NativeRuntimeTests.exe --expected-architecture x64 --require-openssl
if ($LASTEXITCODE -ne 0) { throw 'Native tests failed.' }

# Visual Studio 2026, native ARM64 host and executable
msbuild native-runtime-tests/LHash.NativeRuntimeTests/LHash.NativeRuntimeTests.vcxproj /m /p:Configuration=Release /p:Platform=ARM64 /p:PlatformToolset=v145 /p:BuildProjectReferences=true /p:LHashOpenSslInstallRoot=$openSslRoot
if ($LASTEXITCODE -ne 0) { throw 'Native test build failed.' }
& native-runtime-tests/LHash.NativeRuntimeTests/ARM64/Release/LHash.NativeRuntimeTests.exe --expected-architecture ARM64 --require-openssl
if ($LASTEXITCODE -ne 0) { throw 'Native tests failed.' }
```

The relevant C++ and static MFC components must be installed for the selected
architecture and toolset. The vendor library is Release-only. The test log prints
the executable architecture and the linked OpenSSL runtime version; the command
line checks fail if the architecture or provider configuration is wrong.

Linux cloud validation cannot execute this Windows/MFC suite. Use the native
Windows CI results as the evidence for Windows file handling and ARM64 behavior.
