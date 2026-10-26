# Windows input policy regression tests

These tests compile the current production function bodies for COPYDATA validation,
dialog file-count limits, folder enumeration, registry command generation,
executable-path sizing, clipboard ownership/cleanup, bounded Windows version API results,
and the fail-closed token policy for external file input. Win32/MFC APIs are explicit
test doubles. They test production C++ control flow and bounds on every CI platform;
they do not claim native dialog, clipboard, Shell resolution, or UIPI integration.
`extracted_functions.json` records the source locations and hashes used by the build.

Run with CMake/CTest from this directory. GCC/Clang builds can enable
`-DLHASH_INPUT_SANITIZERS=ON`. Release builds keep the test assertions enabled.

Native Windows follow-up should cover normal and elevated instances, a real file
dialog at 8191/8192/8193 selections, cancelling a large directory scan, copying text
while hashing, and right-click launch from a long installation path with spaces.

The folder UI probe extracts the actual `LoadFolderDialogSelection` wait loop and
input transaction guard. It checks cancel, close and `WM_QUIT` while a worker remains
blocked, stopping the progress dialog before close/quit propagation, rejection of a
second scan, and preservation of the old selection on errors or cancellation.
Its dialog, message, and worker boundary objects are test doubles.

`lhash_background_result_tests` uses the production atomic result state on every
platform, including cancellation before a late result, one-time delivery, concurrent
publication and independent lifetime after the UI releases ownership.

On Windows, `lhash_folder_scan_native_tests` also runs the production CRT worker and
actual `FindFirstFile`/`FindNextFile` against temporary directories, including a Unicode
subdirectory, empty and missing directories. A controlled blocking worker checks
non-waiting cancellation, stale-result rejection and owner destruction; injected
exceptions check failure publication. The blocking case uses a test event, not a
real network outage or faulty filesystem driver. This test does not automate the
Shell folder picker or its progress window.

Folder enumeration now owns its path and result independently of the window.
Cancellation signals an atomic flag and requests `CancelSynchronousIo` on a retained
thread handle, but never requires the UI to join a blocked worker. One cancelled
worker may remain until its system call returns; another folder scan is rejected in
that interval, while direct file selection and application close remain available.
