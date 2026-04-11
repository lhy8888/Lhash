# LHash Project Profile

## One-line summary

LHash is a Windows **trusted local verification tool** for file integrity checking, release review, and repeatable offline verification workflows.

## What the project does

LHash helps users verify local files and release bundles in a way that is:

- local-first
- repeatable
- reviewable
- safer by default than a generic hash utility with unclear trust boundaries

The maintained line is focused on turning file hashing into a **trustworthy verification workflow**, not just a fast digest calculation feature.

## Who the project is for

LHash is most relevant for people who need to answer questions like:

- Did this downloaded file change?
- Does this release asset still match the published checksum?
- Can I verify files locally without uploading it anywhere?
- Can I keep a repeatable verification record for delivery, handover, or evidence review?

Typical users may include:

- advanced end users who verify downloaded software
- developers and maintainers reviewing release artifacts
- IT and infrastructure engineers doing package verification
- reviewers who want a clearer local verification path on Windows
- users in higher-trust environments who prefer offline or local-only checking

## Core positioning

LHash is not trying to be a full malware scanner, EDR product, or universal publisher-authenticity framework.

Its value comes from combining:

- local verification
- safer path handling
- deterministic runtime behavior
- release verification documentation
- published checksums
- SBOM support
- provenance and attestation support
- code-signing documentation

## Security and trust characteristics

The maintained line is built around these principles:

### Local-first by default

- no account requirement
- no telemetry requirement
- no default upload of file contents
- no default upload of hashes to third-party services

### Safer local input handling

The maintained line treats these areas as security-sensitive:

- command-line input
- drag-and-drop input
- shell integration
- `WM_COPYDATA`
- path type handling

Risky path types such as symbolic links, junction points, and reparse points are rejected by default in the maintained line.

### Verification correctness matters

The tool is designed so that verification results are easier to trust:

- per-task execution context
- no shared mutable static hashing state in the main execution path
- 64-bit file size handling
- checked conversions and overflow-aware behavior
- clearer separation between UI layers and core verification runtime

## Release trust posture

LHash is moving toward a more reviewable release model through:

- GitHub Releases as the official public distribution point
- `SHA256SUMS.txt`
- `RELEASE_MANIFEST.txt`
- SBOM generation and publication
- GitHub-native attestation workflows
- code-signing guidance and policy documentation

This gives users more than just a zip file. It gives them a way to review how the release should be trusted.

## Supported algorithm families

The project supports multiple algorithm categories and tries to present them honestly:

- cryptographic hashes
- modern high-performance hashes
- OpenSSL-backed extended algorithms
- non-cryptographic checksums for speed-oriented workflows

The maintained line does not treat all algorithms as if they provide the same security properties.

## Documentation set

The project already includes a review-oriented documentation set:

- `SECURITY.md`
- `CONTRIBUTING.md`
- `SUPPORT.md`
- `docs/THREAT_MODEL.md`
- `docs/SECURITY_MODEL.md`
- `docs/RELEASE_VERIFICATION.md`
- `docs/SUPPLY_CHAIN_SECURITY.md`
- `docs/OPENSSF_PREP.md`

## Honest limits

LHash does **not** claim that:

- a checksum alone proves publisher identity
- a local verification tool can replace endpoint security
- unofficial repackaged builds are trustworthy by default
- all workflow and native build surfaces are already fully mature

The maintained line is stronger than a generic file hash tool, but it is still honest about where its trust model begins and ends.

## Current maturity statement

LHash already has a strong trust-documentation foundation and meaningful release-review improvements in place.

The project is best described as:

- a serious local verification project
- a repository with a clear security narrative
- a project with meaningful supply-chain and release-trust work already landed
- a project that is still continuing to mature in some build and automation areas

## Short external description

LHash is a Windows trusted local verification tool that helps users review files and release artifacts with safer defaults, repeatable results, and clearer release metadata than a typical generic hash utility.
