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

These items were moved out of `trunk/` because:

- the maintained `push` build chain only drives `trunk/fileshash15.sln`
- preview WinUI/UWP jobs build direct project files rather than these legacy solution wrappers
- the archived packaging scripts are not referenced by the active GitHub workflows

The archive is intentionally conservative:

- the active preview surface that remains live is limited to `trunk/source/WinUI`, `sub-proj/fHashClrBridge`, and `sub-proj/fHashWUINative`
- regression and security tests resolve archived legacy-platform files through explicit archive path mappings so the historical source can still be audited without keeping it in the live build tree
