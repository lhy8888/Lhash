# LHash

![Windows Build](https://github.com/lhy8888/Lhash/actions/workflows/windows-build.yml/badge.svg?branch=codex%2Fopenssl-3.5.9-vendor)
![macOS CLI Build](https://github.com/lhy8888/Lhash/actions/workflows/macos-cli-build.yml/badge.svg?branch=codex%2Fopenssl-3.5.9-vendor)
![License](https://img.shields.io/badge/license-GPL--2.0-blue.svg)
![Platform](https://img.shields.io/badge/platform-Windows%20%7C%20macOS-0078D6)

LHash calculates checksums for local files. Use the Windows desktop app or the
macOS command-line tool to calculate SHA-256, compare a downloaded file with an
expected digest, or process a batch of files. Files are read locally; hashing
does not require an account or uploading file contents.

## Download

- Current release: [`v1.12.6`](https://github.com/lhy8888/Lhash/releases/tag/v1.12.6)
- Maintained branch: `codex/openssl-3.5.9-vendor`
- Windows UI mainline: `MFC`

Choose the package for your operating system and processor from the release page:

| Platform | Package | Interface |
| --- | --- | --- |
| Windows x64 | `LHash-windows-x64-<commit>.zip` | Desktop app (`LHash.exe`) |
| Windows ARM64 | `LHash-windows-arm64-<commit>.zip` | Native ARM64 desktop app (`LHash.exe`) |
| macOS Apple Silicon | `LHash-macos-arm64-cli-<commit>.tar.gz` | Native arm64 CLI (`lhash`) |

macOS uses one generic arm64 build for M-series Macs. There is no Intel build
or macOS GUI. The macOS package is unsigned and not notarized; Windows signing
depends on the release's recorded signature state.

The current release includes `SHA256SUMS.txt`, `RELEASE_MANIFEST.txt` and
`LHash-release.cyclonedx.json`. Compare the archive's SHA-256 with its entry in
`SHA256SUMS.txt` before extracting it. For example, Windows PowerShell can use:

```powershell
Get-FileHash .\LHash-windows-x64-*.zip -Algorithm SHA256
```

On macOS:

```sh
shasum -a 256 LHash-macos-arm64-cli-*.tar.gz
```

The [release verification guide](docs/RELEASE_VERIFICATION.md) covers complete
checksum verification, build metadata, signing state and GitHub attestations.

## Quick start

### Windows

1. Extract the matching Windows package and run `LHash.exe`.
2. Open files, open a folder, or drag files into the window.
3. Choose the algorithms you need. The defaults are SHA-256, SHA-512 and SHA3-256.
4. Use **Verify** to find an expected digest in the results, or copy/export the
   calculated values.

The task list shows each input separately, including repeated paths. Stop ends
the active session and marks unfinished tasks accordingly. Folder scanning runs
outside the UI thread so the window remains responsive while collecting files.

### macOS Apple Silicon

Extract the macOS package, then open a terminal in the extracted directory:

```sh
# Check the extracted executable against its bundled checksum.
shasum -a 256 -c SHA256.txt

# Calculate only SHA-256 for a local file.
./lhash -a sha256 "/path/to/file.iso"

# Use the same default algorithms as Windows.
./lhash "/path/to/file.iso"

# List all available algorithms or calculate all of them.
./lhash --list-algorithms
./lhash --all "/path/to/file.iso"

# Select multiple algorithms and write one JSON record per file.
./lhash -a sha256 -a blake3-256 --json "first.bin" "second.bin" > hashes.jsonl

# End option parsing for a filename beginning with a dash.
./lhash -a sha256 -- -example.bin
```

`--parallel` and `--single-threaded` select the digest execution policy.
Exit codes are **0** for success, **1** for file/runtime/output failures and
**2** for invalid arguments. Paths must use UTF-8. See
[macOS CLI support](docs/MACOS_CORE_SUPPORT.md) for build instructions and the
file-handling contract.

## Algorithms

Release builds expose the same **18 algorithms** on Windows and macOS:

| Family | Algorithms |
| --- | --- |
| SHA-2 | SHA-256, SHA-384, SHA-512 |
| SHA-3 | SHA3-256, SHA3-384, SHA3-512 |
| BLAKE2 | BLAKE2b-512, BLAKE2s-256 |
| SHAKE | SHAKE128-256 (32 bytes), SHAKE256-512 (64 bytes) |
| BLAKE3 | BLAKE3-256, BLAKE3-512, BLAKE3 XOF (128 bytes) |
| Legacy hashes | MD5, SHA-1 |
| Non-cryptographic checksums | XXH3-64, XXH3-128, CRC32C |

Choose SHA-256 when comparing a published SHA-256 checksum. MD5 and SHA-1 are
retained for compatibility; XXH3 and CRC32C are non-cryptographic checksums.
A matching digest establishes a content match with that digest, not the
identity of its publisher.

The SHA-2, SHA-3, BLAKE2 and SHAKE implementations use statically linked
**OpenSSL 3.5.9** from the pristine upstream source. Production builds enable
OpenSSL's upstream assembly optimizations and runtime CPU dispatch. An explicit
`portable` build mode remains available for compatibility checks and diagnosis;
see [OpenSSL build modes](docs/OPENSSL_OPTIMIZATION.md). BLAKE3 uses the native
NEON path on Apple Silicon.

## What's new in 1.12.6

- Upgrade the vendored OpenSSL library to 3.5.9 and enable its optimized builds
  on Windows x64/ARM64 and macOS arm64.
- Add the macOS Apple Silicon CLI with all 18 Windows algorithms and include its
  package in the tagged release.
- Fix Windows result text appearing overlapped or out of order during updates;
  append at the actual text end while retaining the user's selection.
- Preserve separate states for duplicate file inputs, and finish task states
  after cancellation or abnormal worker exit.
- Reduce task/text refresh overhead, move folder scanning off the UI thread,
  and measure elapsed time with a monotonic clock.
- Return a failure status when the CLI cannot write or flush its output.

See the [changelog](CHANGELOG.md), [中文更新日志](CHANGELOG.zh-CN.md),
[audit fixes](docs/AUDIT_FIXES.md) and [test coverage](docs/TESTING.md).

## Limits and file handling

Each hashing session accepts up to **8192 files**. Oversized file batches are
rejected; folder scans stop at their budget and warn when results are incomplete.
Path checks reject unsupported input types, and detectable file changes during
reading are reported as failures. These checks do not create a filesystem
snapshot. Details are in the [security model](docs/SECURITY_MODEL.md) and
[macOS file contract](docs/MACOS_CORE_SUPPORT.md#file-and-platform-contract).

## Build, tests and project documents

- [Windows Build workflow](.github/workflows/windows-build.yml): Windows x64 and
  native ARM64 runtime/GUI tests, packaging and tagged release publication.
- [Core Build Matrix workflow](.github/workflows/core-m2.yml): native core smoke
  and runtime checks on Windows and macOS.
- [macOS CLI Build workflow](.github/workflows/macos-cli-build.yml): Apple Silicon
  CLI build, full engine/algorithm tests and packaging.
- [Portable Runtime Tests](.github/workflows/portable-runtime-tests.yml): Linux
  diagnostic and macOS arm64 regressions, including sanitizer builds.
- [Core build instructions](docs/CORE_BUILD.md), [test guide](docs/TESTING.md)
  and [documentation index](docs/README.md).
- [Security policy](SECURITY.md), [supply-chain metadata](docs/SUPPLY_CHAIN_SECURITY.md)
  and [code signing](CODE_SIGNING.md).

Windows GUI coverage includes real x64/ARM64 runs, record ordering across 128
different files, and selection preservation while results arrive. Native
macOS CI validates its hosted arm64 machine; it does not claim individual
hardware testing of every M-series model. Linux is a diagnostic test backend,
not a supported release target.

License: **GPL-2.0-only with an OpenSSL linking exception**. See
[LICENSE](LICENSE) and [LICENSE-OPENSSL-EXCEPTION.md](LICENSE-OPENSSL-EXCEPTION.md).
