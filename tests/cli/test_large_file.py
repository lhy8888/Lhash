"""Optional >4 GiB real-file regression. Uses a sparse temporary file on POSIX."""
import hashlib
import json
from pathlib import Path
import subprocess
import sys
import tempfile


def main():
    executable = str(Path(sys.argv[1]).resolve(strict=True))
    size = (1 << 32) + 4097
    with tempfile.TemporaryDirectory(prefix="lhash-large-file-") as directory:
        path = Path(directory) / "4GiB-plus4097.bin"
        tail = b"LHash-final-bytes!"
        with path.open("wb") as stream:
            stream.truncate(size)
            for offset, value in [(0, b"LHash-start"), ((1 << 31) - 5, b"boundary31"),
                                  ((1 << 32) - 5, b"boundary32"), (size - len(tail), tail)]:
                stream.seek(offset)
                stream.write(value)
        assert path.stat().st_size == size
        hashes = {name: hashlib.new(name) for name in ["md5", "sha1", "sha256"]}
        with path.open("rb") as stream:
            while block := stream.read(8 * 1024 * 1024):
                for digest in hashes.values():
                    digest.update(block)
        expected = {"md5": hashes["md5"].hexdigest(), "sha1": hashes["sha1"].hexdigest(),
                    "openssl-sha-256": hashes["sha256"].hexdigest()}
        for mode in ["--single-threaded", "--parallel"]:
            result = subprocess.run([executable, "--json", mode, "-a", "md5", "-a", "sha1",
                                     "-a", "sha256", str(path)], capture_output=True, text=True, timeout=240)
            assert result.returncode == 0 and not result.stderr, result
            rows = [json.loads(line) for line in result.stdout.splitlines()]
            assert len(rows) == 1 and rows[0]["status"] == "ok", rows
            assert {key: value.lower() for key, value in rows[0]["digests"].items()} == expected, rows
            print(f"PASS {mode}: bytes={size}, independent MD5/SHA1/SHA256", flush=True)


if __name__ == "__main__":
    main()
