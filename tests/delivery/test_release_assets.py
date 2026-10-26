"""Consumer-oriented release tests. All artifacts are harmless local fixtures."""
import contextlib
import hashlib
import importlib.util
import io
import json
import os
from pathlib import Path
import plistlib
import shutil
import stat
import struct
import subprocess
import tempfile
import tarfile
import unittest
import zipfile

REPO = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location("release_assets", REPO / "tools/release_assets.py")
release = importlib.util.module_from_spec(spec)
spec.loader.exec_module(release)


class ReleaseAssetsTest(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix="lhash-release-test-")
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.assets = self.root / "assets"
        self.assets.mkdir()
        self.output = self.root / "release"
        self.version = release.application_version(REPO)
        self.env = dict(GITHUB_SHA="0123456789abcdef" * 2 + "01234567", GITHUB_REF="refs/tags/v" + self.version.removesuffix(".0"),
                        GITHUB_SERVER_URL="https://github.com", GITHUB_REPOSITORY="owner/repo", GITHUB_RUN_ID="1")
        for arch in ("x64", "arm64"):
            with zipfile.ZipFile(self.assets / f"LHash-windows-{arch}-{self.env['GITHUB_SHA'][:7]}.zip", "w") as archive:
                archive.writestr("LHash.exe", b"harmless binary fixture")
                archive.writestr("BUILD_INFO.txt", f"commit={self.env['GITHUB_SHA']}\napplication_version={self.version}\nopenssl_vendor_version=3.5.9\n")
        self.write_macos()
        self.write_macos_app()
        self.scan = self.root / "scan.json"
        self.scan.write_text(json.dumps({"bomFormat": "CycloneDX", "specVersion": "1.6", "version": 1, "components": []}))

    def write_macos(self, metadata=None, checksum=None, extra=None, omit=None):
        root = f"LHash-macos-arm64-cli-{self.env['GITHUB_SHA'][:7]}"
        binary = b"harmless macOS binary fixture"
        digest = hashlib.sha256(binary).hexdigest()
        info = {"commit": self.env["GITHUB_SHA"], "application_version": self.version,
                "platform": "macos-arm64", "artifact_type": "cli",
                "openssl_vendor_version": "3.5.9", "sha256": digest}
        info.update(metadata or {})
        contents = {"lhash": binary, "README.txt": b"local fixture\n",
                    "BUILD_INFO.txt": "".join(f"{key}={value}\n" for key, value in info.items()).encode(),
                    "SHA256.txt": (checksum or f"{digest}  lhash\n").encode()}
        with tarfile.open(self.assets / (root + ".tar.gz"), "w:gz") as archive:
            for name, data in contents.items():
                if name == omit:
                    continue
                member = tarfile.TarInfo(root + "/" + name)
                member.size = len(data)
                member.mode = 0o755 if name == "lhash" else 0o644
                archive.addfile(member, io.BytesIO(data))
            if extra is not None:
                archive.addfile(extra, io.BytesIO(b""))

    def write_macos_app(self):
        app = self.root / "LHash.app"
        (app / "Contents/MacOS").mkdir(parents=True, exist_ok=True)
        (app / "Contents/_CodeSignature").mkdir(parents=True, exist_ok=True)
        # Harmless, non-runnable thin arm64 Mach-O fixture with macOS 14 metadata.
        binary = struct.pack("<8I", 0xfeedfacf, 0x100000c, 0, 2, 1, 24, 0, 0)
        binary += struct.pack("<6I", 0x32, 24, 1, 14 << 16, 14 << 16, 0)
        executable = app / "Contents/MacOS/LHash"
        executable.write_bytes(binary)
        executable.chmod(0o755)
        (app / "Contents/_CodeSignature/CodeResources").write_bytes(b"harmless signature fixture")
        (app / "Contents/Info.plist").write_bytes(plistlib.dumps({
            "CFBundleExecutable": "LHash", "CFBundlePackageType": "APPL",
            "CFBundleShortVersionString": self.version.removesuffix(".0"),
            "CFBundleVersion": self.version, "LSMinimumSystemVersion": "14.0"}))
        self.app_bundle = self.assets / f"LHash-macos-arm64-app-{self.env['GITHUB_SHA'][:7]}.zip"
        result = subprocess.run([os.sys.executable, str(REPO / "tools/package_macos_app.py"),
                                 "--app", str(app), "--output", str(self.assets), "--repo", str(REPO)],
                                env=dict(os.environ, **self.env), capture_output=True, text=True)
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)

    def rewrite_macos_app(self, transform):
        with zipfile.ZipFile(self.app_bundle) as archive:
            entries = [(entry, archive.read(entry)) for entry in archive.infolist()]
        with zipfile.ZipFile(self.app_bundle, "w") as archive:
            for entry, data in transform(entries):
                archive.writestr(entry, data)

    def build(self):
        release.stage(self.assets, self.output, REPO, self.env)
        with contextlib.redirect_stdout(io.StringIO()):
            release.finalize(self.output, self.scan, REPO)

    def test_all_platform_packages_and_metadata_verify_from_download_directory(self):
        self.build()
        checksums = (self.output / release.CHECKSUMS).read_text()
        self.assertEqual(len(checksums.splitlines()), 6)
        self.assertIn("LHash-windows-x64-", checksums)
        self.assertIn("LHash-windows-arm64-", checksums)
        self.assertIn("LHash-macos-arm64-cli-", checksums)
        self.assertIn("LHash-macos-arm64-app-", checksums)
        self.assertNotIn("release-assets/", checksums)
        self.assertIn(f"lhash_application_version={self.version}", (self.output / release.MANIFEST).read_text())
        consumer = self.root / "downloaded"
        shutil.copytree(self.output, consumer)
        with contextlib.redirect_stdout(io.StringIO()):
            release.verify(consumer)
        # This is the actual user command, not just the helper's own parser.
        checker = shutil.which("sha256sum")
        if checker:
            result = subprocess.run([checker, "--check", release.CHECKSUMS], cwd=consumer, capture_output=True, text=True)
            self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        components = json.loads((consumer / release.SBOM).read_text())["components"]
        self.assertEqual({(item["name"], item["version"]) for item in components},
                         {("OpenSSL", "3.5.9"), ("BLAKE3", "1.8.4"), ("xxHash", "0.8.3")})

    def test_finalize_cannot_clobber_existing_checksum_manifest(self):
        self.build()
        before = (self.output / release.CHECKSUMS).read_bytes()
        with self.assertRaisesRegex(ValueError, "overwrite"):
            release.finalize(self.output, self.scan, REPO)
        self.assertEqual((self.output / release.CHECKSUMS).read_bytes(), before)

    def test_modified_arm64_archive_rejected(self):
        self.build()
        next(self.output.glob("LHash-windows-arm64*.zip")).write_bytes(b"changed fixture")
        with self.assertRaisesRegex(ValueError, "checksum mismatch"):
            release.verify(self.output)

    def test_omitted_arm64_checksum_rejected(self):
        self.build()
        path = self.output / release.CHECKSUMS
        path.write_text("\n".join(line for line in path.read_text().splitlines() if "arm64" not in line) + "\n")
        with self.assertRaisesRegex(ValueError, "all platform packages"):
            release.verify(self.output)

    def test_missing_macos_package_rejected(self):
        next(self.assets.glob("*.tar.gz")).unlink()
        with self.assertRaisesRegex(ValueError, "macOS"):
            release.stage(self.assets, self.output, REPO, self.env)

    def test_macos_package_name_must_match_commit(self):
        current = next(self.assets.glob("*.tar.gz"))
        current.rename(self.assets / "LHash-macos-arm64-cli-fffffff.tar.gz")
        with self.assertRaisesRegex(ValueError, "from this commit"):
            release.stage(self.assets, self.output, REPO, self.env)

    def test_omitted_macos_checksum_rejected(self):
        self.build()
        path = self.output / release.CHECKSUMS
        path.write_text("\n".join(line for line in path.read_text().splitlines()
                                  if "LHash-macos-" not in line) + "\n")
        with self.assertRaisesRegex(ValueError, "all platform packages"):
            release.verify(self.output)

    def test_macos_commit_version_and_platform_must_match(self):
        for key, value in (("commit", "f" * 40), ("application_version", "0.0.0.0"),
                           ("platform", "macos-x64"), ("openssl_vendor_version", "0.0.0")):
            with self.subTest(key=key):
                self.write_macos(metadata={key: value})
                with self.assertRaisesRegex(ValueError, "metadata disagrees"):
                    release.stage(self.assets, self.root / ("bad-" + key), REPO, self.env)

    def test_macos_internal_checksum_must_match_executable(self):
        for kind in ("text", "metadata"):
            with self.subTest(kind=kind):
                if kind == "text":
                    self.write_macos(checksum="0" * 64 + "  lhash\n")
                else:
                    self.write_macos(metadata={"sha256": "0" * 64})
                with self.assertRaisesRegex(ValueError, "checksum mismatch"):
                    release.stage(self.assets, self.root / ("bad-" + kind), REPO, self.env)

    def test_macos_archive_rejects_unsafe_duplicate_and_missing_members(self):
        root = f"LHash-macos-arm64-cli-{self.env['GITHUB_SHA'][:7]}"
        symlink = tarfile.TarInfo(root + "/escape")
        symlink.type = tarfile.SYMTYPE
        symlink.linkname = "../../outside"
        extras = [tarfile.TarInfo("../outside"), tarfile.TarInfo("/absolute"),
                  tarfile.TarInfo(root + "/BUILD_INFO.txt"), symlink]
        for index, extra in enumerate(extras):
            with self.subTest(member=extra.name):
                self.write_macos(extra=extra)
                with self.assertRaisesRegex(ValueError, "Unsafe, duplicate"):
                    release.stage(self.assets, self.root / f"bad-member-{index}", REPO, self.env)
        self.write_macos(omit="lhash")
        with self.assertRaisesRegex(ValueError, "Missing executable"):
            release.stage(self.assets, self.root / "bad-missing", REPO, self.env)

    def test_modified_or_omitted_macos_package_rejected(self):
        self.build()
        package = next(self.output.glob("*.tar.gz"))
        package.write_bytes(b"changed fixture")
        with self.assertRaisesRegex(ValueError, "checksum mismatch"):
            release.verify(self.output)
        package.unlink()
        with self.assertRaisesRegex(ValueError, "macOS release archives"):
            release.verify(self.output)

    def test_release_tag_must_match_application_version(self):
        env = dict(self.env, GITHUB_REF="refs/tags/v0.0.0")
        with self.assertRaisesRegex(ValueError, "tag does not match"):
            release.stage(self.assets, self.output, REPO, env)

    def test_existing_windows_only_release_remains_verifiable(self):
        self.build()
        next(self.output.glob("*.tar.gz")).unlink()
        next(self.output.glob("LHash-macos-*.zip")).unlink()
        manifest = self.output / release.MANIFEST
        manifest.write_text("\n".join(line for line in manifest.read_text().splitlines()
                                     if not line.startswith(("macos_arm64=", "macos_arm64_app="))) + "\n")
        (self.output / release.CHECKSUMS).unlink()
        with contextlib.redirect_stdout(io.StringIO()):
            release.finalize(self.output, self.scan, REPO)
            release.verify(self.output)
        self.assertEqual(len((self.output / release.CHECKSUMS).read_text().splitlines()), 4)

    def test_existing_windows_and_cli_release_remains_verifiable(self):
        self.build()
        next(self.output.glob("LHash-macos-*.zip")).unlink()
        manifest = self.output / release.MANIFEST
        manifest.write_text("\n".join(line for line in manifest.read_text().splitlines()
                                     if not line.startswith("macos_arm64_app=")) + "\n")
        (self.output / release.CHECKSUMS).unlink()
        with contextlib.redirect_stdout(io.StringIO()):
            release.finalize(self.output, self.scan, REPO)
            release.verify(self.output)
        self.assertEqual(len((self.output / release.CHECKSUMS).read_text().splitlines()), 5)

    def test_prefixed_or_traversal_checksum_path_rejected(self):
        self.build()
        path = self.output / release.CHECKSUMS
        original = path.read_text()
        for prefix in ("release-assets/", "../"):
            path.write_text(original.replace("  LHash-windows-x64", "  " + prefix + "LHash-windows-x64"))
            with self.assertRaisesRegex(ValueError, "unsafe"):
                release.verify(self.output)

    def test_missing_architecture_rejected(self):
        next(self.assets.glob("LHash-windows-arm64*.zip")).unlink()
        with self.assertRaisesRegex(ValueError, "arm64"):
            release.stage(self.assets, self.output, REPO, self.env)

    def test_valid_short_and_full_commit_asset_names(self):
        for length in (7, 8, 12, 40):
            for arch in ("x64", "arm64"):
                current = next(self.assets.glob(f"LHash-windows-{arch}*.zip"))
                current.rename(self.assets / f"LHash-windows-{arch}-{self.env['GITHUB_SHA'][:length]}.zip")
            output = self.root / f"release-{length}"
            release.stage(self.assets, output, REPO, self.env)
            with contextlib.redirect_stdout(io.StringIO()):
                release.finalize(output, self.scan, REPO)
                release.verify(output)

    def test_wrong_commit_prefix_rejected(self):
        current = next(self.assets.glob("LHash-windows-arm64*.zip"))
        current.rename(self.assets / "LHash-windows-arm64-fffffff.zip")
        with self.assertRaisesRegex(ValueError, "from this commit"):
            release.stage(self.assets, self.output, REPO, self.env)

    def test_mismatched_binary_version_rejected(self):
        path = next(self.assets.glob("LHash-windows-arm64*.zip"))
        with zipfile.ZipFile(path, "w") as archive:
            archive.writestr("LHash.exe", b"harmless binary fixture")
            archive.writestr("BUILD_INFO.txt", f"commit={self.env['GITHUB_SHA']}\napplication_version=0.0.0.0\nopenssl_vendor_version=3.5.9\n")
        with self.assertRaisesRegex(ValueError, "metadata disagrees"):
            release.stage(self.assets, self.output, REPO, self.env)

    def test_unknown_source_version_rejected(self):
        repo = self.root / "bad-source"
        header = repo / "trunk/source/WinMFC/version.h"
        header.parent.mkdir(parents=True)
        header.write_text('#define STR_VERSION_LEGACY "unknown"\n')
        with self.assertRaisesRegex(ValueError, "version"):
            release.stage(self.assets, self.output, repo, self.env)

    def test_duplicate_metadata_keys_rejected(self):
        with self.assertRaisesRegex(ValueError, "duplicate"):
            release.fields("commit=a\ncommit=b\n")

    def test_missing_app_package_rejected(self):
        self.app_bundle.unlink()
        with self.assertRaisesRegex(ValueError, "application bundle"):
            release.stage(self.assets, self.output, REPO, self.env)

    def test_app_archive_rejects_unsafe_and_duplicate_paths_and_links(self):
        original = self.app_bundle.read_bytes()
        prefix = self.app_bundle.stem + "/"
        for name, mode in (("../outside", stat.S_IFREG), ("/absolute", stat.S_IFREG),
                           (prefix + "LHash.app/Contents/../escape", stat.S_IFREG),
                           (prefix + "LHash.app/Contents/link", stat.S_IFLNK),
                           (prefix + "BUILD_INFO.txt", stat.S_IFREG)):
            with self.subTest(name=name, mode=mode):
                self.app_bundle.write_bytes(original)
                entry = zipfile.ZipInfo(name)
                entry.external_attr = (mode | 0o644) << 16
                with zipfile.ZipFile(self.app_bundle, "a") as archive:
                    archive.writestr(entry, b"../../outside")
                with self.assertRaisesRegex(ValueError, "Unsafe, duplicate"):
                    release.validate_macos_app_bundle(self.app_bundle, self.env["GITHUB_SHA"], self.version)

    def test_app_requires_executable_permission_and_arm64_macos14_binary(self):
        original = self.app_bundle.read_bytes()
        def mutate(entries, change):
            for entry, data in entries:
                if entry.filename.endswith("/Contents/MacOS/LHash"):
                    entry, data = change(entry, data)
                yield entry, data
        def no_execute(entry, data):
            entry.external_attr = (stat.S_IFREG | 0o644) << 16
            return entry, data
        cases = [("permission", no_execute),
                 ("arm64", lambda e, d: (e, d[:4] + struct.pack("<I", 0x1000007) + d[8:])),
                 ("deployment", lambda e, d: (e, d[:44] + struct.pack("<I", 15 << 16) + d[48:]))]
        for expected, change in cases:
            with self.subTest(expected=expected):
                self.app_bundle.write_bytes(original)
                self.rewrite_macos_app(lambda entries: mutate(entries, change))
                with self.assertRaisesRegex(ValueError, expected):
                    release.validate_macos_app_bundle(self.app_bundle, self.env["GITHUB_SHA"], self.version)

    def test_app_checksums_cover_metadata_resources_and_licenses(self):
        original = self.app_bundle.read_bytes()
        for suffix in ("/README.txt", "/LICENSES/OpenSSL.txt", "/Contents/_CodeSignature/CodeResources"):
            with self.subTest(suffix=suffix):
                self.app_bundle.write_bytes(original)
                self.rewrite_macos_app(lambda entries: ((e, d + b"changed" if e.filename.endswith(suffix) else d)
                                                        for e, d in entries))
                with self.assertRaisesRegex(ValueError, "checksum mismatch"):
                    release.validate_macos_app_bundle(self.app_bundle, self.env["GITHUB_SHA"], self.version)

    def test_app_plist_version_must_match_release(self):
        def change(entries):
            for entry, data in entries:
                if entry.filename.endswith("/Contents/Info.plist"):
                    info = plistlib.loads(data)
                    info["CFBundleVersion"] = "0.0.0.0"
                    data = plistlib.dumps(info)
                yield entry, data
        self.rewrite_macos_app(change)
        with self.assertRaisesRegex(ValueError, "plist disagrees"):
            release.validate_macos_app_bundle(self.app_bundle, self.env["GITHUB_SHA"], self.version)

    def test_app_package_manifest_and_checksums_detect_omission_or_modification(self):
        self.build()
        package = self.output / self.app_bundle.name
        package.write_bytes(b"changed fixture")
        with self.assertRaisesRegex(ValueError, "checksum mismatch"):
            release.verify(self.output)
        package.unlink()
        with self.assertRaisesRegex(ValueError, "application archives"):
            release.verify(self.output)

    @unittest.skipUnless(shutil.which("unzip") and shutil.which("sha256sum"), "needs ZIP consumer tools")
    def test_actual_app_package_extracts_with_executable_bit_and_valid_checksums(self):
        extracted = self.root / "consumer-app"
        result = subprocess.run(["unzip", "-q", str(self.app_bundle), "-d", str(extracted)], capture_output=True, text=True)
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        root = next(extracted.iterdir())
        self.assertTrue((root / "LHash.app/Contents/MacOS/LHash").stat().st_mode & 0o111)
        result = subprocess.run(["sha256sum", "--check", "SHA256.txt"], cwd=root, capture_output=True, text=True)
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)

    @staticmethod
    def shell_step(workflow, name):
        text = (REPO / ".github/workflows" / workflow).read_text()
        start = text.index("      - name: " + name + "\n")
        end = text.find("\n      - name:", start + 1)
        step = text[start:end if end >= 0 else len(text)]
        body = step.split("        run: |\n", 1)[1]
        return "\n".join(line[10:] if line.startswith("          ") else line for line in body.splitlines())

    @unittest.skipUnless(shutil.which("bash") and shutil.which("shasum"), "needs POSIX packaging tools")
    def test_actual_macos_package_is_verifiable_after_extraction(self):
        executable = self.root / "lhash"
        executable.write_bytes(b"harmless package fixture")
        env = dict(os.environ, CLI_PATH=str(executable), GITHUB_SHA=self.env["GITHUB_SHA"],
                   GITHUB_REF=self.env["GITHUB_REF"], GITHUB_WORKSPACE=str(REPO),
                   GIT_DIR=str(REPO / ".git"), GIT_WORK_TREE=str(REPO))
        result = subprocess.run(["bash", "-e", "-o", "pipefail", "-c", self.shell_step("macos-cli-build.yml", "Package")],
                                cwd=self.root, env=env, capture_output=True, text=True)
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        release.validate_macos_bundle(next((self.root / "dist").glob("*.tar.gz")), self.env["GITHUB_SHA"], self.version)
        downloaded = self.root / "extracted"
        downloaded.mkdir()
        with tarfile.open(next((self.root / "dist").glob("*.tar.gz"))) as archive:
            archive.extractall(downloaded, filter="data")
        bundle = next(downloaded.iterdir())
        result = subprocess.run(["shasum", "-a", "256", "-c", "SHA256.txt"], cwd=bundle, capture_output=True, text=True)
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)

    @unittest.skipUnless(shutil.which("bash") and shutil.which("sha256sum"), "needs POSIX verification tools")
    def test_manual_tag_is_passed_as_literal_data_to_gh(self):
        self.build()
        command_dir = self.root / "bin"
        command_dir.mkdir()
        mock = command_dir / "gh"
        mock.write_text('''#!/usr/bin/env python3
import json, os, shutil, sys
from pathlib import Path
Path(os.environ['ARG_LOG']).write_text(json.dumps(sys.argv[1:]))
shutil.copytree(os.environ['FIXTURE_SOURCE'], sys.argv[sys.argv.index('--dir')+1])
''')
        mock.chmod(0o755)
        tag = "v$(printf${IFS}LOCAL_TAG_PROBE)"
        for workflow in ("release-provenance.yml", "release-sbom.yml"):
            work = self.root / workflow
            (work / "tools").mkdir(parents=True)
            shutil.copyfile(REPO / "tools/release_assets.py", work / "tools/release_assets.py")
            log = work / "args.json"
            env = dict(os.environ, PATH=str(command_dir) + os.pathsep + os.environ["PATH"],
                       RELEASE_TAG=tag, GITHUB_REPOSITORY="owner/repo", FIXTURE_SOURCE=str(self.output), ARG_LOG=str(log))
            result = subprocess.run(["bash", "-c", self.shell_step(workflow, "Download and verify immutable release assets")],
                                    cwd=work, env=env, capture_output=True, text=True)
            self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
            self.assertEqual(json.loads(log.read_text())[-2:], ["--", tag])


if __name__ == "__main__":
    unittest.main()
