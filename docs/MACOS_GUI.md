# macOS desktop app

LHash provides a native AppKit desktop interface for **macOS 14+
on Apple Silicon**. Its layout and file-hashing actions follow the Windows
desktop app. The interface calls the shared C++ engine directly, with the same
18 algorithms and OpenSSL 3.5.9 backend as Windows and the macOS CLI.

Starting with **v1.13.0**, the desktop app is included in the official release
alongside the macOS CLI and Windows apps.

## Get the app

Open the [latest release](https://github.com/lhy8888/Lhash/releases/latest)
and download `LHash-macos-arm64-app-<shortsha>.zip`. Check the archive against
the release's `SHA256SUMS.txt`, then extract it.

Development builds are also available as the `LHash-macos-arm64-app` artifact
from successful [macOS workflow](https://github.com/lhy8888/Lhash/actions/workflows/macos-cli-build.yml)
runs. These artifacts have their own source commit and package checksums.

The package directory contains:

- `LHash.app`: the desktop application.
- `BUILD_INFO.txt`: source commit, version, architecture and signing metadata.
- `SHA256.txt`: checksums for the package's regular files.
- `README.txt` and `LICENSES/`: usage and license notices.

From that directory, verify the files before copying the app to Applications:

```sh
shasum -a 256 -c SHA256.txt
```

The app has a local **ad-hoc signature**, not an Apple Developer ID signature,
and is **not notarized**. Its signature can be checked with:

```sh
codesign --verify --deep --strict LHash.app
codesign -dv --verbose=2 LHash.app
```

Ad-hoc signing checks bundle integrity; it does not establish a verified
developer identity. macOS Gatekeeper may require explicit user approval for
an app downloaded from the internet. Check the source and package checksum
before deciding whether to open it through macOS Privacy & Security settings.

## Use it

The main window keeps the Windows arrangement: action buttons at the top,
selectable result text, a table of file/algorithm/status/progress rows, and
overall progress below. Controls follow native macOS appearance and shortcuts;
this is an AppKit app, not an embedded Windows window. The UI supports Chinese
and English.

| Action | Behavior |
| --- | --- |
| Open | Select one or more local files and start hashing. |
| Open Folder | Collect files from a folder in the background. |
| Drag and drop | Add file or folder URLs from Finder. |
| Settings | Select from the same 18 algorithms as Windows. |
| Verify | Compare an expected hexadecimal digest with calculated results. |
| Copy All | Copy the result text to the macOS clipboard. |
| Export | Save results as UTF-8 text. |
| Stop | Request cancellation; finish task states after the worker exits. |
| Clear | Remove results and task history while idle. |

The initial algorithm selection is **SHA-256, SHA-512 and SHA3-256**. Duplicate
inputs remain separate tasks. Errors on one input do not turn other files into
successful results or discard results that completed. Folder discovery and
hashing run outside the AppKit thread; progress updates are polled and applied
on that thread.

The file-handling contract and the 8192-file per-session limit are shared with
the core. See [macOS file handling](MACOS_CORE_SUPPORT.md#file-and-platform-contract).
Cancellation cannot forcibly interrupt every blocking filesystem operation;
an operation on an unavailable network mount may take time to return. The
window remains responsive while its worker owns that operation.

## Build

On an Apple Silicon Mac, install Xcode Command Line Tools, CMake and Perl, then
run from the repository root:

```sh
bash tools/build_openssl_vendor_posix.sh "$PWD/build/openssl-install" optimized
cmake -S . -B build/macos-app -DCMAKE_BUILD_TYPE=Release \
  -DCMAKE_OSX_ARCHITECTURES=arm64 -DCMAKE_OSX_DEPLOYMENT_TARGET=14.0 \
  -DLHASH_ENABLE_OPENSSL_VENDOR=ON -DOPENSSL_ROOT_DIR="$PWD/build/openssl-install" \
  -DLHASH_BUILD_MACOS_GUI=ON -DLHASH_BUILD_CLI=ON -DLHASH_BUILD_RUNTIME_TESTS=ON
cmake --build build/macos-app --parallel 3
ctest --test-dir build/macos-app --output-on-failure --no-tests=error
open build/macos-app/bin/LHash.app
```

The GUI requires the real vendor backend; it cannot expose a stub-only algorithm
set. The arm64 target covers M-series generations without a chip-model whitelist.
There is no Intel or universal macOS build, and hosted CI does not establish
testing on every individual M-series model.

## Validation

The macOS workflow builds the app and CLI, runs the shared engine/algorithm
regressions and native AppKit tests, verifies the app bundle and ad-hoc signature,
and packages separate GUI and CLI artifacts. Check the workflow run for the
exact commit you download; a successful earlier CLI build is not evidence for
a later GUI build.

Portable session tests can also run against the Linux diagnostic file backend.
They do not replace native AppKit execution or macOS filesystem tests. See the
[test guide](TESTING.md) for commands and coverage boundaries.
