#!/usr/bin/env python3
"""Create and verify immutable Windows/macOS release assets, without networking."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import plistlib
import re
import shutil
import stat
import struct
import sys
import tarfile
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


def matches_macos_bundle(name, commit):
    match = re.fullmatch(r"LHash-macos-arm64-cli-([0-9a-f]{7,40})\.tar\.gz", name)
    return match is not None and commit.startswith(match[1])


def matches_macos_app_bundle(name, commit):
    match = re.fullmatch(r"LHash-macos-arm64-app-([0-9a-f]{7,40})\.zip", name)
    return match is not None and commit.startswith(match[1])


def validate_macos_app_binary(data):
    # A thin arm64 MH_EXECUTE image; reject Intel/universal or a deployment
    # target newer than the macOS 14 contract before publishing the bundle.
    if len(data) < 32:
        raise ValueError("Truncated macOS application executable")
    magic, cpu, _, kind, count, size, _, _ = struct.unpack_from("<8I", data)
    if magic != 0xfeedfacf or cpu != 0x100000c or kind != 2:
        raise ValueError("The macOS application must be a native arm64 executable")
    if size > len(data) - 32 or count > size // 8:
        raise ValueError("Invalid Mach-O load commands")
    cursor, deployment = 32, None
    for _ in range(count):
        if cursor + 8 > 32 + size:
            raise ValueError("Truncated Mach-O load command")
        command, length = struct.unpack_from("<2I", data, cursor)
        if length < 8 or cursor + length > 32 + size:
            raise ValueError("Invalid Mach-O load command length")
        if command == 0x32:  # LC_BUILD_VERSION
            if length < 24:
                raise ValueError("Truncated Mach-O deployment metadata")
            platform, minimum = struct.unpack_from("<2I", data, cursor + 8)
            if platform != 1 or deployment is not None:
                raise ValueError("Invalid Mach-O macOS platform metadata")
            deployment = minimum
        cursor += length
    if cursor != 32 + size or deployment is None or deployment > (14 << 16):
        raise ValueError("The application does not support the macOS 14 deployment target")


def validate_macos_app_plist(data, version):
    info = plistlib.loads(data)
    required = {"CFBundleExecutable": "LHash", "CFBundlePackageType": "APPL",
                "CFBundleShortVersionString": version.removesuffix(".0"),
                "CFBundleVersion": version, "LSMinimumSystemVersion": "14.0"}
    if not isinstance(info, dict) or any(info.get(key) != value for key, value in required.items()):
        raise ValueError("macOS application plist disagrees with the release")


def validate_macos_app_bundle(bundle, commit, version):
    root = bundle.name.removesuffix(".zip") + "/"
    executable_name = "LHash.app/Contents/MacOS/LHash"
    required = {executable_name, "LHash.app/Contents/Info.plist",
                "LHash.app/Contents/_CodeSignature/CodeResources",
                "BUILD_INFO.txt", "README.txt", "SHA256.txt",
                "LICENSES/LICENSE", "LICENSES/LICENSE-OPENSSL-EXCEPTION.md",
                "LICENSES/OpenSSL.txt", "LICENSES/BLAKE3-CC0.txt",
                "LICENSES/BLAKE3-Apache-2.0.txt", "LICENSES/xxHash.txt", "LICENSES/CRC32C.txt"}
    with zipfile.ZipFile(bundle) as archive:
        files, seen, total_size = {}, set(), 0
        for entry in archive.infolist():
            name = entry.filename
            parts = name.rstrip("/").split("/")
            mode = entry.external_attr >> 16
            if (not name.startswith(root) or "\\" in name or
                    any(part in ("", ".", "..") for part in parts) or
                    name in seen or entry.flag_bits & 1 or
                    stat.S_IFMT(mode) not in (0, stat.S_IFREG, stat.S_IFDIR)):
                raise ValueError("Unsafe, duplicate or unsupported macOS application package member")
            seen.add(name)
            if entry.is_dir():
                continue
            relative = name[len(root):]
            if (relative not in required and not relative.startswith("LHash.app/Contents/")):
                raise ValueError("Unexpected macOS application package member")
            total_size += entry.file_size
            if len(seen) > 1000 or entry.file_size > 128 * 1024 * 1024 or total_size > 256 * 1024 * 1024:
                raise ValueError("Oversized macOS application package")
            if relative != executable_name and entry.file_size > 8 * 1024 * 1024:
                raise ValueError("Oversized macOS application resource")
            if relative in ("BUILD_INFO.txt", "SHA256.txt", "LHash.app/Contents/Info.plist") and entry.file_size > 65536:
                raise ValueError("Oversized macOS application metadata")
            files[relative] = entry
        if not required.issubset(files):
            raise ValueError("Missing executable, signature, licenses or metadata in macOS application package")
        info = fields(archive.read(files["BUILD_INFO.txt"]).decode("ascii"))
        expected = {"commit": commit, "application_version": version,
                    "platform": "macos-arm64", "artifact_type": "app",
                    "openssl_vendor_version": OPENSSL_VERSION,
                    "packaged_executable": executable_name,
                    "signature_status": "AdHoc", "notarized": "false"}
        if any(info.get(key) != value for key, value in expected.items()):
            raise ValueError("macOS application build metadata disagrees with the release")
        if not (files[executable_name].external_attr >> 16) & 0o111:
            raise ValueError("macOS application executable has no execute permission")
        binary = archive.read(files[executable_name])
        validate_macos_app_binary(binary)
        validate_macos_app_plist(archive.read(files["LHash.app/Contents/Info.plist"]), version)
        if info.get("sha256") != hashlib.sha256(binary).hexdigest():
            raise ValueError("macOS application executable checksum mismatch")
        checksums = archive.read(files["SHA256.txt"]).decode("utf-8")
        checked = set()
        for line in checksums.splitlines():
            digest, separator, name = line.partition("  ")
            if (not separator or not re.fullmatch(r"[0-9a-f]{64}", digest) or
                    name not in files or name == "SHA256.txt" or name in checked):
                raise ValueError("Invalid macOS application checksum entry")
            with archive.open(files[name]) as stream:
                actual = hashlib.file_digest(stream, "sha256").hexdigest()
            if actual != digest:
                raise ValueError("macOS application file checksum mismatch")
            checked.add(name)
        if checked != set(files) - {"SHA256.txt"}:
            raise ValueError("macOS application checksums must cover every bundled file")


def validate_macos_bundle(bundle, commit, version):
    root = bundle.name.removesuffix(".tar.gz")
    expected = {f"{root}/{name}" for name in ("lhash", "BUILD_INFO.txt", "SHA256.txt", "README.txt")}
    with tarfile.open(bundle, "r:gz") as archive:
        members = archive.getmembers()
        files = {}
        for member in members:
            if member.isdir() and member.name.rstrip("/") == root:
                continue
            if not member.isfile() or member.name not in expected or member.name in files:
                raise ValueError("Unsafe, duplicate or unexpected macOS package member")
            if member.name != f"{root}/lhash" and member.size > 65536:
                raise ValueError("Oversized macOS package metadata")
            files[member.name] = member
        if set(files) != expected:
            raise ValueError("Missing executable or metadata in macOS package")
        info = fields(archive.extractfile(files[f"{root}/BUILD_INFO.txt"]).read().decode("ascii"))
        required = {"commit": commit, "application_version": version,
                    "platform": "macos-arm64", "artifact_type": "cli",
                    "openssl_vendor_version": OPENSSL_VERSION}
        if any(info.get(key) != value for key, value in required.items()):
            raise ValueError("macOS build metadata disagrees with the release")
        with archive.extractfile(files[f"{root}/lhash"]) as executable:
            digest = hashlib.file_digest(executable, "sha256").hexdigest()
        checksum = archive.extractfile(files[f"{root}/SHA256.txt"]).read().decode("ascii")
        if checksum != f"{digest}  lhash\n" or info.get("sha256") != digest:
            raise ValueError("macOS executable checksum mismatch")


def stage(assets, output, repo, env):
    commit = env["GITHUB_SHA"]
    if not re.fullmatch(r"[0-9a-f]{40}", commit):
        raise ValueError("Expected a full build commit SHA")
    version = application_version(repo)
    if env["GITHUB_REF"].startswith("refs/tags/"):
        expected_tag = "refs/tags/v" + version.removesuffix(".0")
        if env["GITHUB_REF"] != expected_tag:
            raise ValueError("Release tag does not match the application version")
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
    candidates = list(assets.glob("LHash-macos-arm64-cli-*.tar.gz"))
    if len(candidates) != 1 or not matches_macos_bundle(candidates[0].name, commit):
        raise ValueError("Expected exactly the macOS ARM64 CLI bundle from this commit")
    macos_bundle = candidates[0]
    validate_macos_bundle(macos_bundle, commit, version)
    shutil.copyfile(macos_bundle, output / macos_bundle.name)
    candidates = list(assets.glob("LHash-macos-arm64-app-*.zip"))
    if len(candidates) != 1 or not matches_macos_app_bundle(candidates[0].name, commit):
        raise ValueError("Expected exactly the macOS ARM64 application bundle from this commit")
    macos_app_bundle = candidates[0]
    validate_macos_app_bundle(macos_app_bundle, commit, version)
    shutil.copyfile(macos_app_bundle, output / macos_app_bundle.name)
    metadata = {
        "release_mode": "tagged-release" if env["GITHUB_REF"].startswith("refs/tags/v") else "rehearsal",
        "ref": env["GITHUB_REF"], "commit": commit, "short_sha": commit[:7],
        "lhash_application_version": version, "frontend": "windows-mfc,macos-cli,macos-appkit",
        "openssl_vendor_version": OPENSSL_VERSION,
        "openssl_vendor_source": f"third_party/openssl/{OPENSSL_VERSION}",
        "openssl_vendor_static": "true", "openssl_vendor_policy": "pristine-upstream-source",
        "openssl_vendor_local_patches": "none",
        "workflow_run": f"{env['GITHUB_SERVER_URL']}/{env['GITHUB_REPOSITORY']}/actions/runs/{env['GITHUB_RUN_ID']}",
        "windows_x64": bundles["x64"], "windows_arm64": bundles["arm64"],
        "macos_arm64": macos_bundle.name,
        "macos_arm64_app": macos_app_bundle.name,
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
    # Existing Windows-only releases remain verifiable without altering them.
    macos_names = set()
    if "macos_arm64" in info:
        name = info["macos_arm64"]
        if not matches_macos_bundle(name, commit):
            raise ValueError("Invalid macOS ARM64 release asset name")
        macos_names.add(name)
        names.append(name)
    if {p.name for p in directory.glob("LHash-macos-*.tar.gz")} != macos_names:
        raise ValueError("macOS release archives do not match the manifest")
    app_names = set()
    if "macos_arm64_app" in info:
        name = info["macos_arm64_app"]
        if not matches_macos_app_bundle(name, commit):
            raise ValueError("Invalid macOS ARM64 application asset name")
        app_names.add(name)
        names.append(name)
    if {p.name for p in directory.glob("LHash-macos-*.zip")} != app_names:
        raise ValueError("macOS application archives do not match the manifest")
    return [*names, MANIFEST, SBOM]


def finalize(directory, scan, repo):
    if (directory / CHECKSUMS).exists():
        raise ValueError("Refusing to overwrite an existing authoritative checksum manifest")
    sbom = json.loads(scan.read_text(encoding="utf-8"))
    if sbom.get("bomFormat") != "CycloneDX" or not str(sbom.get("specVersion", "")).startswith("1."):
        raise ValueError("Expected a CycloneDX SBOM from the release scan")
    # Binary scanners cannot reliably recover statically linked library versions.
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
        raise ValueError("Checksum manifest does not cover all platform packages and metadata")
    sbom = json.loads((directory / SBOM).read_text(encoding="utf-8"))
    if sbom.get("bomFormat") != "CycloneDX":
        raise ValueError("Invalid release SBOM")
    print("Verified all declared platform packages, release metadata and SBOM checksums")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    subparsers = parser.add_subparsers(dest="command", required=True)
    command = subparsers.add_parser("version")
    command.add_argument("--repo", type=Path, default=Path.cwd())
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
        if args.command == "version":
            print(application_version(args.repo))
        elif args.command == "stage":
            stage(args.assets, args.output, args.repo, os.environ)
        elif args.command == "finalize":
            finalize(args.directory, args.scan, args.repo)
        else:
            verify(args.directory)
    except (OSError, ValueError, KeyError, zipfile.BadZipFile, tarfile.TarError) as error:
        print(f"Release asset validation failed: {error}", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
