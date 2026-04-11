# Recommendation Pitch

## Purpose

This document is a short external-facing pitch for communities, maintainers, reviewers, or organizations that may consider listing, reviewing, or recommending LHash.

## Suggested short pitch

LHash is a Windows trusted local verification tool designed for users who want more than a generic hash calculator. It combines local-only verification, safer default path handling, clearer trust boundaries, release verification documentation, published checksums, SBOM support, and GitHub-native attestation workflows.

## What makes LHash more interesting than a typical hash utility

Many file hash tools stop at “compute a digest quickly.” LHash is trying to do more than that.

The maintained line is explicitly moving toward a model where users can review:

- what they downloaded
- whether it matches the published checksum
- what release metadata exists for it
- what the project documents about its threat model and security posture
- how the release and supply-chain story is explained

That makes it more useful for trust-sensitive local verification workflows.

## Why it may be worth reviewing or recommending

### 1. Local-first trust model

LHash is intentionally designed around local verification.

- no account requirement
- no telemetry requirement
- no default content upload
- no default hash submission to third-party services

That is a meaningful property for users who prefer offline or local-only validation.

### 2. Safer-by-default path handling

The maintained line rejects risky path types such as symbolic links, junction points, and reparse points by default.

This helps reduce trust confusion and accidental verification of the wrong target.

### 3. Trust documentation already exists

The repository includes project-level documents for:

- threat model
- security model
- release verification
- supply-chain security
- vulnerability reporting
- support scope
- contribution expectations

Many small utilities never reach this level of reviewability.

### 4. Release review posture is stronger than average

The maintained line is already set up around a more reviewable release model, including:

- official GitHub Releases
- checksum publication
- release manifest metadata
- SBOM generation support
- provenance and SBOM attestation support
- code-signing guidance

### 5. Honest boundaries

LHash does not pretend to solve every authenticity or endpoint-security problem.

That honesty matters. The project is explicit that:

- a hash alone does not prove publisher identity
- the tool is not a malware scanner
- the trust model is narrower than a full software-authenticity platform

This makes the project easier to evaluate seriously.

## Best-fit audiences

LHash may be most relevant for:

- advanced Windows users who verify downloaded software
- open-source maintainers reviewing artifacts
- IT and infrastructure engineers who want local verification workflows
- reviewers interested in trustworthy release distribution practices
- communities that value privacy-friendly, local-first utilities

## Current maturity statement

LHash should be understood as:

- already stronger than a generic hash utility from a trust and documentation perspective
- already meaningful as a release-verification-oriented local tool
- still improving in some build and automation areas

That is a credible and reviewable state for a project at this stage.

## Suggested recommendation framing

If a reviewer or community wants a concise way to describe the project, this wording is reasonable:

> LHash is a Windows local verification tool that goes beyond simple hashing by combining safer defaults, repeatable verification behavior, release documentation, and supply-chain review metadata.

## Suggested submission notes

When presenting LHash to a recommendation list, reviewer, or partner, emphasize:

- local-first verification model
- safer path handling defaults
- release verification guidance
- SBOM and attestation support
- project honesty about limitations
- existing documentation set for threat model and supply-chain posture

## What should not be overstated

Avoid presenting LHash as:

- a full authenticity framework
- a malware protection system
- a mature universal security platform

The strongest case for the project is that it is a **serious, trustworthy local verification tool** with unusually strong documentation and release-review posture for its category.
