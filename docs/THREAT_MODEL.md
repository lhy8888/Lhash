# LHash Threat Model

## Purpose

This document explains what LHash is trying to protect, which trust boundaries matter, and which classes of risk the project is designed to reduce.

LHash is a **local Windows verification tool**. Its main goal is to help users perform repeatable integrity checks on files and release bundles without needing a cloud account, a remote verification service, or a background telemetry channel.

The maintained public product line remains the Windows MFC desktop application. macOS arm64 currently exists as a core support and CLI MVP validation path, not as a full macOS GUI product line or notarized public distribution.

## Assets the project tries to protect

The maintained release line treats the following as security-relevant assets:

- correctness of computed verification results
- integrity of local files being processed
- trustworthiness of the shipped release bundle
- predictability of behavior when handling hostile or malformed local input
- user confidence that verification can be performed locally without silent data export

## Primary users and likely use cases

Typical use cases include:

- verifying downloaded software packages before execution
- checking whether a local file still matches an expected digest
- reviewing a release bundle obtained from GitHub Releases
- running repeatable local verification during delivery, handover, or evidence keeping
- processing large local file batches on Windows systems

## Threat actors and risk sources

The project is primarily concerned with the following threat sources:

### 1. Malicious or malformed local input

Examples:

- crafted file paths
- crafted drag-and-drop input
- malformed command-line parameters
- malformed `WM_COPYDATA` payloads
- reparse-point based path confusion

### 2. Local trust-boundary mistakes

Examples:

- accidentally hashing a different file than the user intended
- following a risky path type into an unexpected location
- producing an incorrect result due to overflow, truncation, or shared mutable state

### 3. Release and supply-chain confusion

Examples:

- users downloading the wrong release asset
- users being unable to distinguish between an official release and a repackaged copy
- missing evidence about how a release artifact was produced

### 4. Operational misuse under load

Examples:

- large batch workloads
- very large files
- concurrency bugs that produce unstable or non-repeatable output

## Security goals

The maintained line aims to meet these goals:

1. **Correctness before convenience**
   LHash should prefer explicit failure over silently producing a misleading verification result.

2. **Local-first processing**
   Verification should work locally without requiring an account or default network submission of file contents or hashes.

3. **Safer path handling**
   Risky path types should be rejected by default when they could create trust confusion or unintended traversal.

4. **Deterministic execution**
   Concurrent workloads should not change the correctness of results.

5. **Reviewable releases**
   Official release assets should be accompanied by checksums, metadata, and supply-chain evidence that help users review what they downloaded.

## Non-goals

LHash does **not** try to solve all authenticity problems by itself.

Important limits:

- a hash alone proves content matching, not publisher identity
- LHash is not a malware detector
- LHash is not a sandbox or application control system
- LHash is not a network security product
- LHash does not claim to protect a compromised operating system from itself

## Trust assumptions

LHash assumes at least the following:

- the local operating system and user account are not already completely compromised
- the compiler toolchain and CI environment are operating within their expected trust model
- users obtain official release artifacts from the project repository or another trusted distribution point
- users understand the difference between **integrity checking** and **publisher authenticity**

## Main trust boundaries

The project treats these boundaries as important:

- UI input to runtime execution
- local path selection to file opening
- worker-thread execution to result reporting
- source repository state to published release artifacts
- official release assets to third-party mirrors or reposts

## Threats explicitly considered in the maintained line

The current security posture is designed to reduce risk from:

- symbolic link, junction, and reparse-point confusion
- malformed desktop-input surfaces such as command-line and `WM_COPYDATA`
- arithmetic mistakes on large file sizes or progress accounting
- concurrency bugs caused by shared mutable hashing state
- release ambiguity caused by missing checksums or missing supply-chain metadata

## Threats not fully addressed by the tool alone

Additional controls outside the tool may still be needed for:

- publisher identity validation
- endpoint compromise and credential theft
- malware execution prevention
- enterprise allow-listing or software approval workflows
- long-term archival chain-of-custody processes with external witnesses

## Practical interpretation

For this project, “secure by default” mainly means:

- keep verification local
- reject risky path constructs by default
- prefer explicit errors to ambiguous output
- keep execution contexts isolated
- ship reviewable release metadata

That security position is intentionally narrower and more honest than claiming the tool can solve every trust problem around files or software distribution.
