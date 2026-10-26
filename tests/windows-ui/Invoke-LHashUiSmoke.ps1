#requires -Version 7.2
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$Executable,
    [string]$ReportDirectory = (Join-Path $PWD 'artifacts/windows-ui-smoke'),
    [ValidateRange(1, 5)][int]$Iterations = 2,
    [ValidateRange(128, 2048)][int]$FileSizeMiB = 512,
    [ValidateRange(3, 10)][int]$TimeoutSeconds = 10
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$nativeSource = @'
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace LHashUiSmoke {
    public static class Native {
        private delegate bool EnumWindowsProc(IntPtr window, IntPtr parameter);
        [DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindowsProc callback, IntPtr parameter);
        [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
        [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr window);
        [DllImport("user32.dll")] private static extern IntPtr GetWindow(IntPtr window, uint command);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassName(IntPtr window, StringBuilder text, int maximum);
        [DllImport("user32.dll")] private static extern IntPtr GetDlgItem(IntPtr window, int id);
        [DllImport("user32.dll")] private static extern bool IsWindowEnabled(IntPtr window);
        [DllImport("user32.dll", EntryPoint = "PostMessageW", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool PostMessage(IntPtr window, uint message, IntPtr wparam, IntPtr lparam);
        [DllImport("user32.dll", EntryPoint = "SendMessageTimeoutW", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr SendMessageTimeout(IntPtr window, uint message, IntPtr wparam, IntPtr lparam, uint flags, uint timeout, out IntPtr result);
        [DllImport("user32.dll", EntryPoint = "SendMessageTimeoutW", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr SendMessageTimeoutText(IntPtr window, uint message, IntPtr wparam, [Out] StringBuilder text, uint flags, uint timeout, out IntPtr result);
        [DllImport("user32.dll", SetLastError = true)] private static extern IntPtr OpenInputDesktop(uint flags, bool inherit, uint access);
        [DllImport("user32.dll")] private static extern bool CloseDesktop(IntPtr desktop);
        [DllImport("user32.dll")] private static extern bool GetGUIThreadInfo(uint threadId, ref GuiThreadInfo info);

        [StructLayout(LayoutKind.Sequential)] private struct Rect { public int Left, Top, Right, Bottom; }
        [StructLayout(LayoutKind.Sequential)] private struct GuiThreadInfo {
            public uint Size, Flags;
            public IntPtr Active, Focus, Capture, MenuOwner, MoveSize, Caret;
            public Rect CaretRect;
        }

        public static bool CanAccessInteractiveDesktop() {
            if (!Environment.UserInteractive) return false;
            IntPtr desktop = OpenInputDesktop(0, false, 0x0001 | 0x0040);
            if (desktop == IntPtr.Zero) return false;
            CloseDesktop(desktop);
            return true;
        }

        private static bool BelongsTo(IntPtr window, uint expectedProcessId) {
            uint actualProcessId;
            return window != IntPtr.Zero && GetWindowThreadProcessId(window, out actualProcessId) != 0
                && actualProcessId == expectedProcessId;
        }

        public static IntPtr FindMainDialog(uint processId) {
            IntPtr found = IntPtr.Zero;
            EnumWindows((window, parameter) => {
                if (!BelongsTo(window, processId) || !IsWindowVisible(window) || GetWindow(window, 4) != IntPtr.Zero)
                    return true; // GW_OWNER == 0; never select another process's window.
                var windowClass = new StringBuilder(256);
                GetClassName(window, windowClass, windowClass.Capacity);
                if (windowClass.ToString() != "#32770") return true;
                found = window;
                return false;
            }, IntPtr.Zero);
            return found;
        }

        public static bool IsHashSessionBusy(IntPtr window, uint processId) {
            if (!BelongsTo(window, processId)) return false;
            // IDs from WinMFC/resource.h. These three states are set together
            // before the actual worker launch; Verify disables Open as well.
            IntPtr open = GetDlgItem(window, 1003);
            IntPtr verify = GetDlgItem(window, 1025);
            IntPtr settings = GetDlgItem(window, 1034);
            return open != IntPtr.Zero && verify != IntPtr.Zero && settings != IntPtr.Zero
                && IsWindowEnabled(open) && !IsWindowEnabled(verify) && !IsWindowEnabled(settings);
        }

        public static bool PostClose(IntPtr window, uint processId) {
            return BelongsTo(window, processId) && PostMessage(window, 0x0010, IntPtr.Zero, IntPtr.Zero);
        }

        public static bool IsHashSessionIdle(IntPtr window, uint processId) {
            if (!BelongsTo(window, processId)) return false;
            IntPtr open = GetDlgItem(window, 1003);
            IntPtr verify = GetDlgItem(window, 1025);
            IntPtr settings = GetDlgItem(window, 1034);
            return BelongsTo(open, processId) && BelongsTo(verify, processId) && BelongsTo(settings, processId)
                && IsWindowEnabled(open) && IsWindowEnabled(verify) && IsWindowEnabled(settings);
        }

        public static string GetStatusOverview(IntPtr window, uint processId) {
            if (!BelongsTo(window, processId)) return null;
            IntPtr status = GetDlgItem(window, 1038); // IDC_STATIC_STATUS_OVERVIEW
            if (!BelongsTo(status, processId)) return null;
            var text = new StringBuilder(2048);
            IntPtr copied;
            // WM_GETTEXT is a standard system-marshalled message. No remote
            // ListView structures, pointers or process-memory access are used.
            if (SendMessageTimeoutText(status, 0x000D, new IntPtr(text.Capacity), text, 0x23, 100, out copied) == IntPtr.Zero)
                return null;
            return copied.ToInt64() >= 0 && copied.ToInt64() < text.Capacity - 1 ? text.ToString() : null;
        }

        public static string GetResultText(IntPtr window, uint processId) {
            if (!BelongsTo(window, processId)) return null;
            IntPtr edit = GetDlgItem(window, 1000); // IDE_TXTMAIN
            if (!BelongsTo(edit, processId)) return null;
            IntPtr size;
            if (SendMessageTimeout(edit, 0x000E, IntPtr.Zero, IntPtr.Zero, 0x23, 100, out size) == IntPtr.Zero)
                return null; // WM_GETTEXTLENGTH
            long length = size.ToInt64();
            if (length < 0 || length > 2 * 1024 * 1024) return null;
            var text = new StringBuilder((int)length + 2);
            IntPtr copied;
            if (SendMessageTimeoutText(edit, 0x000D, new IntPtr(text.Capacity), text, 0x23, 100, out copied) == IntPtr.Zero)
                return null;
            // Retry on the next poll if new output filled the buffer between
            // length and text reads. Both are system-marshalled messages.
            return copied.ToInt64() >= 0 && copied.ToInt64() < text.Capacity - 1 ? text.ToString() : null;
        }

        public static bool PostResultSelection(IntPtr window, uint processId, int start, int end) {
            if (!BelongsTo(window, processId) || start < 0 || end < start || end >= 65536) return false;
            IntPtr edit = GetDlgItem(window, 1000);
            if (!BelongsTo(edit, processId)) return false;
            // EM_SETSEL has integer arguments, not pointers into either process.
            // Queue the interaction after the current UI update: a synchronous
            // cross-thread send can run inside its save/restore-selection calls.
            return PostMessage(edit, 0x00B1, new IntPtr(start), new IntPtr(end));
        }

        public sealed class ResultSelection {
            public bool WindowOwned;
            public bool ReadSucceeded;
            public int Win32Error;
            public uint PackedValue;
            public int Start = -1;
            public int End = -1;
        }

        public static ResultSelection GetResultSelection(IntPtr window, uint processId) {
            var observed = new ResultSelection();
            if (!BelongsTo(window, processId)) return observed;
            IntPtr edit = GetDlgItem(window, 1000);
            if (!BelongsTo(edit, processId)) return observed;
            observed.WindowOwned = true;
            IntPtr selection;
            // Null pointer arguments request EM_GETSEL's packed return value.
            // This test deliberately uses positions below its 16-bit boundary.
            if (SendMessageTimeout(edit, 0x00B0, IntPtr.Zero, IntPtr.Zero, 0x23, 100, out selection) == IntPtr.Zero) {
                observed.Win32Error = Marshal.GetLastWin32Error();
                return observed;
            }
            observed.ReadSucceeded = true;
            observed.PackedValue = unchecked((uint)selection.ToInt64());
            if (observed.PackedValue != uint.MaxValue) {
                observed.Start = (int)(observed.PackedValue & 0xffff);
                observed.End = (int)(observed.PackedValue >> 16);
            }
            return observed;
        }

        public static bool PostResultCommand(IntPtr window, uint processId, int command) {
            if (!IsHashSessionIdle(window, processId) || (command != 1017 && command != 1023)) return false;
            IntPtr control = GetDlgItem(window, command); // Clear / uppercase label
            return BelongsTo(control, processId) && PostMessage(window, 0x0111, new IntPtr(command), control);
        }

        public static bool PostStopCommand(IntPtr window, uint processId) {
            if (!IsHashSessionBusy(window, processId)) return false;
            IntPtr button = GetDlgItem(window, 1003); // IDC_OPEN becomes Stop while busy.
            return BelongsTo(button, processId)
                && PostMessage(window, 0x0111, new IntPtr(1003), button); // WM_COMMAND / BN_CLICKED
        }

        public static int GetHashProgress(IntPtr window, uint processId) {
            if (!BelongsTo(window, processId)) return -1;
            IntPtr progress = GetDlgItem(window, 1014); // IDC_PROG_WHOLE
            if (!BelongsTo(progress, processId)) return -1;
            IntPtr value;
            // PBM_GETPOS = WM_USER + 8. A hung UI cannot block this test forever.
            if (SendMessageTimeout(progress, 0x0408, IntPtr.Zero, IntPtr.Zero, 0x23, 100, out value) == IntPtr.Zero)
                return -1;
            long position = value.ToInt64();
            return position >= 0 && position <= 100 ? (int)position : -1;
        }

        public static bool PostCancelCommand(IntPtr window, uint processId) {
            // WM_COMMAND / IDCANCEL is the dialog cancellation command produced
            // by IsDialogMessage for Escape. This is separately tested below.
            return BelongsTo(window, processId) && PostMessage(window, 0x0111, new IntPtr(2), IntPtr.Zero);
        }

        public static bool PostEscapeKey(IntPtr window, uint processId) {
            if (!BelongsTo(window, processId)) return false;
            uint actualProcessId;
            uint threadId = GetWindowThreadProcessId(window, out actualProcessId);
            var info = new GuiThreadInfo();
            info.Size = (uint)Marshal.SizeOf(typeof(GuiThreadInfo));
            IntPtr target = window;
            if (GetGUIThreadInfo(threadId, ref info) && BelongsTo(info.Focus, processId)) target = info.Focus;
            // Post a real key message to this dialog thread's focused child (or
            // the dialog itself). Its modal pump must route Escape to OnCancel.
            // No SendKeys, global keyboard injection, or foreground changes.
            bool accepted = PostMessage(target, 0x0100, new IntPtr(0x1B), new IntPtr(0x00010001));
            if (accepted) PostMessage(target, 0x0101, new IntPtr(0x1B), new IntPtr(unchecked((int)0xC0010001)));
            return accepted;
        }
    }
}
'@

$results = [System.Collections.Generic.List[object]]::new()
$temporaryDirectory = $null
$suiteFailure = $null
$executablePath = $null
$caseNames = @('idle-close', 'active-close', 'escape-key', 'cancel-command', 'rapid-close', 'duplicate-completion', 'active-stop-summary', 'many-results-reset', 'selection-during-append')
$textInputPaths = @()
$textSha256 = @()
$textSha512 = @()
$textSizes = @()
$selectionInputPaths = @()
$selectionSha256 = @()
$selectionSizes = @()
$abcSha256 = 'ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad'
$reportRoot = [System.IO.Path]::GetFullPath($ReportDirectory)
[System.IO.Directory]::CreateDirectory($reportRoot) | Out-Null

function ConvertFrom-StatusSummary {
    param([string]$Text)
    # The translated labels can contain spaces. The product separates fields
    # with four spaces and puts the seven counts before time and speed.
    $fields = @($Text -split ' {4,}')
    if ($fields.Count -ne 9) { return $null }
    $keys = @('total', 'done', 'failed', 'running', 'cancelled', 'aborted', 'notRun')
    $counts = [ordered]@{}
    for ($index = 0; $index -lt $keys.Count; ++$index) {
        if ($fields[$index] -notmatch '^\S(?:.*\S)? ([0-9]+)$') { return $null }
        $value = 0
        if (-not [int]::TryParse($Matches[1], [ref]$value)) { return $null }
        $counts[$keys[$index]] = $value
    }
    return [pscustomobject]$counts
}

function Test-OrderedResultText {
    param([string]$Text, [string[]]$Paths, [string[]]$Sha256, [long[]]$Sizes, [string[]]$Sha512 = @())
    if ([string]::IsNullOrEmpty($Text) -or $Paths.Count -eq 0 -or
        $Sha256.Count -ne $Paths.Count -or $Sizes.Count -ne $Paths.Count -or
        ($Sha512.Count -ne 0 -and $Sha512.Count -ne $Paths.Count)) { return $false }
    # These private .bin fixtures have exactly two metadata lines and the
    # default three digest lines. Check records in input order, not just presence:
    # the former verifier accepted hashes inserted before their file metadata.
    $lines = @($Text -split '\r?\n' | Where-Object { $_.Length -ne 0 })
    if ($lines.Count -ne $Paths.Count * 6) { return $false }
    for ($index = 0; $index -lt $Paths.Count; ++$index) {
        $first = $index * 6
        $pathPattern = [regex]::Escape($Paths[$index])
        $hashPattern = [regex]::Escape($Sha256[$index])
        $sha512Pattern = if ($Sha512.Count -eq 0) { '[0-9a-f]{128}' } else { [regex]::Escape($Sha512[$index]) }
        if ($lines[$first] -notmatch "^(?:Name|文件名): $pathPattern`$" -or
            $lines[$first + 1] -notmatch "^(?:File Size|文件大小): $($Sizes[$index]) (?:Byte\(s\)|字节)(?: \([^\r\n]+\))?`$" -or
            $lines[$first + 2] -notmatch '^(?:Modified Date|修改日期): [0-9]{4}-[0-9]{2}-[0-9]{2} [0-9]{2}:[0-9]{2}$' -or
            $lines[$first + 3] -notmatch "(?i)^SHA-256: $hashPattern`$" -or
            $lines[$first + 4] -notmatch "(?i)^SHA-512: $sha512Pattern`$" -or
            $lines[$first + 5] -notmatch '^(?i)SHA3-256: [0-9a-f]{64}$') { return $false }
    }
    return $true
}

function Test-CompleteResultText {
    param([string]$Text)
    return Test-OrderedResultText -Text $Text -Paths $textInputPaths `
        -Sha256 $textSha256 -Sha512 $textSha512 -Sizes $textSizes
}

function Get-OwnedCrashEvent {
    param([int]$ProcessId, [datetime]$StartedAt, [string]$ExpectedApp)
    $lastReadError = $null
    for ($attempt = 0; $attempt -lt 10; ++$attempt) {
        try {
            $events = @(Get-WinEvent -FilterHashtable @{
                LogName = 'Application'; ProviderName = @('Application Error', 'Windows Error Reporting')
                Id = @(1000, 1001); StartTime = $StartedAt.AddSeconds(-1)
            } -MaxEvents 40 -ErrorAction SilentlyContinue)
            foreach ($eventRecord in $events) {
                $xml = [xml]$eventRecord.ToXml()
                $fields = @{}
                foreach ($data in $xml.Event.EventData.Data) { $fields[$data.GetAttribute('Name')] = $data.InnerText }
                if (-not $fields.ContainsKey('ProcessId')) { continue }
                $rawProcessId = [string]$fields['ProcessId']
                $eventProcessId = if ($rawProcessId.StartsWith('0x', [StringComparison]::OrdinalIgnoreCase)) {
                    [Convert]::ToInt32($rawProcessId.Substring(2), 16)
                } else { [int]$rawProcessId }
                if ($eventProcessId -ne $ProcessId) { continue }
                if ($fields.ContainsKey('AppPath') -and -not [string]::Equals(
                    [string]$fields['AppPath'], $ExpectedApp, [StringComparison]::OrdinalIgnoreCase)) { continue }

                $eventPath = Join-Path $reportRoot "owned-process-$ProcessId-event-$($eventRecord.Id).xml"
                $eventRecord.ToXml() | Set-Content -LiteralPath $eventPath -Encoding utf8
                $relatedReport = $null
                # WER 1001 commonly omits a process ID. Only associate it through
                # the exact report GUID from an already PID-matched 1000 event.
                $reportId = if ($fields.ContainsKey('IntegratorReportId')) { [string]$fields['IntegratorReportId'] } else { [string]$fields['ReportId'] }
                if ($reportId) {
                    foreach ($candidate in $events) {
                        if ($candidate.Id -ne 1001) { continue }
                        $candidateXml = [xml]$candidate.ToXml()
                        foreach ($datum in $candidateXml.Event.EventData.Data) {
                            if ($datum.GetAttribute('Name') -match '^Report(Id|Identifier)$' -and
                                [string]::Equals($datum.InnerText, $reportId, [StringComparison]::OrdinalIgnoreCase)) {
                                $relatedReport = "owned-process-$ProcessId-event-1001.xml"
                                $candidate.ToXml() | Set-Content -LiteralPath (Join-Path $reportRoot $relatedReport) -Encoding utf8
                            }
                        }
                    }
                }
                return [pscustomobject]@{
                    available = $true
                    eventId = $eventRecord.Id
                    timeUtc = $eventRecord.TimeCreated.ToUniversalTime().ToString('o')
                    processId = $eventProcessId
                    appName = [string]$fields['AppName']
                    appPath = [string]$fields['AppPath']
                    faultingModule = [string]$fields['ModuleName']
                    faultingModulePath = [string]$fields['ModulePath']
                    exceptionCode = [string]$fields['ExceptionCode']
                    faultingOffset = [string]$fields['FaultingOffset']
                    reportId = $reportId
                    relatedEvent1001 = $relatedReport
                }
            }
        }
        catch { $lastReadError = $_.Exception.Message }
        Start-Sleep -Milliseconds 250
    }
    return [pscustomobject]@{
        available = $false
        processId = $ProcessId
        error = $(if ($lastReadError) { $lastReadError } else { 'No PID-matched Application Error/WER event became available within 2.5 seconds.' })
    }
}

function Invoke-OwnedProcessCase {
    param([string]$Name, [string]$InputPath, [int]$Iteration)
    $process = [System.Diagnostics.Process]::new()
    $started = $false
    $caseStartedAt = [datetime]::Now
    $timer = [System.Diagnostics.Stopwatch]::StartNew()
    $record = [ordered]@{
        case = $Name; iteration = $Iteration; processId = $null; busyObserved = $false
        progressObserved = $false; progressValue = $null
        idleObserved = $false; statusSummary = $null; summaryCounts = $null; summaryVerified = $false; stopPosted = $false
        textVerified = $false; rebuildVerified = $false; clearVerified = $false; resultCharacters = 0
        selectionPosted = $false; selectionRetained = $false; orderedRecordsVerified = $false
        selectionObservation = $null; finalSelectionObservation = $null
        resultTextPreview = $null
        actionPosted = $false; exitCode = $null; elapsedMs = 0; passed = $false
        terminatedByTest = $false; crashEvent = $null; error = $null
    }
    try {
        $process.StartInfo = [System.Diagnostics.ProcessStartInfo]::new()
        $process.StartInfo.FileName = $executablePath
        $process.StartInfo.WorkingDirectory = [System.IO.Path]::GetDirectoryName($executablePath)
        $process.StartInfo.UseShellExecute = $false
        $inputCount = switch ($Name) { 'duplicate-completion' { 2 } 'active-stop-summary' { 16 } default { 1 } }
        if ($Name -eq 'many-results-reset') {
            foreach ($path in $textInputPaths) { $process.StartInfo.ArgumentList.Add($path) }
        } elseif ($Name -eq 'selection-during-append') {
            foreach ($path in $selectionInputPaths) { $process.StartInfo.ArgumentList.Add($path) }
        } elseif ($InputPath) {
            foreach ($unused in 1..$inputCount) { $process.StartInfo.ArgumentList.Add($InputPath) }
        }
        if (-not $process.Start()) { throw 'Process.Start returned false.' }
        $started = $true
        $record.processId = $process.Id
        $window = [IntPtr]::Zero
        while ($timer.Elapsed.TotalSeconds -lt $TimeoutSeconds) {
            if ($process.HasExited) { throw 'The application exited before a visible main dialog was found.' }
            $window = [LHashUiSmoke.Native]::FindMainDialog([uint32]$process.Id)
            if ($window -ne [IntPtr]::Zero) { break }
            Start-Sleep -Milliseconds 10
        }
        if ($window -eq [IntPtr]::Zero) { throw 'No visible main dialog in this process; desktop access or application startup failed.' }

        if ($Name -notin @('rapid-close', 'idle-close', 'duplicate-completion', 'many-results-reset')) {
            while ($timer.Elapsed.TotalSeconds -lt $TimeoutSeconds) {
                if ($process.HasExited) { throw 'The application exited before hashing became active.' }
                if ([LHashUiSmoke.Native]::IsHashSessionBusy($window, [uint32]$process.Id)) {
                    $record.busyObserved = $true
                    $progress = [LHashUiSmoke.Native]::GetHashProgress($window, [uint32]$process.Id)
                    if ($progress -gt 0 -and [LHashUiSmoke.Native]::IsHashSessionBusy($window, [uint32]$process.Id)) {
                        if ($Name -eq 'selection-during-append') {
                            $beforeSelection = [LHashUiSmoke.Native]::GetResultText($window, [uint32]$process.Id)
                            # Require metadata already displayed and the first digest
                            # still pending, so this selection precedes a later append.
                            if ($null -eq $beforeSelection -or $beforeSelection.Length -lt 20 -or
                                -not $beforeSelection.Contains($selectionInputPaths[0]) -or
                                $beforeSelection -match '(?m)^SHA-256:') {
                                Start-Sleep -Milliseconds 10
                                continue
                            }
                            $record.selectionPosted = [LHashUiSmoke.Native]::PostResultSelection($window, [uint32]$process.Id, 5, 15)
                            if (-not $record.selectionPosted) { throw 'Could not post the owned result selection while hashing.' }
                            $selectionObserved = $false
                            while ($timer.Elapsed.TotalSeconds -lt $TimeoutSeconds) {
                                if ($process.HasExited) { throw 'Application exited before the posted selection was observed.' }
                                $record.selectionObservation = [LHashUiSmoke.Native]::GetResultSelection($window, [uint32]$process.Id)
                                $afterSelection = [LHashUiSmoke.Native]::GetResultText($window, [uint32]$process.Id)
                                if (($null -ne $afterSelection -and $afterSelection -match '(?m)^SHA-256:') -or
                                    -not [LHashUiSmoke.Native]::IsHashSessionBusy($window, [uint32]$process.Id)) {
                                    throw 'Hashing completed before the posted selection-before-append condition was established.'
                                }
                                $observed = $record.selectionObservation
                                if ($observed.ReadSucceeded -and $observed.Start -eq 5 -and $observed.End -eq 15 -and
                                    $null -ne $afterSelection -and $afterSelection.Contains($selectionInputPaths[0])) {
                                    $selectionObserved = $true
                                    break
                                }
                                Start-Sleep -Milliseconds 10
                            }
                            if (-not $selectionObserved) { throw 'The posted result selection was not observed before the case timeout.' }
                        }
                        $record.progressObserved = $true
                        $record.progressValue = $progress
                        break
                    }
                }
                Start-Sleep -Milliseconds 10
            }
            if (-not $record.progressObserved) { throw 'Did not observe positive hashing progress while still busy; crypto-use close coverage was not achieved.' }
        }

        if ($Name -notin @('duplicate-completion', 'many-results-reset', 'selection-during-append')) {
            $record.actionPosted = switch ($Name) {
                'idle-close' { [LHashUiSmoke.Native]::PostClose($window, [uint32]$process.Id) }
                'active-close' { [LHashUiSmoke.Native]::PostClose($window, [uint32]$process.Id) }
                'escape-key' { [LHashUiSmoke.Native]::PostEscapeKey($window, [uint32]$process.Id) }
                'cancel-command' { [LHashUiSmoke.Native]::PostCancelCommand($window, [uint32]$process.Id) }
                'rapid-close' { [LHashUiSmoke.Native]::PostClose($window, [uint32]$process.Id) }
                'active-stop-summary' { [LHashUiSmoke.Native]::PostStopCommand($window, [uint32]$process.Id) }
                default { throw "Unknown case: $Name" }
            }
            if (-not $record.actionPosted) { throw 'PostMessage failed or the window no longer belongs to the test process.' }
            if ($Name -eq 'active-stop-summary') { $record.stopPosted = $true }
        }
        if ($Name -in @('duplicate-completion', 'active-stop-summary', 'many-results-reset', 'selection-during-append')) {
            while ($timer.Elapsed.TotalSeconds -lt $TimeoutSeconds) {
                if ($process.HasExited) { throw 'The application exited instead of keeping the completed/stopped result window open.' }
                $record.statusSummary = [LHashUiSmoke.Native]::GetStatusOverview($window, [uint32]$process.Id)
                $record.summaryCounts = ConvertFrom-StatusSummary -Text $record.statusSummary
                $idle = [LHashUiSmoke.Native]::IsHashSessionIdle($window, [uint32]$process.Id)
                if ($idle) { $record.idleObserved = $true }
                $counts = $record.summaryCounts
                if ($idle -and $null -ne $counts -and $counts.running -eq 0 -and $counts.failed -eq 0 -and $counts.aborted -eq 0) {
                    if ($Name -in @('duplicate-completion', 'many-results-reset', 'selection-during-append')) {
                        $expected = if ($Name -eq 'many-results-reset') { $textInputPaths.Count } else { 2 }
                        $record.summaryVerified = ($counts.total -eq $expected -and $counts.done -eq $expected -and $counts.cancelled -eq 0 -and $counts.notRun -eq 0)
                    } else {
                        # Stop can arrive between files: zero cancelled rows is
                        # valid when completed rows and not-run rows account for all inputs.
                        $record.summaryVerified = ($counts.total -eq 16 -and $counts.notRun -ge 1 -and
                            ($counts.done + $counts.cancelled + $counts.notRun) -eq $counts.total)
                    }
                    if ($record.summaryVerified) { break }
                }
                Start-Sleep -Milliseconds 10
            }
            if (-not $record.summaryVerified) { throw "The idle result summary did not match $Name within the case timeout. Last text: $($record.statusSummary)" }
            if ($Name -eq 'selection-during-append') {
                while ($timer.Elapsed.TotalSeconds -lt $TimeoutSeconds) {
                    if ($process.HasExited) { throw 'Application exited before the selected-append result could be verified.' }
                    $text = [LHashUiSmoke.Native]::GetResultText($window, [uint32]$process.Id)
                    $record.orderedRecordsVerified = Test-OrderedResultText -Text $text -Paths $selectionInputPaths -Sha256 $selectionSha256 -Sizes $selectionSizes
                    if ($record.orderedRecordsVerified) { break }
                    Start-Sleep -Milliseconds 10
                }
                $record.finalSelectionObservation = [LHashUiSmoke.Native]::GetResultSelection($window, [uint32]$process.Id)
                $observed = $record.finalSelectionObservation
                $record.selectionRetained = ($observed.ReadSucceeded -and $observed.Start -eq 5 -and $observed.End -eq 15)
                if (-not $record.orderedRecordsVerified -or -not $record.selectionRetained) {
                    if ($null -ne $text) { $record.resultTextPreview = $text.Substring(0, [Math]::Min(1800, $text.Length)) }
                    throw 'Incremental output changed record order or did not preserve the selection made while hashing.'
                }
                $record.textVerified = $true
                $record.resultCharacters = $text.Length
            }
            if ($Name -eq 'many-results-reset') {
                $expectedUppercase = $false
                foreach ($phase in @('complete', 'uppercase-reset', 'clear')) {
                    if ($phase -ne 'complete') {
                        $command = if ($phase -eq 'clear') { 1017 } else { 1023 }
                        if (-not [LHashUiSmoke.Native]::PostResultCommand($window, [uint32]$process.Id, $command)) {
                            throw "Could not post the owned result-view command: $phase"
                        }
                    }
                    $verified = $false
                    while ($timer.Elapsed.TotalSeconds -lt $TimeoutSeconds) {
                        if ($process.HasExited) { throw "Application exited during text phase $phase." }
                        $text = [LHashUiSmoke.Native]::GetResultText($window, [uint32]$process.Id)
                        if ($phase -eq 'clear') {
                            $summary = ConvertFrom-StatusSummary -Text ([LHashUiSmoke.Native]::GetStatusOverview($window, [uint32]$process.Id))
                            $verified = ($null -ne $text -and $text.Length -eq 0 -and $null -ne $summary -and $summary.total -eq 0)
                        } else {
                            $verified = Test-CompleteResultText -Text $text
                            if ($verified -and $phase -eq 'uppercase-reset') {
                                foreach ($hash in $textSha256) {
                                    $casedHash = if ($expectedUppercase) { $hash.ToUpperInvariant() } else { $hash }
                                    if ([regex]::Matches($text, "\b$casedHash\b").Count -ne 1) { $verified = $false; break }
                                }
                            }
                        }
                        if ($verified) { break }
                        Start-Sleep -Milliseconds 10
                    }
                    if (-not $verified) {
                        if ($null -ne $text) { $record.resultTextPreview = $text.Substring(0, [Math]::Min(1800, $text.Length)) }
                        throw "Result text phase $phase was incomplete, misordered, duplicated or stale within the case timeout."
                    }
                    switch ($phase) {
                        'complete' {
                            $record.textVerified = $true
                            $record.orderedRecordsVerified = $true
                            $record.resultCharacters = $text.Length
                            $expectedUppercase = $text.Contains($textSha256[0])
                        }
                        'uppercase-reset' { $record.rebuildVerified = $true }
                        'clear' { $record.clearVerified = $true }
                    }
                }
            }
            # The window must survive normal completion/Stop. Close it only
            # after checking its user-visible aggregate state, then check exit.
            $record.actionPosted = [LHashUiSmoke.Native]::PostClose($window, [uint32]$process.Id)
        }
        if (-not $record.actionPosted) { throw 'PostMessage failed or the window no longer belongs to the test process.' }
        $remainingMs = [int][Math]::Max(0, ($TimeoutSeconds * 1000) - $timer.ElapsedMilliseconds)
        if ($remainingMs -eq 0 -or -not $process.WaitForExit($remainingMs)) {
            throw "The owned process did not exit within the $TimeoutSeconds second total case timeout."
        }
        $record.exitCode = $process.ExitCode
        if ($process.ExitCode -ne 0) {
            $record.crashEvent = Get-OwnedCrashEvent -ProcessId $process.Id -StartedAt $caseStartedAt -ExpectedApp $executablePath
            throw "The application exited abnormally with code $($process.ExitCode)."
        }
        $record.passed = $true
    }
    catch {
        $record.error = $_.Exception.Message
    }
    finally {
        if ($started) {
            try {
                if (-not $process.HasExited) {
                    # Keep the Process object opened by Start: never look up or
                    # kill by name, and never touch an existing user's process.
                    $record.terminatedByTest = $true
                    $process.Kill()
                    if (-not $process.WaitForExit(2000)) { throw 'Owned process did not terminate after Kill.' }
                }
                if ($process.HasExited) {
                    $record.exitCode = $process.ExitCode
                    if ($process.ExitCode -ne 0 -and -not $record.terminatedByTest -and $null -eq $record.crashEvent) {
                        $record.crashEvent = Get-OwnedCrashEvent -ProcessId $process.Id -StartedAt $caseStartedAt -ExpectedApp $executablePath
                    }
                }
            }
            catch {
                $record.passed = $false
                $record.error = "$($record.error) Cleanup: $($_.Exception.Message)"
            }
        }
        $record.elapsedMs = $timer.ElapsedMilliseconds
        $process.Dispose()
        $results.Add([pscustomobject]$record)
    }
    if (-not $record.passed) { throw "$Name iteration $Iteration failed: $($record.error)" }
    Write-Host "$Name iteration $Iteration passed in $($record.elapsedMs) ms."
}

try {
    if (-not $IsWindows) { throw 'This smoke test requires a real Windows GUI environment; no non-Windows skip is considered a pass.' }
    $executablePath = (Resolve-Path -LiteralPath $Executable).Path
    if (-not [System.IO.File]::Exists($executablePath)) { throw 'Executable must be an existing file.' }
    if (-not ('LHashUiSmoke.Native' -as [type])) { Add-Type -TypeDefinition $nativeSource -Language CSharp }
    if (-not [LHashUiSmoke.Native]::CanAccessInteractiveDesktop()) {
        throw 'No accessible interactive input desktop. Run on an unlocked Windows desktop runner; GUI coverage was not performed.'
    }
    $temporaryDirectory = Join-Path ([System.IO.Path]::GetTempPath()) ('lhash-ui-smoke-' + [guid]::NewGuid().ToString('N'))
    [System.IO.Directory]::CreateDirectory($temporaryDirectory) | Out-Null
    $inputPath = Join-Path $temporaryDirectory 'large-test-file.bin'
    $stream = [System.IO.FileStream]::new($inputPath, [System.IO.FileMode]::CreateNew, [System.IO.FileAccess]::ReadWrite, [System.IO.FileShare]::Read)
    try {
        $stream.SetLength([int64]$FileSizeMiB * 1024 * 1024)
        $stream.WriteByte(0xA5)
        $stream.Flush()
    }
    finally { $stream.Dispose() }
    $smallInputPath = Join-Path $temporaryDirectory 'duplicate-small-file.bin'
    [System.IO.File]::WriteAllBytes($smallInputPath, [byte[]](0x61, 0x62, 0x63))
    $textInputPaths = @(foreach ($index in 0..127) {
        $path = Join-Path $temporaryDirectory ('text-result-{0:D4}.bin' -f $index)
        $payload = [System.Text.Encoding]::UTF8.GetBytes(('LHash GUI ordered record {0:D4}' -f $index))
        [System.IO.File]::WriteAllBytes($path, $payload)
        $textSizes += [long]$payload.Length
        $textSha256 += [Convert]::ToHexString([System.Security.Cryptography.SHA256]::HashData($payload)).ToLowerInvariant()
        $textSha512 += [Convert]::ToHexString([System.Security.Cryptography.SHA512]::HashData($payload)).ToLowerInvariant()
        $path
    })
    $selectionSmallPath = Join-Path $temporaryDirectory 'selection-文件-small.bin'
    [System.IO.File]::WriteAllBytes($selectionSmallPath, [byte[]](0x61, 0x62, 0x63))
    $referenceStream = [System.IO.File]::OpenRead($inputPath)
    $referenceHasher = [System.Security.Cryptography.SHA256]::Create()
    try { $largeSha256 = [Convert]::ToHexString($referenceHasher.ComputeHash($referenceStream)).ToLowerInvariant() }
    finally { $referenceHasher.Dispose(); $referenceStream.Dispose() }
    $selectionInputPaths = @($inputPath, $selectionSmallPath)
    $selectionSha256 = @($largeSha256, $abcSha256)
    $selectionSizes = @(([long]$FileSizeMiB * 1024 * 1024), [long]3)

    foreach ($iteration in 1..$Iterations) {
        foreach ($name in $caseNames) {
            try {
                $caseInput = switch ($name) { 'idle-close' { '' } 'duplicate-completion' { $smallInputPath } default { $inputPath } }
                Invoke-OwnedProcessCase -Name $name -InputPath $caseInput -Iteration $iteration
            }
            catch {
                if (-not $suiteFailure) { $suiteFailure = $_.Exception.Message }
                Write-Host "$name iteration $iteration failed; continuing independent owned-process cases for diagnosis."
            }
        }
    }
}
catch {
    $suiteFailure = $_.Exception.Message
}
finally {
    if ($temporaryDirectory -and [System.IO.Directory]::Exists($temporaryDirectory)) {
        try { Remove-Item -LiteralPath $temporaryDirectory -Recurse -Force }
        catch { $suiteFailure = "$suiteFailure Temporary-file cleanup failed: $($_.Exception.Message)" }
    }
    $summary = [ordered]@{
        executable = $executablePath
        processArchitecture = [System.Runtime.InteropServices.RuntimeInformation]::ProcessArchitecture.ToString()
        platform = [System.Runtime.InteropServices.RuntimeInformation]::OSDescription
        timeoutSecondsPerProcess = $TimeoutSeconds
        fileSizeMiB = $FileSizeMiB
        expectedCases = $Iterations * $caseNames.Count
        completedCases = $results.Count
        passed = (-not $suiteFailure -and $results.Count -eq $Iterations * $caseNames.Count)
        failure = $suiteFailure
        cases = $results.ToArray()
    }
    $summary | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $reportRoot 'ui-smoke-results.json') -Encoding utf8
}

if ($suiteFailure) {
    if ($env:GITHUB_ACTIONS -eq 'true') {
        function Write-GitHubUiFailure {
            param([string]$Message)
            if ($Message.Length -gt 3500) { $Message = $Message.Substring(0, 3500) + ' [truncated]' }
            # Escape workflow-command data in this order; never interpolate
            # an unescaped error or path into a public annotation command.
            $encoded = $Message.Replace('%', '%25').Replace("`r", '%0D').Replace("`n", '%0A')
            Write-Host "::error::$encoded"
        }
        Write-GitHubUiFailure -Message "LHash GUI smoke: $suiteFailure"
        foreach ($failedCase in ($results | Where-Object { -not $_.passed } | Select-Object -First 3)) {
            $details = [ordered]@{
                case = $failedCase.case
                iteration = $failedCase.iteration
                processId = $failedCase.processId
                busyObserved = $failedCase.busyObserved
                progressObserved = $failedCase.progressObserved
                progressValue = $failedCase.progressValue
                idleObserved = $failedCase.idleObserved
                statusSummary = $failedCase.statusSummary
                summaryCounts = $failedCase.summaryCounts
                summaryVerified = $failedCase.summaryVerified
                textVerified = $failedCase.textVerified
                rebuildVerified = $failedCase.rebuildVerified
                clearVerified = $failedCase.clearVerified
                resultCharacters = $failedCase.resultCharacters
                selectionPosted = $failedCase.selectionPosted
                selectionRetained = $failedCase.selectionRetained
                selectionObservation = $failedCase.selectionObservation
                finalSelectionObservation = $failedCase.finalSelectionObservation
                orderedRecordsVerified = $failedCase.orderedRecordsVerified
                resultTextPreview = $failedCase.resultTextPreview
                stopPosted = $failedCase.stopPosted
                actionPosted = $failedCase.actionPosted
                exitCode = $failedCase.exitCode
                elapsedMs = $failedCase.elapsedMs
                terminatedByTest = $failedCase.terminatedByTest
                crashEvent = $failedCase.crashEvent
                error = $failedCase.error
            } | ConvertTo-Json -Compress
            Write-GitHubUiFailure -Message $details
        }
    }
    throw $suiteFailure
}
Write-Host "All $($results.Count) real application GUI smoke cases passed."
