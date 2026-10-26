# Changelog

Release notes for the maintained LHash line. Earlier entries below describe
their respective releases; the historical upstream release log is not duplicated.

## 1.13.0 - 2026-10-26

LHash comes to the Mac desktop. This release adds a native Apple Silicon app
with the familiar Windows workflow, alongside the existing Windows apps and
macOS command-line tool.

### New macOS desktop app

- Open files or folders, or drag them from Finder, and calculate checksums with
  the same 18 algorithms available on Windows.
- Verify an expected digest, copy results or individual hashes, and export
  results to a text file.
- Follow each file's status and progress, stop a batch, and keep completed
  results for comparison.
- Choose algorithms and letter case in Settings, with preferences saved for
  the next launch.
- Use a Chinese or English interface with native Mac controls and keyboard
  shortcuts on macOS 14 or later with Apple Silicon.

### Release and validation

- Add `LHash-macos-arm64-app-<commit>.zip` to the release alongside Windows x64,
  Windows ARM64 and the macOS CLI, with shared release checksums and metadata.
- Add native Mac interface tests for file handling, verification, clipboard,
  export, settings, drag and drop, result updates and cancellation.
- Retain the OpenSSL 3.5.9 backend and shared algorithm set across platforms.
- Refresh the README around features, everyday uses and getting started.

## 1.12.6 - 2026-10-26

This release updates the hash library, adds the macOS Apple Silicon CLI to the
release packages, and fixes Windows result rendering and task lifecycle issues.

### Hashing and platform support

- Update the pristine OpenSSL vendor source to 3.5.9, with the official archive
  SHA-256 and per-file source manifest recorded alongside the import.
- Enable upstream OpenSSL assembly optimizations for Windows x64/ARM64 and
  macOS arm64. Keep an explicit `portable` mode and validate target/mode metadata
  before reusing a built library.
- Connect the macOS CLI to the same 18-algorithm registry as Windows, including
  JSON output, algorithm selection, and sequential/parallel execution options.
- Target Apple Silicon through one generic arm64 build; remove macOS Intel
  support. The macOS package remains unsigned and unnotarized.

### Windows output and task state

- Fix an incremental-display regression that inserted new text at an old
  selection, causing overdraw and file/digest order mismatches. Append at the
  actual control end and preserve the user's selection.
- Track updates by session and task identity so duplicate file paths retain
  separate completion states.
- Finalize unfinished task rows after cancellation or abnormal worker exit,
  preserving completed results and keeping later sessions independent.
- Avoid dialog-manager message collisions in worker notifications.

### Responsiveness and error reporting

- Use task-to-row indexing and incremental status counts, and deliver text
  changes without holding the producer lock during UI updates.
- Move folder enumeration to a worker with bounded scanning, cancellation and
  shutdown handling that does not wait for a blocked enumeration call.
- Measure elapsed time with a monotonic clock instead of counting timer messages.
- Flush and check CLI stdout before returning success, including algorithm lists
  and JSON output; return a failure status when writing fails.

### Release and validation

- Publish Windows x64, Windows ARM64 and macOS arm64 CLI packages under the same
  tag, with shared checksums, a release manifest, a CycloneDX SBOM and attestations.
- Update application metadata to `1.12.6.0`, refresh the README and synchronize
  issue templates and release metadata checks.
- Expand native Windows GUI coverage to 18 cases per architecture, including
  ordered records from 128 different files and selections held during appends.
  Add a real EDIT-control regression test with the old API behavior as a
  negative control.
- Exercise optimized and portable builds on native Windows x64/ARM64 and macOS
  arm64, plus Linux/macOS sanitizer and shared engine/CLI regressions.

See [audit fixes](docs/AUDIT_FIXES.md) and [testing](docs/TESTING.md) for the
implementation details and coverage boundaries.

## 1.12.5 - 2026-05-05

Patch release focused on the architecture cleanup pass, release documentation
refresh, and version metadata alignment.

### Architecture cleanup

- renamed the active MFC bridge layer to clearer adapter names
- removed dead legacy compatibility and managed bridge islands from the active tree
- split common headers into clearer domain, runtime, and platform boundary files

### Documentation and release metadata

- updated the current release references to `v1.12.5`
- refreshed the README, changelog, issue templates, and release metadata tests
- updated the maintained Windows version metadata to `1.12.5.0`

### Consistency cleanup

- tightened the bridge and source-list consistency checks
- clarified the OpenSSL provider availability model
- kept the maintained release line aligned with the current native and CI boundaries

## 1.12.4 - 2026-04-30

Patch release focused on the pristine OpenSSL 3.5.6 vendor upgrade, release
documentation refresh, and a cleanup pass over stale consistency checks.

### Release and packaging

- upgraded the maintained Windows vendor tree to pristine OpenSSL 3.5.6
- removed the retired `third_party/openssl/3.0.20` vendor tree
- kept the Windows x64 and ARM64 release surface aligned with the current
  vendor policy

### Documentation and release metadata

- updated the current release references to `v1.12.4`
- refreshed the README, changelog, issue templates, and release metadata tests
- updated the maintained Windows version metadata to `1.12.4.0`

### Consistency cleanup

- aligned algorithm-order assertions with the current registry order
- tracked the newly registered native runtime tests
- removed retired Windows OsUtils variants from the active source tree
- changed `HashDigestRuntimePlan` to own its request and queue-plan state by
  value
