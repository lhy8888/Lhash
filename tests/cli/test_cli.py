"""Exercise the shipped CLI's complete Windows-compatible algorithm set."""
import hashlib
import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile


DEFAULTS = {"openssl-sha-256", "openssl-sha-512", "openssl-sha3-256"}
VECTORS = json.loads(Path(__file__).with_name("algorithm_vectors.json").read_text())["vectors"]


def payload_for(vector):
    if vector["name"] == "abc":
        return b"abc"
    size = vector["bytes"]
    return (bytes(range(256)) * ((size + 255) // 256))[:size]


def main():
    executable = str(Path(sys.argv[1]).resolve(strict=True))
    cases = 0

    def run(arguments, expected_code=0, cwd=None):
        nonlocal cases
        result = subprocess.run([executable, *map(str, arguments)], capture_output=True,
                                text=True, encoding="utf-8", timeout=45, cwd=cwd)
        assert result.returncode == expected_code, (arguments, result.returncode, result.stdout, result.stderr)
        cases += 1
        return result

    def record(arguments):
        result = run(["--json", *arguments])
        assert not result.stderr, result.stderr
        rows = [json.loads(line) for line in result.stdout.splitlines()]
        assert len(rows) == 1 and rows[0]["status"] == "ok", rows
        rows[0]["digests"] = {key: value.lower() for key, value in rows[0]["digests"].items()}
        return rows[0]

    with tempfile.TemporaryDirectory(prefix="lhash-cli-") as directory:
        root = Path(directory)
        assert "Usage:" in run([], 2).stderr
        assert "Usage:" in run(["--help"]).stdout
        listing = json.loads(run(["--json", "--list-algorithms"]).stdout)
        ids = {row["id"] for row in listing}
        assert len(listing) == 18 and ids == set(VECTORS[0]["digests"]), listing
        assert {row["id"] for row in listing if row["default"]} == DEFAULTS, listing
        assert len(run(["--list-algorithms"]).stdout.splitlines()) == 18
        labels = {row["id"]: row["label"] for row in listing}

        for vector in VECTORS:
            path = root / (vector["name"] + ".bin")
            path.write_bytes(payload_for(vector))
            for policy in ["--single-threaded", "--parallel"]:
                row = record(["--all", policy, path])
                assert row["path"] == str(path) and row["digests"] == vector["digests"], row
            defaults = record([path])["digests"]
            assert defaults == {key: vector["digests"][key] for key in DEFAULTS}, defaults
        abc = root / "abc.bin"
        abc_vector = VECTORS[1]["digests"]
        for algorithm in sorted(ids):
            assert record(["-a", algorithm, abc])["digests"] == {algorithm: abc_vector[algorithm]}
        assert record(["--algorithm=SHA256", "-a", "sha256", abc])["digests"] == {"openssl-sha-256": abc_vector["openssl-sha-256"]}
        assert record(["-a", "md5", "-a", "sha1", abc])["digests"] == {key: abc_vector[key] for key in ["md5", "sha1"]}
        for args in [["--all", "-a", "sha256", abc], ["--algorithm"], ["--algorithm=", abc],
                     ["-a", "unknown", abc], ["--bogus", abc], ["--list-algorithms", abc]]:
            error = run(args, 2)
            assert "ERROR:" in error.stderr and not error.stdout, error

        for size in (0, 1, 55, 56, 63, 64, 65, 111, 112, 127, 128,
                     239, 240, 241, 1023, 1024, 1025, 4095, 4096, 4097,
                     4 * 1024 * 1024 - 1, 4 * 1024 * 1024, 4 * 1024 * 1024 + 17):
            payload = (bytes(range(256)) * ((size + 255) // 256))[:size]
            path = root / f"摘要 😀 file {size}.bin"
            path.write_bytes(payload)
            row = record([path])
            assert row["path"] == str(path), row
            assert row["digests"] == {"openssl-sha-256": hashlib.sha256(payload).hexdigest(),
                                      "openssl-sha-512": hashlib.sha512(payload).hexdigest(),
                                      "openssl-sha3-256": hashlib.sha3_256(payload).hexdigest()}, row

        # Both human output and JSON must preserve record boundaries for names
        # containing control characters. JSON round-trips the original filename.
        strange = root / 'line\n\r\x1b[31m\x08"\\file.bin'
        strange.write_bytes(b"abc")
        row = record(["--all", strange])
        assert row["path"] == str(strange) and row["digests"] == abc_vector, row
        human = run([strange]).stdout
        assert len(human.splitlines()) == 4 and "\\n" in human and "\\x1b" in human, repr(human)
        assert not any(c in human for c in ["\r", "\x1b", "\x08"]), repr(human)
        plain = run(["--all", abc]).stdout.splitlines()
        assert len(plain) == 19 and plain[0] == str(abc), plain
        actual = dict(line.strip().rsplit(None, 1) for line in plain[1:])
        assert actual == {labels[key]: value.upper() for key, value in abc_vector.items()}, actual
        dash = root / "-input.bin"
        dash.write_bytes(b"abc")
        assert record(["--", dash])["digests"]["openssl-sha-256"] == abc_vector["openssl-sha-256"]
        escaped_option = run(["--json", "--", "-input.bin"], cwd=root)
        assert json.loads(escaped_option.stdout)["path"] == "-input.bin"

        missing = root / "missing\n\x1b.bin"
        error = run([missing], 1)
        assert len(error.stderr.splitlines()) == 1 and "\\n" in error.stderr and "\x1b" not in error.stderr
        assert "ERROR:" in run([root], 1).stderr
        mixed = run(["--json", missing, abc, root, abc], 1)
        rows = [json.loads(line) for line in mixed.stdout.splitlines()]
        assert [row["status"] for row in rows] == ["error", "ok", "error", "ok"], rows
        assert not mixed.stderr, mixed.stderr
        for row in rows:
            if row["status"] == "error": assert "digests" not in row and row["error"]
        symlink = root / "link.bin"
        symlink.symlink_to(abc)
        assert "ERROR:" in run([symlink], 1).stderr

        # Small outputs can fail only at the final flush. A read-only stdout
        # descriptor gives deterministic EBADF on both macOS and Linux, without
        # relying on /dev/full or SIGPIPE. The many-record case also forces an
        # error while output is still being produced.
        output_cases = [["--help"], ["-h"], ["--list-algorithms"],
                        ["--json", "--list-algorithms"], [abc], ["--json", abc],
                        ["--all", "--json", abc], ["--parallel", "--json", abc],
                        ["--json", *([abc] * 40)]]
        if os.name == "posix":
            for arguments in output_cases:
                normal = run(arguments)
                assert normal.stdout and not normal.stderr, normal
                with abc.open("rb") as readonly_stdout:
                    failed = subprocess.run([executable, *map(str, arguments)],
                                            stdout=readonly_stdout, stderr=subprocess.PIPE,
                                            text=True, encoding="utf-8", timeout=45)
                assert failed.returncode == 1 and "output" in failed.stderr.lower(), (arguments, failed)
                cases += 1
            assert abc.read_bytes() == b"abc", "The read-only output fixture was modified"
        if sys.platform.startswith("linux"):
            for arguments in [["--json", abc], ["--list-algorithms"]]:
                with open("/dev/full", "wb", buffering=0) as full_stdout:
                    failed = subprocess.run([executable, *map(str, arguments)],
                                            stdout=full_stdout, stderr=subprocess.PIPE,
                                            text=True, encoding="utf-8", timeout=45)
                assert failed.returncode == 1 and "output" in failed.stderr.lower(), (arguments, failed)
                cases += 1

        # Reject before accessing any paths or hashing, preserving the common limit.
        assert "8192" in run(["--json", *([abc] * 8193)], 2).stderr
        if sys.platform.startswith("linux"):
            bad = subprocess.run([os.fsencode(executable), b"bad-\xff"], capture_output=True, timeout=10)
            assert bad.returncode == 2 and b"valid UTF-8" in bad.stderr
            cases += 1
    print(f"PASS CLI cases={cases}: 18 algorithm parity, independent vectors, default selection, all/individual modes, UTF-8, escaped records, failure continuation, stdout failures")


if __name__ == "__main__":
    main()
