"""Consumer-oriented release tests. All artifacts are harmless local fixtures."""
import contextlib
import importlib.util
import io
import json
import os
from pathlib import Path
import shutil
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
        self.env = dict(GITHUB_SHA="0123456789abcdef" * 2 + "01234567", GITHUB_REF="refs/tags/v1.12.5",
                        GITHUB_SERVER_URL="https://github.com", GITHUB_REPOSITORY="owner/repo", GITHUB_RUN_ID="1")
        self.version = release.application_version(REPO)
        for arch in ("x64", "arm64"):
            with zipfile.ZipFile(self.assets / f"LHash-windows-{arch}-{self.env['GITHUB_SHA'][:7]}.zip", "w") as archive:
                archive.writestr("LHash.exe", b"harmless binary fixture")
                archive.writestr("BUILD_INFO.txt", f"commit={self.env['GITHUB_SHA']}\napplication_version={self.version}\nopenssl_vendor_version=3.5.9\n")
        self.scan = self.root / "scan.json"
        self.scan.write_text(json.dumps({"bomFormat": "CycloneDX", "specVersion": "1.6", "version": 1, "components": []}))

    def build(self):
        release.stage(self.assets, self.output, REPO, self.env)
        with contextlib.redirect_stdout(io.StringIO()):
            release.finalize(self.output, self.scan, REPO)

    def test_both_architectures_and_metadata_verify_from_download_directory(self):
        self.build()
        checksums = (self.output / release.CHECKSUMS).read_text()
        self.assertEqual(len(checksums.splitlines()), 4)
        self.assertIn("LHash-windows-x64-", checksums)
        self.assertIn("LHash-windows-arm64-", checksums)
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
        next(self.output.glob("*arm64*.zip")).write_bytes(b"changed fixture")
        with self.assertRaisesRegex(ValueError, "checksum mismatch"):
            release.verify(self.output)

    def test_omitted_arm64_checksum_rejected(self):
        self.build()
        path = self.output / release.CHECKSUMS
        path.write_text("\n".join(line for line in path.read_text().splitlines() if "arm64" not in line) + "\n")
        with self.assertRaisesRegex(ValueError, "both architectures"):
            release.verify(self.output)

    def test_prefixed_or_traversal_checksum_path_rejected(self):
        self.build()
        path = self.output / release.CHECKSUMS
        original = path.read_text()
        for prefix in ("release-assets/", "../"):
            path.write_text(original.replace("  LHash-windows-x64", "  " + prefix + "LHash-windows-x64"))
            with self.assertRaisesRegex(ValueError, "unsafe"):
                release.verify(self.output)

    def test_missing_architecture_rejected(self):
        next(self.assets.glob("*arm64*.zip")).unlink()
        with self.assertRaisesRegex(ValueError, "arm64"):
            release.stage(self.assets, self.output, REPO, self.env)

    def test_valid_short_and_full_commit_asset_names(self):
        for length in (7, 8, 12, 40):
            for arch in ("x64", "arm64"):
                current = next(self.assets.glob(f"*{arch}*.zip"))
                current.rename(self.assets / f"LHash-windows-{arch}-{self.env['GITHUB_SHA'][:length]}.zip")
            output = self.root / f"release-{length}"
            release.stage(self.assets, output, REPO, self.env)
            with contextlib.redirect_stdout(io.StringIO()):
                release.finalize(output, self.scan, REPO)
                release.verify(output)

    def test_wrong_commit_prefix_rejected(self):
        current = next(self.assets.glob("*arm64*.zip"))
        current.rename(self.assets / "LHash-windows-arm64-fffffff.zip")
        with self.assertRaisesRegex(ValueError, "from this commit"):
            release.stage(self.assets, self.output, REPO, self.env)

    def test_mismatched_binary_version_rejected(self):
        path = next(self.assets.glob("*arm64*.zip"))
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
                   GIT_DIR=str(REPO / ".git"), GIT_WORK_TREE=str(REPO))
        result = subprocess.run(["bash", "-e", "-o", "pipefail", "-c", self.shell_step("macos-cli-build.yml", "Package")],
                                cwd=self.root, env=env, capture_output=True, text=True)
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
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
