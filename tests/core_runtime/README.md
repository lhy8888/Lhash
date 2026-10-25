# File-engine runtime tests

`lhash_core_runtime.cpp` exercises the actual synchronous `RunHashRequest`, OS file backend,
digest scheduler and result events. It uses temporary files and fails with a nonzero exit
code on any mismatch. No network or installed command-line hash tools are required.

The 43 cases cover:

- Standard MD5, SHA1, CRC32C and BLAKE3 vectors through real files.
- Twelve binary file sizes under AUTO, SINGLE_THREADED and PARALLEL: empty, 1,
  63/64/65, 4095/4096/4097, 4 MiB minus 1, exactly 4 MiB, 4 MiB plus 17,
  and 8 MiB plus 17 bytes. Expected MD5/SHA1 values are fixed independent
  Python `hashlib` results, not computed with the implementation under test.
- UTF-8 filenames containing Chinese text, spaces, accented text and an emoji,
  preserving duplicate inputs and their byte accounting.
- Missing files and directories reporting errors while a later valid file succeeds.
- Cancellation before starting and during a multi-buffer file, for both single-pass
  and parallel digest execution, without publishing partial digests.
- Four simultaneous callers completing 32 independent requests with separate state.

CMake can link the pinned static OpenSSL 3.5.9 installation when
`LHASH_ENABLE_OPENSSL_VENDOR=ON` and `OPENSSL_ROOT_DIR` are set. This suite
retains its baseline vectors; the full CLI regression exercises all 18 Windows
algorithms through the same engine and real files, and the provider suite checks
EVP failure, lifecycle and configuration-isolation behavior.

Build integration: link the executable with `lhash_core` and `Threads::Threads`, define
`LHASH_CORE_BUILD=1`, and include `cmake/posix-stubs` before `trunk/source` on macOS.
On Windows also define `UNICODE` and `_UNICODE` to match the core library's string ABI.
Use `/utf-8` with MSVC. Give the CTest entry a timeout (for example 180 seconds) so a
queue or cancellation regression cannot leave CI waiting indefinitely.

These tests run the Windows or POSIX file backend. Linux requires the explicit
`LHASH_ENABLE_POSIX_TEST_BACKEND=ON` diagnostic option; Linux execution does not
substitute for native Windows or Darwin validation.
