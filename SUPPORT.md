# Support and Maintenance Scope

## Maintained line

The actively maintained release line is centered on:

- the `future-winui-was2` branch
- the latest GitHub Release published from that maintained line

Older tags, historical forks, and legacy snapshots may still be useful for reference, but they should be treated as **best effort only** unless explicitly stated otherwise.

## What is currently supported

The maintained line currently focuses on:

- Windows desktop usage
- local file hashing and verification workflows
- release-bundle verification and reviewability improvements
- the repository’s documented release and supply-chain metadata workflow

## What “supported” means here

For this repository, supported generally means:

- bugs may be fixed on the maintained line
- release and trust-model documentation may be updated
- workflow and supply-chain metadata may be improved over time
- security-sensitive issues on the maintained line are handled with higher priority than ordinary cosmetic issues

Support does **not** mean every older version, experimental branch, or downstream modification will be debugged in depth.

## Best-effort areas

The following areas are best effort unless explicitly called out in a release note or repository note:

- older historical tags
- unofficial repackaged builds
- user-modified local builds
- experimental or partially migrated interface layers
- edge-case compatibility with external tooling that the maintained line does not officially document

## Security and trust issues

If you believe you found a vulnerability or a trust-boundary issue, do **not** open a public bug report first.

Use the process in [SECURITY.md](SECURITY.md).

## Verification and release questions

If your question is about whether a release is official, expected, or reviewable, start with:

- [docs/RELEASE_VERIFICATION.md](docs/RELEASE_VERIFICATION.md)
- [docs/SUPPLY_CHAIN_SECURITY.md](docs/SUPPLY_CHAIN_SECURITY.md)

## What may still change significantly

The maintained line is still evolving. The following may continue to change as the project matures:

- release metadata structure
- verification guidance
- workflow layout
- native and managed build integration
- UI modernization details
- dependency maintenance policy for selected components

## What users should prefer

For the most stable experience, users should prefer:

- the latest GitHub Release on the maintained line
- the project’s documented verification path
- official release assets and metadata from the repository

## Practical summary

If you want the version that the repository is actively trying to keep trustworthy, reviewable, and supportable, use:

- `future-winui-was2`
- or the latest GitHub Release produced from it
