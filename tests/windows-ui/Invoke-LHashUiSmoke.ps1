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
        [DllImport("user32.dll", SetLastError = true)] private static extern bool PostMessage(IntPtr window, uint message, IntPtr wparam, IntPtr lparam);
        [DllImport("user32.dll", SetLastError = true)] private static extern IntPtr SendMessageTimeout(IntPtr window, uint message, IntPtr wparam, IntPtr lparam, uint flags, uint timeout, out IntPtr result);
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
$caseNames = @('idle-close', 'active-close', 'escape-key', 'cancel-command', 'rapid-close')
$reportRoot = [System.IO.Path]::GetFullPath($ReportDirectory)
[System.IO.Directory]::CreateDirectory($reportRoot) | Out-Null

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
        actionPosted = $false; exitCode = $null; elapsedMs = 0; passed = $false
        terminatedByTest = $false; crashEvent = $null; error = $null
    }
    try {
        $process.StartInfo = [System.Diagnostics.ProcessStartInfo]::new()
        $process.StartInfo.FileName = $executablePath
        $process.StartInfo.WorkingDirectory = [System.IO.Path]::GetDirectoryName($executablePath)
        $process.StartInfo.UseShellExecute = $false
        if ($InputPath) { $process.StartInfo.ArgumentList.Add($InputPath) }
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

        if ($Name -notin @('rapid-close', 'idle-close')) {
            while ($timer.Elapsed.TotalSeconds -lt $TimeoutSeconds) {
                if ($process.HasExited) { throw 'The application exited before hashing became active.' }
                if ([LHashUiSmoke.Native]::IsHashSessionBusy($window, [uint32]$process.Id)) {
                    $record.busyObserved = $true
                    $progress = [LHashUiSmoke.Native]::GetHashProgress($window, [uint32]$process.Id)
                    if ($progress -gt 0 -and [LHashUiSmoke.Native]::IsHashSessionBusy($window, [uint32]$process.Id)) {
                        $record.progressObserved = $true
                        $record.progressValue = $progress
                        break
                    }
                }
                Start-Sleep -Milliseconds 10
            }
            if (-not $record.progressObserved) { throw 'Did not observe positive hashing progress while still busy; crypto-use close coverage was not achieved.' }
        }

        $record.actionPosted = switch ($Name) {
            'idle-close' { [LHashUiSmoke.Native]::PostClose($window, [uint32]$process.Id) }
            'active-close' { [LHashUiSmoke.Native]::PostClose($window, [uint32]$process.Id) }
            'escape-key' { [LHashUiSmoke.Native]::PostEscapeKey($window, [uint32]$process.Id) }
            'cancel-command' { [LHashUiSmoke.Native]::PostCancelCommand($window, [uint32]$process.Id) }
            'rapid-close' { [LHashUiSmoke.Native]::PostClose($window, [uint32]$process.Id) }
            default { throw "Unknown case: $Name" }
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

    foreach ($iteration in 1..$Iterations) {
        foreach ($name in $caseNames) {
            try {
                $caseInput = if ($name -eq 'idle-close') { '' } else { $inputPath }
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
