LHash vendors the official google/crc32c source snapshot from:

- Repository: https://github.com/google/crc32c
- Upstream tag: 1.1.2
- Upstream commit: 02e65f4fd3065d27b2e29324800ca6d04df16126

Imported content:

- `include/crc32c/crc32c.h` official C API header snapshot
- `src/` runtime implementation files required by the C API
- `src/crc32c_capi_unittest.c` official C API vector coverage snapshot
- upstream `LICENSE` and `AUTHORS`

LHash integration notes:

- Runtime adapter layer lives in `trunk/source/Runtime/Hash/CRC32CHashProvider.*`
- Algorithm variant exposed through the registry:
  - `crc32c`
- LHash provides a fixed Windows-oriented `include/crc32c/crc32c_config.h` instead of upstream CMake generation.
- LHash adds a Windows ARM64 processor-feature probe in `src/crc32c_arm64_check.h` so the official ARM64 backend can participate in native Windows builds.
