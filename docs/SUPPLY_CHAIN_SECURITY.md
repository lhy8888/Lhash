# LHash Supply-Chain Security

## Purpose

This document explains how the maintained LHash release line approaches supply-chain security.

For this project, supply-chain security means making it easier to answer questions such as:

- what source repository produced this release
- what workflow produced the published asset
- what metadata exists to review the release
- how dependencies and third-party code are brought into the project
- which automated checks help detect security regressions over time

## Source of truth

The primary source of truth for the maintained release line is the official `lhy8888/Lhash` repository and its published GitHub Releases.

The project aims to make official artifacts distinguishable from reposted or repackaged copies by publishing reviewable metadata alongside release assets.

## Supply-chain objectives

The maintained line is working toward these goals:

- keep official release production inside documented repository workflows
- make release artifacts easier to trace to repository state
- publish enough metadata that users can independently review a release
- reduce avoidable dependency drift in automation and build inputs
- continuously improve repository posture using automated checks

## Current supply-chain controls

### 1. GitHub Actions-based release workflows

The repository uses GitHub Actions as the documented automation surface for build and release handling.

Security value:

- the release path is visible in repository configuration
- workflow definitions are versioned with the source tree
- release metadata can be tied back to a specific workflow and commit context

### 2. Release checksums

Official release workflows publish checksum metadata such as `SHA256SUMS.txt`.

Security value:

- users can verify that downloaded assets match the published digest
- accidental corruption or tampering becomes easier to detect

Limit:

- checksum matching alone does not prove publisher identity

### 3. Release manifest metadata

The maintained release line also uses `RELEASE_MANIFEST.txt` to improve traceability.

Typical value:

- records release mode, reference, commit SHA, and asset naming context
- helps users or reviewers tie a bundle back to a repository state and workflow run

### 4. SBOM publication

The repository now includes workflow support for release SBOM generation using CycloneDX JSON output.

Security value:

- makes release composition easier to inspect
- supports internal software review and dependency awareness
- improves visibility over time as the release process matures

### 5. GitHub-native artifact attestations

The maintained line now includes workflow support for GitHub-native provenance attestation and SBOM attestation for release assets.

Security value:

- improves traceability from release assets back to repository automation
- gives users more evidence than a standalone zip file reposted elsewhere
- strengthens the repository's release posture for future audits and recommendations

### 6. CodeQL scanning

The repository includes a CodeQL workflow for the managed C# surface.

Security value:

- gives the project a repeatable static-analysis baseline
- helps surface security-relevant code issues through GitHub code scanning

The native C/C++ surface can be extended further in later hardening passes.

### 7. Dependabot configuration

The repository includes Dependabot configuration for GitHub Actions and NuGet.

Security value:

- reduces unnoticed drift in automation dependencies
- helps keep update review visible in pull-request form
- improves response to known vulnerable dependency versions over time

### 8. OpenSSF Scorecard workflow

The repository includes an OpenSSF Scorecard workflow.

Security value:

- gives the project a recurring baseline review of repository security posture
- helps identify gaps such as token hygiene, workflow safety, branch-protection posture, and release practices
- turns supply-chain posture into a trackable process rather than a one-time checklist

## Dependency strategy

The maintained line uses a mixed dependency model.

### Vendored third-party components

Some upstream algorithm families and related components are vendored into the repository from fixed-version snapshots.

Security value:

- version choice is explicit in repository history
- review surface stays tied to the repository state used for the build
- reduces ambiguity from silently pulling moving targets during the build

Trade-off:

- vendored code still requires active review and refresh decisions
- fixed snapshots are only safer when they are documented and maintained intentionally

### Managed dependencies and automation dependencies

For managed ecosystems and GitHub Actions, the repository is moving toward more visible update tracking and review.

Security value:

- update proposals become easier to inspect
- supply-chain change enters repository history instead of happening invisibly on developer machines

## Trust limits and honest boundaries

The maintained line does not claim perfect supply-chain security.

Important limits:

- release metadata is only as strong as the surrounding platform and account trust assumptions
- hash and manifest files do not prove identity by themselves
- unofficial mirrors can still confuse users if they ignore official verification steps
- code scanning and Scorecard improve posture, but they do not replace expert review or targeted audits

## Recommended reviewer checklist

For a cautious reviewer, the project's supply-chain posture is best evaluated by checking:

1. the source repository is the official one
2. the release came from the official GitHub Releases page
3. checksum metadata is present and matches the downloaded artifact
4. release manifest data is present and consistent
5. SBOM is present for the release
6. GitHub attestation data is present where expected
7. repository automation and scanning workflows are active and green

## Future strengthening areas

The current posture is a good foundation, but stronger future work can include:

- deeper CodeQL coverage for native C/C++ surfaces
- stricter pinning and review of reusable workflow inputs
- stronger code-signing deployment on public release assets
- clearer dependency refresh cadence for vendored snapshots
- formal external review or security audit once the release process stabilizes further

## Summary

The LHash supply-chain posture is built around visibility and traceability:

- documented repository workflows
- published release checksums
- release manifest metadata
- SBOM generation
- GitHub-native attestations
- dependency review workflows
- recurring Scorecard and code-scanning checks

That approach does not eliminate all supply-chain risk, but it makes the project materially easier to review, explain, and trust than a plain unsigned zip with no supporting evidence.
