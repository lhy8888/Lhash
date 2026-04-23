# LHash

![Windows Build](https://github.com/lhy8888/Lhash/actions/workflows/windows-build.yml/badge.svg)
![License](https://img.shields.io/badge/license-GPL--2.0-blue.svg)
![Platform](https://img.shields.io/badge/platform-Windows-0078D6)

## Trusted local verification for Windows

LHash is a security-first Windows verification tool for local files, release bundles, and repeatable integrity checks.

It is built for users who want to answer practical trust questions such as:

- Did this downloaded file change?
- Does this release bundle still match the expected artifact?
- Can I verify files locally without uploading them anywhere?
- Can I produce repeatable results for later review or evidence keeping?

The maintained release line turns LHash from a traditional hash utility into a **trusted verification tool** with safer local defaults, clearer execution boundaries, and a more reviewable release posture.

## Current release line

- Current release: [`v1.12.2`](https://github.com/lhy8888/Lhash/releases/tag/v1.12.2)
- Download: [`GitHub Releases`](https://github.com/lhy8888/Lhash/releases)
- Main branch: `future-winui-was2`
- Windows UI mainline: `MFC`
- Legacy WinUI / CLR bridge: retired from the maintained release line
- CI: [`Windows Build workflow`](https://github.com/lhy8888/Lhash/actions/workflows/windows-build.yml)
- Security policy: [SECURITY.md](SECURITY.md)
- Code signing: [CODE_SIGNING.md](CODE_SIGNING.md)
- Code signing policy: [CODE_SIGNING_POLICY.md](CODE_SIGNING_POLICY.md)
- License note: `GPL-2.0-only with an OpenSSL linking exception`; see [LICENSE-OPENSSL-EXCEPTION.md](LICENSE-OPENSSL-EXCEPTION.md)

## Why LHash is positioned as a verification tool

LHash is not only about computing digests quickly. It is built around the idea that **verification results must themselves be trustworthy**.

That means the maintained line focuses on:

- safer local file handling
- deterministic results under concurrency
- explicit treatment of large files and overflow boundaries
- clearer separation between UI code and verification runtime code
- release and supply-chain metadata that can be reviewed later

## Security and verification documents

Start here if you want the project’s trust model in a more reviewable form:

- [Documentation index](docs/README.md)
- [Threat model](docs/THREAT_MODEL.md)
- [Security model](docs/SECURITY_MODEL.md)
- [Release verification guide](docs/RELEASE_VERIFICATION.md)
- [Supply-chain security](docs/SUPPLY_CHAIN_SECURITY.md)

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

## What the tool is for

LHash is a good fit for:

- verifying downloaded software packages before use
- checking release artifacts from GitHub Releases
- confirming that archived files or evidence copies did not change
- batch verification of local directories
- comparing expected and actual digests during delivery or handover

## Input limits

LHash currently processes up to `8192` files per hashing session.

- drag-and-drop and `WM_COPYDATA` batches above this limit are rejected with a visible warning
- folder recursion stops at the limit and warns that additional files may not have been processed
- the legacy multi-select file dialog warns when the returned selection may have hit the same limit

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

Windows UI
- WinMFC

LegacyCompat
third_party
```

This makes it easier to reason about trust boundaries, execution behavior, and future interface expansion.

## Roadmap direction

Near-term roadmap items that reinforce the verification positioning include:

- stronger release verification documentation
- broader supply-chain metadata on releases
- clearer threat-model documentation
- deeper static analysis coverage across managed and native code
- continued MFC mainline hardening

## One-line positioning

**LHash is a Windows MFC trusted verification tool for local integrity checking, repeatable review, and safer release validation.**
