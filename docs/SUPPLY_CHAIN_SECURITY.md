# LHash supply-chain security

The maintained release line builds from `lhy8888/Lhash`. Release metadata and
checksums support verification against that repository; a checksum alone does
not identify the publisher.

## Release production

`.github/workflows/windows-build.yml` builds Windows x64 and ARM64 packages and
invokes the reusable macOS workflow for the Apple Silicon packages before
release publication. Publication depends on these builds, Windows native and
portable runtime tests, native ARM64 GUI tests, and release tooling tests. The
macOS build runs its engine, CLI, desktop session and native AppKit suites before packaging. Each Windows
native job also executes the root CMake runtime and platform regressions with
the vendored static OpenSSL library.

The actual MFC application runs through 18 GUI cases on each of x64 and native
ARM64, including close/cancel, duplicate inputs, ordered results from 128 files
and selection preservation while new results arrive. Missing interactive desktop
access fails the coverage requirement. Console output and the per-case JSON
report are retained even on failure. These checks inspect text and interaction
state; they are not pixel comparisons across every display configuration.

The release job checks out the same commit, validates the version resource and
commit recorded inside the Windows ZIPs, macOS CLI tarball and desktop app ZIP, and creates
`RELEASE_MANIFEST.txt`. Missing or inconsistent application versions fail the
job; `unknown` is not accepted.

Before publishing a v* tag release, that same job:

1. Scans the staged artifacts with Syft to produce a CycloneDX SBOM. The build's
   statically linked OpenSSL, BLAKE3 and xxHash versions are explicitly added as
   **build-declared** components because binary scanners cannot reliably recover
   all static library identities. This does not claim that every source file or
   transitive build tool was discovered by the scanner.
2. Generates one `SHA256SUMS.txt` containing all platform packages,
   the release manifest and the final SBOM, using plain filenames relative to
   the download directory.
3. Verifies the complete set and creates provenance and SBOM attestations that
   include every platform package recorded in that release manifest.
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

The new app ZIP belongs to the maintained branch's future release path. The
already published v1.12.6 release has three platform packages: Windows x64,
Windows ARM64 and the macOS CLI. Its original checksum and attestation set is
not rewritten to include the desktop app.

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

## macOS ARM64 artifacts

The macOS workflow builds static OpenSSL 3.5.9 for arm64 and enables the vendor
backend in the CLI. CTest executes the engine tests and the CLI's full algorithm
regression suite before packaging. The CLI provides the same 18 algorithm
registrations as Windows; SHA256 is available through `-a sha256`, with
`--list-algorithms`, `--all` and `--json` supported.

The artifact contains `lhash`, `README.txt`, `BUILD_INFO.txt` and `SHA256.txt`.
The workflow extracts the produced tarball to a fresh directory and verifies
`SHA256.txt` there. Starting with v1.12.6, the tarball is included in the same
tagged release and checksum/manifest/SBOM set as the Windows packages; branch
builds also retain it as a workflow artifact. It remains unsigned and not
notarized. Only Apple Silicon/arm64 is supported; an arm64 build is not a record
of testing individual M-series hardware models.

The maintained branch also builds `LHash-macos-arm64-app-<shortsha>.zip` as a
separate Actions artifact. It contains `LHash.app`, package metadata, a checksum
list and license notices. The app uses the same statically linked core and
algorithm set as the CLI. Packaging checks the arm64 executable and bundle
version, applies a local ad-hoc signature, verifies that signature, and records
checksums for the package files. Ad-hoc signing does not establish a Developer
ID identity; the app is not notarized. Future tagged releases include the app
archive in the checksum, manifest, SBOM and attestation set.

See the [desktop guide](MACOS_GUI.md) for obtaining and verifying the branch
artifact. A successful branch build is not itself a new published release.

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
