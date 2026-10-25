#!/usr/bin/env python3
"""Create and verify the immutable Windows release assets, without networking."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import sys
import zipfile

MANIFEST = "RELEASE_MANIFEST.txt"
CHECKSUMS = "SHA256SUMS.txt"
SBOM = "LHash-release.cyclonedx.json"
OPENSSL_VERSION = "3.5.9"


def sha256(path):
    with path.open("rb") as stream:
        return hashlib.file_digest(stream, "sha256").hexdigest()


def fields(text):
    result = {}
    for line in text.splitlines():
        key, separator, value = line.partition("=")
        if not separator or not key or key in result:
            raise ValueError("Malformed or duplicate release metadata field")
        result[key] = value
    return result


def application_version(repo):
    text = (repo / "trunk/source/WinMFC/version.h").read_text()
    match = re.search(r'^#define\s+STR_VERSION_LEGACY\s+"(\d+\.\d+\.\d+\.\d+)"\s*$', text, re.M)
    if not match:
        raise ValueError("Cannot determine the application's four-part version")
    return match[1]


def matches_bundle(name, architecture, commit):
    match = re.fullmatch(rf"LHash-windows-{architecture}-([0-9a-f]{{7,40}})\.zip", name)
    return match is not None and commit.startswith(match[1])


def stage(assets, output, repo, env):
    commit = env["GITHUB_SHA"]
    if not re.fullmatch(r"[0-9a-f]{40}", commit):
        raise ValueError("Expected a full build commit SHA")
    version = application_version(repo)
    if output.exists() and any(output.iterdir()):
        raise ValueError("Release staging directory must be empty")
    output.mkdir(parents=True, exist_ok=True)
    bundles = {}
    for arch in ("x64", "arm64"):
        candidates = list(assets.glob(f"LHash-windows-{arch}-*.zip"))
        if len(candidates) != 1 or not matches_bundle(candidates[0].name, arch, commit):
            raise ValueError(f"Expected exactly the {arch} bundle from this commit")
        bundle = candidates[0]
        with zipfile.ZipFile(bundle) as archive:
            if archive.namelist().count("BUILD_INFO.txt") != 1 or archive.namelist().count("LHash.exe") != 1:
                raise ValueError(f"Missing/duplicate binary or metadata in {bundle.name}")
            info = fields(archive.read("BUILD_INFO.txt").decode("ascii"))
        required = {"commit": commit, "application_version": version,
                    "openssl_vendor_version": OPENSSL_VERSION}
        if any(info.get(key) != value for key, value in required.items()):
            raise ValueError(f"Build metadata disagrees with the release: {bundle.name}")
        shutil.copyfile(bundle, output / bundle.name)
        bundles[arch] = bundle.name
    metadata = {
        "release_mode": "tagged-release" if env["GITHUB_REF"].startswith("refs/tags/v") else "rehearsal",
        "ref": env["GITHUB_REF"], "commit": commit, "short_sha": commit[:7],
        "lhash_application_version": version, "frontend": "windows-mfc",
        "openssl_vendor_version": OPENSSL_VERSION,
        "openssl_vendor_source": f"third_party/openssl/{OPENSSL_VERSION}",
        "openssl_vendor_static": "true", "openssl_vendor_policy": "pristine-upstream-source",
        "openssl_vendor_local_patches": "none",
        "workflow_run": f"{env['GITHUB_SERVER_URL']}/{env['GITHUB_REPOSITORY']}/actions/runs/{env['GITHUB_RUN_ID']}",
        "windows_x64": bundles["x64"], "windows_arm64": bundles["arm64"],
    }
    if any("\n" in value or "\r" in value for value in metadata.values()):
        raise ValueError("Release metadata must not contain line breaks")
    (output / MANIFEST).write_text("".join(f"{key}={value}\n" for key, value in metadata.items()), encoding="utf-8")


def release_names(directory):
    info = fields((directory / MANIFEST).read_text(encoding="utf-8"))
    if not re.fullmatch(r"\d+\.\d+\.\d+\.\d+", info.get("lhash_application_version", "")):
        raise ValueError("Missing application version in release manifest")
    commit = info.get("commit", "")
    if not re.fullmatch(r"[0-9a-f]{40}", commit):
        raise ValueError("Missing full commit in release manifest")
    names = []
    for arch in ("x64", "arm64"):
        name = info.get(f"windows_{arch}", "")
        if not matches_bundle(name, arch, commit):
            raise ValueError(f"Invalid {arch} release asset name")
        names.append(name)
    if {p.name for p in directory.glob("LHash-windows-*.zip")} != set(names):
        raise ValueError("Release archives do not match the exact two-architecture manifest")
    return [*names, MANIFEST, SBOM]


def finalize(directory, scan, repo):
    if (directory / CHECKSUMS).exists():
        raise ValueError("Refusing to overwrite an existing authoritative checksum manifest")
    sbom = json.loads(scan.read_text(encoding="utf-8"))
    if sbom.get("bomFormat") != "CycloneDX" or not str(sbom.get("specVersion", "")).startswith("1."):
        raise ValueError("Expected a CycloneDX SBOM from the release scan")
    # PE scanners cannot reliably recover statically linked library versions.
    # Record the build-declared libraries explicitly, alongside scanned entries.
    components = sbom.setdefault("components", [])
    source_info = fields((repo / "third_party/openssl/OPENSSL_3_5_9_SOURCE_INFO.txt").read_text())
    declared = (("OpenSSL", OPENSSL_VERSION, "third_party/openssl/3.5.9"),
                ("BLAKE3", "1.8.4", "third_party/blake3/1.8.4"),
                ("xxHash", "0.8.3", "third_party/xxhash/0.8.3"))
    for name, version, path in declared:
        reference = f"lhash:static-dependency:{name}:{version}"
        component = {"type": "library", "bom-ref": reference, "name": name,
                     "version": version, "scope": "required", "properties": [
                         {"name": "lhash:evidence", "value": "build-declared-static-dependency"},
                         {"name": "lhash:source-path", "value": path}]}
        if name == "OpenSSL":
            component["externalReferences"] = [{"type": "distribution", "url": source_info["download"],
                                                "hashes": [{"alg": "SHA-256", "content": source_info["archive_sha256"]}]}]
        if any(item.get("bom-ref") == reference for item in components):
            raise ValueError("Duplicate build-declared SBOM component")
        components.append(component)
    (directory / SBOM).write_text(json.dumps(sbom, indent=2) + "\n", encoding="utf-8")
    names = release_names(directory)
    with (directory / CHECKSUMS).open("x", encoding="ascii", newline="\n") as stream:
        for name in names:
            stream.write(f"{sha256(directory / name)}  {name}\n")
    verify(directory)


def verify(directory):
    names = release_names(directory)
    seen = set()
    for line in (directory / CHECKSUMS).read_text(encoding="ascii").splitlines():
        match = re.fullmatch(r"([0-9a-f]{64})  ([A-Za-z0-9_.-]+)", line)
        if not match or match[2] not in names or match[2] in seen:
            raise ValueError("Invalid, duplicate, unsafe or unexpected checksum entry")
        seen.add(match[2])
        if sha256(directory / match[2]) != match[1]:
            raise ValueError(f"Release checksum mismatch: {match[2]}")
    if seen != set(names):
        raise ValueError("Checksum manifest does not cover both architectures and metadata")
    sbom = json.loads((directory / SBOM).read_text(encoding="utf-8"))
    if sbom.get("bomFormat") != "CycloneDX":
        raise ValueError("Invalid release SBOM")
    print("Verified x64, ARM64, release metadata and SBOM checksums")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    subparsers = parser.add_subparsers(dest="command", required=True)
    command = subparsers.add_parser("stage")
    command.add_argument("--assets", type=Path, required=True)
    command.add_argument("--output", type=Path, required=True)
    command.add_argument("--repo", type=Path, default=Path.cwd())
    command = subparsers.add_parser("finalize")
    command.add_argument("--directory", type=Path, required=True)
    command.add_argument("--scan", type=Path, required=True)
    command.add_argument("--repo", type=Path, default=Path.cwd())
    command = subparsers.add_parser("verify")
    command.add_argument("--directory", type=Path, required=True)
    args = parser.parse_args()
    try:
        if args.command == "stage":
            stage(args.assets, args.output, args.repo, os.environ)
        elif args.command == "finalize":
            finalize(args.directory, args.scan, args.repo)
        else:
            verify(args.directory)
    except (OSError, ValueError, KeyError, zipfile.BadZipFile) as error:
        print(f"Release asset validation failed: {error}", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
