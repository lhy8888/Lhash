# Contributing to LHash

Thank you for helping improve LHash.

The maintained release line is not just trying to add features. It is trying to improve **local verification trust**, **release reviewability**, and **engineering reliability** at the same time. Contributions are welcome, but changes should be made carefully.

## Before you start

Please read these first if your change touches security, trust, release handling, or dependency integration:

- [README.md](README.md)
- [SECURITY.md](SECURITY.md)
- [docs/THREAT_MODEL.md](docs/THREAT_MODEL.md)
- [docs/SECURITY_MODEL.md](docs/SECURITY_MODEL.md)
- [docs/RELEASE_VERIFICATION.md](docs/RELEASE_VERIFICATION.md)
- [docs/SUPPLY_CHAIN_SECURITY.md](docs/SUPPLY_CHAIN_SECURITY.md)

## What kinds of contributions are welcome

Examples of useful contributions:

- bug fixes with clear reproduction steps
- correctness fixes in hashing, verification, or result handling
- improvements to local-safety defaults
- test coverage improvements
- better release documentation and verification guidance
- carefully reviewed dependency maintenance
- UI improvements that do not weaken the trust model

## What needs extra care

The following areas should be treated as security-sensitive:

- file and path handling
- command-line parsing
- drag-and-drop input
- shell integration
- `WM_COPYDATA` handling
- hashing runtime state and concurrency behavior
- release automation and artifact publication
- vendored third-party code updates
- code-signing or release-verification changes

If your change touches any of these, explain the impact clearly in the pull request.

## Contribution principles

### 1. Correctness before convenience

LHash should prefer explicit failure over silently producing a misleading verification result.

### 2. Local-first behavior

Do not add features that quietly upload file contents, hashes, or verification metadata to external services by default.

### 3. Keep trust boundaries clear

Avoid changes that mix UI state, verification runtime state, and release trust logic in a way that becomes harder to reason about later.

### 4. Keep algorithm semantics honest

Do not present legacy algorithms, modern cryptographic hashes, and non-cryptographic checksums as if they provided the same security properties.

## Reporting bugs

For normal bugs, use the bug report issue template.

For security vulnerabilities or suspected trust-boundary issues, do **not** open a public issue first. Follow [SECURITY.md](SECURITY.md).

## Pull request expectations

A good pull request should normally include:

- a clear description of the change
- the reason for the change
- the user-visible or trust-model impact
- test notes or validation notes
- release or documentation impact, if any

For security-sensitive changes, include:

- which trust boundary is affected
- whether the change modifies default behavior
- whether the change affects release verification or supply-chain posture

## Dependency and third-party code updates

Dependency updates are welcome, but should be handled carefully.

### GitHub Actions and automation dependencies

When updating GitHub Actions or automation dependencies:

- explain why the update is needed
- avoid mixing unrelated workflow changes in the same PR
- note any permission or runner requirement changes

### Vendored third-party code

For vendored components such as fixed-version upstream algorithm code:

- keep the upstream source and version explicit
- document why the update is being made
- avoid silent bulk refreshes with no review notes
- note any API, ABI, or build-system changes that affect the maintained line

## Release-facing changes

If your change affects release output, release metadata, or verification guidance, please also review:

- `release-provenance.yml`
- `release-sbom.yml`
- `docs/RELEASE_VERIFICATION.md`
- `docs/SUPPLY_CHAIN_SECURITY.md`

## Coding and review style

Please keep changes:

- focused
- reviewable
- well-scoped
- honest about limitations

Small, clearly justified PRs are much easier to review and much safer to merge than broad mixed-purpose changes.

## If you are unsure

If you are unsure whether a change belongs in the maintained release line, open an issue first and describe:

- what you want to change
- why it matters
- whether it affects correctness, security, release trust, or compatibility

That is usually better than sending a large PR with unclear scope.
