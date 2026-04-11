# OpenSSF Preparation Notes

## Purpose

This document tracks the repository’s current preparation state for external review, OpenSSF-style best-practice work, and future recommendation or audit conversations.

It is not a formal badge submission. It is a working checklist that helps the repository stay honest about what is already in place and what still needs work.

## What is already in place

### Repository security and reporting

- `SECURITY.md` exists
- repository security posture is documented in:
  - `docs/THREAT_MODEL.md`
  - `docs/SECURITY_MODEL.md`
  - `docs/RELEASE_VERIFICATION.md`
  - `docs/SUPPLY_CHAIN_SECURITY.md`
- bug and verification issue templates exist

### Release and supply-chain documentation

- release verification guidance exists
- supply-chain security guidance exists
- project README now presents LHash as a trusted local verification tool rather than only a generic hash utility

### GitHub Actions and automation

- CodeQL workflow exists
- Scorecard workflow exists
- release provenance workflow exists
- release SBOM workflow exists
- Dependabot configuration exists

### Release reviewability

- release checksum workflow support exists
- release manifest handling exists
- SBOM generation workflow exists
- GitHub-native attestation workflow support exists
- code-signing documentation exists

## What is partially in place

These areas exist, but are not yet fully mature:

### Code scanning

- CodeQL currently covers only the managed C# surface
- native C/C++ coverage still needs a more stable dedicated path

### Scorecard usage

- Scorecard workflow exists
- it should be treated as a baseline signal, not as proof that the repository is “finished” from a security standpoint

### CI and build stability

- the repository has substantial build and test automation
- however, the maintained line still has workflow-noise and build-path issues that should be cleaned up before presenting the project as fully polished

### Release trust posture

- checksums, SBOM, and attestation support exist
- those workflows should continue to be validated against current release practices as the release line evolves

## What still needs manual repository configuration or owner attention

These items are not solved by files alone:

- ensure GitHub **Private Vulnerability Reporting** stays enabled
- ensure dependency graph and related dependency alerts remain enabled
- ensure branch-protection posture is set appropriately for the maintained branch
- review repository Actions permissions and reusable-workflow policy periodically
- decide how GitHub Actions dependency updates should be handled when workflow stability is sensitive

## What still needs engineering work

### Build-chain cleanup

Examples:

- reduce unnecessary workflow noise on documentation-only changes
- stabilize problematic workflow paths before treating CI as fully mature
- improve separation between routine contributor checks and heavier release-oriented jobs

### Native analysis expansion

Examples:

- extend static-analysis coverage for native C/C++ components
- make native dependency and build inputs easier to reason about for external reviewers

### Dependency lifecycle clarity

Examples:

- document refresh expectations for vendored upstream snapshots
- keep action and automation dependency updates more intentionally controlled

### Release process hardening

Examples:

- continue improving release-signing clarity
- keep attestation and SBOM outputs aligned with the actual release assets users download
- ensure release verification guidance remains accurate for the current line

## Suggested next steps before any external submission

Before using the repository for badge-style submission or recommendation outreach, it is reasonable to complete the following:

1. make sure the maintained branch workflow set is stable enough that routine repository activity does not create distracting failures
2. confirm that release verification artifacts are present and correct on the latest maintained releases
3. confirm that repository settings needed for vulnerability reporting and dependency visibility are enabled
4. review Scorecard output and capture any intentional exceptions or known gaps
5. decide how to present current CI limitations honestly in any external review context

## Honest positioning

The current repository is already far stronger than a project that only publishes binaries and source code without any security narrative.

However, it should still be described honestly as:

- a project with a strong trust-documentation foundation
- a project with meaningful release-review improvements already in place
- a project that is still maturing in some workflow and native-build areas

That is a credible position, and it is better than overstating readiness.
