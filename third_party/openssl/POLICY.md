# OpenSSL Vendor Policy

`third_party/openssl/<version>/` is a pristine upstream OpenSSL source tree.

Do not edit files inside versioned OpenSSL source directories.

LHash-specific integration must live outside the vendor source tree:
- `trunk/build_openssl_vendor.ps1`
- `NativeOpenSslVendor.targets`
- `.github/workflows/windows-build.yml`
- release metadata and documentation
- optional patches under `third_party/openssl/patches/<version>/`

If patches are ever required, they must be applied only to the temporary build copy, not to the checked-in upstream source directory.
