# Changelog

This file only records the current maintained LHash release note. Older version
entries have been removed, and the historical upstream fHash log is not
duplicated here.

## 1.12.4 - 2026-04-30

Patch release focused on the pristine OpenSSL 3.5.6 vendor upgrade, release
documentation refresh, and a cleanup pass over stale consistency checks.

### Release and packaging

- upgraded the maintained Windows vendor tree to pristine OpenSSL 3.5.6
- removed the retired `third_party/openssl/3.0.20` vendor tree
- kept the Windows x64 and ARM64 release surface aligned with the current
  vendor policy

### Documentation and release metadata

- updated the current release references to `v1.12.4`
- refreshed the README, changelog, issue templates, and release metadata tests
- updated the maintained Windows version metadata to `1.12.4.0`

### Consistency cleanup

- aligned algorithm-order assertions with the current registry order
- tracked the newly registered native runtime tests
- removed retired Windows OsUtils variants from the active source tree
- changed `HashDigestRuntimePlan` to own its request and queue-plan state by
  value
