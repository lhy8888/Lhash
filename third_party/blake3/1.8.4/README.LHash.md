LHash vendors the official BLAKE3 C implementation from:

- Repository: https://github.com/BLAKE3-team/BLAKE3
- Upstream tag: 1.8.4
- Upstream commit: b97a24f8754819755ef78d8016c0391c65c943c5

Imported content:

- `c/` official C implementation snapshot
- `test_vectors/` official test vectors snapshot
- upstream license files (`LICENSE_A2`, `LICENSE_A2LLVM`, `LICENSE_CC0`)

LHash integration notes:

- Runtime adapter layer lives in `trunk/source/Runtime/Hash/BLAKE3HashProvider.*`
- Algorithm variants are exposed as registry descriptors:
  - `blake3-256`
  - `blake3-512`
  - `blake3-xof`
- The current integration uses the official portable/dispatch-safe C path without enabling the optional SIMD backends in the project files yet, to keep all existing native build targets stable.
