# Release Verification Guide

## Purpose

This document explains how to review and verify an official LHash release.

It is written for users who want more than “download and trust.” The goal is to make it practical to verify that a release asset is the expected one, that the project published reviewable metadata for it, and that the release can be tied back to the repository's documented build and release process.

## Official release location

The primary public distribution point for official builds is the repository's **GitHub Releases** page.

Preferred starting point:

1. Open the official release page for the tag you want
2. Review the listed release assets
3. Download the release bundle and the verification metadata published for that same tag

## What to look for in an official release

For the current maintained line, users should expect release metadata such as:

- Windows x64 release bundle such as `LHash-windows-x64-<shortsha>.zip`
- Windows ARM64 release bundle such as `LHash-windows-arm64-<shortsha>.zip`
- `RELEASE_MANIFEST.txt`
- `SHA256SUMS.txt`
- `LHash-release.cyclonedx.json`
- GitHub-native provenance attestation
- GitHub-native SBOM attestation

Not every historical release will contain the full modern metadata set. Newer releases should be preferred for verification-sensitive use cases.

## Related CI artifacts

The repository also publishes a macOS arm64 CLI MVP tarball through its
dedicated workflow:

- `LHash-macos-arm64-cli-<shortsha>.tar.gz`

The CLI tarball currently contains:

- `lhash`
- `README.txt`
- `BUILD_INFO.txt`
- `SHA256.txt`

Treat that file as a workflow artifact unless the release notes for a tag
explicitly list it as part of the published release bundle set.

## Basic verification workflow

### Step 1: Download the release assets from the official repository

Download at minimum:

- the desktop release bundle(s)
- `SHA256SUMS.txt`
- `RELEASE_MANIFEST.txt`

If available, also download:

- `LHash-release.cyclonedx.json`
- attestation information exposed by GitHub for the release assets

### Step 2: Verify the checksum locally

On Windows PowerShell:

```powershell
Get-FileHash .\LHash-windows-x64-<shortsha>.zip -Algorithm SHA256
```

Compare the returned SHA-256 value with the entry recorded in `SHA256SUMS.txt`.

Expected outcome:

- the file name matches the bundle you downloaded
- the SHA-256 digest matches exactly

If the values do not match, do **not** use the downloaded file.

### Step 3: Review the release manifest

`RELEASE_MANIFEST.txt` is intended to help users review the release context.

It can contain values such as:

- release mode
- git reference or tag
- commit SHA
- short SHA
- release asset name
- workflow run link

This helps answer:

- which commit the release came from
- which workflow run produced the package
- whether the asset name matches the expected release contents

### Step 4: Review the SBOM

If `LHash-release.cyclonedx.json` is present, review it as a release inventory document.

Use cases:

- understand what was packaged into the release asset set
- inspect dependency and component metadata
- compare releases over time
- support internal software review or approval workflows

The SBOM is an aid to transparency. It is not a substitute for a checksum or an authenticity review.

### Step 5: Review GitHub attestation data

For releases that include GitHub-native attestations, review the attestation information exposed by GitHub for the asset.

Practical goal:

- confirm that the published metadata was generated through the repository's configured GitHub Actions workflow
- tie the artifact back to the repository and release process rather than relying only on the zip file itself

### Step 6: Review code-signing state where applicable

If the executable inside the release bundle is Authenticode-signed, review the signature on the extracted executable.

On Windows PowerShell:

```powershell
Get-AuthenticodeSignature .\LHash.exe | Format-List
```

Review at least:

- signature status
- signer certificate subject
- signer certificate thumbprint
- timestamp information, if present

Code signing helps with publisher identity and Windows trust UX. It is complementary to release checksums and attestations, not a replacement for them.

## What each signal means

### Checksum match

A matching checksum helps show that the downloaded file still matches the published digest.

It does **not** by itself prove who created the file.

### Release manifest

The manifest helps explain release context and traceability.

It does **not** by itself prove that a third-party mirror distributed the right file.

### SBOM

The SBOM helps explain release composition.

It does **not** by itself prove that the downloaded bundle is untampered.

### Attestation

Attestation helps connect published metadata or artifacts to the repository's configured build and release workflow.

It is stronger than a plain unchecked file repost, but it still belongs in a broader trust review.

### Code signing

Code signing can help identify the publisher presented to Windows and to the user.

It does **not** replace checksum checking or release metadata review.

## Minimum recommended verification for cautious users

At minimum, cautious users should:

1. download from the official GitHub release page
2. verify the SHA-256 value against `SHA256SUMS.txt`
3. review `RELEASE_MANIFEST.txt`
4. prefer releases that also publish SBOM and attestation data

## Stronger verification for higher-trust workflows

For delivery, handover, review, or evidence-oriented workflows, use a stronger checklist:

1. verify checksum
2. review release manifest
3. review SBOM
4. review GitHub attestation data
5. review code-signing state on the extracted executable
6. record the release tag, commit SHA, and local verification time in your own notes

## Red flags

Treat the release as suspicious if you notice any of the following:

- checksum mismatch
- asset name mismatch
- missing or inconsistent release manifest data
- executable signature that is invalid, unexpected, or absent when a signed release is expected
- release assets obtained from an unofficial mirror without matching official metadata

## Summary

A trustworthy release review for LHash should combine:

- official distribution location
- checksum verification
- manifest review
- SBOM review
- attestation review
- code-signing review when applicable

No single signal is enough on its own. The release posture is designed so users can combine multiple signals and make a more defensible trust decision.
