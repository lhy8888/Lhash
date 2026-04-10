LHash vendors the official OpenSSL 3 source snapshot from:

- Repository: https://github.com/openssl/openssl
- Upstream tag: openssl-3.0.20
- Upstream commit: 5aada9c299a3b28fc82348f4e2b93805fa0a0e9c

Imported content:

- Full official OpenSSL 3.0.20 source snapshot is retained in-tree as the
  fixed-version vendor baseline
- Upstream `LICENSE.txt` (Apache License 2.0)
- Upstream build metadata, provider sources, applications, demos, tests,
  documentation, and auxiliary Windows/perl assets

Retention policy:

- LHash keeps the complete upstream vendor tree instead of trimming directories
  out of the snapshot
- This avoids future build drift when OpenSSL `Configure`, Windows
  `install_dev`, or other build-time assumptions reach into directories that
  are not part of the hot path today
- The shipping product still builds a narrow static `libcrypto`, but the vendor
  snapshot remains complete for auditability and reproducible upgrades

LHash integration notes:

- LHash builds a local static `libcrypto` with:
  - `no-shared`
  - `no-tests`
  - `no-module`
  - `no-ssl`
  - `no-asm`
- OpenSSL 3.0.20 `Configure` does not accept `no-apps` or `no-docs`, so LHash
  relies on `build_generated` + `build_libs` and only stages `include/` plus
  `libcrypto.lib` instead of trying to suppress those trees with unsupported
  options
- Keeping the full upstream snapshot does not mean these disabled product
  surfaces are linked into LHash; they remain excluded by the build flags above
- The local vendor build stages `include/` and `libcrypto.lib` manually after
  `Configure`, `build_generated`, and `build_libs`, rather than depending on
  OpenSSL's broader `install_dev` packaging target
- Each build step writes a dedicated log (`configure.log`,
  `build-generated.log`, `build-libs.log`) plus a combined
  `build-openssl-vendor.log` so CI failures surface the actual OpenSSL stderr
  instead of only the outer PowerShell wrapper error
- Runtime adapter layer lives in `trunk/source/Runtime/Hash/OpenSslEvpHashProvider.*`
- OpenSSL-backed algorithm descriptors are exposed through the registry with
  `openssl-*` stable ids, so they can coexist with the legacy in-tree SHA2
  implementations.
- Current OpenSSL-backed descriptors include `SHA-256`, `SHA-384`, `SHA-512`,
  `SHA3-256`, `SHA3-384`, `SHA3-512`, `BLAKE2b-512`, `BLAKE2s-256`,
  `SHAKE128-256`, and `SHAKE256-512`.
- LHash keeps the original legacy `sha256` / `sha512` ids and labels untouched.
