# macOS Core Support Criteria

## Scope

This document defines the macOS core support contract. The contract covers the
macOS arm64 core runtime and its security boundaries. A separate macOS CLI MVP
workflow exists and is tracked independently as a workflow artifact built on the
same core entry point.

This is intentionally narrower than a full macOS product line:

- no WinUI
- no `fHashClrBridge`
- no Linux
- no macOS GUI
- no OpenSSL 4.0 upgrade work
- no claim that the CLI MVP is a full macOS product line

## What M3 establishes

- macOS arm64 core builds through the new core-only CMake entry point
- baseline algorithms run through a minimal smoke verification
- BLAKE3 uses the native Apple arm64 NEON path and is checked against fixed vectors
- Darwin file handling rejects symlinked hash targets
- Darwin file handling validates opened files through descriptor-level checks
- macOS-specific security regression coverage exists for the hashing path policy
- core-only CI runs the macOS build and verification steps

## Related macOS CLI MVP

The separate macOS CLI MVP workflow:

- builds `lhash` from the shared core entry point
- packages a `tar.gz` workflow artifact
- is useful as a lightweight validation surface
- does not change the macOS core support contract above

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
- full CLI productization beyond the MVP workflow
- OpenSSL extension backend restoration on macOS
- Linux support
- full coverage of every special filesystem boundary on Darwin

Those items remain for later phases.

## Non-mainline reference trees

The retired WinUI and CLR bridge trees remain in the repository for reference
and auditability, but they are marked as non-mainline and are not part of the
macOS core support contract.
