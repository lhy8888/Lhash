# Windows application lifecycle smoke

`Invoke-LHashUiSmoke.ps1` drives the **built MFC application**, using only windows
whose process ID belongs to an application instance started by the script. It
does not send global keyboard input, change another window's foreground status,
or find/terminate processes by executable name.

```powershell
pwsh -File tests/windows-ui/Invoke-LHashUiSmoke.ps1 `
  -Executable trunk/x64/Release/LHash.exe `
  -ReportDirectory artifacts/windows-ui-smoke
```

Supply the actual built EXE path; keep its normal deployment dependencies beside
it. The script requires PowerShell 7.2+ and an accessible, unlocked Windows input
desktop. A service-only/headless or locked desktop is an explicit failure, never
a skipped test reported as successful. Preserve `ui-smoke-results.json` with the
CI artifacts, including failed runs.

CI runs this suite on Windows x64 and on a native Windows ARM64 runner. The ARM64
job downloads the packaged application from the same run and verifies its checksum,
commit/version metadata and ARM64 PE machine type before launch. Both jobs gate
release publication. The crash debugger described below is currently x64 only.

For each of two iterations it creates eight independent application processes
(16 cases by default). The idle control has no file argument; completion uses
a private three-byte file; active cases use a private 512 MiB temporary file.
It performs:

1. An idle `WM_CLOSE` without initializing a hash worker, to distinguish dialog
   teardown failures from hashing/OpenSSL-related exit failures.
2. `WM_CLOSE` after observing the UI's active hashing controls **and positive
   whole-file progress while still busy**, ensuring actual hashing/crypto use
   rather than only the pre-launch disabled controls.
3. A posted `WM_KEYDOWN(VK_ESCAPE)` / `WM_KEYUP` pair to the dialog thread's own
   focused child (or the dialog). This exercises the MFC modal message pump's
   `IsDialogMessage` / `OnCancel` route. If that route does not close the app, this
   case fails; there is no fallback to a different cancellation method.
4. `WM_COMMAND(IDCANCEL)` separately, which tests the actual dialog cancellation
   command without depending on keyboard focus or message preprocessing.
5. `WM_CLOSE` immediately after discovering the main dialog, without waiting for
   the startup hashing timer. This stresses rapid launch/close; exact scheduling
   of the worker is not claimed deterministic.
6. The same small file supplied twice on the command line, waiting for the
   application to become idle with **Files=2, Done=2, Running=0**, and zero
   failed/cancelled/aborted/not-run counts. This checks that equal paths remain
   distinct tasks through the engine, UI adapter and displayed summary. The
   test then closes the still-open window and requires a zero exit code.
7. Sixteen occurrences of the large file, waiting for positive progress while
   busy, then posting the actual Stop button's `WM_COMMAND(IDC_OPEN)`. The
   window must stay open and become idle, with Running/Failed/Aborted all zero,
   at least one Not run row, and Done + Cancelled + Not run = 16. Stop may arrive
   between files, so Cancelled=0 is legitimate; deterministic running-to-cancelled
   transitions are covered by the task-model tests. Only after these checks does
   the test close the result window and require a zero exit code.
8. 128 distinct three-byte files supplied together. The idle summary must show
   all 128 completed. The actual result edit control must contain each filename
   exactly once and all 128 known SHA256 values. It then invokes the existing
   uppercase command and verifies a complete replacement in the opposite case,
   without missing or duplicated records. Finally Clear must empty the actual
   edit control and reset the summary to zero. This exercises incremental
   append, rebuild/reset and clear in the same process before normal close.

Each owned process has a total 10-second budget, including discovery and waiting
for the busy state. A timeout, inability to observe busy controls, missing main
dialog, failed PostMessage, or nonzero process exit fails the suite. On failure
the script terminates only the process instance it just created and records that
forced termination. It continues independent cases to preserve useful controls,
but any failure still fails the entire suite. No success is inferred from a
forced kill. Abnormal natural exits additionally wait up to 2.5 seconds for a
PID-matched Windows Application Error event (1000), with WER event 1001 associated
only by the exact report GUID. The module/offset/exception fields are saved and
published as safely escaped GitHub annotations. Its own temporary input is
removed when the suite ends.

The active close, Escape, cancel-command and Stop cases all require positive
progress before their action. The busy/idle check uses current resource IDs `IDC_OPEN=1003`, `IDC_FIND=1025`,
`IDC_SETTINGS=1034`, and the progress check uses `IDC_PROG_WHOLE=1014` with
`PBM_GETPOS` through a PID-checked `SendMessageTimeout` capped at 100 ms. Update
these checks if the resource IDs or session control behavior change.
The summary check uses the ordinary static text control
`IDC_STATIC_STATUS_OVERVIEW=1038` and `SendMessageTimeoutW(WM_GETTEXT)` with a
bounded `StringBuilder`. Windows marshals this standard message across processes;
the test never reads remote process memory or sends custom ListView structures.
It parses the seven count fields in their displayed order before Time and Speed,
independently of the English/Chinese labels, and saves the original summary and
parsed counts in `ui-smoke-results.json` and failure annotations. Missing,
truncated or unparseable summaries fail the case. These aggregate checks do not
claim to inspect every individual row or inject a native worker exception.
The many-results case reads `IDE_TXTMAIN=1000` with standard `WM_GETTEXTLENGTH`
and `WM_GETTEXT`, capped at 2 MiB and 100 ms per call. It retries if a concurrent
append fills the read buffer. It uses the existing `IDC_STATIC_UPPER=1023` and
`IDC_CLEAN=1017` command handlers only while idle. The JSON records text/rebuild/
clear checks and character count without including the full result text.
Folder scanning has separate production-worker/Win32 enumeration tests in the
full native runtime suite. This smoke does not claim to automate the Shell folder
picker or reproduce a real network-share stall.
This is a lifecycle smoke, not a replacement for PageHeap/ASan or deterministic
worker-barrier tests in `tests/ui_lifecycle`. A zero exit code alone cannot prove
the absence of every memory error. No native Windows execution is implied by
the script's availability or by Linux syntax checking.

When that smoke fails, CI builds `lhash_windows_crash_driver` from this directory
and runs it against the same x64 EXE. Its arguments are the absolute EXE path and
the diagnostic output directory. This separate Windows Debug API process starts
only its own child, waits for positive hashing progress, then posts `WM_CLOSE`.
It records second-chance exceptions with up to 20 DbgHelp stack frames, using the
EXE's adjacent PDB for function and source-line information. If an instruction
pointer is null, raw stack candidates are explicitly labeled as unverified.

The child receives only a small Windows environment allowlist and a System32
PATH; CI signing variables and tokens are not inherited. A crash produces a
normal/thread-information minidump, never a full-memory dump, plus the matching
EXE/PDB when available. Diagnostics are saved in `crash-stack.txt` and safely
escaped annotations. The driver terminates only its own child after a bounded
timeout and removes its private 512 MiB input on all exit paths. A clean debugger
run does not change the original smoke's failed result.
