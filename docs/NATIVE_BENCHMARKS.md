# Native Benchmarks

LHash keeps BLAKE3 SIMD decisions behind benchmark evidence rather than enabling every available translation unit by default in the shipping discussion.

## Current benchmark matrix

Scenarios:
- `small-single-64k`: 1 file x 64 KiB
- `many-small-256x64k`: 256 files x 64 KiB
- `large-single-128m`: 1 file x 128 MiB

Algorithm sets:
- `openssl-sha-256`
- `blake3-256`
- `classic-4` (`md5`, `sha1`, `openssl-sha-256`, `openssl-sha-512`)
- `hybrid-4` (`openssl-sha-256`, `blake3-256`, `blake3-512`, `blake3-xof`)

Profiles:
- `portable`: forces the vendored BLAKE3 C code onto the portable path by disabling SIMD translation units through `FHashBlake3SimdProfile=portable`
- `current`: builds the current desktop native-core configuration with the checked-in platform-specific BLAKE3 settings

## Workflow

The benchmark workflow lives at:
- [native-benchmarks.yml](D:\hash\fhash\.github\workflows\native-benchmarks.yml)

It runs both profiles per platform and uploads one artifact per platform:
- `FHash-native-benchmarks-x64`
- `FHash-native-benchmarks-win32`
- `FHash-native-benchmarks-arm64`

Current runners:
- `x64`: `windows-2022`
- `Win32`: `windows-2022`
- `ARM64`: `windows-11-arm`

Each artifact contains:
- build logs
- run logs
- raw CSV results for `portable` and `current`
- a markdown summary with throughput deltas

## Decision rules

Use the benchmark summary before changing shipping SIMD defaults:
- If `blake3-256` and `hybrid-4` show a clear uplift on `large-single-128m` and `many-small-256x64k`, while `openssl-sha-256` stays effectively flat, then that platform's BLAKE3 SIMD shipping path is justified.
- If gains are only visible on the large-file case and disappear on many-small-file workloads, prefer a narrower platform-specific enablement and keep broader paths off.
- Do not use one platform's benchmark to justify another platform's SIMD changes. `x64`, `Win32`, and `ARM64` each need their own measurements.
- If the delta is within expected runner noise, keep the portable path as the safer default and revisit only with stronger data.

## Current decision

The current maintained conclusion is to keep BLAKE3 SIMD enabled on every platform that now has direct benchmark evidence:

- `x64`: keep `SSE2`, `SSE4.1`, `AVX2`, and `AVX512`
- `Win32`: keep `SSE2`, `SSE4.1`, and `AVX2`
- `ARM64`: keep `NEON`

Headline large-file (`large-single-128m`) `BLAKE3-256` results from the current benchmark runs:

- `x64`: about `481 MiB/s -> 1641 MiB/s`
- `Win32`: about `390 MiB/s -> 1369 MiB/s`
- `ARM64`: about `532 MiB/s -> 901 MiB/s`

Small-file gains remain modest, but the large-file uplift is strong enough on each measured platform that the current SIMD-backed shipping configuration should stay enabled.
