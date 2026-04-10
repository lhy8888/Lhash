# Security Policy

## Supported Versions

LHash is actively maintained on the `future-winui-was2` branch and through the latest tagged release published from that line.

| Version / line | Status |
| --- | --- |
| `future-winui-was2` | Supported |
| Latest GitHub Release | Supported |
| Older tags and historical forks | Best effort only |

## What to report

Please report security issues that could affect any of the following:

- Local file verification, hashing, and result handling
- Windows desktop input surfaces such as command-line, drag-and-drop, shell integration, and `WM_COPYDATA`
- Release packaging, signing, build pipeline, or supply-chain integrity
- Vendored dependency integration when it is shipped by this repository

Examples include memory corruption, privilege boundary mistakes, unsafe path handling, signature or release-integrity problems, sandbox or trust-boundary bypasses, and issues that could silently produce incorrect verification results.

## How to report a vulnerability

Please do **not** open a public issue for a suspected security vulnerability.

Use one of these private channels instead:

1. GitHub **Private Vulnerability Reporting** for this repository, when enabled
2. Email: **ipholhy@me.com**

When possible, include:

- A clear description of the issue and impact
- Reproduction steps or a proof of concept
- Affected version, branch, commit, or release tag
- Whether the issue requires local access, elevated privileges, or a crafted file/path
- Any suggested mitigation or patch direction

## Coordinated disclosure

Please allow time for investigation and a fix before public disclosure.

Target response goals:

- Acknowledgement within **7 business days**
- Status update after triage within **14 business days** when the report is reproducible
- Coordinated disclosure after a fix or mitigation is available

These are targets, not guarantees, but good-faith reports will be handled as seriously and quickly as possible.

## Scope notes

The following are normally out of scope unless they create a concrete security impact:

- Cosmetic UI issues
- Requests for algorithm additions without a security flaw
- Bugs in unsupported historical forks not maintained in this repository
- Vulnerabilities that exist only in a user-modified or locally patched build

## Safe harbor

Good-faith security research intended to help protect users is welcome. Please avoid privacy violations, destructive testing on third-party systems, social engineering, or actions that would place real users or infrastructure at risk.
