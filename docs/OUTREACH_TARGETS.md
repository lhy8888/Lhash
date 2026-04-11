# Outreach Targets

## Purpose

This document lists realistic places to introduce, submit, or discuss LHash as a Windows trusted local verification tool.

It is intentionally practical: start with the easiest and most relevant targets first.

## Tier 1: easiest and most realistic first

### 1. AlternativeTo

Why it is a good target:

- it is a crowdsourced software discovery platform
- users actively look for alternatives to existing tools
- a local Windows verification tool fits the site’s discovery model well

What to prepare before adding LHash:

- short one-line product description
- screenshots
- supported platform list
- clear license information
- a few accurate tags and features

Recommended positioning there:

- Windows
- Open Source
- Local verification
- Checksum or file integrity
- Privacy-friendly / no account required

### 2. GitHub Topics

Why it matters:

- GitHub Topics improve discovery inside GitHub itself
- users searching by topic can find the project more easily
- this is low effort and immediate

Recommended first pass Topics are documented in:

- `docs/REPO_METADATA_SUGGESTIONS.md`

### 3. Awesome lists on GitHub

The best early candidates are lists where maintainers routinely accept pull requests for new tools.

Good first candidates:

- Windows privacy / security lists
- privacy tools lists
- sysadmin and verification-adjacent lists, if the fit is honest

Before submitting:

- make sure the short project description is accurate
- keep the suggested entry one sentence long
- do not oversell the project category

## Tier 2: stronger but more selective targets

### 4. Privacy Guides Community

Why it can matter:

- it is a large privacy and security community with active tool discussion
- it can be useful for feedback even before formal recommendation is realistic

Best approach:

- start as a transparent tool-introduction or tool-suggestion discussion
- do not open with “please recommend my project”
- instead explain the trust model, local-first design, release verification posture, and current limits

### 5. OpenSSF Best Practices / Baseline path

Why it matters:

- it gives external users a recognizable signal for security and project maturity
- it helps translate repository work into a badge-style trust signal

Best approach:

- use `docs/OPENSSF_PREP.md` as the current internal checklist
- start with the baseline-oriented path and be honest about remaining gaps

## Tier 3: later, after more maturity or traction

### 6. Recommendation-style security and privacy lists

These can be valuable later, but they are more selective and often expect:

- stronger ecosystem fit
- clearer user base
- more project maturity
- consistent documentation and release quality over time

LHash can become a stronger candidate here after the project demonstrates continued stable releases and clearer real-world user adoption.

## Submission order recommendation

If you want a practical order, use this:

1. GitHub About + Topics
2. AlternativeTo entry
3. one or two relevant awesome lists
4. Privacy Guides Community discussion thread
5. OpenSSF baseline or best-practice submission work
6. more selective recommendation lists later

## Suggested one-line entry for list submissions

Use a short sentence like this:

> LHash is a Windows trusted local verification tool for file integrity checks, release review, and safer offline verification workflows.

Alternative shorter version:

> Windows local verification tool with safer defaults, release checksums, SBOM support, and reviewable trust documentation.

## Suggested short community intro

A reasonable short intro for communities is:

> I maintain LHash, a Windows local verification tool that goes beyond simple hashing by focusing on safer defaults, repeatable results, and more reviewable releases through checksums, documentation, SBOM support, and attestation workflows.

## What not to do in outreach

Avoid:

- claiming LHash is a full authenticity platform
- claiming it is a malware scanner or endpoint protection tool
- pitching it as universally recommended before the ecosystem fit is proven
- submitting to too many unrelated lists at once

## Best current outreach angle

The strongest current angle is not “the fastest hash utility.”

The strongest current angle is:

- local-first verification
- safer Windows defaults
- clearer trust boundaries
- better release reviewability than a generic checksum tool
