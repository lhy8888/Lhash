# macOS Core Support Criteria

## M3 scope

M3 establishes that the LHash core is not only buildable on macOS arm64, but also has a
minimum trustworthy runtime and security contract.

This is intentionally narrower than a full macOS product line:

- no WinUI
- no `fHashClrBridge`
- no Linux
- no macOS GUI
- no CLI productization
- no OpenSSL 4.0 upgrade work

## What M3 establishes

- macOS arm64 core builds through the new core-only CMake entry point
- baseline algorithms run through a minimal portable smoke verification
- Darwin file handling rejects symlinked hash targets
- Darwin file handling validates opened files through descriptor-level checks
- macOS-specific security regression coverage exists for the hashing path policy
- core-only CI runs the macOS build and verification steps

## Darwin path policy

The macOS hashing path is expected to:

- reject symbolic links
- reject non-regular files
- use no-follow semantics when opening eligible targets
- validate the opened file descriptor against the validated path identity
- prefer descriptor-based metadata reads instead of reopening by path

The policy is intentionally conservative. It is not a promise that every macOS
filesystem edge case is fully covered yet.

## Remaining gaps

M3 does not claim:

- a macOS GUI
- a CLI packaging story
- OpenSSL extension backend restoration on macOS
- Linux support
- full coverage of every special filesystem boundary on Darwin

Those items remain for later phases.
