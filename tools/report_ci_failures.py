"""Expose bounded compiler/test diagnostics in Actions annotations as well as logs."""
from pathlib import Path
import re
import sys
import xml.etree.ElementTree as ET


FAILURE_PATTERN = re.compile(
    r"(?:fatal error|error [A-Z]+\d+|CMake Error|\bFAIL[: ]|\bFAILED\b|runtime error:|"
    r"ERROR: AddressSanitizer|\bException\b|\bTimeout\b|\bTimed out\b|\bAborted\b|"
    r"\bcannot\b|not found|ParserError|AssertionError|Traceback)", re.I
)
SUCCESS_SUMMARY = re.compile(r"\b0 (?:tests? )?failed\b|100% tests passed", re.I)


def junit_failures(path):
    """CTest can report a timeout only in JUnit, without a matching log error."""
    try:
        root = ET.parse(path).getroot()
    except (ET.ParseError, OSError):
        return
    for case in root.iter("testcase"):
        failures = list(case.findall("failure")) + list(case.findall("error"))
        status = case.get("status", "").lower()
        if not failures and status not in {"fail", "failed", "failure", "error", "timeout", "aborted"}:
            continue
        name = case.get("name", "unnamed testcase")
        reasons = [element.get("message", "") for element in failures]
        reasons += [element.text or "" for element in failures]
        reason = "; ".join(value.strip() for value in reasons if value.strip())
        output = "\n".join(filter(None, (case.findtext("system-out"), case.findtext("system-err"))))
        # Retain the test name and failure reason even when a test prints a lot.
        tail = "\n".join(output.splitlines()[-16:])[-1400:]
        message = f"{path.name}: testcase {name[:200]} [{status or 'failed'}] {reason[:400]}"
        if tail:
            message += "\n" + tail
        yield message


def collect_failures(directories):
    roots = [Path(directory) for directory in directories]
    # Structured results take priority over summary lines and compiler noise.
    for root in roots:
        for path in sorted(root.rglob("*.xml")):
            yield from junit_failures(path)
    for root in roots:
        for path in sorted(root.rglob("*.log")):
            for line in path.read_text(encoding="utf-8", errors="replace").splitlines():
                if not SUCCESS_SUMMARY.search(line) and FAILURE_PATTERN.search(line):
                    yield f"{path.name}: {line[:1600]}"


def main(directories):
    seen = set()
    for message in collect_failures(directories):
        if message in seen:
            continue
        seen.add(message)
        escaped = message.replace("%", "%25").replace("\r", "%0D").replace("\n", "%0A")
        print(f"::error::{escaped}")
        if len(seen) == 12:
            break


if __name__ == "__main__":
    main(sys.argv[1:])
