# LHash

![LHash Logo](trunk/source/WinUI/Assets/StoreLogo.scale-400.png)

![Windows Build](https://github.com/lhy8888/Lhash/actions/workflows/windows-build.yml/badge.svg)
![License](https://img.shields.io/badge/license-GPL--2.0-blue.svg)
![Platform](https://img.shields.io/badge/platform-Windows-0078D6)

## Current release

- Current release: [`v1.12.1`](https://github.com/lhy8888/Lhash/releases/tag/v1.12.1)
- Download: [`GitHub Releases`](https://github.com/lhy8888/Lhash/releases)
- Main branch: `future-winui-was2`
- CI: [`Windows Build workflow`](https://github.com/lhy8888/Lhash/actions/workflows/windows-build.yml)
- Code signing: [CODE_SIGNING.md](CODE_SIGNING.md)
- Code signing policy: [CODE_SIGNING_POLICY.md](CODE_SIGNING_POLICY.md)
- License note: `GPL-2.0-only with an OpenSSL linking exception`; see [LICENSE-OPENSSL-EXCEPTION.md](LICENSE-OPENSSL-EXCEPTION.md)

## LHash

High-performance, security-first, re-architected local hashing tool.

LHash is a modern local file hashing tool designed for engineering-grade reliability, security, and scalability.

The latest maintained release line introduces a complete architectural redesign and security hardening, transforming LHash from a traditional utility into a modular, extensible, and safety-oriented native application.

## ✨ Features
⚡ Fast multi-threaded hashing engine

📦 Portable single executable (no installation required)

🖱️ Drag & drop support for files and directories

📊 Real-time progress tracking

📤 Easy export and copy of hash results

🧩 Extensible algorithm framework


## Architecture

LHash has been refactored from a monolithic MFC structure into a modular architecture:

```text
NativeCore
- Domain        # HashRequest / HashResult / algorithm descriptors
- Runtime       # Scheduling, execution, providers, progress
- Algorithms    # Legacy built-in implementations
- Common        # Shared utilities and safety primitives

Adapters
- UiBridge      # Isolation seam between UI and runtime events

WinMFC           # Lightweight native desktop UI
LegacyCompat     # Compatibility layer (gradually shrinking)
third_party      # Fixed-version vendored upstream algorithm families
```

## Key improvements

- Removed UI-core coupling
- Rebuilt the hash engine as a reusable core for multiple front ends
- Introduced an extensible algorithm registry
- Moved execution to per-task contexts for thread safety
- Strengthened separation of concerns across core, adapters, and legacy seams

## 🧮 Supported Algorithms

🔐 Cryptographic Hashes

MD5

SHA-1

SHA-256

SHA-512

⚡ Modern High-Performance Hashes

BLAKE3-256

BLAKE3-512

🧪 OpenSSL 3 (Extended Algorithms)

SHA-256 

SHA-384

SHA-512 

SHA3-256

SHA3-384

SHA3-512

BLAKE2b-512

BLAKE2s-256

🚀 Non-Cryptographic (Fast Checksums)

XXHash3-64

XXHash3-128

CRC32C

SHAKE128-256

SHAKE256-512


## Security features

LHash is designed with defensive programming and real-world misuse scenarios in mind.

### Safe file handling

Rejects by default:

- Symbolic links
- Junction points
- Reparse points

Prevents:

- Accidental access to sensitive system files
- Misuse under elevated privileges

### Thread safety

- Independent hash context per task
- No shared mutable static state
- Deterministic results under concurrency

This removes silent data corruption risks.

### Large file safety

- Full 64-bit file size handling with `uint64_t`
- Checked arithmetic for overflow protection and safe conversions
- Stable processing of very large files

### Resource safety

- RAII-based handle management
- No reliance on manual `CloseHandle`
- Safe under large batch workloads

### UI stability

- Throttled UI updates to avoid message flooding
- Strict separation of UI and worker threads
- Stable behavior under high concurrency

### System-level protections

Modern mitigations are enabled where the toolchain supports them:

- Control Flow Guard (CFG)
- ASLR and High Entropy VA
- DEP (NX)
- `/GS` and `/sdl`
- Hardened DLL loading policy

## Core capabilities

- Batch hashing for files and directories
- Multi-threaded processing
- Real-time progress tracking
- Explicit error reporting for permissions and I/O failures
- Extensible algorithm framework
- Built-in `BLAKE3-256`, `BLAKE3-512`, and `BLAKE3 XOF` variants for modern high-speed hashing
- Built-in `XXH3-64`, `XXH3-128`, and `CRC32C` variants vendored from fixed upstream snapshots
- Fixed-version `OpenSSL 3 EVP` family with distinct `SHA-256`, `SHA-384`, `SHA-512`, `SHA3-256`, `SHA3-384`, `SHA3-512`, `BLAKE2b-512`, `BLAKE2s-256`, `SHAKE128-256`, and `SHAKE256-512` descriptors
- Legacy `SHA256` / `SHA512` kept intact so the original built-in family can coexist with the new OpenSSL-backed family during migration

## SIMD-backed algorithm status

The current native-core configuration keeps the benchmark-backed SIMD paths enabled on the platforms where evidence now exists:

- `BLAKE3`
  - `x64`: `SSE2`, `SSE4.1`, `AVX2`, `AVX512`
  - `Win32`: `SSE2`, `SSE4.1`, `AVX2`
  - `ARM64`: `NEON`
- `CRC32C`
  - `x64`: upstream `SSE4.2`
  - `ARM64`: upstream ARM64 backend with Windows processor-feature probing
- `XXH3`
  - fixed-version official code integrated through the same provider/descriptor seam as BLAKE3

The benchmark workflow now measures `portable` vs `current` on all three platforms and keeps the decision local to each platform instead of guessing from x64 alone.

Observed headline results on the current GitHub-hosted runners:

- `x64`: `BLAKE3-256` on `large-single-128m` improves from about `481 MiB/s` to about `1641 MiB/s`
- `Win32`: `BLAKE3-256` on `large-single-128m` improves from about `390 MiB/s` to about `1369 MiB/s`
- `ARM64`: `BLAKE3-256` on `large-single-128m` improves from about `532 MiB/s` to about `901 MiB/s`

These measurements justify keeping the current SIMD-backed BLAKE3 paths enabled while the additional `XXH3` and `CRC32C` variants stay vendored behind the same fixed-version provider pattern.

## Design principles

LHash follows strict engineering principles:

- Secure by default
- Separation of concerns
- Extensibility
- Lightweight deployment
- Deterministic behavior

## UI philosophy

- Native Windows UI (MFC-based)
- Fluent / Windows 11 inspired modernization on the shipped native line
- Minimal, task-focused interface
- Progress-driven interaction model

## Current status

- Core architecture redesigned
- Major security hardening completed
- Native desktop UX continuously refined on the maintained release line
- Benchmark-backed BLAKE3 SIMD policy established for `x64`, `Win32`, and `ARM64`
- Fixed-version `XXH3-64`, `XXH3-128`, and `CRC32C` integrated into the native runtime
- Fixed-version `OpenSSL 3 EVP` family integrated alongside the legacy SHA-2 algorithms without changing their ids or display names

Roadmap:

- CLI mode
- Shell integration

## One-liner

LHash is not just a hashing tool. It is a security-aware, re-architected local computation engine.
