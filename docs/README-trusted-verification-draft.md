# LHash README reference

The project overview, downloads and usage instructions are maintained in the
[main README](../README.md). This older draft location is retained as a short
reference for existing links.

LHash is a local file checksum tool with a Windows desktop app and a macOS Apple
Silicon command-line tool. Both release builds expose the same 18 algorithms,
including SHA-256, through the shared engine and static OpenSSL 3.5.9 backend.

- Current release: [`v1.12.6`](https://github.com/lhy8888/Lhash/releases/tag/v1.12.6)
- Maintained branch: `codex/openssl-3.5.9-vendor`
- Windows UI mainline: `MFC`
- Windows builds: x64 and ARM64.
- macOS build: generic Apple Silicon arm64 CLI; unsigned and not notarized.
- [Core Build Matrix workflow](../.github/workflows/core-m2.yml)
- [macOS CLI Build workflow](../.github/workflows/macos-cli-build.yml)
- [Windows Build workflow](../.github/workflows/windows-build.yml)
- [Release verification](RELEASE_VERIFICATION.md)
- [macOS CLI usage and build](MACOS_CORE_SUPPORT.md)
- [Test coverage](TESTING.md) and [audit fixes](AUDIT_FIXES.md)
- [Security policy](../SECURITY.md) and [code signing](../CODE_SIGNING.md)

Use the release's checksum list to verify downloaded archives. A matching hash
checks content against that digest; it does not independently identify the
publisher. The complete instructions and platform limits are in the main README.
