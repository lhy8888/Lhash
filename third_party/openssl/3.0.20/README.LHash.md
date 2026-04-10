LHash vendors the official OpenSSL 3 source snapshot from:

- Repository: https://github.com/openssl/openssl
- Upstream tag: openssl-3.0.20
- Upstream commit: 5aada9c299a3b28fc82348f4e2b93805fa0a0e9c

Imported content:

- Official source snapshot required to build a fixed-version `libcrypto`
  vendor library for Windows
- Upstream `LICENSE.txt` (Apache License 2.0)
- Upstream build metadata and provider sources used by EVP digest fetch
- Upstream `external/perl` fallback subset (`MODULES.txt` and `Text::Template`)
  required by OpenSSL `Configure`

LHash integration notes:

- LHash builds a local static `libcrypto` with:
  - `no-shared`
  - `no-tests`
  - `no-apps`
  - `no-docs`
  - `no-module`
  - `no-ssl`
  - `no-asm`
- Runtime adapter layer lives in `trunk/source/Runtime/Hash/OpenSslEvpHashProvider.*`
- OpenSSL-backed algorithm descriptors are exposed through the registry with
  `openssl-*` stable ids, so they can coexist with the legacy in-tree SHA2
  implementations.
- LHash keeps the original legacy `sha256` / `sha512` ids and labels untouched.
