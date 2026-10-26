# macOS session regression tests

These tests call the same C++ session worker used by the AppKit interface and
the real shared hash engine. They run on native Apple Silicon and on the Linux
diagnostic file backend with the pinned OpenSSL vendor build enabled.

Coverage includes the 18 independent `abc` digest vectors, Windows-compatible
defaults, upper/lowercase and parallel calculation, Unicode names, duplicate
input identities, successive sessions, missing/symlink/FIFO failure continuation,
recursive folders without following links, depth/file budgets and visible
warnings, deferred polling of 8192 terminal results, cancellation of running and
pending files, monotonic elapsed time and rejected invalid input.

The test executable is `lhash_macos_session_tests`. Native AppKit interaction
tests separately cover the actual macOS window and actions. The session tests
do not emulate macOS controls or claim real faulty-network filesystem testing.
