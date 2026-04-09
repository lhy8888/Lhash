# Code Signing

LHash can only remove the Windows `Publisher unknown` experience by using a **publicly trusted code-signing identity**.

Self-signed certificates, local test certificates, or private enterprise trust chains are useful for internal validation, but they do **not** solve the public Windows trust problem for normal users.

## Supported release signing paths

### 1. Azure Artifact Signing Public Trust

This is the preferred managed path for the maintained release line.

GitHub Actions support is wired into [windows-build.yml](.github/workflows/windows-build.yml) through `azure/trusted-signing-action`.

Configure these GitHub repository secrets:

- `LHASH_TRUSTED_SIGNING_TENANT_ID`
- `LHASH_TRUSTED_SIGNING_CLIENT_ID`
- `LHASH_TRUSTED_SIGNING_CLIENT_SECRET`
- `LHASH_TRUSTED_SIGNING_ENDPOINT`
- `LHASH_TRUSTED_SIGNING_ACCOUNT_NAME`
- `LHASH_TRUSTED_SIGNING_CERTIFICATE_PROFILE_NAME`

The release workflow signs the shipped native `LHash.exe` when all six values are present.

Recommended Microsoft setup path:

1. Create an Azure Artifact Signing account.
2. Complete identity validation.
3. Create a **Public Trust** certificate profile.
4. Grant the signing identity the `Artifact Signing Certificate Profile Signer` role.
5. Store the GitHub secrets above.
6. Publish a tag such as `v1.10.1`.

### 2. Traditional PFX certificate

The legacy fallback path remains available.

Configure these GitHub repository secrets:

- `LHASH_SIGN_PFX_BASE64`
- `LHASH_SIGN_PFX_PASSWORD`

This path uses [sign_legacy_exe.ps1](trunk/sign_legacy_exe.ps1) and `signtool.exe`.

## Behavior in CI

- If Artifact Signing secrets are present, the release workflow uses Artifact Signing first.
- If Artifact Signing is not configured but PFX secrets are present, the release workflow falls back to the PFX path.
- If neither signing path is configured, the build still completes, but Windows will continue to show `Publisher unknown`.

## Important notes

- Timestamping is required for long-term signature validity. The workflow uses `http://timestamp.acs.microsoft.com` for Artifact Signing and keeps timestamping enabled in the PFX path as well.
- Public trust signing improves Windows trust, but SmartScreen reputation can still depend on publisher and file reputation over time.
- The shipped default release line is the lightweight native desktop package, so that is the executable currently wired for formal signing.

## Official references

- [Artifact Signing overview](https://learn.microsoft.com/en-us/azure/trusted-signing/overview)
- [Artifact Signing quickstart](https://learn.microsoft.com/en-us/azure/artifact-signing/quickstart)
- [Set up signing integrations to use Artifact Signing](https://learn.microsoft.com/en-us/azure/artifact-signing/how-to-signing-integrations)
- [Artifact Signing trust models](https://learn.microsoft.com/en-us/azure/artifact-signing/concept-trust-models)
