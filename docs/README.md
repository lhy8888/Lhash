# LHash Documentation

This directory contains the project’s security, trust, release, and verification documents.

## Start here

- [Threat model](THREAT_MODEL.md)
- [Security model](SECURITY_MODEL.md)
- [Release verification guide](RELEASE_VERIFICATION.md)
- [Supply-chain security](SUPPLY_CHAIN_SECURITY.md)

## How to read these documents

### If you are a normal user

Start with:

1. [Release verification guide](RELEASE_VERIFICATION.md)
2. [Supply-chain security](SUPPLY_CHAIN_SECURITY.md)

These explain how to review a release and what metadata the project publishes to make releases easier to trust.

### If you are reviewing the project’s security posture

Start with:

1. [Threat model](THREAT_MODEL.md)
2. [Security model](SECURITY_MODEL.md)
3. [Supply-chain security](SUPPLY_CHAIN_SECURITY.md)

These explain what the project is trying to protect, which risks it is designed to reduce, and how the maintained release line approaches local safety and release trust.

### If you are evaluating LHash for recommendation or audit preparation

Read all four documents in this order:

1. [Threat model](THREAT_MODEL.md)
2. [Security model](SECURITY_MODEL.md)
3. [Release verification guide](RELEASE_VERIFICATION.md)
4. [Supply-chain security](SUPPLY_CHAIN_SECURITY.md)

## Related repository files

- [Security policy](../SECURITY.md)
- [Code signing](../CODE_SIGNING.md)
- [Code signing policy](../CODE_SIGNING_POLICY.md)
- [Project README](../README.md)

## Intent

The goal of this document set is to make LHash easier to explain, easier to review, and easier to trust than a repository that only publishes binaries and source code without any security or verification narrative.
