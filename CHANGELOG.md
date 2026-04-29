# Changelog

This file only records the current maintained LHash release note. Older version
entries have been removed, and the historical upstream fHash log is not
duplicated here.

## 1.12.2 - 2026-04-17

Follow-up patch release focused on safer defaults, cleaner algorithm UX, and continued runtime hardening.

### Algorithm defaults and UX

- enabled `SHA3-256` by default alongside the maintained OpenSSL `SHA-256` and `SHA-512` defaults
- replaced the one-shot settings submenu for algorithm selection with a dedicated multi-toggle dialog, so multiple algorithms can be changed before closing settings
- fully removed the legacy built-in `SHA256` / `SHA512` implementations from the active code path and kept OpenSSL `SHA-256` / `SHA-512` as the only maintained SHA-2 variants

### Runtime and safety hardening

- made OpenSSL EVP update failures sticky and explicit, so digest update/finalize errors no longer degrade into empty or misleading digest output
- removed the legacy `CSHA1::HashFile()` file-I/O helper that bypassed the hardened `OsFile` path
- cleaned up remaining global scratch and registry snapshot edge cases in digest/result access seams
- tightened Windows legacy helpers by removing stale OS-version detection code and hardening shell/DLL helper behavior

### Input and platform robustness

- unified the per-session file-count limit handling across dialog, drag-and-drop, folder recursion, and `WM_COPYDATA` inputs with explicit user-visible outcomes
- strengthened Win32 long-path handling and continued reducing TOCTOU-style drift in metadata and file-open paths
- hardened the POSIX/Darwin string and file helpers around conversion, `O_CREAT`, and metadata lookup behavior
