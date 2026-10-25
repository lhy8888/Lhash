"""Exercise the shipped macOS baseline CLI against Python's independent hashes."""
import hashlib
from pathlib import Path
import subprocess
import sys
import tempfile


def main():
    executable = str(Path(sys.argv[1]).resolve(strict=True))
    cases = 0

    def run(paths, expected_code):
        nonlocal cases
        result = subprocess.run([executable, *map(str, paths)], capture_output=True,
                                text=True, encoding="utf-8", timeout=30)
        assert result.returncode == expected_code, (result.returncode, result.stdout, result.stderr)
        cases += 1
        return result

    with tempfile.TemporaryDirectory(prefix="lhash-cli-") as directory:
        root = Path(directory)
        assert "Usage:" in run([], 2).stderr
        for size in (0, 1, 55, 56, 63, 64, 65, 4095, 4096, 4097,
                     4 * 1024 * 1024 - 1, 4 * 1024 * 1024, 4 * 1024 * 1024 + 17):
            payload = (bytes(range(256)) * ((size + 255) // 256))[:size]
            path = root / f"摘要 file {size}.bin"
            path.write_bytes(payload)
            result = run([path], 0)
            lines = [line.split() for line in result.stdout.splitlines()[1:]]
            actual = {parts[0].replace("-", "").lower(): parts[1].lower()
                      for parts in lines if len(parts) == 2}
            assert actual == {"md5": hashlib.md5(payload).hexdigest(),
                              "sha1": hashlib.sha1(payload).hexdigest()}, result.stdout
            assert not result.stderr, result.stderr
        missing = root / "missing.bin"
        assert "ERROR:" in run([missing], 1).stderr
        assert "ERROR:" in run([root], 1).stderr
        mixed = run([missing, path], 1)
        assert hashlib.md5(payload).hexdigest() in mixed.stdout.lower(), mixed.stdout
        symlink = root / "link.bin"
        symlink.symlink_to(path)
        assert "ERROR:" in run([symlink], 1).stderr
    print(f"PASS CLI cases={cases}: exact digests, Unicode paths, boundaries, exit codes, error continuation")


if __name__ == "__main__":
    main()
