# Release verification

Download official releases from
<https://github.com/lhy8888/Lhash/releases>. Starting with v1.12.6, expect
`LHash-windows-x64-<shortsha>.zip`, `LHash-windows-arm64-<shortsha>.zip` and
`LHash-macos-arm64-cli-<shortsha>.tar.gz`, plus:

- `RELEASE_MANIFEST.txt`
- `SHA256SUMS.txt`
- `LHash-release.cyclonedx.json`
- GitHub provenance and SBOM attestations covering all three packages

Historical releases may have less metadata. The current release workflow
validates all three packages and creates these files and attestations before
publishing. The manual recovery workflows verify existing files without
replacing them.

## Check downloaded files

To verify the complete checksum list, download all three platform packages and
three metadata files into the same directory. On Linux:

```sh
sha256sum --check SHA256SUMS.txt
```

On macOS:

```sh
shasum -a 256 -c SHA256SUMS.txt
```

Each entry uses the file's basename, without a CI working-directory prefix.
The list covers both ZIPs, the macOS tarball, `RELEASE_MANIFEST.txt` and the SBOM. A missing file or
mismatched digest must fail verification. Downloading only one architecture is
fine, but then compare that individual file's entry rather than treating the
missing files as a successful full-list verification.

For an individual ZIP on Windows PowerShell:

```powershell
Get-FileHash .\LHash-windows-arm64-<shortsha>.zip -Algorithm SHA256
```

Compare its complete SHA-256 value and filename with the matching entry in
`SHA256SUMS.txt`. Use the x64 filename on x64. After extraction, `SHA256.txt`
inside the ZIP also provides the checksum of `LHash.exe`.

## Check origin and metadata

`RELEASE_MANIFEST.txt` records the application version, full commit, tag/ref,
all asset names, workflow run and OpenSSL version. Check that they match the
release you intended to download. The release job rejects missing versions and
packages whose embedded build metadata disagree with that commit/version. Tagged
releases also require the tag to match the application version. The macOS
package's executable checksum and archive members are checked before publication.

The SBOM combines the artifact scan with explicitly marked build-declared
OpenSSL, BLAKE3 and xxHash static dependencies. It supports dependency review;
it does not independently authenticate the files.

With a recent GitHub CLI, verify provenance against the official repository and
its main publishing workflow (repeat for the other architecture if downloaded):

```sh
gh attestation verify LHash-windows-arm64-<shortsha>.zip \
  --repo lhy8888/Lhash \
  --signer-workflow lhy8888/Lhash/.github/workflows/windows-build.yml
```

Review the returned repository, workflow and source revision. Attestations from
the manual recovery workflows describe re-attestation of already published
files; they do not replace the main workflow's original production provenance.
A checksum copied from an untrusted mirror is not proof of publisher identity.

## Check Windows signing state

If a release claims Authenticode signing, inspect the extracted executable:

```powershell
Get-AuthenticodeSignature .\LHash.exe | Format-List
```

Check signature validity, signer subject and thumbprint. `BUILD_INFO.txt` also
records the packaging-time signature state. Signing is conditional on repository
credentials, so a successful build does not by itself promise a signed program.

## Verify the macOS ARM64 CLI package

The macOS workflow produces
`LHash-macos-arm64-cli-<shortsha>.tar.gz`. It is included in tagged releases from
v1.12.6 onward and remains unsigned and not notarized. Verify its entry in the
release checksum list, then extract it, enter the extracted directory, and verify
the executable before using it:

```sh
tar -xzf LHash-macos-arm64-cli-<shortsha>.tar.gz
cd LHash-macos-arm64-cli-<shortsha>
shasum -a 256 -c SHA256.txt
./lhash --list-algorithms
./lhash -a sha256 /path/to/file
```

`BUILD_INFO.txt` records the application version, commit, arm64 architecture and static OpenSSL 3.5.9
backend. The CLI supports all 18 registered algorithms, `--all` and `--json`;
its default selection matches Windows. MD5/SHA1 and non-cryptographic algorithms
remain compatibility choices, so choose SHA256 for a SHA256 checksum. Only
Apple Silicon is supported; this artifact makes no claim of testing a particular
M5/M6 machine.

Branch builds also expose the same package as an Actions artifact. Older
Windows-only release manifests remain verifiable by the recovery tooling; the
tooling never adds a macOS asset to an already published checksum manifest.
