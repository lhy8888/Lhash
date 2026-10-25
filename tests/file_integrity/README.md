This regression target links the production hash core and file backend.

On Linux, enable `LHASH_ENABLE_POSIX_TEST_BACKEND`; the test executable uses GNU
linker wrapping to inject one short read, EINTR, EIO, controlled concurrent writes,
and an lstat/open FIFO replacement into its private temporary files. Both single
and parallel execution must produce the full known digest or reject the file.
The known answers cover MD5/SHA1 and, when enabled, the actual OpenSSL SHA256 provider.
The FIFO child is checked after 500 ms and terminated if it remains blocked.

On macOS the same target runs native ordinary-file consistency checks (including
same-size overwrite with restored mtime), path policy, and concurrent timestamp
formatting. Linux fault injection is excluded; its result does not substitute for
native Darwin testing. Windows retains its separate native runtime coverage.

`ctest -R lhash_file_integrity --output-on-failure` runs the target after building.
For example, from the repository root on Linux:

```sh
cmake -S . -B build/file-integrity -DLHASH_ENABLE_POSIX_TEST_BACKEND=ON -DLHASH_BUILD_RUNTIME_TESTS=ON
cmake --build build/file-integrity --target lhash_file_integrity
ctest --test-dir build/file-integrity -R lhash_file_integrity --output-on-failure
```

Enable `LHASH_ENABLE_OPENSSL_VENDOR` and point `OPENSSL_ROOT_DIR` at the supported
OpenSSL installation to include SHA256 in these cases.

The test reports an error for changed or incompletely read files; successful
metadata comparison is a best-effort change check and does not promise a filesystem
snapshot against an adversary capable of manipulating filesystem metadata.
