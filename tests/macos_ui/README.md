# macOS native interface integration tests

`macos_ui_tests.mm` links the production `AppController.mm`, `HashSession` and
hash core. It opens real AppKit windows and drives their controller on the main
thread while pumping the native event loop. It does not require Accessibility
automation permission or a mock widget library.

The eight test groups cover:

- The desktop layout, native selectable/read-only text, four task columns,
  Unicode filenames, and the same three default digests as Windows.
- Duplicate files, task history across sessions, exact case-insensitive digest
  verification, invalid/prefix input, clearing verification and results.
- All 18 algorithm values against the independent CLI reference fixture,
  algorithm-selection validation and persistence, uppercase rerendering.
- The real pasteboard, exported UTF-8 BOM/content, and export error reporting.
- 128 ordered result appends while an existing native text range is selected.
- Folder recursion, a symbolic-link cycle, and failure continuation.
- The native file-URL drag pasteboard/destination handler, rejection of a
  network URL, and individual checksum links copying to the real pasteboard.
- Cancellation, a subsequent session, and closing/releasing a busy controller
  without waiting for file I/O or leaving queued UI callbacks with stale owners.

Build on macOS 14+ Apple Silicon with `LHASH_BUILD_MACOS_GUI=ON` and
`LHASH_BUILD_RUNTIME_TESTS=ON`. Run the registered CTest test or invoke its
executable with `tests/cli/algorithm_vectors.json` as the sole argument.
`LHASH_UI_ARTIFACT_DIR` optionally selects a directory for a PNG captured from
the actual native content view. GitHub Actions retains it with test diagnostics.

Each run uses an isolated preferences suite and temporary files. The cancellation
fixture is sparse; its logical size is 8 GiB without allocating 8 GiB of data.
The test writes the runner's clipboard. It exercises in-process native controls
and controllers; it does not claim Finder dialogs, every display scale, or
manual Gatekeeper/notarization validation.
