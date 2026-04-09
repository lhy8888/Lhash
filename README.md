# LHash

![LHash Logo](trunk/source/WinUI/Assets/StoreLogo.scale-400.png)

![Windows Build](https://github.com/lhy8888/Lhash/actions/workflows/windows-build.yml/badge.svg)
![License](https://img.shields.io/badge/license-GPL--2.0-blue.svg)
![Platform](https://img.shields.io/badge/platform-Windows-0078D6)


## Current release

- Current release: [`v1.11.0`](https://github.com/lhy8888/Lhash/releases/tag/v1.11.0)
- Download: [`GitHub Releases`](https://github.com/lhy8888/Lhash/releases)
- Main branch: `future-winui-was2`
- CI: [`Windows Build workflow`](https://github.com/lhy8888/Lhash/actions/workflows/windows-build.yml)
- Code signing: [CODE_SIGNING.md](CODE_SIGNING.md)
- Code signing policy: [CODE_SIGNING_POLICY.md](CODE_SIGNING_POLICY.md)

## LHash

High-performance, security-first, re-architected local hashing tool.

LHash is a modern local file hashing tool designed for engineering-grade reliability, security, and scalability.

The latest maintained release line introduces a complete architectural redesign and security hardening, transforming LHash from a traditional utility into a modular, extensible, and safety-oriented native application.

## Positioning

- High-performance local hash computation
- Clean layered architecture with the core fully decoupled from the UI
- Security-first design with safe defaults
- Lightweight and portable native delivery without a heavy runtime dependency

## Architecture

LHash has been refactored from a monolithic MFC structure into a modular architecture:

```text
NativeCore (Core Engine)
├── Domain          # Data models (HashRequest / Result / Algorithm)
├── Runtime         # Execution engine (scheduler / task / progress)
├── Algorithms      # Hash implementations (MD5 / SHA / CRC / extensible)
├── Common          # Utilities and safety primitives

Adapters
└── UiBridge        # Isolation layer between UI and core

WinMFC              # Lightweight native UI
LegacyCompat        # Compatibility layer (gradually shrinking)
```

## Key improvements

- Removed UI-core coupling
- Rebuilt the hash engine as a reusable core for multiple front ends
- Introduced an extensible algorithm registry
- Moved execution to per-task contexts for thread safety
- Strengthened separation of concerns across core, adapters, and legacy seams

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
- Built-in BLAKE3 variants for modern high-speed hashing

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

## Why not WinUI as the default release

LHash intentionally avoids making a heavy UI runtime the default public release.

- WinUI introduces much larger runtime and package overhead
- It is less suitable for a lightweight portable hashing tool
- Startup and deployment costs are higher than the native maintained line

LHash therefore keeps the default release smaller, faster, and more predictable with the native desktop implementation, while still retaining preview WinUI work in the repository.

## Current status

- Core architecture redesigned
- Major security hardening completed
- Native desktop UX continuously refined on the maintained release line

Roadmap:

- Additional algorithms such as SHA3
- CLI mode
- Shell integration

## One-liner

LHash is not just a hashing tool. It is a security-aware, re-architected local computation engine.
