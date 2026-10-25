# OpenSSL provider regression tests

This standalone CMake project compiles Lhash's actual
`OpenSslEvpHashProvider.cpp` and `strhelper.cpp` against static OpenSSL **3.5.9**.
Both the headers and linked runtime must report that version. It supports native
Windows x64/ARM64, macOS arm64 (Apple Silicon), and Linux; it does not require MFC or the full
hash engine's platform file backend.

The six CTest groups cover:

- Exact header/runtime version.
- 30 known digest vectors: empty, `abc`, and one million `a` bytes for SHA256,
  SHA384, SHA512, SHA3-256/384/512, BLAKE2b/s, and SHAKE128/256; plus eight SHA256
  update chunk sizes (including single-byte and 64-byte boundaries).
- 11 binary file lengths around SHA256 blocks, 4 KiB, and 4 MiB read boundaries,
  using a temporary path with Unicode and spaces. File contents are `i % 251`;
  expected digests are fixed constants cross-checked with Python `hashlib`.
- Update/finalization failure propagation and subsequent recovery.
- Invalid initialization, alias fallback, reinitialization, repeated finalization,
  and repeated cleanup.
- 800 SHA256 operations from eight threads, including concurrent first use of the
  provider cache.

These are provider tests: the file group reads with C++ streams and passes bytes
to the production adapter. The full engine, OS file handling, cancellation, and
desktop UI need their separate runtime tests. Sanitizer instrumentation covers
the adapter and test code; it does not instrument a prebuilt OpenSSL archive.

## Installed OpenSSL

Use the installation produced by `trunk/build_openssl_vendor.ps1` on Windows,
or a native OpenSSL 3.5.9 static installation on macOS/Linux:

```sh
cmake -S tests/openssl-provider -B build/provider -DOPENSSL_ROOT_DIR=/path/to/openssl-3.5.9 -DCMAKE_BUILD_TYPE=Release
cmake --build build/provider --config Release --parallel
ctest --test-dir build/provider -C Release --output-on-failure --verbose
```

For a Visual Studio multi-configuration generator, add `-A x64` or `-A ARM64`
to configure, and execute tests on a runner of that architecture. Use Release to
match the Windows vendor library's runtime configuration.

## Uninstalled OpenSSL build

An out-of-tree OpenSSL build has generated headers separate from source headers.
Pass both explicitly, in that order, along with the static archive:

```sh
cmake -S tests/openssl-provider -B build/provider \
  -DCMAKE_BUILD_TYPE=Release \
  '-DLHASH_OPENSSL_INCLUDE_DIRS=/path/to/openssl-build/include;/path/to/openssl-source/include' \
  -DLHASH_OPENSSL_CRYPTO_LIBRARY=/path/to/openssl-build/libcrypto.a
cmake --build build/provider --parallel
ctest --test-dir build/provider --output-on-failure --verbose
```

GCC/Clang builds can add `-DLHASH_PROVIDER_SANITIZERS=ON` in a separate build
directory to enable AddressSanitizer and UndefinedBehaviorSanitizer.
