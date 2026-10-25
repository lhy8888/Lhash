# LHash supply-chain security

The maintained release line builds from `lhy8888/Lhash`. Release metadata and
checksums support verification against that repository; a checksum alone does
not identify the publisher.

## Windows release production

`.github/workflows/windows-build.yml` builds Windows x64 and ARM64 packages.
Publication depends on both builds, the native runtime test matrix, and release
tooling tests. Each native job also builds and executes the root CMake runtime
and platform regressions with the vendored static OpenSSL library.
The x64 application build additionally runs the actual MFC executable through
close, Escape/cancel and rapid-start/close GUI cases before signing or packaging.
Missing interactive desktop access is a failed coverage requirement, not a
successful skip. Console output and the per-case JSON report are retained even
on failure. Windows ARM64 is not claimed to have this x64 GUI automation run.

The release job checks out the same commit, validates the version resource and
commit recorded inside both ZIPs, and creates `RELEASE_MANIFEST.txt`. Missing or
inconsistent application versions fail the job; `unknown` is not accepted.

Before publishing a v* tag release, that same job:

1. Scans the staged artifacts with Syft to produce a CycloneDX SBOM. The build's
   statically linked OpenSSL, BLAKE3 and xxHash versions are explicitly added as
   **build-declared** components because binary scanners cannot reliably recover
   all static library identities. This does not claim that every source file or
   transitive build tool was discovered by the scanner.
2. Generates one `SHA256SUMS.txt` containing both ZIPs, the release manifest and
   the final SBOM, using plain filenames relative to the download directory.
3. Verifies the complete set and creates provenance and SBOM attestations that
   include **both** architecture ZIPs.
4. Publishes the artifacts only after the preceding steps succeed.

The checksum file is created once. The manual recovery workflows
`release-provenance.yml` and `release-sbom.yml` verify existing release assets
and re-attest them; they have no release-write permission and do not upload or
replace the checksum/SBOM files. Normal publication does not rely on a
`release.published` event created by `GITHUB_TOKEN` triggering a second workflow.
Manual release tags enter shell commands through environment variables and
quoted arguments, rather than shell-source interpolation.

A manual Windows Build dispatch produces a rehearsal artifact and verifies its
metadata without publishing a release or creating release attestations.

## Vendored OpenSSL integrity

OpenSSL 3.5.9 is stored as pristine upstream source in
`third_party/openssl/3.5.9`. Its source metadata records the upstream release URL
and archive SHA-256. The separate `OPENSSL_3_5_9_FILES.sha256` manifest contains
all 5779 upstream file hashes, generated from that verified archive.

`tools/verify_openssl_vendor_pristine.ps1 -Version 3.5.9` rejects changed,
missing and additional files and filesystem links, then checks the established
source-layout and local-marker rules. The manifest is part of the reviewed Git
commit, not a mechanism that can protect against an attacker who controls both
the source and the repository's trust configuration.

Windows cache identity includes version, target, configuration and build-script
hash. Workflow cache keys additionally include vendored source and the reviewed
file manifest. Main and benchmark jobs keep the script-owned build log separate
from console capture, so a cold build does not create two writers for one file.

## macOS ARM64 CLI artifacts

The macOS workflow builds static OpenSSL 3.5.9 for arm64 and enables the vendor
backend in the CLI. CTest executes the engine tests and the CLI's full algorithm
regression suite before packaging. The CLI provides the same 18 algorithm
registrations as Windows; SHA256 is available through `-a sha256`, with
`--list-algorithms`, `--all` and `--json` supported.

The artifact contains `lhash`, `README.txt`, `BUILD_INFO.txt` and `SHA256.txt`.
The workflow extracts the produced tarball to a fresh directory and verifies
`SHA256.txt` there. It is a workflow artifact, currently unsigned and not
notarized, and is not included in the Windows tagged-release set. Only Apple
Silicon/arm64 is supported; an arm64 build is not a record of testing individual
M-series hardware models.

## Automation controls and limitations

- Top-level workflow action references are pinned to commits resolved from
  official upstream repositories. Dependabot proposes updates. This does not
  imply every tool downloaded internally by those actions is independently
  pinned by this repository.
- Build/test jobs use read-only repository permission. Release-write and OIDC/
  attestation permissions are scoped to the publishing job or manual attestation
  workflows that need them.
- CodeQL runs separate managed C# analysis and a traced native C++ application
  build. It runs on pull requests and maintained branch pushes as well as manual
  dispatch. CodeQL findings complement runtime regressions and code review.
- Portable tests cover Linux and macOS ARM64, including ASan/UBSan builds with
  the actual static OpenSSL backend. Native Windows tests execute on x64 and
  ARM64 runners. UI/native OS behavior still needs its platform-specific tests;
  a source assertion is not a substitute for execution.
- Scorecard remains a manually triggered repository-posture check. Neither it
  nor CodeQL is a guarantee that the program is vulnerability-free.
- Authenticode is conditional on configured signing credentials. Windows package
  metadata records signature state; unsigned builds must not be represented as
  signed. See [release verification](RELEASE_VERIFICATION.md).
