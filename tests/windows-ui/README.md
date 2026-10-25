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

For each of two iterations it creates a new application process with a private
512 MiB temporary file argument, then performs:

1. `WM_CLOSE` after observing the UI's active hashing controls.
2. A posted `WM_KEYDOWN(VK_ESCAPE)` / `WM_KEYUP` pair to the dialog thread's own
   focused child (or the dialog). This exercises the MFC modal message pump's
   `IsDialogMessage` / `OnCancel` route. If that route does not close the app, this
   case fails; there is no fallback to a different cancellation method.
3. `WM_COMMAND(IDCANCEL)` separately, which tests the actual dialog cancellation
   command without depending on keyboard focus or message preprocessing.
4. `WM_CLOSE` immediately after discovering the main dialog, without waiting for
   the startup hashing timer. This stresses rapid launch/close; exact scheduling
   of the worker is not claimed deterministic.

Each owned process has a total 10-second budget, including discovery and waiting
for the busy state. A timeout, inability to observe busy controls, missing main
dialog, failed PostMessage, or nonzero process exit fails the suite. On failure
the script terminates only the process instance it just created, records that
forced termination, and removes its own temporary input. No success is inferred
from a forced kill.

The busy check uses current resource IDs `IDC_OPEN=1003`, `IDC_FIND=1025`,
`IDC_SETTINGS=1034`; update it if these IDs or session control behavior change.
This is a lifecycle smoke, not a replacement for PageHeap/ASan or deterministic
worker-barrier tests in `tests/ui_lifecycle`. A zero exit code alone cannot prove
the absence of every memory error. No native Windows execution is implied by
the script's availability or by Linux syntax checking.
