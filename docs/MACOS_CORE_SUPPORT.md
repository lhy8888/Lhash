# macOS Apple Silicon CLI and core

LHash supports the generic `arm64` target on Apple Silicon M-series Macs. The
macOS CLI uses the same engine, algorithm registry and static OpenSSL 3.5.9
backend as Windows. All **18** Windows algorithms are available; the default
selection is also shared: SHA-256, SHA-512 and SHA3-256.

## Download

Starting with [v1.12.6](https://github.com/lhy8888/Lhash/releases/tag/v1.12.6),
the tagged release includes `LHash-macos-arm64-cli-<shortsha>.tar.gz` alongside
the Windows packages. The release's `SHA256SUMS.txt`, `RELEASE_MANIFEST.txt`,
CycloneDX SBOM and attestations cover this archive too. See
[release verification](RELEASE_VERIFICATION.md) to verify the downloaded archive
and its source commit.

Extract the macOS archive, enter its directory and run:

```sh
shasum -a 256 -c SHA256.txt
./lhash -a sha256 /path/to/file
```

The package remains unsigned and unnotarized. Apple Silicon uses the same
generic arm64 executable across M-series generations; this is not a claim that
CI has run on each individual model.

## Build and run

Use native arm64 development tools, CMake, Perl and Xcode Command Line Tools:

```sh
bash tools/build_openssl_vendor_posix.sh "$PWD/build/openssl-install"
cmake -S . -B build/macos-cli -DCMAKE_BUILD_TYPE=Release \
  -DCMAKE_OSX_ARCHITECTURES=arm64 \
  -DLHASH_ENABLE_OPENSSL_VENDOR=ON -DOPENSSL_ROOT_DIR="$PWD/build/openssl-install" \
  -DLHASH_BUILD_CLI=ON -DLHASH_BUILD_RUNTIME_TESTS=ON
cmake --build build/macos-cli --parallel 3
ctest --test-dir build/macos-cli --output-on-failure --no-tests=error
build/macos-cli/bin/lhash --list-algorithms
build/macos-cli/bin/lhash --algorithm sha256 example.bin
build/macos-cli/bin/lhash --all --json example.bin
```

The CLI refuses a stub-only build. CMake requires the selected OpenSSL installation
to be version 3.5.9 and resolves both headers and static library inside that root.
No system OpenSSL installation is silently substituted.

## CLI interface

- `lhash <file> [file...]`: calculate the shared default algorithms.
- `--list-algorithms`: list stable IDs, labels and default selections; combine with
  `--json` for a machine-readable array.
- `-a ID` / `--algorithm ID`: select one algorithm; repeat for multiple algorithms.
  Stable Windows IDs and normalized aliases such as `sha256`, `sha3-256`, `md5`,
  `blake3-256`, `xxh3-128` and `crc32c` are accepted.
- `--all`: calculate all 18 algorithms, including explicitly labelled legacy
  MD5/SHA1 options. BLAKE3 XOF returns 128 bytes, SHAKE128-256 returns 32 bytes,
  and SHAKE256-512 returns 64 bytes, matching Windows.
- `--parallel` / `--single-threaded`: choose the digest execution policy.
- `--json`: one JSON object per file, with `path`, `status`, and either `digests`
  keyed by stable ID or `error`. Human output escapes control characters in paths.
- `--`: end options, allowing filenames beginning with `-`.

Exit codes: 0 for success, 1 for a file/runtime/output failure, 2 for invalid arguments.
A write or flush failure on standard output returns a nonzero status, including
algorithm-list output and JSON records.
A failed file does not prevent later valid files from being processed. Arguments
must be UTF-8; file inputs share the Windows limit of 8192 per request.

## File and platform contract

The file backend rejects leaf symbolic links and non-regular files, opens with
no-follow/nonblocking semantics before checking the descriptor identity, and
uses descriptor-based metadata. Positive short reads are consumed until actual
EOF, interrupted reads are retried, and detectable changes to size/timestamps or
read length prevent publishing a successful digest. This check detects unstable
files; it does not provide a filesystem snapshot guarantee. Ancestor symlinks
remain allowed, and this utility does not promise directory sandbox confinement.

The [macOS CLI workflow](../.github/workflows/macos-cli-build.yml) packages the
arm64 executable, checksum and build metadata after the engine/CLI tests pass.
It also supplies the macOS package to the tagged release workflow. Branch CI
retains its downloadable workflow artifact for testing between releases.
Artifacts remain unsigned and unnotarized. There is no macOS GUI or Intel build.
All M-series models use the same arm64 target; hosted CI validates its selected
machine, not every M5/M6 hardware model. Linux's explicitly enabled POSIX test
backend is for cloud regressions and does not establish native macOS filesystem
results.

See [testing](TESTING.md) for native, sanitizer, exception and file-integrity
coverage, and [core build](CORE_BUILD.md) for Windows build commands.
