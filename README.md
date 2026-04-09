# LHash

![LHash Logo](trunk/source/WinUI/Assets/StoreLogo.scale-400.png)

![Windows Build](https://github.com/lhy8888/Lhash/actions/workflows/windows-build.yml/badge.svg)
![License](https://img.shields.io/badge/license-GPL--2.0-blue.svg)
![Platform](https://img.shields.io/badge/platform-Windows-0078D6)

LHash is a maintained Windows-focused hash utility forked from [fHash](https://github.com/sunjw/fhash).
The current maintained release line starts at `v1.10.0`.

🔹 LHash

High-performance · Security-first · Re-architected local hashing tool

LHash is a modern local file hashing tool designed for engineering-grade reliability, security, and scalability.

The latest version introduces a complete architectural redesign and security hardening, transforming LHash from a traditional utility into a modular, extensible, and safety-oriented native application.

🚀 Positioning
⚡ High-performance local hash computation
🧱 Clean layered architecture (core fully decoupled from UI)
🔐 Security-first design (safe by default)
📦 Lightweight & portable (no heavy runtime dependency)
🧠 Architecture (Rebuilt from the ground up)

LHash has been refactored from a monolithic MFC structure into a modular architecture:

NativeCore (Core Engine)
├── Domain          # Data models (HashRequest / Result / Algorithm)
├── Runtime         # Execution engine (scheduler / task / progress)
├── Algorithms      # Hash implementations (MD5 / SHA / CRC / extensible)
├── Common          # Utilities & safety primitives

Adapters
└── UiBridge        # Isolation layer between UI and core

WinMFC              # Lightweight native UI
LegacyCompat        # Compatibility layer (gradually shrinking)
✨ Key Improvements
❌ Removed UI–core coupling
✅ Fully reusable hash engine (GUI / CLI ready)
✅ Algorithm registry (extensible design)
✅ Per-task execution context (thread-safe)
✅ Strict separation of concerns
🔐 Security Features (Core Focus)

LHash is designed with defensive programming and real-world misuse scenarios in mind.

🛡 Safe File Handling
Rejects by default:
Symbolic links
Junction points
Reparse points

Prevents:

Accidental access to sensitive system files
Misuse under elevated privileges
🧵 Thread Safety
Independent hash context per task
No shared mutable static state
Deterministic results under concurrency

👉 Eliminates silent data corruption risks

🧮 Large File Safety
Full 64-bit file size handling (uint64_t)
Checked arithmetic:
Overflow protection
Safe conversions

Supports stable processing of very large files

🧷 Resource Safety
RAII-based handle management
No reliance on manual CloseHandle
Safe under large batch workloads
🧠 UI Stability
Throttled UI updates (no message flooding)
Strict separation of UI and worker threads
Stable under high concurrency
🧱 System-Level Protections

Modern security mitigations enabled:

Control Flow Guard (CFG)
ASLR + High Entropy VA
DEP (NX)
/GS + /sdl
Hardened DLL loading policy
⚙️ Core Capabilities
Batch hashing (files & directories)
Multi-threaded processing
Real-time progress tracking
Explicit error reporting (permissions, IO, etc.)
Extensible algorithm framework
🎯 Design Principles

LHash follows strict engineering principles:

Secure by default
Separation of concerns
Extensibility
Lightweight deployment
Deterministic behavior
🖥 UI Philosophy
Native Windows UI (MFC-based)
Fluent / Windows 11 inspired design
Minimal and task-focused interface
Progress-driven interaction model
📦 Why not WinUI?

LHash intentionally avoids heavy UI frameworks.

WinUI introduces large runtime overhead (>100MB)
Not suitable for portable, single-executable tools
Higher startup and deployment cost

👉 LHash chooses:

Smaller · Faster · More predictable native implementation

📈 Current Status
✅ Core architecture redesigned
✅ Major security hardening completed
🔄 UI modernization (Fluent-style MFC)
🔜 Roadmap:
Additional algorithms (BLAKE3, SHA3)
CLI mode
Shell integration
📌 One-liner

LHash is not just a hashing tool — it is a security-aware, re-architected local computation engine.
