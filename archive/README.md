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

These items were moved out of `trunk/` because:

- the maintained `push` build chain only drives `trunk/fileshash15.sln`
- preview WinUI/UWP jobs build direct project files rather than these legacy solution wrappers
- the archived packaging scripts are not referenced by the active GitHub workflows

The archive is intentionally conservative:

- active preview assets such as `trunk/fHashWUIWap`, `trunk/fHashUwpWap`, `trunk/source/WinUI`, `trunk/source/WinUWP`, and `trunk/source/OSXUI` remain in place because preview projects or regression tests still reference them
