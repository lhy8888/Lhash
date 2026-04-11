# Repository Metadata Suggestions

## Purpose

This document suggests practical GitHub repository metadata for LHash, especially the short description and Topics.

These values are designed to match the project’s current positioning as a **Windows trusted local verification tool**.

## Suggested GitHub repository description

Recommended primary description:

> Windows trusted local verification tool for file integrity checking, release review, and safer offline verification workflows.

Shorter alternative:

> Windows local verification tool with safer defaults, release checksums, SBOM support, and reviewable trust documentation.

More technical alternative:

> Security-first Windows verification tool for local file integrity checks, release artifact review, and repeatable offline verification.

## Suggested website field

If you do not yet want to use a separate project website, use the repository itself or the releases page as the public entry point.

Recommended current choices:

- repository URL
- GitHub Releases URL

## Suggested Topics

Use a focused set first. Do not add too many unrelated topics.

Recommended core Topics:

- windows
- verification
- checksum
- hashing
- file-integrity
- release-verification
- sbom
- software-supply-chain
- security-tools
- privacy-tools
- windows-security
- local-first
- offline
- blake3
- sha256

## Suggested ordering strategy

Put the most identity-defining Topics first:

1. windows
2. verification
3. checksum
4. file-integrity
5. release-verification
6. security-tools
7. software-supply-chain
8. sbom

Then add algorithm or workflow-specific Topics if they fit.

## Topics to avoid or use carefully

Avoid Topics that imply a broader product category than the project actually covers.

Examples to avoid unless the project scope truly expands:

- antivirus
- malware-detection
- endpoint-protection
- edr
- forensic-suite
- zero-trust-platform

Those labels would create the wrong expectation.

## Suggested GitHub About strategy

The About section should communicate three things quickly:

- it is for Windows
- it is for verification, not general file utilities
- it is more review-oriented than a generic hash calculator

That is why the best short description emphasizes:

- trusted local verification
- release review
- offline or local workflows

## Suggested README / About alignment

Keep the About text and README opening aligned.

The simplest alignment is:

- About: concise one-line project identity
- README: expanded explanation of why LHash is a trusted verification tool

## Practical recommendation

If you only change one thing, use this:

**Windows trusted local verification tool for file integrity checking, release review, and safer offline verification workflows.**
