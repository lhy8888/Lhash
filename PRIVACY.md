# Privacy

LHash is a local file-hash utility.

## What the app does not do by default

- no account system
- no telemetry
- no background analytics
- no automatic upload of file content
- no automatic upload of hash values to third-party services in the maintained Windows release line

## What the app processes locally

When you hash files, LHash may locally process:

- file paths you choose
- file metadata needed for display and hashing
- the digest values produced from those files

This processing is performed locally so the app can show results to you.

## Data retention

LHash does not maintain a hosted backend for user data in this repository's release flow.
Hashes and file details are handled locally by the application unless you explicitly export, copy, or share them yourself.

## Releases and GitHub

GitHub Actions is used to build and publish release artifacts for this repository.
Standard GitHub platform logging and download records are governed by GitHub's own policies, not by an LHash-operated service.

## Contact and scope

This document describes the maintained LHash release line in this repository.
It replaces older upstream wording that referenced unrelated websites or services.
