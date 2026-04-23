# Archive

This directory keeps historical project shells and packaging scripts that are no longer part of the current mainline build chain.

Archived on the current mainline cleanup pass:

- `legacy-projects/trunk/fHash.xcworkspace`
- `legacy-projects/trunk/fHashMacUI.xcodeproj`
- `legacy-projects/trunk/fhashwui17.sln`
- `legacy-projects/trunk/fhashwui18.slnx`
- `legacy-projects/trunk/fileshashuwp17.sln`
- `legacy-projects/trunk/package_macos_dmg.sh`
- `legacy-projects/trunk/package_win_mfc64.py`
- `legacy-platforms/trunk/fHashWUIWap`
- `legacy-platforms/trunk/fHashUwpWap`
- `legacy-platforms/trunk/source/WinUWP`
- `legacy-platforms/trunk/source/OSXUI`
- `legacy-platforms/sub-proj/fHashWinRtBridge`
- `legacy-platforms/sub-proj/fHashUwpNative`
- `legacy-platforms/sub-proj/fHashUwpShellExt`
- `legacy-platforms/sub-proj/fHashWUIShellExt`
- `legacy-algorithms/trunk/source/Algorithms/sha256.cpp`
- `legacy-algorithms/trunk/source/Algorithms/sha256.h`
- `legacy-algorithms/trunk/source/Algorithms/sha512.cpp`
- `legacy-algorithms/trunk/source/Algorithms/sha512.h`

These items were moved out of `trunk/` because:

- the maintained `push` build chain only drives `trunk/fileshash15.sln`
- the repository no longer treats the retired WinUI/UWP/CLR preview line as a maintained delivery route
- the archived packaging scripts are not referenced by the active GitHub workflows
- the legacy in-tree SHA256/SHA512 implementations were superseded by the
  maintained OpenSSL SHA-256 / SHA-512 provider path

The archive is intentionally conservative:

- the retired WinUI/UWP/CLR preview surface is kept here for reference only
- regression and security tests resolve archived legacy-platform files through explicit archive path mappings so the historical source can still be audited without keeping it in the live build tree
