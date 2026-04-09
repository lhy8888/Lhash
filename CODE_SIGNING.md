# Code Signing

LHash can only remove the Windows `Publisher unknown` experience by using a **publicly trusted code-signing identity**.

Self-signed certificates, local test certificates, or private enterprise trust chains are useful for internal validation, but they do **not** solve the public Windows trust problem for normal users.

## Preferred public-trust path: SignPath Foundation

For this project, the preferred no-cost public code-signing path is **SignPath Foundation** for open source software.

Official project pages:

- [SignPath Foundation home](https://signpath.org/)
- [Apply for a free SignPath.io subscription](https://signpath.org/apply.html)
- [Terms](https://signpath.org/terms.html)

Why this is the preferred route:

- it is intended for qualifying open source projects
- it can provide publicly trusted Windows code signing without a monthly Azure subscription
- it fits LHash better than a paid enterprise signing service

## What SignPath requires

Before applying, the project should clearly publish a code-signing policy and keep release ownership easy to understand.

LHash publishes that policy here:

- [Code signing policy](CODE_SIGNING_POLICY.md)

## Current CI behavior

The current GitHub workflow keeps only a simple optional PFX signing fallback for internal or temporary use.

- If `LHASH_SIGN_PFX_BASE64` and `LHASH_SIGN_PFX_PASSWORD` are configured, CI can still Authenticode-sign builds with that certificate.
- If they are not configured, the build still completes, but Windows will continue to show `Publisher unknown`.

This fallback is **not** the preferred public release plan.

## Important note about SmartScreen

Even after moving to a publicly trusted signing path, Microsoft Defender SmartScreen can still depend on reputation over time.

Public trust signing fixes the publisher trust chain. It does not guarantee instant SmartScreen reputation for a newly signed app.

## What remains to do

1. Apply to SignPath Foundation.
2. Wait for approval.
3. Add the approved SignPath project details and secrets to GitHub.
4. Wire the GitHub release workflow to the approved SignPath project.
5. Publish the next release as the first publicly trusted signed LHash build.
