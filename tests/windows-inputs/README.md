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
