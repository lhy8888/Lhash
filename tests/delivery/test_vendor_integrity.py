"""Exercise the real PowerShell verifier against an isolated source copy."""
import os
from pathlib import Path
import shutil
import subprocess
import tempfile
import unittest

REPO = Path(__file__).resolve().parents[2]


@unittest.skipUnless(shutil.which("pwsh"), "PowerShell is required for the vendor integrity fixture")
class VendorIntegrityTest(unittest.TestCase):
    def test_exact_tree_and_changed_added_missing_files(self):
        with tempfile.TemporaryDirectory(prefix="lhash-vendor-integrity-") as temporary:
            root = Path(temporary)
            source = root / "third_party/openssl/3.5.9"
            shutil.copytree(REPO / "third_party/openssl/3.5.9", source)
            shutil.copyfile(REPO / "third_party/openssl/OPENSSL_3_5_9_FILES.sha256",
                            source.parent / "OPENSSL_3_5_9_FILES.sha256")

            def check(expected, message=""):
                result = subprocess.run(["pwsh", "-NoProfile", "-File", str(REPO / "tools/verify_openssl_vendor_pristine.ps1"),
                                         "-Version", "3.5.9", "-RepoRoot", str(root)],
                                        capture_output=True, text=True, timeout=120, env=os.environ)
                self.assertEqual(result.returncode == 0, expected, result.stdout + result.stderr)
                if message:
                    self.assertIn(message, result.stdout + result.stderr)

            check(True)
            file = source / "crypto/sha/sha256.c"
            original = file.read_bytes()
            file.write_bytes(original + b"\n/* harmless integrity fixture */\n")
            check(False, "upstream file has changed")
            file.write_bytes(original)
            extra = source / "unlisted-source.txt"
            extra.write_text("harmless extra fixture\n")
            check(False, "Unexpected file")
            extra.unlink()
            file.unlink()
            check(False, "Missing OpenSSL upstream files")


if __name__ == "__main__":
    unittest.main()
