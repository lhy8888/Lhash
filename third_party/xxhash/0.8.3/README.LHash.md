LHash vendors the official xxHash source snapshot from:

- Repository: https://github.com/Cyan4973/xxHash
- Upstream tag: v0.8.3
- Upstream commit: e626a72bc2321cd320e953a0ccf1584cad60f363

Imported content:

- `xxhash.c` official implementation snapshot
- `xxhash.h` official public header snapshot
- `tests/sanity_test_vectors.h` official sanity vectors snapshot
- upstream `LICENSE`

LHash integration notes:

- Runtime adapter layer lives in `trunk/source/Runtime/Hash/XXHash3HashProvider.*`
- Algorithm variants are exposed as registry descriptors:
  - `xxh3-64`
  - `xxh3-128`
- The runtime tests use the official xxHash sanity buffer generation rule and official vector values for the fixed-length seed-0 cases used by LHash.
