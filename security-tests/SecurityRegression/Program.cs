using System.Runtime.InteropServices;
using System.Text;

internal static partial class Program
{
    private static int Main()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        string repoRoot = FindRepoRoot();
        var failures = new List<string>();

        Run("CommandLine parser handles quoted file lists safely", TestCommandLineParsing, failures);
        Run("CommandLine parser covers empty, invalid, boundary, and compatibility cases", TestCommandLineEdgeCases, failures);
        Run("Windows junction attack harness reproduces ancestor reparse-point traversal", TestJunctionAncestorAttackSurface, failures);
        Run("Windows hash-style open harness reproduces sharing violations for locked files", TestHashStyleOpenSharingViolation, failures);
        Run("LHash branding, package metadata, and logo assets are consistent", () =>
        {
            string workflow = ReadRepoFile(repoRoot, @".github\workflows\windows-build.yml");
            string readme = ReadRepoFile(repoRoot, @"README.md");
            string archiveReadme = ReadRepoFile(repoRoot, @"archive\README.md");
            string mfcBaseStrings = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\UIStringsBase.cpp");
            string mfcZhStrings = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\UIStringsZHCN.cpp");
            string mfcRc2 = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\res\fileshash.rc2");
            string mfcRc = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\fileshash.rc");
            string fileshashProject = ReadRepoFile(repoRoot, @"trunk\fileshash.vcxproj");
            string legacyShellStrings = ReadRepoFile(repoRoot, @"sub-proj\LHashShlExt\LHashShlExtStringsBase.cpp");
            string legacyShellStringsZh = ReadRepoFile(repoRoot, @"sub-proj\LHashShlExt\LHashShlExtStringsZHCN.cpp");

            AssertContains(fileshashProject, "<ProjectName>LHash</ProjectName>", "Legacy project still exposes the old project name.");
            AssertContains(fileshashProject, "$(OutDir)$(ProjectName).exe", "Legacy project no longer emits the unified LHash.exe output.");
            AssertNonEmptyFile(repoRoot, @"archive\legacy-projects\trunk\package_win_mfc64.py");
            AssertNonEmptyFile(repoRoot, @"archive\legacy-platforms\trunk\source\WinUI\Strings\en-US\Resources.resw");
            AssertNonEmptyFile(repoRoot, @"archive\legacy-platforms\trunk\source\WinUWP\Package.appxmanifest");
            AssertNonEmptyFile(repoRoot, @"archive\legacy-platforms\sub-proj\fHashWUIShellExt\ExplorerCommandVerb.cpp");
            AssertNonEmptyFile(repoRoot, @"archive\legacy-platforms\sub-proj\fHashUwpShellExt\ExplorerCommandVerb.cpp");
            if (Directory.Exists(Path.Combine(repoRoot, @"sub-proj\fHashClrBridge")) ||
                Directory.Exists(Path.Combine(repoRoot, @"sub-proj\LHashClrBridge")))
            {
                failures.Add("The retired CLR bridge should live only under archive/legacy-platforms, not in the active tree.");
            }

            if (Directory.Exists(Path.Combine(repoRoot, @"sub-proj\fHashWUINative")) ||
                Directory.Exists(Path.Combine(repoRoot, @"sub-proj\LHashWUINative")))
            {
                failures.Add("The retired WinUI native support should live only under archive/legacy-platforms, not in the active tree.");
            }

            if (Directory.Exists(Path.Combine(repoRoot, @"trunk\source\WinUI")))
            {
                failures.Add("trunk/source/WinUI should not remain in the active source tree.");
            }

            AssertContains(archiveReadme, "retired WinUI/UWP/CLR preview surface is kept here for reference only", "Security regression no longer records the WinUI/CLR preview surface as reference-only material.");
            AssertContains(archiveReadme, "archived packaging scripts are not referenced by the active GitHub workflows", "Security regression no longer records archived packaging scripts as non-mainline material.");
            AssertContains(archiveReadme, "legacy in-tree SHA256/SHA512 implementations were superseded by the", "Security regression no longer records archived hash implementations as superseded material.");
            AssertContains(archiveReadme, "maintained OpenSSL SHA-256 / SHA-512 provider path", "Security regression no longer records archived hash implementations as superseded material.");
            AssertContains(workflow, "LHash.exe", "CI packaging no longer looks for the renamed executable.");
            AssertContains(workflow, "LHash-windows-x64", "CI workflow no longer packages the Windows x64 desktop artifact.");
            AssertContains(workflow, "LHash-windows-arm64", "CI workflow no longer packages the Windows ARM64 desktop artifact.");
            AssertDoesNotContain(workflow, "build-winui-bridge-x64:", "The main Windows build workflow should no longer compile the WinUI preview path on routine runs.");
            AssertContains(readme, "Windows UI mainline: `MFC`", "Security regression no longer marks MFC as the sole Windows UI mainline.");
            AssertContains(readme, "Legacy WinUI / CLR bridge: archived under `archive/legacy-platforms/`", "Security regression no longer records the WinUI / CLR bridge as archived under archive/legacy-platforms.");
            AssertContains(readme, "WinUI / CLR bridge trees: reference-only snapshots and not part of the active build", "Security regression no longer records the WinUI / CLR bridge trees as reference-only snapshots.");
            AssertContains(archiveReadme, "retired WinUI/UWP/CLR preview surface is kept here for reference only", "Security regression no longer records the WinUI/CLR preview surface as reference-only material.");
            AssertDoesNotContain(workflow, "fHash-legacy-x64", "CI artifact naming still references the old fHash bundle name.");
            AssertDoesNotContain(workflow, "fHash64.exe", "CI packaging still searches for the legacy fHash64.exe output.");

            AssertContains(mfcRc, "IDD_MAIN_DIALOG DIALOGEX 0, 0, 624, 360", "Legacy MFC main dialog is no longer using the tightened compact height.");
            AssertContains(mfcRc, "FONT 9, \"Segoe UI\"", "Legacy MFC dialog no longer uses the refreshed Win11-style typography.");
            AssertContains(mfcRc, "DEFPUSHBUTTON   \"BUTTON_OPEN\",IDC_OPEN,8,8,60,18", "Legacy MFC command bar no longer starts with the tightened open button.");
            AssertContains(mfcRc, "PUSHBUTTON      \"BUTTON_OPEN_FOLDER\",IDC_OPEN_FOLDER,72,8,78,18", "Legacy MFC command bar is missing the tightened open-folder action.");
            AssertContains(mfcRc, "PUSHBUTTON      \"BUTTON_SETTINGS\",IDC_SETTINGS,340,8,54,18", "Legacy MFC command bar is missing the tightened settings entry.");
            AssertContains(mfcRc, "EDITTEXT        IDE_TXTMAIN,8,32,608,224", "Legacy MFC result text area no longer matches the tightened command-bar layout.");
            AssertContains(mfcRc, "CONTROL         \"\",IDC_TASK_LIST,\"SysListView32\"", "Legacy MFC task list area is missing from the redesigned layout.");
            AssertContains(mfcRc, "LTEXT           \"STATUS_OVERVIEW\",IDC_STATIC_STATUS_OVERVIEW", "Legacy MFC status overview line is missing from the redesigned layout.");

            AssertContains(mfcRc, "CAPTION \"LHash\"", "Legacy MFC dialog caption still shows the old app name.");
            AssertContains(mfcBaseStrings, "About LHash", "Legacy MFC About dialog title still shows the old app name.");
            AssertContains(mfcBaseStrings, "LHash: Files Hash Calculator", "Legacy MFC English About text still shows the old product name.");
            AssertContains(mfcBaseStrings, "Copyright (C) 2026- LHY.", "Legacy MFC English About text still shows the old copyright.");
            AssertContains(mfcBaseStrings, "https://github.com/lhy8888/Lhash", "Legacy MFC English About link still points to the old GitHub repo.");
            AssertContains(mfcZhStrings, "LHash", "Legacy MFC Chinese About title still shows the old app name.");
            AssertContains(mfcZhStrings, "LHash:", "Legacy MFC Chinese About text still shows the old product name.");
            AssertContains(mfcRc2, "VALUE \"FileDescription\", \"LHash: Files Hash Calculator\"", "Legacy MFC version resources still expose the old product description.");
            AssertContains(mfcRc2, "VALUE \"InternalName\", \"LHash.exe\"", "Legacy MFC version resources still expose the old executable name.");
            AssertContains(mfcRc2, "VALUE \"OriginalFilename\", \"LHash.exe\"", "Legacy MFC version resources still expose the old original filename.");
            AssertContains(mfcRc2, "VALUE \"LegalCopyright\", \"(C) 2026- LHY.\"", "Legacy MFC version resources still expose the old copyright.");

            AssertContains(legacyShellStrings, "Hash with LHash", "Legacy shell extension menu text still shows the old app name.");
            AssertContains(legacyShellStringsZh, "LHash", "Legacy shell extension Chinese menu text still shows the old app name.");

            AssertPngAsset(repoRoot, @"archive\legacy-platforms\trunk\source\WinUI\Assets\AboutLogo.large.png", 200, 200, 512);
            AssertPngAsset(repoRoot, @"archive\legacy-platforms\trunk\source\WinUWP\Assets\AboutLogo.large.png", 200, 200, 512);
            AssertNonEmptyFile(repoRoot, @"trunk\source\WinMFC\res\icon1.ico");
            AssertNonEmptyFile(repoRoot, @"archive\legacy-platforms\trunk\source\WinUI\Assets\fHashWUI.ico");
        }, failures);
        Run("WinMFC copy-data validation guard exists", () =>
        {
            string dialogContent = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashDlg.cpp");
            string messageController = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashMessageController.cpp");
            string inputController = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashInputController.cpp");
            string content = string.Join(Environment.NewLine, dialogContent, messageController, inputController);

            AssertContains(content, "IsValidCopyDataString", "Missing WM_COPYDATA input validation helper.");
            AssertContains(content, "CommandLineToArgvW", "Missing hardened Windows command-line parsing.");
            AssertContains(content, "CopyDraggedPath", "Missing long-path-safe drag/drop path extraction.");
            AssertContains(content, "pCopyDataStruct->dwData == 0 &&", "WM_COPYDATA handler no longer gates parsing on the expected payload type.");
            AssertContains(content, "cbData < sizeof(TCHAR)", "WM_COPYDATA validation no longer rejects undersized payloads.");
            AssertContains(content, "% sizeof(TCHAR)", "WM_COPYDATA validation no longer checks character alignment.");
            AssertContains(content, "pCopyDataStruct == NULL || pCopyDataStruct->lpData == NULL", "WM_COPYDATA validation no longer rejects null buffers.");
            AssertContains(content, "szData[charCount - 1] != _T('\\0')", "WM_COPYDATA validation no longer enforces a strict trailing null terminator.");
            AssertContains(content, "charCount > GetCopyDataCommandCharLimit()", "WM_COPYDATA validation no longer bounds total payload length.");
            AssertContains(content, "parameters.size() > kMaxHashFilesPerSession", "WM_COPYDATA ingestion no longer caps parsed path counts.");
            AssertContains(content, "FileLoadResult::RejectedOverLimit", "WM_COPYDATA and drag-drop ingestion no longer surface explicit over-limit outcomes.");
            AssertContains(content, "DispatchFileLoadOutcome(", "WinMFC file-loading outcomes are no longer dispatched through the shared message controller.");
            AssertContains(content, "static_cast<size_t>(droppedFileCount) > kMaxHashFilesPerSession", "Drag-and-drop ingestion no longer rejects oversize batches before loading.");
            AssertContains(content, "scanOutcome.truncated = truncated;", "Folder recursion no longer preserves explicit truncation state.");
            AssertContains(content, "IsTrustedCopyDataSender(pSenderWnd)", "WM_COPYDATA handler no longer checks the sender process.");
            AssertContains(content, "QueryFullProcessImageName(senderProcess.get(), 0, processPath.data(), &cchExecutable)", "WM_COPYDATA sender validation no longer resolves the sender image path.");
            AssertContains(content, "GetTrustedExplorerImagePath()", "WM_COPYDATA sender validation no longer resolves the trusted explorer path.");
            AssertContains(content, "GetCurrentExecutableImagePath()", "WM_COPYDATA sender validation no longer resolves the current executable path.");
            AssertContains(content, "if (sawTerminator)", "WM_COPYDATA validation no longer rejects non-empty data after the first terminator.");
            AssertContains(content, "return sawTerminator;", "WM_COPYDATA validation no longer accepts a single strict terminator.");
            AssertContains(content, "std::deque<sunjwbase::tstring> pendingFolders;", "Folder enumeration no longer uses an explicit queue/stack traversal.");
            AssertContains(content, "!IsThreadDataWorking(*m_threadData)", "WM_COPYDATA handler no longer rejects requests while hashing is in progress.");
        }, failures);
        Run("WinMFC drag and drop still works across the resized result area", () =>
        {
            string dialogContent = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashDlg.cpp");
            string initializationController = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashInitializationController.cpp");
            string sessionController = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashSessionController.cpp");
            string hyperEditHashHeader = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\HyperEditHash.h");
            string hyperEditHashSource = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\HyperEditHash.cpp");
            string dialogResource = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\fileshash.rc");
            string dialogAndSession = dialogContent + Environment.NewLine + sessionController;
            string dialogAndInitialization = dialogContent + Environment.NewLine + initializationController;

            AssertContains(dialogAndSession, "ModifyStyleEx(0, WS_EX_ACCEPTFILES, 0);", "Shared drop-target helper no longer advertises file-drop support on the receiving windows.");
            AssertContains(dialogAndSession, "ModifyStyleEx(WS_EX_ACCEPTFILES, 0, 0);", "Shared drop-target helper no longer removes file-drop style while hashing is in progress.");
            AssertContains(dialogAndSession, "pWnd->DragAcceptFiles(bAccept);", "Shared drop-target helper no longer toggles file-drop acceptance.");
            AssertContains(sessionController, "PrepareDropTarget(m_parentWnd, TRUE);", "Main dialog no longer restores drag-and-drop handling through the session controller helper.");
            AssertContains(sessionController, "PrepareDropTarget(m_mainEditDropTarget, TRUE);", "Main result edit control no longer restores drag-and-drop handling through the session controller helper.");
            AssertContains(dialogContent, "m_btnFind.ShowWindow(SW_SHOW);", "Legacy MFC verify command is not restored in the visible command bar.");
            AssertContains(dialogContent, "m_btnSettings.SetWindowText(GetStringByKey(MAINDLG_SETTINGS));", "Legacy MFC dialog does not yet initialize the visible settings command.");
            AssertContains(dialogAndSession, "ChangeWindowMessageFilterEx", "Elevated drag-and-drop compatibility handling is missing.");
            AssertContains(dialogAndSession, "ChangeWindowMessageFilter", "Legacy message-filter compatibility fallback is missing.");
            AssertContains(dialogAndSession, "AllowMessageForWindow(pWnd->GetSafeHwnd(), WM_DROPFILES);", "WM_DROPFILES is no longer allowed through the window message filter.");
            AssertContains(dialogAndSession, "AllowMessageForWindow(pWnd->GetSafeHwnd(), WM_COPYDATA);", "WM_COPYDATA is no longer allowed through the window message filter.");
            AssertContains(dialogAndSession, "AllowMessageForWindow(pWnd->GetSafeHwnd(), 0x0049);", "WM_COPYGLOBALDATA is no longer allowed through the window message filter.");
            AssertDoesNotContain(dialogAndSession, "PCHANGEFILTERSTRUCT", "Drag-and-drop compatibility still depends on SDK-specific ChangeWindowMessageFilterEx declarations.");
            AssertContains(dialogResource, "PUSHBUTTON      \"BUTTON_FIND\",IDC_FIND,154,8,54,18", "Legacy MFC verify button is not positioned in the tightened visible command bar.");
            AssertContains(dialogResource, "CONTROL         \"\",IDC_TASK_LIST,\"SysListView32\",LVS_REPORT | LVS_OWNERDATA | LVS_SINGLESEL | WS_TABSTOP | WS_BORDER,8,262,608,72", "Legacy MFC task list is no longer using the compact virtual-list viewport.");
            AssertContains(ReadRepoFile(repoRoot, @"trunk\source\WinMFC\UIStringsZHCN.cpp"), "m_stringsMap[_T(\"MAINDLG_SETTINGS_ALGORITHMS\")] = _T(\"算法选择\");", "Legacy settings menu no longer labels algorithm controls as 算法选择 in Simplified Chinese.");
            AssertContains(dialogContent, "ShowAlgorithmSelectionDialog();", "Legacy settings flow no longer routes algorithm selection through a dedicated toggle dialog.");
            AssertContains(dialogContent, "CAlgorithmSelectionDialog", "Legacy settings flow no longer defines a dedicated toggle dialog for algorithm selection.");
            AssertContains(dialogResource, "IDD_ALGORITHM_DIALOG DIALOGEX", "Legacy MFC resources no longer include the dedicated algorithm selection dialog.");
            AssertContains(dialogResource, "LISTBOX         IDC_LIST_ALGORITHMS", "Legacy MFC algorithm selection dialog no longer exposes a checklist listbox.");
            AssertContains(hyperEditHashHeader, "afx_msg void OnDropFiles(HDROP hDropInfo);", "HyperEditHash is missing the drop forwarding declaration.");
            AssertContains(hyperEditHashSource, "ON_WM_DROPFILES()", "HyperEditHash no longer subscribes to WM_DROPFILES.");
            AssertContains(hyperEditHashSource, "parentWnd->SendMessage(WM_DROPFILES, reinterpret_cast<WPARAM>(hDropInfo), 0);", "Dropped files over the enlarged result area are no longer forwarded to the main dialog.");
        }, failures);
        Run("Win32 read failures propagate as errors", () =>
        {
            string winApi = ReadRepoFile(repoRoot, @"trunk\source\OsUtils\OsFileWinApi.cpp");
            string winUwp = ReadRepoFile(repoRoot, @"archive\legacy-platforms\trunk\source\OsUtils\OsFileWinUwp.cpp");
            string hashEngine = ReadHashEngineImplementation(repoRoot);

            if (File.Exists(Path.Combine(repoRoot, @"trunk\source\OsUtils\OsFileWinAfx.cpp")))
            {
                failures.Add("trunk/source/OsUtils/OsFileWinAfx.cpp should not remain in the active source tree.");
            }

            if (File.Exists(Path.Combine(repoRoot, @"trunk\source\OsUtils\OsFileWinUwp.cpp")))
            {
                failures.Add("trunk/source/OsUtils/OsFileWinUwp.cpp should not remain in the active source tree.");
            }

            AssertContains(winApi, "return -1;", "OsFileWinApi.cpp no longer returns -1 on ReadFile/WriteFile failure.");
            AssertContains(winUwp, "return -1;", "OsFileWinUwp.cpp no longer returns -1 on ReadFile/WriteFile failure.");
            AssertContains(hashEngine, "fileAttemptState->readFailed = false;", "HashEngine.cpp is missing explicit read failure tracking.");
            AssertContains(hashEngine, "RESULT_ERROR", "HashEngine.cpp is missing the read-failure error path.");
            AssertContains(hashEngine, "Failed to read file while hashing.", "HashEngine.cpp is missing the user-visible read failure message.");
        }, failures);
        Run("Shell extension hardening is present", () =>
        {
            string legacyShell = ReadRepoFile(repoRoot, @"sub-proj\LHashShlExt\LHashShellExt.cpp");
            string wuiShell = ReadRepoFile(repoRoot, @"archive\legacy-platforms\sub-proj\fHashWUIShellExt\ExplorerCommandVerb.cpp");
            string uwpShell = ReadRepoFile(repoRoot, @"archive\legacy-platforms\sub-proj\fHashUwpShellExt\ExplorerCommandVerb.cpp");
            string shellCore = ReadRepoFile(repoRoot, @"trunk\source\WinCommon\ShellExplorerCommandCore.h");
            string windowsUtils = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\WindowsUtils.cpp");
            string mfcDialog = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashDlg.cpp");
            string mfcInputController = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashInputController.cpp");
            string mfcDialogAndInput = mfcDialog + Environment.NewLine + mfcInputController;

            AssertContains(legacyShell, "PROCESS_QUERY_LIMITED_INFORMATION", "Legacy shell extension still asks for excessive process rights.");
            AssertContains(legacyShell, "#include \"WinCommon/WinHandleGuard.h\"", "Legacy shell extension does not include the shared HANDLE RAII wrappers.");
            AssertContains(legacyShell, "WinHandleGuard::UniqueWinHandle threadHandle(pInfo.hThread);", "Legacy shell extension does not wrap thread handles after CreateProcess.");
            AssertContains(legacyShell, "WinHandleGuard::UniqueWinHandle processHandle(pInfo.hProcess);", "Legacy shell extension does not wrap process handles after CreateProcess.");
            AssertDoesNotContain(legacyShell, "CloseHandle(pInfo.hThread);", "Legacy shell extension still closes the CreateProcess thread handle manually.");
            AssertDoesNotContain(legacyShell, "CloseHandle(pInfo.hProcess);", "Legacy shell extension still closes the CreateProcess process handle manually.");
            AssertContains(legacyShell, "CopyDraggedPath", "Legacy shell extension still relies on fixed-size drag/drop buffers.");
            AssertContains(legacyShell, "DragQueryFile(hDrop, index, NULL, 0)", "Legacy shell extension no longer queries drag/drop path lengths before copying.");
            AssertContains(legacyShell, "std::vector<TCHAR> cmdBuffer(cmdLen, static_cast<TCHAR>(0));", "Legacy shell extension still uses a manual CreateProcess buffer.");
            AssertContains(legacyShell, "CreateProcess(tstrLHashPath.c_str(), cmdBuffer.data(),", "Legacy shell extension no longer launches with the buffered command line.");
            AssertDoesNotContain(legacyShell, "new TCHAR[cmdLen]", "Legacy shell extension still allocates the CreateProcess buffer manually.");
            AssertDoesNotContain(legacyShell, "memset(pszCmd, 0, cmdLen);", "Legacy shell extension still zeroes the CreateProcess buffer with the wrong byte count.");
            AssertDoesNotContain(legacyShell, "delete [] pszCmd;", "Legacy shell extension still manually frees the CreateProcess buffer.");
            AssertContains(shellCore, "BOOL bCreated = CreateProcess", "Shared shell-command core launch hardening is missing.");
            AssertContains(shellCore, "#include \"WinCommon/WinHandleGuard.h\"", "Shared shell-command core does not include the shared HANDLE RAII wrappers.");
            AssertContains(shellCore, "WinHandleGuard::UniqueWinHandle threadHandle(pInfo.hThread);", "Shared shell-command core does not wrap thread handles after CreateProcess.");
            AssertContains(shellCore, "WinHandleGuard::UniqueWinHandle processHandle(pInfo.hProcess);", "Shared shell-command core does not wrap process handles after CreateProcess.");
            AssertContains(shellCore, "0, 0, FALSE,", "Shared shell-command core still allows inheritable handles when launching the target process.");
            AssertDoesNotContain(shellCore, "0, 0, TRUE,", "Shared shell-command core regressed to inheriting parent handles.");
            AssertDoesNotContain(shellCore, "CloseHandle(pInfo.hThread);", "Shared shell-command core still closes thread handles manually.");
            AssertDoesNotContain(shellCore, "CloseHandle(pInfo.hProcess);", "Shared shell-command core still closes process handles manually.");
            AssertContains(shellCore, "if (pszExecName == NULL || pszPath == NULL || cchPath == 0)", "Shared shell-command core does not validate executable-path inputs.");
            AssertContains(shellCore, "if (psia == NULL || ptstrExecCmd == NULL || tstrExecPath.empty())", "Shared shell-command core does not validate command-line inputs.");
            AssertContains(wuiShell, "#include \"WinCommon/ShellExplorerCommandCore.h\"", "WinUI shell extension does not consume the shared hardened shell-command core.");
            AssertContains(wuiShell, "LaunchShellCommandLine(tstrExecPath, tstrExecCmd);", "WinUI shell extension no longer routes detached process launch through the hardened shared helper.");
            AssertContains(uwpShell, "#include \"WinCommon/ShellExplorerCommandCore.h\"", "UWP shell extension does not consume the shared hardened shell-command core.");
            AssertContains(uwpShell, "LaunchShellCommandLine(tstrExecPath, tstrExecCmd);", "UWP shell extension no longer routes detached process launch through the hardened shared helper.");
            AssertContains(legacyShell, "0, 0, FALSE,", "Legacy shell extension still allows inheritable handles when launching the target process.");
            AssertDoesNotContain(legacyShell, "0, 0, TRUE,", "Legacy shell extension regressed to inheriting parent handles.");
            AssertContains(mfcDialogAndInput, "DragQueryFile(hDropInfo, index, NULL, 0)", "MFC drag/drop path extraction no longer queries required buffer sizes.");
            AssertContains(windowsUtils, "WinHandleGuard::UniqueFindHandle hFind", "WindowsUtils shell-extension registration helpers do not yet wrap FindFirstFile handles in RAII.");
            AssertContains(windowsUtils, "WinHandleGuard::UniqueModuleHandle hModule", "WindowsUtils shell-extension registration helpers do not yet wrap module handles in RAII.");
            AssertContains(windowsUtils, "return LoadLibraryEx(pszDllPath, NULL, LOAD_WITH_ALTERED_SEARCH_PATH);", "WindowsUtils still falls back to bare LoadLibrary for shell-extension DLL loading.");
            AssertDoesNotContain(windowsUtils, "return LoadLibrary(pszDllPath);", "WindowsUtils regressed to a bare LoadLibrary fallback for shell-extension DLL loading.");
            AssertContains(windowsUtils, "bool IsAcceptableContextMenuDeleteResult(LONG deleteResult)", "WindowsUtils context-menu removal no longer uses explicit delete-result normalization.");
            AssertContains(windowsUtils, "deleteResult == ERROR_SUCCESS || deleteResult == ERROR_FILE_NOT_FOUND", "WindowsUtils context-menu removal no longer treats missing keys as acceptable cleanup.");
            AssertContains(windowsUtils, "deleteSucceeded = deleteSucceeded && IsAcceptableContextMenuDeleteResult(keyShell.RecurseDeleteKey(CONTEXT_MENU_ITEM_EN_US));", "WindowsUtils context-menu removal no longer aggregates key deletion success explicitly.");
            AssertDoesNotContain(windowsUtils, "lResult &= keyShell.RecurseDeleteKey", "WindowsUtils context-menu removal regressed to bitwise folding of Win32 error codes.");
            AssertDoesNotContain(windowsUtils, "lResult &= keyShellEx.RecurseDeleteKey", "WindowsUtils shell-extension cleanup regressed to bitwise folding of Win32 error codes.");
            AssertContains(windowsUtils, "GetNativeSystemInfo(&systemInfo);", "WindowsUtils no longer uses the native system architecture API for 64-bit detection.");
            AssertContains(windowsUtils, "case PROCESSOR_ARCHITECTURE_ARM64:", "WindowsUtils no longer treats ARM64 as a 64-bit Windows architecture.");
            AssertDoesNotContain(windowsUtils, "QueryStringValue(lpszArchKeyName", "WindowsUtils regressed to registry-based processor architecture probing.");
            AssertDoesNotContain(windowsUtils, "FreeLibrary(hModule);", "WindowsUtils shell-extension registration helpers still release modules manually.");
            AssertContains(windowsUtils, "SetClipboardData", "Clipboard helper no longer transfers ownership safely.");
        }, failures);
        Run("Filesystem and runtime hardening is present", () =>
        {
            string osFileHeader = ReadRepoFile(repoRoot, @"trunk\source\OsUtils\OsFile.h");
            string osFilePosixDarwin = ReadRepoFile(repoRoot, @"trunk\source\OsUtils\OsFilePosixDarwin.cpp");
            string osFileWinApi = ReadRepoFile(repoRoot, @"trunk\source\OsUtils\OsFileWinApi.cpp");
            string osFileWinUwp = ReadRepoFile(repoRoot, @"archive\legacy-platforms\trunk\source\OsUtils\OsFileWinUwp.cpp");
            string hashEngineResult = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngineResult.cpp");
            string global = ReadRepoFile(repoRoot, @"trunk\source\Common\Global.h");
            string checkedArithmetic = ReadRepoFile(repoRoot, @"trunk\source\Common\CheckedArithmetic.h");
            string threadAccess = ReadRepoFile(repoRoot, @"trunk\source\LegacyCompat\ThreadDataExecutionAccess.h");
            string executionContext = ReadRepoFile(repoRoot, @"trunk\source\Runtime\HashExecutionContext.h");
            string progressTracker = ReadRepoFile(repoRoot, @"trunk\source\Common\HashProgressTracker.cpp");
            string digestQueue = ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestQueue.cpp");
            string digestStateAccess = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDigestStateAccess.h");
            string digestValueAccess = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDigestValueAccess.h");
            string md5 = ReadRepoFile(repoRoot, @"trunk\source\Algorithms\MD5.cpp");
            string sha1 = ReadRepoFile(repoRoot, @"trunk\source\Algorithms\SHA1.cpp");
            string strhelper = ReadRepoFile(repoRoot, @"trunk\source\Common\strhelper.cpp");
            string uiBridgeHeader = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\UIBridgeMFC.h");
            string uiBridge = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\UIBridgeMFC.cpp");
            string nativeRuntimeSource = ReadRepoFile(repoRoot, @"native-runtime-tests\LHash.NativeRuntimeTests\HashEngineRuntimeTests.cpp");

            AssertContains(osFileHeader, "bool isHashTargetAllowed(void *exception = NULL);", "OsFile no longer exposes the hash-target policy hook.");
            AssertContains(osFilePosixDarwin, "static const int kNoFollowFlag = O_NOFOLLOW;", "POSIX Darwin file handling no longer defines the no-follow contract flag.");
            AssertContains(osFilePosixDarwin, "static bool TryGetPathStatus(const std::string& filePath, bool allowMissingPath, struct stat *fileStatus, bool *pathExists)", "POSIX Darwin file handling no longer centralizes pre-open path validation.");
            AssertContains(osFilePosixDarwin, "static bool TryValidatePathPolicy(const std::string& filePath, bool allowMissingPath, struct stat *pathStatus, bool *pathExists, char *errorBuffer)", "POSIX Darwin file handling no longer centralizes path policy validation.");
            AssertContains(osFilePosixDarwin, "if (lstat(filePath.c_str(), &pathStatus) != 0)", "POSIX Darwin file handling no longer inspects the path object without following symlinks.");
            AssertContains(osFilePosixDarwin, "if (IsSymbolicLink(fileStatus))", "POSIX Darwin file handling no longer rejects symbolic links explicitly.");
            AssertContains(osFilePosixDarwin, "Refusing to hash a symbolic link.", "POSIX Darwin file handling no longer emits a clear symlink rejection error.");
            AssertContains(osFilePosixDarwin, "if (IsOpenModeCreate(posixFlag))", "POSIX Darwin file handling no longer branches on O_CREAT before opening files.");
            AssertContains(osFilePosixDarwin, "openFlags = posixFlag | kNoFollowFlag", "POSIX Darwin file handling no longer forces no-follow on opens.");
            AssertContains(osFilePosixDarwin, "ValidateOpenedHandleAgainstPathPolicy(*fd, strFilePath, pathStatus, pathExists, pFileExc)", "POSIX Darwin file handling no longer validates opened handles against the validated path.");
            AssertContains(osFilePosixDarwin, "errno == ELOOP", "POSIX Darwin file handling no longer maps symlink-open failures to an explicit refusal message.");
            AssertContains(osFilePosixDarwin, "static bool TryGetCurrentFileStatus(int *fd, const std::string& filePath, struct stat *fileStatus)", "POSIX Darwin metadata reads no longer centralize on the current-file status helper.");
            AssertContains(osFilePosixDarwin, "if (fstat(fileHandle, &openedStatus) != 0)", "POSIX Darwin file handling no longer validates the opened file descriptor with fstat.");
            AssertContains(osFilePosixDarwin, "if (!IsRegularFile(openedStatus))", "POSIX Darwin file handling no longer rejects non-regular file descriptors after open.");
            AssertContains(osFilePosixDarwin, "if (TryGetCurrentFileStatus(fd, strFilePath, &st))", "POSIX Darwin metadata reads no longer reuse the current-file status helper.");
            AssertContains(osFilePosixDarwin, "if (fd == NULL || *fd == -1)", "POSIX Darwin file operations no longer self-guard invalid file descriptors.");
            AssertDoesNotContain(osFilePosixDarwin, "stat(strFilePath.c_str()", "POSIX Darwin file handling regressed to path-based stat lookups.");
            AssertDoesNotContain(osFilePosixDarwin, "if ((statRet = stat(strFilePath.c_str(), &st)) == 0", "POSIX Darwin file handling regressed to a stat-before-open TOCTOU gate.");
            AssertDoesNotContain(osFilePosixDarwin, "Open first, we don't check here.", "POSIX Darwin file operations regressed to unchecked library-boundary assumptions.");
            AssertContains(osFileWinApi, "FILE_ATTRIBUTE_REPARSE_POINT", "Win32 file hashing no longer checks for reparse points.");
            AssertContains(osFileWinApi, "Refusing to hash a symbolic link, junction, mount point, or other reparse point.", "Win32 file hashing no longer rejects reparse points with an explicit message.");
            AssertContains(osFileWinApi, "HasReparsePointInPathHierarchy", "Win32 file hashing no longer walks ancestor path segments when checking for reparse points.");
            AssertContains(osFileWinApi, "if (!TryLongPathFix(_filePath, &fixedPath, pFileExc))", "Win32 file hashing no longer canonicalizes long paths before opening files.");
            AssertContains(osFileWinApi, "if (TryRejectReparsePointPath(fixedPath, pFileExc))", "Win32 file hashing no longer rejects validated reparse-point paths before opening files.");
            AssertContains(osFileWinApi, "FILE_FLAG_OPEN_REPARSE_POINT", "Win32 file hashing no longer opens the leaf object with reparse-point awareness.");
            AssertContains(osFileWinApi, "GetFileInformationByHandleEx(", "Win32 file hashing no longer validates opened handle attributes.");
            AssertContains(osFileWinApi, "GetFinalPathNameByHandle(", "Win32 file hashing no longer revalidates the resolved final path after open.");
            AssertContains(osFileWinApi, "Cannot verify the final opened file path. Refusing to hash.", "Win32 file hashing still fails open when final-path verification cannot complete.");
            AssertContains(osFileWinApi, "kWindowsMaxExtendedPath = 32767", "Win32 long-path handling no longer enforces the extended path limit.");
            AssertContains(osFileWinApi, "ERROR_FILENAME_EXCED_RANGE", "Win32 long-path handling no longer reports explicit extended-path overflow errors.");
            AssertDoesNotContain(osFileWinApi, "CreateFileFromAppW", "Win32 file hashing still carries the retired UWP open-file branch.");
            AssertDoesNotContain(osFileWinApi, "LHASH_UWP_LIB", "Win32 file hashing still carries the retired UWP open-file macro.");
            AssertDoesNotContain(osFileWinApi, "LHASH_WUI_LIB", "Win32 file hashing still carries the retired WinUI open-file macro.");
            AssertContains(osFileWinUwp, "FILE_ATTRIBUTE_REPARSE_POINT", "UWP file hashing no longer checks for reparse points.");
            AssertContains(osFileWinUwp, "HasReparsePointInPathHierarchy", "UWP file hashing no longer walks ancestor path segments when checking for reparse points.");
            AssertContains(hashEngineResult, "result.meta.modifiedDate = osFile.getModifiedTimeFormat();", "HashEngine metadata flow no longer relies on the opened file handle.");
            AssertDoesNotContain(hashEngineResult, "GetFileAttributesEx", "HashEngine metadata flow still uses path-based attribute lookup.");

            AssertContains(checkedArithmetic, "TryAddUInt64", "Checked arithmetic helpers no longer expose checked uint64 addition.");
            AssertContains(checkedArithmetic, "TrySubtractUInt64", "Checked arithmetic helpers no longer expose checked uint64 subtraction.");
            AssertContains(checkedArithmetic, "TryMultiplyUInt64", "Checked arithmetic helpers no longer expose checked uint64 multiplication.");
            AssertContains(checkedArithmetic, "SaturatingAddUInt64", "Checked arithmetic helpers no longer expose saturating addition.");
            AssertContains(checkedArithmetic, "ReplaceSizedValueUInt64", "Checked arithmetic helpers no longer expose bounded replace arithmetic.");
            AssertContains(global, "std::atomic<uint64_t> countedSize;", "Hash job state no longer stores counted size as an atomic field.");
            AssertContains(executionContext, "class NullHashProgressSink : public HashProgressSink", "HashExecutionContext no longer exposes a null-object progress sink.");
            AssertContains(executionContext, "HashProgressSink& progressSinkObserver;", "HashExecutionContext no longer models the progress sink as a non-owning observer reference.");
            AssertContains(executionContext, "sink != NULL ? *sink : GetNullHashProgressSink()", "HashExecutionContext no longer provides a null-safe progress sink fallback.");
            AssertDoesNotContain(executionContext, "HashProgressSink *progressSink;", "HashExecutionContext regressed to a raw stored progress sink pointer.");
            AssertContains(executionContext, "SaturatingAddUInt64", "HashExecutionContext no longer uses saturating size accounting.");
            AssertContains(executionContext, "ReplaceSizedValueUInt64", "HashExecutionContext no longer uses checked replace arithmetic.");
            AssertContains(executionContext, "countedSize.load(std::memory_order_relaxed)", "HashExecutionContext no longer reads the counted size atomically.");
            AssertContains(executionContext, "countedSize.store(0, std::memory_order_relaxed);", "HashExecutionContext no longer resets the counted size atomically.");
            AssertContains(executionContext, "countedSize.compare_exchange_weak(", "HashExecutionContext no longer updates counted size with atomic compare-exchange loops.");
            AssertContains(threadAccess, "SaturatingAddUInt64", "Legacy ThreadData size accounting no longer uses saturating arithmetic.");
            AssertContains(threadAccess, "ReplaceSizedValueUInt64", "Legacy ThreadData replacement accounting no longer uses checked arithmetic.");
            AssertContains(threadAccess, "countedSize.load(std::memory_order_relaxed)", "Legacy ThreadData size accounting no longer reads the counted size atomically.");
            AssertContains(threadAccess, "countedSize.store(0, std::memory_order_relaxed);", "Legacy ThreadData size accounting no longer resets the counted size atomically.");
            AssertContains(threadAccess, "countedSize.compare_exchange_weak(", "Legacy ThreadData size accounting no longer uses atomic compare-exchange loops.");
            AssertContains(progressTracker, "CalculateBoundedProgressValue", "Progress tracking no longer uses bounded progress calculations.");
            AssertContains(digestQueue, "SaturatingAddUInt64(fileSize, static_cast<uint64_t>(bufferLength) - 1)", "Digest queue sizing no longer uses saturating chunk arithmetic.");
            AssertContains(digestStateAccess, "TryGetMutableDigestStorageValueById(ResultDigestStorage& digestStorage, const HashAlgorithmId& algorithmId, sunjwbase::tstring **digestValue)", "Digest storage access no longer exposes explicit mutable failure handling.");
            AssertContains(digestValueAccess, "TryGetMutableResultDigestById(ResultData& result, const HashAlgorithmId& algorithmId, sunjwbase::tstring **digestValue)", "Digest value access no longer exposes explicit mutable failure handling.");
            AssertDoesNotContain(digestStateAccess, "GetInvalidDigestStorageScratch()", "Digest storage access still relies on a shared invalid-digest scratch string.");

            AssertContains(md5, "static const unsigned char PADDING[64]", "MD5 padding is no longer held as immutable shared algorithm state.");
            AssertContains(md5, "void MD5Init (MD5_CTX *mdContext)", "Standard MD5 initialization no longer stays separate from legacy seeded behavior.");
            AssertContains(md5, "void MD5InitSeededLegacy (MD5_CTX *mdContext, uint32_t pseudoRandomNumber)", "Legacy seeded MD5 initialization is no longer explicitly named as legacy-only.");
            AssertDoesNotContain(md5, "void MD5Init (MD5_CTX *mdContext, uint32_t pseudoRandomNumber)", "Standard MD5 initialization still exposes the seeded legacy footgun.");
            AssertDoesNotContain(sha1, "static unsigned char workspace[64];", "SHA1 still shares mutable static workspace across concurrent runs.");
            AssertContains(sha1, "unsigned char workspace[64];", "SHA1 no longer uses stack-local transform workspace.");
            AssertDoesNotContain(sha1, "HashFile(", "Legacy SHA1 file-helper entrypoint should be removed once OsFile owns file hashing.");
            AssertDoesNotContain(sha1, "fopen(", "Legacy SHA1 helper should no longer open files directly with fopen.");
            AssertDoesNotContain(sha1, "fread(", "Legacy SHA1 helper should no longer read files directly with fread.");
            AssertDoesNotContain(sha1, "ferror(", "Legacy SHA1 helper should no longer perform direct stdio error handling.");
            AssertDoesNotContain(sha1, "uint32_t ulFileSize", "SHA1 file hashing still relies on a 32-bit file-size accumulator.");
            AssertDoesNotContain(sha1, "ftell(fIn)", "SHA1 file hashing still uses ftell for total-size driven chunk planning.");
            AssertDoesNotContain(sha1, "fseek(fIn, 0, SEEK_END)", "SHA1 file hashing still seeks to the end of the file to precompute chunk counts.");
            AssertContains(strhelper, "std::wstring_convert<std::codecvt_utf8<wchar_t>>", "POSIX string helpers no longer use explicit UTF-8 wide-string conversion.");
            AssertContains(strhelper, "size_t iconvResult = iconv(cd, inleft > 0 ? &in : NULL, &inleft, &out, &outleft);", "POSIX string helpers no longer validate the iconv return value explicitly.");
            AssertContains(strhelper, "if (errno == E2BIG)", "POSIX string helpers no longer resize iconv output buffers safely.");
            AssertDoesNotContain(strhelper, "setlocale(LC_ALL", "POSIX string helpers still mutate the global process locale.");
            AssertDoesNotContain(strhelper, "wcstombs(", "POSIX string helpers still rely on wcstombs-driven locale conversions.");
            AssertDoesNotContain(strhelper, "mbstowcs(", "POSIX string helpers still rely on mbstowcs-driven locale conversions.");
            AssertDoesNotContain(strhelper, "\"UTF-8\", \"ASCII\"", "POSIX UTF-8 helpers still route through an ASCII bridge conversion.");
            AssertDoesNotContain(strhelper, "\"ASCII\", \"UTF-8\"", "POSIX UTF-8 helpers still route through an ASCII bridge conversion.");
            if (File.Exists(Path.Combine(repoRoot, @"trunk\source\OsUtils\OsFileWinAfx.cpp")))
            {
                failures.Add("trunk/source/OsUtils/OsFileWinAfx.cpp should not remain in the active source tree.");
            }

            if (File.Exists(Path.Combine(repoRoot, @"trunk\source\OsUtils\OsFileWinUwp.cpp")))
            {
                failures.Add("trunk/source/OsUtils/OsFileWinUwp.cpp should not remain in the active source tree.");
            }

            AssertContains(uiBridgeHeader, "struct ProgressDispatchState", "MFC UI bridge no longer exposes a throttled progress dispatch state.");
            AssertContains(uiBridge, "kUiProgressDispatchIntervalMs = 80", "MFC UI bridge no longer throttles progress dispatch.");
            AssertContains(uiBridge, "ShouldPostProgressValue(m_totalProgressDispatchState, value)", "MFC UI bridge no longer gates total-progress posts through the throttling helper.");
            AssertContains(nativeRuntimeSource, "HashThreadFunc_ProducesConsistentDigestsAcrossConcurrentRuns", "Native runtime tests no longer cover concurrent digest consistency.");
            AssertContains(nativeRuntimeSource, "HashThreadFunc_AllowsMetadataOnlyRequestsWithoutEnabledAlgorithms", "Native runtime tests no longer cover metadata-only requests without enabled algorithms.");
            AssertContains(nativeRuntimeSource, "std::async(std::launch::async, runSingleRequest)", "Native runtime tests no longer exercise concurrent hashing via async tasks.");
        }, failures);
        Run("BLAKE3 provider and descriptor variants stay covered by hardening gates", () =>
        {
            string registryCore = ReadRepoFile(repoRoot, @"trunk\source\Domain\HashAlgorithmRegistryCore.h");
            string providerHeader = ReadRepoFile(repoRoot, @"trunk\source\Runtime\Hash\BLAKE3HashProvider.h");
            string providerImplementation = ReadRepoFile(repoRoot, @"trunk\source\Runtime\Hash\BLAKE3HashProvider.cpp");
            string runtimeSource = ReadRepoFile(repoRoot, @"native-runtime-tests\LHash.NativeRuntimeTests\HashEngineRuntimeTests.cpp");
            string securityRuntimeSource = ReadRepoFile(repoRoot, @"native-runtime-tests\LHash.NativeRuntimeTests\HashEngineSecurityRuntimeTests.cpp");
            string nativeCoreProject = ReadRepoFile(repoRoot, @"sub-proj\LHashNativeCore\LHashNativeCore.vcxproj");
            string uwpNativeProject = ReadRepoFile(repoRoot, @"archive\legacy-platforms\sub-proj\fHashUwpNative\fHashUwpNative.vcxproj");
            string securityHarness = ReadRepoFile(repoRoot, @"security-tests\SecurityRegression\WindowsSecurityRuntimeHarness.cs");

            AssertContains(registryCore, "{ \"blake3-256\", \"BLAKE3-256\", true, false }", "The algorithm registry no longer carries the BLAKE3-256 descriptor variant.");
            AssertContains(registryCore, "{ \"blake3-512\", \"BLAKE3-512\", true, false }", "The algorithm registry no longer carries the BLAKE3-512 descriptor variant.");
            AssertContains(registryCore, "{ \"blake3-xof\", \"BLAKE3 XOF\", true, false }", "The algorithm registry no longer carries the BLAKE3 XOF descriptor variant.");
            AssertContains(providerHeader, "BLAKE3_256_OUTPUT_BYTES = BLAKE3_OUT_LEN", "The BLAKE3 provider no longer exposes the 256-bit output profile.");
            AssertContains(providerHeader, "BLAKE3_512_OUTPUT_BYTES = 64", "The BLAKE3 provider no longer exposes the 512-bit output profile.");
            AssertContains(providerHeader, "BLAKE3_XOF_OUTPUT_BYTES = 128", "The BLAKE3 provider no longer exposes the XOF output profile.");
            AssertContains(providerImplementation, "blake3_hasher_init", "The BLAKE3 provider no longer initializes through the official C API.");
            AssertContains(providerImplementation, "blake3_hasher_update", "The BLAKE3 provider no longer updates through the official C API.");
            AssertContains(providerImplementation, "blake3_hasher_finalize", "The BLAKE3 provider no longer finalizes through the official C API.");
            AssertDoesNotContain(providerImplementation, "static blake3_hasher", "The BLAKE3 provider reintroduced shared mutable hasher state.");

            AssertContains(runtimeSource, "HashThreadFunc_ComputesOfficialBlake3DigestsForKnownVector", "Native runtime coverage no longer includes the official BLAKE3 vector.");
            AssertContains(runtimeSource, "RunHashRequest_Blake3UppercaseFlagRemainsDeterministicAcrossVariants", "Native runtime coverage no longer includes uppercase BLAKE3 behavior.");
            AssertContains(runtimeSource, "RunHashRequest_Blake3UnknownIdsAreIgnoredAndKnownVariantsStayOrdered", "Native runtime coverage no longer includes BLAKE3 id normalization behavior.");
            AssertContains(runtimeSource, "HashThreadFunc_Blake3VariantsRemainStableAcrossConcurrentRuns", "Native runtime coverage no longer includes concurrent BLAKE3 stability.");
            AssertContains(runtimeSource, "CreateAlgorithmId(\"blake3-1024\")", "Native runtime coverage no longer probes unsupported BLAKE3 ids.");
            AssertContains(securityRuntimeSource, "OsFile_RejectsLeafPathsNestedUnderDirectoryJunctions", "Native runtime coverage no longer exercises nested junction attack paths.");
            AssertContains(securityRuntimeSource, "OsFile_ReportsSharingViolationsForLockedFiles", "Native runtime coverage no longer exercises locked-file sharing violations.");

            AssertContains(nativeCoreProject, @"blake3_sse2.c", "Desktop native core no longer compiles the BLAKE3 SSE2 implementation for x64.");
            AssertContains(nativeCoreProject, @"blake3_sse41.c", "Desktop native core no longer compiles the BLAKE3 SSE4.1 implementation for x64.");
            AssertContains(nativeCoreProject, @"blake3_avx2.c", "Desktop native core no longer compiles the BLAKE3 AVX2 implementation for x64.");
            AssertContains(nativeCoreProject, @"blake3_avx512.c", "Desktop native core no longer compiles the BLAKE3 AVX512 implementation for x64.");
        AssertContains(nativeCoreProject, "LHashBlake3SimdProfile", "Desktop native core no longer exposes a benchmark-selectable BLAKE3 SIMD profile.");
        AssertContains(nativeCoreProject, "Condition=\"'$(LHashBlake3SimdProfile)'=='portable'\">BLAKE3_USE_NEON=0;BLAKE3_NO_SSE2;BLAKE3_NO_SSE41;BLAKE3_NO_AVX2;BLAKE3_NO_AVX512;%(PreprocessorDefinitions)</PreprocessorDefinitions>", "Desktop native core no longer exposes the portable BLAKE3 benchmark control.");
        AssertContains(nativeCoreProject, "ExcludedFromBuild Condition=\"'$(LHashBlake3SimdProfile)'=='portable'\"", "Desktop native core no longer allows benchmark runs to disable SIMD translation units.");
            AssertDoesNotContain(nativeCoreProject, "Condition=\"'$(Platform)'=='Win32'\">/arch:SSE2", "Desktop native core still carries the retired Win32 SSE2 BLAKE3 path.");
            AssertDoesNotContain(nativeCoreProject, "Condition=\"'$(Platform)'=='Win32'\">/arch:AVX", "Desktop native core still carries the retired Win32 SSE4.1-compatible BLAKE3 path.");
            AssertContains(nativeCoreProject, "/arch:AVX2", "Desktop native core no longer enables AVX2 for the dedicated BLAKE3 translation unit.");
            AssertContains(nativeCoreProject, "/arch:AVX512", "Desktop native core no longer enables AVX512 for the dedicated BLAKE3 translation unit.");
            AssertContains(uwpNativeProject, @"blake3_sse2.c", "UWP native core no longer compiles the BLAKE3 SSE2 implementation for x64.");
            AssertContains(uwpNativeProject, @"blake3_sse41.c", "UWP native core no longer compiles the BLAKE3 SSE4.1 implementation for x64.");
            AssertContains(uwpNativeProject, @"blake3_avx2.c", "UWP native core no longer compiles the BLAKE3 AVX2 implementation for x64.");
            AssertContains(uwpNativeProject, @"blake3_avx512.c", "UWP native core no longer compiles the BLAKE3 AVX512 implementation for x64.");
            AssertContains(uwpNativeProject, @"blake3_neon.c", "UWP native core no longer compiles the BLAKE3 NEON implementation for ARM64.");
            AssertContains(uwpNativeProject, "/arch:AVX2", "UWP native core no longer enables AVX2 for the dedicated BLAKE3 translation unit.");
            AssertContains(uwpNativeProject, "/arch:AVX512", "UWP native core no longer enables AVX512 for the dedicated BLAKE3 translation unit.");
            AssertContains(uwpNativeProject, "Condition=\"'$(Platform)'=='ARM64'\">BLAKE3_USE_NEON=1;BLAKE3_NO_SSE2;BLAKE3_NO_SSE41;BLAKE3_NO_AVX2;BLAKE3_NO_AVX512;%(PreprocessorDefinitions)</PreprocessorDefinitions>", "UWP native core no longer enables the ARM64 NEON BLAKE3 path.");
            AssertContains(securityHarness, "TestJunctionAncestorAttackSurface", "Security regression no longer runs the junction ancestor attack harness.");
            AssertContains(securityHarness, "TestHashStyleOpenSharingViolation", "Security regression no longer runs the sharing-violation harness.");
            AssertContains(securityHarness, "CreateDirectoryJunction", "Security regression no longer builds the runtime junction attack harness.");
            AssertContains(securityHarness, "ERROR_SHARING_VIOLATION = 32", "Security regression no longer validates sharing-violation attack semantics.");
        }, failures);
        Run("XXH3 and CRC32C providers stay covered by hardening gates", () =>
        {
            string registryCore = ReadRepoFile(repoRoot, @"trunk\source\Domain\HashAlgorithmRegistryCore.h");
            string xxh3ProviderHeader = ReadRepoFile(repoRoot, @"trunk\source\Runtime\Hash\XXHash3HashProvider.h");
            string xxh3ProviderImplementation = ReadRepoFile(repoRoot, @"trunk\source\Runtime\Hash\XXHash3HashProvider.cpp");
            string crc32cProviderHeader = ReadRepoFile(repoRoot, @"trunk\source\Runtime\Hash\CRC32CHashProvider.h");
            string crc32cProviderImplementation = ReadRepoFile(repoRoot, @"trunk\source\Runtime\Hash\CRC32CHashProvider.cpp");
            string runtimeSource = ReadRepoFile(repoRoot, @"native-runtime-tests\LHash.NativeRuntimeTests\HashEngineRuntimeTests.cpp");
            string nativeCoreProject = ReadRepoFile(repoRoot, @"sub-proj\LHashNativeCore\LHashNativeCore.vcxproj");
            string uwpNativeProject = ReadRepoFile(repoRoot, @"archive\legacy-platforms\sub-proj\fHashUwpNative\fHashUwpNative.vcxproj");
            string xxhashNote = ReadRepoFile(repoRoot, @"third_party\xxhash\0.8.3\README.LHash.md");
            string crc32cNote = ReadRepoFile(repoRoot, @"third_party\crc32c\1.1.2\README.LHash.md");
            string crc32cArm64Check = ReadRepoFile(repoRoot, @"third_party\crc32c\1.1.2\src\crc32c_arm64_check.h");

            AssertContains(registryCore, "{ \"xxh3-64\", \"XXH3-64\", true, false }", "The algorithm registry no longer carries the XXH3-64 descriptor variant.");
            AssertContains(registryCore, "{ \"xxh3-128\", \"XXH3-128\", true, false }", "The algorithm registry no longer carries the XXH3-128 descriptor variant.");
            AssertContains(registryCore, "{ \"crc32c\", \"CRC32C\", true, false }", "The algorithm registry no longer carries the CRC32C descriptor variant.");

            AssertContains(xxh3ProviderHeader, "XXH3_64_OUTPUT_BYTES = sizeof(XXH64_hash_t)", "The XXH3 provider no longer exposes the 64-bit output profile.");
            AssertContains(xxh3ProviderHeader, "XXH3_128_OUTPUT_BYTES = sizeof(XXH128_hash_t)", "The XXH3 provider no longer exposes the 128-bit output profile.");
            AssertContains(xxh3ProviderImplementation, "XXH3_64bits_reset", "The XXH3 provider no longer initializes through the official xxHash C API.");
            AssertContains(xxh3ProviderImplementation, "XXH3_64bits_update", "The XXH3 provider no longer updates through the official xxHash C API.");
            AssertContains(xxh3ProviderImplementation, "XXH64_canonicalFromHash", "The XXH3 provider no longer canonicalizes the 64-bit xxHash output.");
            AssertContains(xxh3ProviderImplementation, "XXH128_canonicalFromHash", "The XXH3 provider no longer canonicalizes the 128-bit xxHash output.");
            AssertDoesNotContain(xxh3ProviderImplementation, "static XXH3_state_t", "The XXH3 provider reintroduced shared mutable hasher state.");

            AssertContains(crc32cProviderHeader, "CRC32C_OUTPUT_BYTES = sizeof(uint32_t)", "The CRC32C provider no longer exposes the 32-bit output profile.");
            AssertContains(crc32cProviderImplementation, "crc32c_extend", "The CRC32C provider no longer updates through the official google/crc32c C API.");
            AssertDoesNotContain(crc32cProviderImplementation, "static uint32_t", "The CRC32C provider reintroduced shared mutable checksum state.");
            AssertContains(crc32cArm64Check, "PF_ARM_V8_CRC32_INSTRUCTIONS_AVAILABLE", "The vendored CRC32C ARM64 runtime check no longer probes Windows CRC32 instructions.");
            AssertContains(crc32cArm64Check, "PF_ARM_V8_CRYPTO_INSTRUCTIONS_AVAILABLE", "The vendored CRC32C ARM64 runtime check no longer probes Windows crypto instructions.");

            AssertContains(runtimeSource, "HashThreadFunc_ComputesOfficialXXH3DigestsForKnownVector", "Native runtime coverage no longer includes the official XXH3 vector.");
            AssertContains(runtimeSource, "HashThreadFunc_ComputesOfficialCRC32CDigestForKnownVector", "Native runtime coverage no longer includes the official CRC32C vector.");
            AssertContains(runtimeSource, "HashThreadFunc_ComputesOfficialCRC32CBoundaryDigestsForKnownVectors", "Native runtime coverage no longer includes the CRC32C boundary vector sweep.");
            AssertContains(runtimeSource, "RunHashRequest_XXH3AndCRC32CUnknownIdsAreIgnoredAndKnownVariantsStayOrdered", "Native runtime coverage no longer includes XXH3/CRC32C request normalization behavior.");
            AssertContains(runtimeSource, "HashThreadFunc_XXH3AndCRC32CRemainStableAcrossConcurrentRuns", "Native runtime coverage no longer includes concurrent XXH3/CRC32C stability.");
            AssertContains(runtimeSource, "RunHashRequest_XXH3AndCRC32CMultiFileConcurrentMatchesSingleRun", "Native runtime coverage no longer compares xxHash3/CRC32C multi-file concurrent runs against a single-run baseline.");
            AssertContains(runtimeSource, "54247382A8D6B94D", "Native runtime coverage no longer preserves the official XXH3-64 vector.");
            AssertContains(runtimeSource, "20EFC49FF02422EA54247382A8D6B94D", "Native runtime coverage no longer preserves the official XXH3-128 vector.");
            AssertContains(runtimeSource, "46DD794E", "Native runtime coverage no longer preserves the official CRC32C vector.");
            AssertContains(runtimeSource, "8A9136AA", "Native runtime coverage no longer preserves the CRC32C zero-input vector.");
            AssertContains(runtimeSource, "62A8AB43", "Native runtime coverage no longer preserves the CRC32C all-0xFF vector.");
            AssertContains(runtimeSource, "113FDB5C", "Native runtime coverage no longer preserves the CRC32C descending-input vector.");
            AssertContains(runtimeSource, "D9963A56", "Native runtime coverage no longer preserves the CRC32C iSCSI vector.");

            AssertContains(nativeCoreProject, @"third_party\xxhash\0.8.3\xxhash.c", "Desktop native core no longer compiles the vendored xxHash source snapshot.");
            AssertContains(nativeCoreProject, @"third_party\crc32c\1.1.2\src\crc32c.cc", "Desktop native core no longer compiles the vendored CRC32C source snapshot.");
            AssertContains(nativeCoreProject, @"third_party\crc32c\1.1.2\src\crc32c_sse42.cc", "Desktop native core no longer compiles the x64 CRC32C SSE4.2 path.");
            AssertContains(nativeCoreProject, @"third_party\crc32c\1.1.2\src\crc32c_arm64.cc", "Desktop native core no longer compiles the ARM64 CRC32C path.");
            AssertContains(uwpNativeProject, @"third_party\xxhash\0.8.3\xxhash.c", "UWP native core no longer compiles the vendored xxHash source snapshot.");
            AssertContains(uwpNativeProject, @"third_party\crc32c\1.1.2\src\crc32c.cc", "UWP native core no longer compiles the vendored CRC32C source snapshot.");
            AssertContains(uwpNativeProject, @"third_party\crc32c\1.1.2\src\crc32c_arm64.cc", "UWP native core no longer compiles the ARM64 CRC32C path.");

            AssertContains(xxhashNote, "Upstream tag: v0.8.3", "The vendored xxHash note no longer pins the official upstream tag.");
            AssertContains(xxhashNote, "e626a72bc2321cd320e953a0ccf1584cad60f363", "The vendored xxHash note no longer pins the official upstream commit.");
            AssertContains(crc32cNote, "Upstream tag: 1.1.2", "The vendored CRC32C note no longer pins the official upstream tag.");
            AssertContains(crc32cNote, "02e65f4fd3065d27b2e29324800ca6d04df16126", "The vendored CRC32C note no longer pins the official upstream commit.");
        }, failures);
        Run("OpenSSL EVP providers stay covered by hardening gates", () =>
        {
            string registryCore = ReadRepoFile(repoRoot, @"trunk\source\Domain\HashAlgorithmRegistryCore.h");
            string digestRegistry = ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestOperationRegistry.cpp");
            string providerHeader = ReadRepoFile(repoRoot, @"trunk\source\Runtime\Hash\OpenSslEvpHashProvider.h");
            string providerImplementation = ReadRepoFile(repoRoot, @"trunk\source\Runtime\Hash\OpenSslEvpHashProvider.cpp");
            string runtimeSource = ReadRepoFile(repoRoot, @"native-runtime-tests\LHash.NativeRuntimeTests\HashEngineRuntimeTests.cpp");
            string nativeCoreProject = ReadRepoFile(repoRoot, @"sub-proj\LHashNativeCore\LHashNativeCore.vcxproj");
            string uwpNativeProject = ReadRepoFile(repoRoot, @"archive\legacy-platforms\sub-proj\fHashUwpNative\fHashUwpNative.vcxproj");
            string clrBridgeProject = ReadRepoFile(repoRoot, @"archive\legacy-platforms\sub-proj\fHashClrBridge\fHashClrBridge.vcxproj");
            string uwpBridgeProject = ReadRepoFile(repoRoot, @"archive\legacy-platforms\sub-proj\fHashWinRtBridge\fHashWinRtBridge.vcxproj");
            string vendorTargets = ReadRepoFile(repoRoot, @"NativeOpenSslVendor.targets");
            string vendorScript = ReadRepoFile(repoRoot, @"trunk\build_openssl_vendor.ps1");
            string workflow = ReadRepoFile(repoRoot, @".github\workflows\windows-build.yml");
            string sourceInfo = ReadRepoFile(repoRoot, @"third_party\openssl\OPENSSL_3_5_6_SOURCE_INFO.txt");
            string vendorPolicy = ReadRepoFile(repoRoot, @"third_party\openssl\POLICY.md");
            string exceptionNote = ReadRepoFile(repoRoot, @"LICENSE-OPENSSL-EXCEPTION.md");

            AssertContains(registryCore, "{ \"md5\", \"MD5 (Deprecated)\", true, false }", "The deprecated MD5 descriptor variant is missing.");
            AssertContains(registryCore, "{ \"sha1\", \"SHA1 (Deprecated)\", true, false }", "The deprecated SHA1 descriptor variant is missing.");
            AssertContains(registryCore, "{ \"openssl-sha-256\", \"SHA-256\", true, true }", "The OpenSSL SHA-256 descriptor variant is missing or not enabled by default.");
            AssertContains(registryCore, "{ \"openssl-sha-384\", \"SHA-384\", true, false }", "The OpenSSL SHA-384 descriptor variant is missing.");
            AssertContains(registryCore, "{ \"openssl-sha-512\", \"SHA-512\", true, true }", "The OpenSSL SHA-512 descriptor variant is missing or not enabled by default.");
            AssertContains(registryCore, "{ \"openssl-sha3-256\", \"SHA3-256\", true, true }", "The OpenSSL SHA3-256 descriptor variant is missing or not enabled by default.");
            AssertContains(registryCore, "{ \"openssl-sha3-384\", \"SHA3-384\", true, false }", "The OpenSSL SHA3-384 descriptor variant is missing.");
            AssertContains(registryCore, "{ \"openssl-sha3-512\", \"SHA3-512\", true, false }", "The OpenSSL SHA3-512 descriptor variant is missing.");
            AssertContains(registryCore, "{ \"openssl-blake2b-512\", \"BLAKE2b-512\", true, false }", "The OpenSSL BLAKE2b-512 descriptor variant is missing.");
            AssertContains(registryCore, "{ \"openssl-blake2s-256\", \"BLAKE2s-256\", true, false }", "The OpenSSL BLAKE2s-256 descriptor variant is missing.");
            AssertContains(registryCore, "{ \"openssl-shake128-256\", \"SHAKE128-256\", true, false }", "The OpenSSL SHAKE128-256 descriptor variant is missing.");
            AssertContains(registryCore, "{ \"openssl-shake256-512\", \"SHAKE256-512\", true, false }", "The OpenSSL SHAKE256-512 descriptor variant is missing.");

        AssertContains(providerImplementation, "EVP_MD_fetch", "The OpenSSL provider no longer fetches digest implementations through EVP.");
        AssertContains(providerImplementation, "GetCachedDigestImplementation", "The OpenSSL provider no longer caches fetched digest implementations across file contexts.");
        AssertContains(providerImplementation, "EVP_DigestInit_ex2", "The OpenSSL provider no longer initializes digest contexts through EVP.");
        AssertContains(providerImplementation, "OSSL_DIGEST_PARAM_SIZE", "The OpenSSL provider no longer configures truncated digest output through OSSL params.");
        AssertContains(providerImplementation, "EVP_DigestUpdate", "The OpenSSL provider no longer updates digest contexts through EVP.");
        AssertContains(providerImplementation, "EVP_DigestUpdate(hashContext.mdContext, data, dataLen) != 1", "The OpenSSL provider no longer treats EVP_DigestUpdate failure as a sticky error.");
        AssertContains(providerImplementation, "if (hashContext.updateFailed)", "The OpenSSL provider no longer short-circuits follow-up updates after an EVP failure.");
        AssertContains(providerImplementation, "hashContext.updateFailed = true;", "The OpenSSL provider no longer records EVP update failures.");
        AssertContains(providerImplementation, "EVP_DigestFinal_ex", "The OpenSSL provider no longer finalizes fixed-size digests through EVP.");
        AssertContains(providerImplementation, "EVP_DigestFinalXOF", "The OpenSSL provider no longer finalizes XOF digests through EVP.");
        AssertContains(providerImplementation, "ConfigureOpenSslEvpFailureInjection", "The OpenSSL provider no longer exposes failure injection hooks for runtime error-path coverage.");
        AssertDoesNotContain(providerImplementation, "static EVP_MD_CTX", "The OpenSSL provider unexpectedly reintroduced a shared mutable EVP context.");
        AssertContains(providerHeader, "updateFailed(false)", "The OpenSSL provider header no longer resets the sticky update failure flag during context construction.");
        AssertContains(providerHeader, "bool updateFailed;", "The OpenSSL provider header no longer tracks sticky EVP update failures.");
        AssertContains(providerHeader, "struct OpenSslEvpHashFinalizeResult", "The OpenSSL provider header no longer exposes explicit finalize success/failure semantics.");
        AssertContains(providerHeader, "OPENSSL_SHA_384_OUTPUT_BYTES = 48", "The OpenSSL provider header no longer exposes the SHA-384 output profile.");
            AssertContains(providerHeader, "OPENSSL_SHA3_384_OUTPUT_BYTES = 48", "The OpenSSL provider header no longer exposes the SHA3-384 output profile.");
            AssertContains(providerHeader, "OPENSSL_BLAKE2B_512_OUTPUT_BYTES = 64", "The OpenSSL provider header no longer exposes the BLAKE2b-512 output profile.");
            AssertContains(providerHeader, "OPENSSL_BLAKE2S_256_OUTPUT_BYTES = 32", "The OpenSSL provider header no longer exposes the BLAKE2s-256 output profile.");
            AssertContains(providerHeader, "OPENSSL_SHAKE256_512_OUTPUT_BYTES = 64", "The OpenSSL provider header no longer exposes the SHAKE256-512 output profile.");

            AssertContains(digestRegistry, "InitializeOpenSslSha256DigestContext", "The digest registry no longer exposes the OpenSSL SHA-256 initialization hook.");
            AssertContains(digestRegistry, "InitializeOpenSslSha384DigestContext", "The digest registry no longer exposes the OpenSSL SHA-384 initialization hook.");
            AssertContains(digestRegistry, "InitializeOpenSslSha512DigestContext", "The digest registry no longer exposes the OpenSSL SHA-512 initialization hook.");
            AssertContains(digestRegistry, "InitializeOpenSslSha3_256DigestContext", "The digest registry no longer exposes the OpenSSL SHA3-256 initialization hook.");
            AssertContains(digestRegistry, "InitializeOpenSslSha3_384DigestContext", "The digest registry no longer exposes the OpenSSL SHA3-384 initialization hook.");
            AssertContains(digestRegistry, "InitializeOpenSslSha3_512DigestContext", "The digest registry no longer exposes the OpenSSL SHA3-512 initialization hook.");
            AssertContains(digestRegistry, "InitializeOpenSslBlake2b_512DigestContext", "The digest registry no longer exposes the OpenSSL BLAKE2b-512 initialization hook.");
            AssertContains(digestRegistry, "InitializeOpenSslBlake2s_256DigestContext", "The digest registry no longer exposes the OpenSSL BLAKE2s-256 initialization hook.");
            AssertContains(digestRegistry, "InitializeOpenSslShake128_256DigestContext", "The digest registry no longer exposes the OpenSSL SHAKE128-256 initialization hook.");
            AssertContains(digestRegistry, "InitializeOpenSslShake256_512DigestContext", "The digest registry no longer exposes the OpenSSL SHAKE256-512 initialization hook.");

        AssertContains(runtimeSource, "HashThreadFunc_ComputesOfficialOpenSslDigestsForKnownVector", "The native runtime suite no longer covers the OpenSSL official-vector test.");
        AssertContains(runtimeSource, "RunHashRequest_OpenSslSha2VariantsStayDistinctWithinOpenSslFamily", "The native runtime suite no longer verifies that the OpenSSL SHA-2 variants stay distinct inside the OpenSSL family.");
        AssertContains(runtimeSource, "RunHashRequest_OpenSslUnknownIdsAreIgnoredAndKnownVariantsStayOrdered", "The native runtime suite no longer covers unknown OpenSSL algorithm ids.");
        AssertContains(runtimeSource, "HashThreadFunc_OpenSslVariantsRemainStableAcrossConcurrentRuns", "The native runtime suite no longer covers OpenSSL concurrent stability.");
        AssertContains(runtimeSource, "RunHashRequest_OpenSslDigestUpdateFailureProducesExplicitFileError", "The native runtime suite no longer covers OpenSSL EVP update-failure propagation.");
        AssertContains(runtimeSource, "RunHashRequest_OpenSslDigestFinalizeFailureProducesExplicitFileError", "The native runtime suite no longer covers OpenSSL EVP finalize-failure propagation.");
        AssertContains(runtimeSource, "CreateAlgorithmId(\"openssl-sha3\")", "The OpenSSL runtime suite no longer exercises the unknown OpenSSL id path.");
            AssertContains(runtimeSource, "CreateAlgorithmId(\"openssl-blake2b-512\")", "The OpenSSL runtime suite no longer covers the BLAKE2b-512 variant path.");
            AssertContains(runtimeSource, "CreateAlgorithmId(\"openssl-blake2s-256\")", "The OpenSSL runtime suite no longer covers the BLAKE2s-256 variant path.");
            AssertContains(runtimeSource, "CB00753F45A35E8BB5A03D699AC65007272C32AB0EDED1631A8B605A43FF5BED", "The OpenSSL runtime suite no longer carries the SHA-384 known vector.");
            AssertContains(runtimeSource, "EC01498288516FC926459F58E2C6AD8DF9B473CB0FC08C2596DA7CF0E49BE4B2", "The OpenSSL runtime suite no longer carries the SHA3-384 known vector.");
            AssertContains(runtimeSource, "BA80A53F981C4D0D6A2797B69F12F6E94C212F14685AC4B74B12BB6FDBFFA2D17D87C5392AAB792DC252D5DE4533CC9518D38AA8DBF1925AB92386EDD4009923", "The OpenSSL runtime suite no longer carries the BLAKE2b-512 known vector.");
            AssertContains(runtimeSource, "508C5E8C327C14E2E1A72BA34EEB452F37458B209ED63A294D999B4C86675982", "The OpenSSL runtime suite no longer carries the BLAKE2s-256 known vector.");
            AssertContains(runtimeSource, "483366601360A8771C6863080CC4114D8DB44530F8F1E1EE4F94EA37E78B5739", "The OpenSSL runtime suite no longer carries the SHAKE256 known vector.");

            AssertContains(nativeCoreProject, @"Runtime\Hash\OpenSslEvpHashProvider.cpp", "The maintained native core project no longer builds the OpenSSL provider.");
            AssertContains(uwpNativeProject, @"Runtime\Hash\OpenSslEvpHashProvider.cpp", "The UWP native core project no longer builds the OpenSSL provider.");
            AssertContains(clrBridgeProject, @"$(FHashOpenSslLibDir)\libcrypto.lib", "The CLR bridge no longer links libcrypto explicitly when the vendored OpenSSL root is present.");
            AssertContains(uwpBridgeProject, @"$(FHashOpenSslLibDir)\libcrypto.lib", "The WinRT bridge no longer links libcrypto explicitly when the vendored OpenSSL root is present.");
            AssertContains(vendorTargets, "LHASH_WITH_OPENSSL3_VENDOR=1", "The shared OpenSSL vendor targets no longer define the OpenSSL build flag.");
            AssertContains(vendorTargets, "libcrypto.lib", "The shared OpenSSL vendor targets no longer link libcrypto.");
            AssertContains(vendorScript, "ValidateSet('x64', 'ARM64')", "The OpenSSL vendor build script no longer restricts the supported platforms to x64/ARM64.");
            AssertDoesNotContain(vendorScript, "VC-WIN32", "The OpenSSL vendor build script still supports the retired Win32 platform.");
            AssertContains(vendorScript, "openssl-3.5.6", "The OpenSSL vendor build script no longer targets 3.5.6.");
            AssertContains(vendorScript, "VC-WIN64A", "The OpenSSL vendor build script no longer covers x64.");
            AssertContains(vendorScript, "VC-WIN64-ARM", "The OpenSSL vendor build script no longer covers ARM64.");
            AssertContains(workflow, "build_openssl_vendor.ps1", "The Windows build workflow no longer builds the vendored OpenSSL package.");
            AssertContains(workflow, "Verify pristine OpenSSL vendor source", "The Windows build workflow no longer verifies the pristine OpenSSL vendor tree before build.");
            AssertContains(workflow, "tools/verify_openssl_vendor_pristine.ps1", "The Windows build workflow no longer invokes the OpenSSL pristine checker.");
            AssertContains(workflow, "openssl_vendor_version=3.5.6", "The Windows build workflow no longer records the OpenSSL vendor version in release metadata.");
            AssertContains(workflow, "openssl_vendor_source=third_party/openssl/3.5.6", "The Windows build workflow no longer records the OpenSSL vendor source directory in release metadata.");
            AssertContains(workflow, "openssl_vendor_policy=pristine-upstream-source", "The Windows build workflow no longer records the OpenSSL vendor policy in release metadata.");
            AssertContains(workflow, "openssl_vendor_local_patches=none", "The Windows build workflow no longer records that the OpenSSL vendor build is patch-free.");
            AssertContains(workflow, "LHashOpenSslInstallRoot", "The Windows build workflow no longer passes the OpenSSL install root to native builds.");
            AssertContains(sourceInfo, "version=openssl-3.5.6", "The vendored OpenSSL source info no longer pins the upstream version.");
            AssertContains(sourceInfo, "source_policy=pristine upstream tarball extraction", "The vendored OpenSSL source info no longer records the pristine source policy.");
            AssertContains(sourceInfo, "vendor_directory=third_party/openssl/3.5.6", "The vendored OpenSSL source info no longer records the vendor directory.");
            AssertContains(sourceInfo, "local_patches=none", "The vendored OpenSSL source info no longer records the patch-free policy.");
            AssertContains(sourceInfo, "imported_for=LHash Windows x64/ARM64 static libcrypto vendor build", "The vendored OpenSSL source info no longer records the imported-for context.");
            AssertContains(vendorPolicy, "pristine upstream OpenSSL source tree", "The OpenSSL vendor policy no longer states the pristine-tree rule.");
            AssertContains(vendorPolicy, "temporary build copy", "The OpenSSL vendor policy no longer states that patches apply only to the temporary build copy.");
            AssertContains(vendorPolicy, "third_party/openssl/patches/<version>/", "The OpenSSL vendor policy no longer allows optional patches in the dedicated patch directory.");
            AssertDoesNotContain("legacy `sha256` / `sha512` ids and labels untouched", sourceInfo, "The vendored OpenSSL source info still documents removed legacy SHA coexistence.");
            AssertContains(exceptionNote, "OpenSSL Linking Exception", "The repository no longer carries the OpenSSL linking exception note.");
        }, failures);
        Run("DLL search path hardening is present", () =>
        {
            string filesHashApp = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHash.cpp");
            string windowsUtilsHeader = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\WindowsUtils.h");
            string windowsUtils = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\WindowsUtils.cpp");
            string winUiProgram = ReadRepoFile(repoRoot, @"archive\legacy-platforms\trunk\source\WinUI\Program.cs");
            string winUiWin32Helper = ReadRepoFile(repoRoot, @"archive\legacy-platforms\trunk\source\WinUI\Win32Helper.cs");
            string fileshashProject = ReadRepoFile(repoRoot, @"trunk\fileshash.vcxproj");

            AssertContains(windowsUtilsHeader, "bool InitializeProcessDllSearchPolicy();", "MFC startup is missing the explicit DLL search policy declaration.");
            AssertContains(filesHashApp, "WindowsUtils::InitializeProcessDllSearchPolicy();", "MFC startup no longer enables secure DLL search directories.");
            AssertContains(windowsUtils, "GetProcAddress(kernel32Module, \"SetDefaultDllDirectories\")", "WindowsUtils no longer resolves SetDefaultDllDirectories safely at runtime.");
            AssertContains(windowsUtils, "setDefaultDllDirectories(LOAD_LIBRARY_SEARCH_DEFAULT_DIRS)", "WindowsUtils no longer tightens the default DLL search path.");
            AssertContains(windowsUtils, "LoadLibraryEx(", "WindowsUtils explicit DLL loads no longer use LoadLibraryEx.");
            AssertContains(windowsUtils, "LOAD_LIBRARY_SEARCH_DLL_LOAD_DIR | LOAD_LIBRARY_SEARCH_DEFAULT_DIRS", "WindowsUtils explicit DLL loads no longer use secure DLL search flags.");
            AssertDoesNotContain(windowsUtils, "LoadLibrary(pszShlDllPath);", "WindowsUtils still uses the legacy unsafe shell-extension LoadLibrary path.");
            AssertContains(winUiProgram, "Win32Helper.TryEnableSecureDllSearchDirectories();", "WinUI startup no longer enables secure DLL search directories.");
            AssertContains(winUiWin32Helper, "SetDefaultDllDirectories(LOAD_LIBRARY_SEARCH_DEFAULT_DIRS)", "WinUI helper no longer tightens the default DLL search path.");
            AssertContains(winUiWin32Helper, "if (IsAppPackaged())", "WinUI helper no longer skips DLL search hardening for packaged activation.");
            AssertContains(fileshashProject, "<RuntimeLibrary>MultiThreaded</RuntimeLibrary>", "Legacy MFC release build no longer uses the static CRT hardening baseline.");
            AssertDoesNotContain(fileshashProject, "<RuntimeLibrary>MultiThreadedDLL</RuntimeLibrary>", "Legacy MFC release build unexpectedly switched back to the dynamic CRT.");
            AssertContains(ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashCommandController.cpp"), "CFile::modeCreate | CFile::modeWrite | CFile::typeBinary", "Legacy export no longer writes a deterministic binary text file.");
            AssertContains(ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashCommandController.cpp"), "tstrtostrutf8(exportText)", "Legacy export no longer converts visible hash output to UTF-8.");
            AssertContains(ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashResultViewController.cpp"), "m_mainEdit->GetWindowText(currentText);", "Legacy export no longer falls back to the visible result text.");
            AssertContains(ReadRepoFile(repoRoot, @"trunk\source\WinMFC\UIBridgeMFC.h"), "InterlockedCompareExchange(&m_taskUpdatePending, 1, 0) == 0", "Legacy task updates no longer coalesce duplicate flush messages.");
            AssertContains(ReadRepoFile(repoRoot, @"trunk\source\WinMFC\UIBridgeMFC.cpp"), "taskUpdates.swap(m_pendingTaskUpdates);", "Legacy task updates no longer drain the pending batch in one handoff.");
            AssertDoesNotContain(ReadRepoFile(repoRoot, @"trunk\source\WinMFC\UIBridgeMFC.cpp"), "new FilesHashTaskUpdate(taskUpdate)", "Legacy task updates unexpectedly reintroduced per-message heap allocations.");
            AssertContains(ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashDlg.cpp"), "m_hashProgressController.ApplyTaskUpdates(taskUpdates);", "Legacy dialog no longer routes WP_TASK_UPDATE through batched task refresh.");
            AssertContains(ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashDlg.cpp"), "ON_NOTIFY(LVN_GETDISPINFO, IDC_TASK_LIST, &CFilesHashDlg::OnTaskListGetDispInfo)", "Legacy dialog no longer provides virtual-list display callbacks for the task list.");
            AssertContains(ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashProgressController.cpp"), "taskRowState.state == FILES_HASH_TASK_PENDING ||", "Legacy task list no longer keeps older completed rows stable while updating current rows.");
            AssertContains(ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashProgressController.cpp"), "m_taskListCtrl->SetRedraw(FALSE);", "Legacy task list no longer suspends redraws during batched task updates.");
            AssertContains(ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashProgressController.cpp"), "m_taskListCtrl->SetRedraw(TRUE);", "Legacy task list no longer resumes redraws after batched task updates.");
            AssertContains(ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashProgressController.cpp"), "m_taskListCtrl->SetItemCountEx(static_cast<int>(m_taskRows.size()), LVSICF_NOINVALIDATEALL | LVSICF_NOSCROLL);", "Legacy task list no longer drives the list control through virtual-list item counts.");
            AssertContains(ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashProgressController.cpp"), "RefreshTaskRow(rowIndex, ensureVisible);", "Legacy task list no longer routes row refresh through the explicit ensure-visible flag.");
            AssertContains(ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashProgressController.cpp"), "if (ensureVisible)", "Legacy task list no longer gates row auto-scrolling behind an explicit ensure-visible check.");
            AssertContains(ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashProgressController.cpp"), "m_taskListCtrl->EnsureVisible(rowIndex, FALSE);", "Legacy task list no longer performs the final ensure-visible scroll when explicitly requested.");
            AssertContains(ReadRepoFile(repoRoot, @"trunk\source\WinMFC\fileshash.rc"), "LVS_OWNERDATA", "Legacy task list resource no longer uses the virtual-list owner-data style.");
            AssertContains(ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestQueue.cpp"), "vector<unique_ptr<DigestDataBuffer>> digestBufferPool;", "Parallel digest execution no longer preallocates a reusable data-buffer pool.");
            AssertContains(ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestQueue.cpp"), "queue<size_t> availableBufferIndices;", "Parallel digest execution no longer tracks reusable buffer slots.");
        }, failures);
        Run("Native compiler and linker mitigations are imported consistently", () =>
        {
            string nativeSecurityTargets = ReadRepoFile(repoRoot, @"NativeSecurity.targets");
            string[] vcxProjects = Directory.GetFiles(repoRoot, "*.vcxproj", SearchOption.AllDirectories);

            AssertContains(nativeSecurityTargets, "<BufferSecurityCheck>true</BufferSecurityCheck>", "Shared native security targets do not enable /GS.");
            AssertContains(nativeSecurityTargets, "<SDLCheck>true</SDLCheck>", "Shared native security targets do not enable /sdl.");
            AssertContains(nativeSecurityTargets, "<LHashEnableControlFlowGuard>true</LHashEnableControlFlowGuard>", "Shared native security targets do not enable CFG by default.");
            AssertContains(nativeSecurityTargets, "<LHashEnableControlFlowGuard Condition=\"'$(CLRSupport)'!=''\">false</LHashEnableControlFlowGuard>", "Shared native security targets do not exempt managed CLR bridge projects from CFG.");
            AssertContains(nativeSecurityTargets, "<ControlFlowGuard Condition=\"'$(LHashEnableControlFlowGuard)'=='true'\">Guard</ControlFlowGuard>", "Shared native security targets do not enable CFG for eligible native projects.");
            AssertContains(nativeSecurityTargets, "<RandomizedBaseAddress>true</RandomizedBaseAddress>", "Shared native security targets do not enable ASLR.");
            AssertContains(nativeSecurityTargets, "<HighEntropyVA>true</HighEntropyVA>", "Shared native security targets do not enable high-entropy VA.");
            AssertContains(nativeSecurityTargets, "<DataExecutionPrevention>true</DataExecutionPrevention>", "Shared native security targets do not enable DEP.");

            foreach (string projectPath in vcxProjects)
            {
                string relativePath = Path.GetRelativePath(repoRoot, projectPath).Replace('/', '\\');
                string projectContents = ReadRepoFile(repoRoot, relativePath);
                AssertContains(projectContents, "NativeSecurity.targets", $"{relativePath} does not import the shared native security targets.");
            }
        }, failures);
        Run("UTF-8 native compiler settings and resource code pages are imported consistently", () =>
        {
            string nativeUtf8Targets = ReadRepoFile(repoRoot, @"NativeUtf8.targets");
            string legacyProject = ReadRepoFile(repoRoot, @"trunk\fileshash.vcxproj");
            string nativeCoreProject = ReadRepoFile(repoRoot, @"sub-proj\LHashNativeCore\LHashNativeCore.vcxproj");
            string runtimeTestsProject = ReadRepoFile(repoRoot, @"native-runtime-tests\LHash.NativeRuntimeTests\LHash.NativeRuntimeTests.vcxproj");
            string benchmarkProject = ReadRepoFile(repoRoot, @"native-benchmarks\LHash.NativeBenchmarks\LHash.NativeBenchmarks.vcxproj");
            string shellProject = ReadRepoFile(repoRoot, @"sub-proj\LHashShlExt\LHashShlExt.vcxproj");
            string mfcRc = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\fileshash.rc");
            string mfcRc2 = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\res\fileshash.rc2");

            AssertContains(nativeUtf8Targets, "<AdditionalOptions>/utf-8 %(AdditionalOptions)</AdditionalOptions>", "Shared native UTF-8 targets do not enable /utf-8.");
            AssertContains(nativeUtf8Targets, "<AdditionalOptions>/c65001 %(AdditionalOptions)</AdditionalOptions>", "Shared native UTF-8 targets do not enable /c65001 for resources.");

            foreach (string projectContents in new[] { legacyProject, nativeCoreProject, runtimeTestsProject, benchmarkProject, shellProject })
            {
                AssertContains(projectContents, "NativeUtf8.targets", "A native project does not import the shared UTF-8 targets.");
                AssertDoesNotContain(projectContents, "/source-charset:.936", "A native project still forces the old 936 source charset.");
                AssertDoesNotContain(projectContents, "/execution-charset:.936", "A native project still forces the old 936 execution charset.");
                AssertDoesNotContain(projectContents, "/c936", "A native project still forces the old 936 resource compiler code page.");
            }
            AssertDoesNotContain(benchmarkProject, "Debug|Win32", "The native benchmark project still carries a retired Win32 debug configuration.");
            AssertDoesNotContain(benchmarkProject, "Release|Win32", "The native benchmark project still carries a retired Win32 release configuration.");
            AssertDoesNotContain(shellProject, "Debug|Win32", "The shell extension project still carries a retired Win32 debug configuration.");
            AssertDoesNotContain(shellProject, "Release|Win32", "The shell extension project still carries a retired Win32 release configuration.");

            AssertContains(mfcRc, "#pragma code_page(65001)", "Legacy MFC resource chain does not yet use UTF-8 resource code pages.");
            AssertContains(mfcRc2, "BLOCK \"080404b0\"", "Legacy MFC version resource block is not yet migrated to Unicode translation metadata.");
            AssertContains(mfcRc2, "VALUE \"Translation\", 0x804, 1200", "Legacy MFC version resource translation is not yet migrated to Unicode metadata.");
            AssertContains(ReadRepoFile(repoRoot, @"sub-proj\LHashShlExt\LHashShlExt.rc"), "#pragma code_page(65001)", "Legacy shell extension resource chain does not yet use UTF-8 code pages.");
            AssertContains(ReadRepoFile(repoRoot, @"sub-proj\LHashShlExt\LHashShlExt.rc"), "VALUE \"Translation\", 0x804, 1200", "Legacy shell extension version resource translation is not yet migrated to Unicode metadata.");
            AssertContains(ReadRepoFile(repoRoot, @"archive\legacy-platforms\sub-proj\fHashWinRtBridge\fHashWinRtBridge.rc"), "#pragma code_page(65001)", "WinRT bridge resource chain does not yet use UTF-8 code pages.");
            AssertContains(ReadRepoFile(repoRoot, @"archive\legacy-platforms\sub-proj\fHashWUIShellExt\fHashWUIShellExt.rc"), "#pragma code_page(65001)", "WinUI shell extension resource chain does not yet use UTF-8 code pages.");
            AssertContains(ReadRepoFile(repoRoot, @"archive\legacy-platforms\sub-proj\fHashUwpShellExt\fHashUwpShellExt.rc"), "#pragma code_page(65001)", "UWP shell extension resource chain does not yet use UTF-8 code pages.");
        }, failures);
        Run("WinMFC context-menu controller preserves elevation and context-menu safety flow", () =>
        {
            string dialog = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashDlg.cpp");
            string controller = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashContextMenuController.cpp");

            AssertContains(dialog, "m_hashContextMenuController.HandleButtonClick(", "WinMFC dialog no longer routes context-menu clicks through the dedicated controller.");
            AssertContains(controller, "WindowsComm::IsWindowsVistaOrGreater()", "WinMFC context-menu controller no longer gates elevation by the modern Windows-version helper.");
            AssertContains(controller, "WindowsUtils::ElevateProcess()", "WinMFC context-menu controller no longer uses the hardened elevation helper.");
            AssertContains(controller, "WindowsUtils::RemoveContextMenu(); // Try to delete all items related to LHash", "WinMFC context-menu controller no longer performs the defensive pre-add cleanup.");
            AssertContains(controller, "WindowsUtils::AddContextMenu()", "WinMFC context-menu controller no longer uses the shared context-menu add helper.");
            AssertContains(controller, "SetStatusText(addFailedText);", "WinMFC context-menu controller no longer surfaces add failures to the UI.");
            AssertContains(controller, "SetStatusText(removeFailedText);", "WinMFC context-menu controller no longer surfaces remove failures to the UI.");
        }, failures);
        Run("UWP and WinUI attack surface stays minimal", () =>
        {
            string uwpManifest = ReadRepoFile(repoRoot, @"archive\legacy-platforms\trunk\source\WinUWP\Package.appxmanifest");
            string uwpHelper = ReadRepoFile(repoRoot, @"archive\legacy-platforms\trunk\source\WinUWP\UwpHelper.cs");
            string uwpMainPage = ReadRepoFile(repoRoot, @"archive\legacy-platforms\trunk\source\WinUWP\MainPage.xaml.cs");
            string uwpEn = ReadRepoFile(repoRoot, @"archive\legacy-platforms\trunk\source\WinUWP\Strings\en-US\Resources.resw");
            string uwpZh = ReadRepoFile(repoRoot, @"archive\legacy-platforms\trunk\source\WinUWP\Strings\zh-CN\Resources.resw");
            string winUiProject = ReadRepoFile(repoRoot, @"archive\legacy-platforms\trunk\source\WinUI\fHashWUI.csproj");
            string winUiHelper = ReadRepoFile(repoRoot, @"archive\legacy-platforms\trunk\source\WinUI\WinUIHelper.cs");
            string winUiMainPage = ReadRepoFile(repoRoot, @"archive\legacy-platforms\trunk\source\WinUI\MainPage.xaml.cs");
            string winUiEn = ReadRepoFile(repoRoot, @"archive\legacy-platforms\trunk\source\WinUI\Strings\en-US\Resources.resw");
            string winUiZh = ReadRepoFile(repoRoot, @"archive\legacy-platforms\trunk\source\WinUI\Strings\zh-CN\Resources.resw");
            string winUiMarker = ReadRepoFile(repoRoot, @"archive\legacy-platforms\trunk\source\WinUI\NON_MAINLINE.md");
            string clrBridgeMarker = ReadRepoFile(repoRoot, @"archive\legacy-platforms\sub-proj\fHashClrBridge\NON_MAINLINE.md");
            string winMfcDlg = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashDlg.cpp");
            string winMfcRes = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\fileshash.rc");
            string winMfcBaseStrings = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\UIStringsBase.cpp");
            string winMfcZhStrings = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\UIStringsZHCN.cpp");

            AssertDoesNotContain(uwpManifest, "internetClient", "UWP manifest still requests Internet client capability.");
            AssertDoesNotContain(uwpManifest, "privateNetworkClientServer", "UWP manifest still requests private network capability.");
            AssertContains(uwpHelper, "Launcher.LaunchUriAsync", "UWP URL launching path is missing.");
            AssertDoesNotContain(uwpHelper, "HttpClient", "UWP helper unexpectedly added an in-app HTTP client.");
            AssertDoesNotContain(winUiProject, "Microsoft.Web.WebView2", "WinUI project still carries an explicit WebView2 package reference.");
            AssertDoesNotContain(winUiProject, "x86;x64;ARM64", "The WinUI project still advertises x86 as a supported platform.");
            AssertDoesNotContain(winUiProject, "win-x86", "The WinUI project still advertises x86 as a supported runtime identifier.");
            AssertDoesNotContain(winUiProject, "win10-x86", "The WinUI project still advertises x86 as a supported legacy runtime identifier.");
            AssertContains(winUiMarker, "Non-Mainline Reference Tree", "WinUI tree does not carry the expected non-mainline marker.");
            AssertContains(clrBridgeMarker, "Non-Mainline Reference Tree", "CLR bridge tree does not carry the expected non-mainline marker.");
            AssertContains(winUiHelper, "Launcher.LaunchUriAsync", "WinUI URL launching path is missing.");
            AssertDoesNotContain(uwpMainPage, "MenuItemGoogle", "UWP UI still exposes a Google hash-search action.");
            AssertDoesNotContain(uwpMainPage, "MenuItemVirusTotal", "UWP UI still exposes a VirusTotal hash-search action.");
            AssertDoesNotContain(uwpEn, "Search Google", "UWP English resources still advertise Google hash search.");
            AssertDoesNotContain(uwpEn, "Search VirusTotal", "UWP English resources still advertise VirusTotal hash search.");
            AssertDoesNotContain(uwpZh, "闂傚倷鑳堕幊鎾诲触鐎ｎ剙鍨濋幖娣妼绾?Google", "UWP Chinese resources still advertise Google hash search.");
            AssertDoesNotContain(uwpZh, "闂傚倷鑳堕幊鎾诲触鐎ｎ剙鍨濋幖娣妼绾?VirusTotal", "UWP Chinese resources still advertise VirusTotal hash search.");
            AssertDoesNotContain(winUiMainPage, "MenuItemGoogle", "WinUI UI still exposes a Google hash-search action.");
            AssertDoesNotContain(winUiMainPage, "MenuItemVirusTotal", "WinUI UI still exposes a VirusTotal hash-search action.");
            AssertDoesNotContain(winUiEn, "Search Google", "WinUI English resources still advertise Google hash search.");
            AssertDoesNotContain(winUiEn, "Search VirusTotal", "WinUI English resources still advertise VirusTotal hash search.");
            AssertDoesNotContain(winUiZh, "闂傚倷鑳堕幊鎾诲触鐎ｎ剙鍨濋幖娣妼绾?Google", "WinUI Chinese resources still advertise Google hash search.");
            AssertDoesNotContain(winUiZh, "闂傚倷鑳堕幊鎾诲触鐎ｎ剙鍨濋幖娣妼绾?VirusTotal", "WinUI Chinese resources still advertise VirusTotal hash search.");
            AssertDoesNotContain(winMfcDlg, "Searchgoogle", "WinMFC dialog still exposes a Google hash-search command.");
            AssertDoesNotContain(winMfcDlg, "Searchvirustotal", "WinMFC dialog still exposes a VirusTotal hash-search command.");
            AssertDoesNotContain(winMfcRes, "[Search Google]", "WinMFC menu resources still expose Google hash search.");
            AssertDoesNotContain(winMfcRes, "[Search VirusTotal]", "WinMFC menu resources still expose VirusTotal hash search.");
            AssertDoesNotContain(winMfcBaseStrings, "Search Google", "WinMFC English strings still advertise Google hash search.");
            AssertDoesNotContain(winMfcBaseStrings, "Search VirusTotal", "WinMFC English strings still advertise VirusTotal hash search.");
            AssertDoesNotContain(winMfcZhStrings, "闂傚倷鑳堕幊鎾诲触鐎ｎ剙鍨濋幖娣妼绾?Google", "WinMFC Chinese strings still advertise Google hash search.");
            AssertDoesNotContain(winMfcZhStrings, "闂傚倷鑳堕幊鎾诲触鐎ｎ剙鍨濋幖娣妼绾?VirusTotal", "WinMFC Chinese strings still advertise VirusTotal hash search.");
        }, failures);

        if (failures.Count > 0)
        {
            Console.Error.WriteLine("Security regression checks failed:");
            foreach (string failure in failures)
            {
                Console.Error.WriteLine($"- {failure}");
            }

            return 1;
        }

        Console.WriteLine("All security regression checks passed.");
        return 0;
    }

    private static void TestCommandLineParsing()
    {
        string[] args = SplitCommandLine(" \"C:\\Program Files\\a.bin\" \"D:\\hash.txt\"");
        string[] filtered = args.Where(static arg => !string.IsNullOrEmpty(arg)).ToArray();

        if (filtered.Length != 2 ||
            filtered[0] != @"C:\Program Files\a.bin" ||
            filtered[1] != @"D:\hash.txt")
        {
            throw new InvalidOperationException("Quoted path parsing no longer matches the hardened parser expectations.");
        }

        string expectedUnicodePath = "C:\\\u6D4B\u8BD5 \u76EE\u5F55\\\u54C8\u5E0C \u6587\u4EF6.txt";
        string[] unicodeArgs = SplitCommandLine($" \"{expectedUnicodePath}\"");
        string[] unicodeFiltered = unicodeArgs.Where(static arg => !string.IsNullOrEmpty(arg)).ToArray();
        if (unicodeFiltered.Length != 1 || unicodeFiltered[0] != expectedUnicodePath)
        {
            throw new InvalidOperationException("Unicode path parsing no longer matches the hardened parser expectations.");
        }

        _ = SplitCommandLine("\"C:\\unterminated");
    }

    private static void TestCommandLineEdgeCases()
    {
        string[] simpleArgs = SplitCommandLine(" C:\\hash\\plain.txt ");
        string[] simpleFiltered = simpleArgs.Where(static arg => !string.IsNullOrEmpty(arg)).ToArray();
        if (simpleFiltered.Length != 1 || simpleFiltered[0] != @"C:\hash\plain.txt")
        {
            throw new InvalidOperationException("Simple unquoted path parsing regressed.");
        }

        string[] emptyAndValidArgs = SplitCommandLine(" \"\" \"D:\\hash.txt\"");
        string[] emptyAndValidFiltered = emptyAndValidArgs.Where(static arg => !string.IsNullOrEmpty(arg)).ToArray();
        if (emptyAndValidFiltered.Length != 1 || emptyAndValidFiltered[0] != @"D:\hash.txt")
        {
            throw new InvalidOperationException("Empty command-line arguments are no longer filtered safely.");
        }

        string longLeaf = new('a', 280);
        string expectedLongPath = $@"C:\long path\{longLeaf}.bin";
        string[] longArgs = SplitCommandLine($" \"{expectedLongPath}\"");
        string[] longFiltered = longArgs.Where(static arg => !string.IsNullOrEmpty(arg)).ToArray();
        if (longFiltered.Length != 1 || longFiltered[0] != expectedLongPath)
        {
            throw new InvalidOperationException("Long path parsing no longer preserves boundary-length inputs.");
        }

        string[] malformedArgs = SplitCommandLine("\"C:\\unterminated");
        if (malformedArgs.Length == 0 || string.IsNullOrEmpty(malformedArgs[0]))
        {
            throw new InvalidOperationException("Malformed command lines no longer return a recoverable argument array.");
        }
    }

    private static string ReadRepoFile(string repoRoot, string relativePath)
    {
        string path = ResolveRepoPath(repoRoot, relativePath);
        return File.ReadAllText(path, DetectEncoding(path));
    }

    private static string ResolveRepoPath(string repoRoot, string relativePath)
    {
        string livePath = Path.Combine(repoRoot, relativePath);
        if (File.Exists(livePath) || Directory.Exists(livePath))
        {
            return livePath;
        }

        foreach ((string livePrefix, string archivePrefix) in GetArchivePathMappings())
        {
            if (!relativePath.StartsWith(livePrefix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string archivedRelativePath = archivePrefix + relativePath.Substring(livePrefix.Length);
            string archivedPath = Path.Combine(repoRoot, archivedRelativePath);
            if (File.Exists(archivedPath) || Directory.Exists(archivedPath))
            {
                return archivedPath;
            }
        }

        return livePath;
    }

    private static IEnumerable<(string LivePrefix, string ArchivePrefix)> GetArchivePathMappings()
    {
        yield return (@"archive\legacy-platforms\trunk\fHashWUIWap\", @"archive\legacy-platforms\trunk\fHashWUIWap\");
        yield return (@"archive\legacy-platforms\trunk\fHashUwpWap\", @"archive\legacy-platforms\trunk\fHashUwpWap\");
        yield return (@"archive\legacy-platforms\trunk\source\WinUWP\", @"archive\legacy-platforms\trunk\source\WinUWP\");
        yield return (@"archive\legacy-platforms\trunk\source\OSXUI\", @"archive\legacy-platforms\trunk\source\OSXUI\");
        yield return (@"archive\legacy-platforms\sub-proj\fHashWinRtBridge\", @"archive\legacy-platforms\sub-proj\fHashWinRtBridge\");
        yield return (@"archive\legacy-platforms\sub-proj\fHashUwpNative\", @"archive\legacy-platforms\sub-proj\fHashUwpNative\");
        yield return (@"archive\legacy-platforms\sub-proj\fHashUwpShellExt\", @"archive\legacy-platforms\sub-proj\fHashUwpShellExt\");
        yield return (@"archive\legacy-platforms\sub-proj\fHashWUIShellExt\", @"archive\legacy-platforms\sub-proj\fHashWUIShellExt\");
    }

    private static Encoding DetectEncoding(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        if (bytes.Length >= 3 &&
            bytes[0] == 0xEF &&
            bytes[1] == 0xBB &&
            bytes[2] == 0xBF)
        {
            return new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
        }

        try
        {
            Encoding strictUtf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
            _ = strictUtf8.GetString(bytes);
            return strictUtf8;
        }
        catch (ArgumentException)
        {
            // Fall through to the legacy code-page reader for historical files
            // that have not yet been migrated.
        }

        return Encoding.GetEncoding(936);
    }

    private static string FindRepoRoot()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (current != null)
        {
            if (Directory.Exists(Path.Combine(current.FullName, ".git")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Unable to locate the repository root.");
    }

    private static void Run(string name, Action test, List<string> errors)
    {
        try
        {
            test();
            Console.WriteLine($"PASS: {name}");
        }
        catch (Exception ex)
        {
            errors.Add($"{name}: {ex.Message}");
            Console.WriteLine($"FAIL: {name}");
        }
    }

    private static void AssertContains(string content, string expected, string message)
    {
        if (!content.Contains(expected, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(message);
        }
    }

    private static void AssertDoesNotContain(string content, string unexpected, string message)
    {
        if (content.Contains(unexpected, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(message);
        }
    }

    private static string[] SplitCommandLine(string commandLine)
    {
        IntPtr argv = IntPtr.Zero;
        try
        {
            argv = CommandLineToArgvW(commandLine, out int argc);
            if (argv == IntPtr.Zero)
            {
                throw new InvalidOperationException("CommandLineToArgvW returned null.");
            }

            var args = new string[argc];
            for (int i = 0; i < argc; i++)
            {
                IntPtr argPtr = Marshal.ReadIntPtr(argv, i * IntPtr.Size);
                args[i] = Marshal.PtrToStringUni(argPtr) ?? string.Empty;
            }

            return args;
        }
        finally
        {
            if (argv != IntPtr.Zero)
            {
                _ = LocalFree(argv);
            }
        }
    }

    [LibraryImport("shell32.dll", EntryPoint = "CommandLineToArgvW", StringMarshalling = StringMarshalling.Utf16)]
    private static partial IntPtr CommandLineToArgvW(string commandLine, out int argc);

    [LibraryImport("kernel32.dll", SetLastError = false)]
    private static partial IntPtr LocalFree(IntPtr hMem);

    private static void AssertPngAsset(string repoRoot, string relativePath, int expectedWidth, int expectedHeight, int minBytes)
    {
        string path = ResolveRepoPath(repoRoot, relativePath);
        byte[] bytes = File.ReadAllBytes(path);
        if (bytes.Length < minBytes)
        {
            throw new InvalidOperationException($"PNG asset {relativePath} is unexpectedly small.");
        }

        byte[] pngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        if (!bytes.Take(pngSignature.Length).SequenceEqual(pngSignature))
        {
            throw new InvalidOperationException($"PNG asset {relativePath} has an invalid PNG signature.");
        }

        int width = ReadBigEndianInt32(bytes, 16);
        int height = ReadBigEndianInt32(bytes, 20);
        if (width != expectedWidth || height != expectedHeight)
        {
            throw new InvalidOperationException($"PNG asset {relativePath} has unexpected dimensions {width}x{height}.");
        }

        if (!Encoding.ASCII.GetString(bytes).Contains("IDAT", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"PNG asset {relativePath} is missing image data.");
        }
    }

    private static void AssertNonEmptyFile(string repoRoot, string relativePath)
    {
        string path = ResolveRepoPath(repoRoot, relativePath);
        var info = new FileInfo(path);
        if (!info.Exists || info.Length <= 0)
        {
            throw new InvalidOperationException($"Required file {relativePath} is missing or empty.");
        }
    }

    private static int ReadBigEndianInt32(byte[] bytes, int offset)
    {
        return (bytes[offset] << 24) |
               (bytes[offset + 1] << 16) |
               (bytes[offset + 2] << 8) |
               bytes[offset + 3];
    }

    private static string ReadHashEngineImplementation(string repoRoot)
    {
        return string.Join(
            "\r\n",
            ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngine.cpp"),
            ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngineInternal.h"),
            ReadRepoFile(repoRoot, @"trunk\source\Common\HashFileAttemptWorkflow.cpp"),
            ReadRepoFile(repoRoot, @"trunk\source\Common\HashFileAttemptStateOps.cpp"),
            ReadRepoFile(repoRoot, @"trunk\source\Common\HashSchedulerDispatch.cpp"),
            ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestExecution.cpp"),
            ReadRepoFile(repoRoot, @"trunk\source\Common\HashJobExecutionPlan.cpp"),
            ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestQueue.cpp"),
            ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestPipeline.cpp"),
            ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestSinglePass.cpp"),
            ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestUpdater.cpp"),
            ReadRepoFile(repoRoot, @"trunk\source\Common\HashProgressTracker.cpp"),
            ReadRepoFile(repoRoot, @"trunk\source\Common\HashEnginePreparation.cpp"),
            ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngineResult.cpp"),
            ReadRepoFile(repoRoot, @"trunk\source\Common\HashResultEventWorkflow.cpp"),
            ReadRepoFile(repoRoot, @"trunk\source\Common\HashResultPublisher.cpp"));
    }
}
