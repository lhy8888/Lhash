# LHash

![Windows Build](https://github.com/lhy8888/Lhash/actions/workflows/windows-build.yml/badge.svg)
![License](https://img.shields.io/badge/license-GPL--2.0-blue.svg)
![Platform](https://img.shields.io/badge/platform-Windows-0078D6)

## Trusted local verification for Windows

LHash is a security-first Windows verification tool for local files, release bundles, and repeatable integrity checks.

It is designed for users who need to answer practical trust questions such as:

- Did this downloaded file change?
- Does this release bundle still match the expected artifact?
- Can I verify files locally without uploading them anywhere?
- Can I produce repeatable results for later review or evidence keeping?

This maintained release line turns LHash from a traditional hash utility into a **trusted verification tool** with stronger local-safety defaults, clearer execution boundaries, and a more auditable release posture.

## Current release line

- Current release: [`v1.12.3`](https://github.com/lhy8888/Lhash/releases/tag/v1.12.3)
- Download: [`GitHub Releases`](https://github.com/lhy8888/Lhash/releases)
- Main branch: `future-winui-was2`
- Windows UI mainline: `MFC`
- CI: [`Windows Build workflow`](https://github.com/lhy8888/Lhash/actions/workflows/windows-build.yml)
- Core CI: [`Core Build Matrix workflow`](../.github/workflows/core-m2.yml)
- macOS CLI MVP: [`macOS CLI MVP Build workflow`](../.github/workflows/macos-cli-build.yml)
- macOS core support: [`macOS core support criteria`](MACOS_CORE_SUPPORT.md)
- Code signing: [CODE_SIGNING.md](../CODE_SIGNING.md)
- Code signing policy: [CODE_SIGNING_POLICY.md](../CODE_SIGNING_POLICY.md)
- Security policy: [SECURITY.md](../SECURITY.md)

## Why LHash is positioned as a verification tool

LHash is not only about computing digests quickly. It is built around the idea that **verification results must themselves be trustworthy**.

That means the maintained line focuses on:

- safer local file handling
- deterministic results under concurrency
- explicit treatment of large files and overflow boundaries
- clearer separation between UI code and verification runtime code
- release and supply-chain metadata that can be reviewed later

## Security model highlights

### Local-first by default

LHash is intended for local verification workflows.

- no account requirement
- no telemetry requirement
- no default upload of file contents
- no default upload of hashes to third-party services

### Safer path handling

By default, LHash rejects risky path types such as:

- symbolic links
- junction points
- reparse points

This reduces accidental trust mistakes, privilege surprises, and unintended traversal into sensitive paths.

### Verification stability under load

LHash uses:

- per-task hash contexts
- no shared mutable static hashing state
- checked integer conversions and 64-bit file size handling
- RAII-style resource ownership

The goal is not just speed, but repeatable and reviewable results under real workloads.

### macOS core support

The macOS arm64 core support line is intentionally baseline-only:

- the core builds through the cross-platform entry point
- baseline algorithms run through a minimal smoke target
- Darwin file handling rejects symlinks and validates opened file descriptors
- a dedicated macOS security regression target exercises the path policy

This is enough to establish core support without claiming a full macOS product
line or GUI.

## What the tool is for

LHash is a good fit for:

- verifying downloaded software packages before use
- checking release artifacts from GitHub Releases
- confirming that archived files or evidence copies did not change
- batch verification of local directories
- comparing expected and actual digests during delivery or handover

## What the tool is not trying to be

LHash is not presented as a complete authenticity framework by itself.

A hash proves that content matches a digest. It does **not** by itself prove publisher identity.

That is why this repository is also moving toward:

- code-signing clarity
- release checksum publication
- GitHub-native artifact attestations
- release SBOM publication

Together, those make the project more useful as a trusted verification tool instead of just a fast hash calculator.

## Supported algorithms

### Cryptographic hashes

- MD5
- SHA-1
- SHA-256
- SHA-512

### Modern high-performance hashes

- BLAKE3-256
- BLAKE3-512

### OpenSSL 3 extended algorithms

- SHA-256
- SHA-384
- SHA-512
- SHA3-256
- SHA3-384
- SHA3-512
- BLAKE2b-512
- BLAKE2s-256
- SHAKE128-256
- SHAKE256-512

### Non-cryptographic checksums

- XXHash3-64
- XXHash3-128
- CRC32C

## Architecture direction

The maintained line has been reworked into clearer layers:

```text
NativeCore
- Domain
- Runtime
- Algorithms
- Common

Adapters
- UiBridge

WinMFC
LegacyCompat
third_party
```

This makes it easier to reason about trust boundaries, execution behavior, and future interface expansion.

## Roadmap direction

Near-term roadmap items that reinforce the verification positioning include:

- stronger release verification documentation
- broader supply-chain metadata on releases
- CLI mode for repeatable verification flows
- clearer threat-model documentation
- deeper static analysis coverage across managed and native code

## One-line positioning

**LHash is a Windows trusted verification tool for local integrity checking, repeatable review, and safer release validation.**
