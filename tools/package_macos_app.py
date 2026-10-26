#!/usr/bin/env python3
"""Package the already ad-hoc-signed Apple Silicon application without networking."""
import argparse
import os
from pathlib import Path
import re
import shutil
import stat
import sys
import tempfile
import zipfile

from release_assets import (application_version, sha256, validate_macos_app_binary,
                            validate_macos_app_bundle, validate_macos_app_plist)


def package(app, output, repo, env):
    commit = env["GITHUB_SHA"]
    ref = env["GITHUB_REF"]
    if not re.fullmatch(r"[0-9a-f]{40}", commit) or "\n" in ref or "\r" in ref:
        raise ValueError("Invalid build commit or ref")
    version = application_version(repo)
    if app.name != "LHash.app" or app.is_symlink() or not app.is_dir():
        raise ValueError("Expected the built LHash.app directory")
    for path in app.rglob("*"):
        if path.is_symlink() or not (path.is_dir() or path.is_file()):
            raise ValueError("Application bundle must contain only regular files and directories")
    executable = app / "Contents/MacOS/LHash"
    validate_macos_app_binary(executable.read_bytes())
    validate_macos_app_plist((app / "Contents/Info.plist").read_bytes(), version)
    if not executable.stat().st_mode & 0o111:
        raise ValueError("Application binary must be executable")
    if not (app / "Contents/_CodeSignature/CodeResources").is_file():
        raise ValueError("Ad-hoc sign and verify the application before packaging")
    output.mkdir(parents=True, exist_ok=True)
    basename = f"LHash-macos-arm64-app-{commit[:7]}"
    archive_path = output / (basename + ".zip")
    if archive_path.exists():
        raise ValueError("Refusing to overwrite an existing application package")
    with tempfile.TemporaryDirectory(prefix="lhash-macos-package-") as temporary:
        root = Path(temporary) / basename
        root.mkdir()
        shutil.copytree(app, root / "LHash.app")
        license_dir = root / "LICENSES"
        license_dir.mkdir()
        licenses = {"LICENSE": "LICENSE", "LICENSE-OPENSSL-EXCEPTION.md": "LICENSE-OPENSSL-EXCEPTION.md",
                    "OpenSSL.txt": "third_party/openssl/3.5.9/LICENSE.txt",
                    "BLAKE3-CC0.txt": "third_party/blake3/1.8.4/LICENSE_CC0",
                    "BLAKE3-Apache-2.0.txt": "third_party/blake3/1.8.4/LICENSE_A2",
                    "xxHash.txt": "third_party/xxhash/0.8.3/LICENSE",
                    "CRC32C.txt": "third_party/crc32c/1.1.2/LICENSE"}
        for name, source in licenses.items():
            shutil.copyfile(repo / source, license_dir / name)
        info = {"commit": commit, "short_sha": commit[:7], "ref": ref,
                "application_version": version, "platform": "macos-arm64", "artifact_type": "app",
                "minimum_macos": "14.0", "openssl_enabled": "true", "openssl_vendor_version": "3.5.9",
                "openssl_vendor_assembly_mode": "optimized", "openssl_vendor_target": "darwin64-arm64-cc",
                "signature_status": "AdHoc", "notarized": "false",
                "packaged_executable": "LHash.app/Contents/MacOS/LHash", "sha256": sha256(executable)}
        (root / "BUILD_INFO.txt").write_text("".join(f"{key}={value}\n" for key, value in info.items()), encoding="ascii")
        (root / "README.txt").write_text(
            f"LHash {version} for macOS 14+ Apple Silicon\n\n"
            "Verify this package from its extracted directory:\n"
            "  shasum -a 256 -c SHA256.txt\n\n"
            "Copy LHash.app to Applications, then open it.\n"
            "Use Open / Open Folder or drag files and folders into the window.\n"
            "Settings offers the same 18 algorithms as the Windows desktop app.\n"
            "Files are read locally. No account or file upload is required.\n\n"
            "This build has only a local ad-hoc code signature. It is not Developer ID\n"
            "signed or notarized by Apple. macOS may require approval in Privacy &\n"
            "Security after you verify the download and attempt to open the app.\n"
            "The separate CLI package remains available.\n\n"
            f"Source commit: {commit}\n"
            "Source: https://github.com/lhy8888/Lhash\n"
            "Licenses and OpenSSL linking exception: LICENSES/\n", encoding="utf-8")
        paths = sorted(path for path in root.rglob("*") if path.is_file())
        (root / "SHA256.txt").write_text("".join(f"{sha256(path)}  {path.relative_to(root).as_posix()}\n"
                                                   for path in paths), encoding="utf-8")
        # Keep the Mach-O executable bit after Finder's ZIP extraction. Explicit
        # Unix modes avoid inheriting a CI umask or marking metadata executable.
        with zipfile.ZipFile(archive_path, "x", compression=zipfile.ZIP_DEFLATED) as archive:
            for path in sorted(root.rglob("*")):
                if not path.is_file():
                    continue
                entry = zipfile.ZipInfo.from_file(path, path.relative_to(root.parent).as_posix())
                entry.create_system = 3
                permissions = 0o755 if path.relative_to(root).as_posix() == info["packaged_executable"] else 0o644
                entry.external_attr = (stat.S_IFREG | permissions) << 16
                entry.compress_type = zipfile.ZIP_DEFLATED
                with path.open("rb") as source, archive.open(entry, "w") as destination:
                    shutil.copyfileobj(source, destination)
        try:
            validate_macos_app_bundle(archive_path, commit, version)
        except Exception:
            archive_path.unlink()
            raise
    print(archive_path)
    return archive_path


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--app", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    parser.add_argument("--repo", type=Path, default=Path.cwd())
    args = parser.parse_args()
    try:
        package(args.app, args.output, args.repo, os.environ)
    except (OSError, ValueError, KeyError, zipfile.BadZipFile) as error:
        print(f"Application packaging failed: {error}", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
