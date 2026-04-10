# LHash Security Model

## Overview

LHash is implemented as a **local Windows verification tool** with a security model centered on correctness, predictable local execution, and a reviewable release posture.

The maintained release line does not treat security as a single feature. Instead, it combines local-safety defaults, runtime isolation, careful file handling, and release metadata so that the verification workflow is easier to trust and easier to audit.

## Core principles

### Local-first execution

LHash is designed to work locally.

The maintained line is intentionally aligned with these defaults:

- no account requirement
- no telemetry requirement
- no default upload of file contents
- no default upload of hashes to third-party services

This reduces privacy surprises and keeps the trust boundary closer to the user's machine.

### Correctness over convenience

The project prefers explicit failure to silent ambiguity.

If the tool cannot safely process an input or cannot trust a path shape, the intended behavior is to fail clearly rather than continue and risk a misleading result.

### Narrow trust boundaries

The maintained line tries to keep the runtime model understandable:

- UI logic should not directly own hashing state
- worker execution should not rely on shared mutable static hashing state
- file-handling decisions should be explicit and reviewable
- release verification metadata should be published alongside official artifacts

## File and path handling model

The security model treats path handling as one of the most important risk areas for a local verification tool.

### Rejected-by-default path types

The maintained line rejects these risky path types by default:

- symbolic links
- junction points
- reparse points

Reason:

These path types can create trust confusion, unintended traversal, or accidental processing of a different file than the user believed they selected.

### Defensive input surfaces

The project also treats these desktop input surfaces as security-relevant:

- command-line parsing
- drag-and-drop input
- shell integration entry points
- `WM_COPYDATA` message handling

Those paths are part of the local attack surface because they accept data that may be malformed, unexpected, or intentionally crafted.

## Runtime execution model

### Per-task execution context

The maintained line uses per-task execution context patterns instead of shared global hashing state.

Security value:

- reduces cross-task interference
- improves determinism under concurrency
- lowers the chance of silent corruption during batch processing

### Thread-safety posture

The project aims for:

- independent hash context per task
- no shared mutable static state in the hashing path
- deterministic output under concurrency

This is important because a verification tool that occasionally produces unstable output is itself a trust failure.

### Resource-safety posture

The maintained line relies on ownership-oriented patterns such as RAII-style handle management and checked conversions.

Security value:

- fewer lifetime mistakes
- safer cleanup under error paths
- reduced risk of truncation or overflow on large file handling

## Large-file safety model

Large files are treated as a normal use case, not an edge case.

The maintained line is designed around:

- 64-bit file size handling
- checked arithmetic and conversion boundaries
- explicit error handling for failed reads or I/O faults

The goal is to avoid producing an apparently valid digest when the processing path actually encountered a boundary error.

## Algorithm model

LHash supports multiple algorithm families, but the security model distinguishes between them.

### Cryptographic integrity algorithms

Examples:

- SHA-256
- SHA-512
- SHA-3 family
- BLAKE2
- BLAKE3

These are relevant when the user wants a modern integrity check.

### Compatibility or legacy algorithms

Examples:

- MD5
- SHA-1

These may still be useful for compatibility with external systems, but they are not presented as equivalent to stronger modern choices.

### Non-cryptographic checksums

Examples:

- XXH3
- CRC32C

These are useful for speed-oriented comparison and validation workflows, but they should not be confused with strong cryptographic authenticity claims.

## UI and architecture model

The maintained line continues to move toward a more modular structure:

- core verification logic in reusable runtime layers
- adapters as interface seams
- desktop UI kept separate from core execution behavior
- legacy compatibility layers gradually reduced in importance

Security value:

- clearer review surface
- fewer accidental couplings between UI state and verification state
- easier testing and future hardening

## Release and trust model

LHash does not treat the release zip alone as sufficient trust evidence.

The maintained line is moving toward a release posture that includes:

- GitHub Releases as the primary public distribution point
- published checksums
- SBOM publication
- GitHub-native artifact attestations
- code-signing documentation and policy

This helps users answer not only “what digest did I compute?” but also “what exactly did the project publish?”

## What this model does not guarantee

The security model does not guarantee:

- protection against a fully compromised operating system
- proof of publisher identity from hash output alone
- malware detection
- safety of unofficial repackaged builds distributed elsewhere
- enterprise policy enforcement outside the tool itself

## Summary

The LHash security model can be summarized as:

- keep verification local
- fail clearly on risky or ambiguous input
- isolate execution contexts
- handle large files intentionally
- separate modern integrity algorithms from compatibility checksums
- publish enough release metadata that users can review and verify the official build chain
