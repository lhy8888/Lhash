"""Expose bounded compiler/test diagnostics in Actions annotations as well as logs."""
from pathlib import Path
import re
import sys

pattern = re.compile(r"(?:fatal error|error [A-Z]+\d+|CMake Error|FAIL[: ]|FAILED|runtime error:|ERROR: AddressSanitizer|Exception|cannot|not found|ParserError|AssertionError|Traceback)", re.I)
remaining = 12
for directory in sys.argv[1:]:
    for path in sorted(Path(directory).rglob("*.log")):
        for line in path.read_text(encoding="utf-8", errors="replace").splitlines():
            success_summary = re.search(r"\b0 (?:tests? )?failed\b|100% tests passed", line, re.I)
            if remaining and not success_summary and pattern.search(line):
                message = f"{path.name}: {line[:1200]}"
                message = message.replace("%", "%25").replace("\r", "%0D").replace("\n", "%0A")
                print(f"::error::{message}")
                remaining -= 1
