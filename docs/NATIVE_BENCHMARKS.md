# Native Benchmarks

LHash keeps BLAKE3 SIMD decisions behind benchmark evidence rather than enabling every available translation unit by default in the shipping discussion.

## Current benchmark matrix

Scenarios:
- `small-single-64k`: 1 file x 64 KiB
- `many-small-256x64k`: 256 files x 64 KiB
- `large-single-128m`: 1 file x 128 MiB

Algorithm sets:
- `sha256`
- `blake3-256`
- `classic-4` (`md5`, `sha1`, `sha256`, `sha512`)
- `hybrid-4` (`sha256`, `blake3-256`, `blake3-512`, `blake3-xof`)

Profiles:
- `portable`: forces the vendored BLAKE3 C code onto the portable path by disabling SIMD translation units through `FHashBlake3SimdProfile=portable`
- `current`: builds the current desktop native-core configuration with the checked-in x64 Release BLAKE3 settings

## Workflow

The benchmark workflow lives at:
- [native-benchmarks.yml](D:\hash\fhash\.github\workflows\native-benchmarks.yml)

It runs both profiles on the same x64 GitHub-hosted runner and uploads:
- build logs
- run logs
- raw CSV results for `portable` and `current`
- a markdown summary with throughput deltas

## Decision rules

Use the benchmark summary before changing shipping SIMD defaults:
- If `blake3-256` and `hybrid-4` show a clear uplift on `large-single-128m` and `many-small-256x64k`, while `sha256` stays effectively flat, then x64 Release-only BLAKE3 SIMD is justified.
- If gains are only visible on the large-file case and disappear on many-small-file workloads, prefer a narrower x64 Release-only enablement and keep broader paths off.
- Do not use the x64 benchmark alone to justify Win32 or ARM64 shipping changes. Those need their own measurements on matching hardware.
- If the delta is within expected runner noise, keep the portable path as the safer default and revisit only with stronger data.
