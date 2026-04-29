# Changelog

This file only records the current maintained LHash release note. Older version
entries have been removed, and the historical upstream fHash log is not
duplicated here.

## 1.12.3 - 2026-04-29

Patch release focused on Windows release packaging, the Windows ARM64 package
line, and the macOS CLI MVP that now sits beside the macOS core support
contract.

### Release and packaging

- normalized the Windows x64 release artifact name to `LHash-windows-x64`
- added Windows ARM64 MFC packaging and tagged-release publishing
- added the macOS arm64 CLI MVP workflow and its `tar.gz` workflow artifact

### Core and documentation alignment

- kept the core build matrix and smoke coverage aligned with the shared core
  entry point
- clarified that macOS core support is tracked separately from the macOS CLI
  MVP workflow
- aligned release-verification and supply-chain documentation with the current
  artifact layout

### Runtime and safety hardening

- retained the previously hardened digest-update and file-open contracts
- kept the Windows and Darwin security regressions green through the current
  mainline shape
