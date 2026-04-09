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
            string mfcBaseStrings = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\UIStringsBase.cpp");
            string mfcZhStrings = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\UIStringsZHCN.cpp");
            string mfcRc2 = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\res\fileshash.rc2");
            string mfcRc = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\fileshash.rc");
            string fileshashProject = ReadRepoFile(repoRoot, @"trunk\fileshash.vcxproj");
            string legacyPackScript = ReadRepoFile(repoRoot, @"trunk\package_win_mfc64.py");
            string winUiEn = ReadRepoFile(repoRoot, @"trunk\source\WinUI\Strings\en-US\Resources.resw");
            string winUiZh = ReadRepoFile(repoRoot, @"trunk\source\WinUI\Strings\zh-CN\Resources.resw");
            string winUiAssembly = ReadRepoFile(repoRoot, @"trunk\source\WinUI\Properties\AssemblyInfo.cs");
            string winUiWap = ReadRepoFile(repoRoot, @"trunk\fHashWUIWap\Package.appxmanifest");
            string winUiWapDev = ReadRepoFile(repoRoot, @"trunk\fHashWUIWap\Package-DEV.appxmanifest");
            string uwpEn = ReadRepoFile(repoRoot, @"trunk\source\WinUWP\Strings\en-US\Resources.resw");
            string uwpZh = ReadRepoFile(repoRoot, @"trunk\source\WinUWP\Strings\zh-CN\Resources.resw");
            string uwpAssembly = ReadRepoFile(repoRoot, @"trunk\source\WinUWP\Properties\AssemblyInfo.cs");
            string uwpManifest = ReadRepoFile(repoRoot, @"trunk\source\WinUWP\Package.appxmanifest");
            string uwpManifestDev = ReadRepoFile(repoRoot, @"trunk\source\WinUWP\Package-DEBUG.appxmanifest");
            string uwpWap = ReadRepoFile(repoRoot, @"trunk\fHashUwpWap\Package.appxmanifest");
            string uwpWapDev = ReadRepoFile(repoRoot, @"trunk\fHashUwpWap\Package-DEBUG.appxmanifest");
            string legacyShellStrings = ReadRepoFile(repoRoot, @"sub-proj\fHashShlExt\fHashShlExtStringsBase.cpp");
            string legacyShellStringsZh = ReadRepoFile(repoRoot, @"sub-proj\fHashShlExt\fHashShlExtStringsZHCN.cpp");
            string wuiShellVerb = ReadRepoFile(repoRoot, @"sub-proj\fHashWUIShellExt\ExplorerCommandVerb.cpp");
            string wuiShellStrings = ReadRepoFile(repoRoot, @"sub-proj\fHashWUIShellExt\AppxShellExtStringsBase.cpp");
            string wuiShellStringsZh = ReadRepoFile(repoRoot, @"sub-proj\fHashWUIShellExt\AppxShellExtStringsZHCN.cpp");
            string uwpShellVerb = ReadRepoFile(repoRoot, @"sub-proj\fHashUwpShellExt\ExplorerCommandVerb.cpp");
            string uwpShellStrings = ReadRepoFile(repoRoot, @"sub-proj\fHashUwpShellExt\UwpShellExtStringsBase.cpp");
            string uwpShellStringsZh = ReadRepoFile(repoRoot, @"sub-proj\fHashUwpShellExt\UwpShellExtStringsZHCN.cpp");

            AssertContains(fileshashProject, "<ProjectName>LHash</ProjectName>", "Legacy project still exposes the old project name.");
            AssertContains(fileshashProject, "$(OutDir)$(ProjectName).exe", "Legacy project no longer emits the unified LHash.exe output.");
            AssertContains(legacyPackScript, "EXE_FILE_NAME = 'LHash.exe'", "Legacy packaging script still packages the old executable name.");
            AssertContains(legacyPackScript, "'LHash-%s-win64.zip'", "Legacy packaging script still emits the old archive name.");
            AssertContains(workflow, "LHash.exe", "CI packaging no longer looks for the renamed executable.");
            AssertContains(workflow, "LHash-legacy-x64", "CI workflow no longer packages the lightweight native desktop artifact.");
            AssertContains(workflow, "LHash-winui-preview-x64", "CI workflow no longer keeps the WinUI build available as a preview artifact.");
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

            AssertContains(winUiEn, "<value>LHash</value>", "WinUI English title still shows the old app name.");
            AssertContains(winUiEn, "<value>About LHash</value>", "WinUI English About title still shows the old app name.");
            AssertContains(winUiEn, "<value>LHash: Files Hash Calculator</value>", "WinUI English About text still shows the old product name.");
            AssertContains(winUiEn, "<value>Copyright (C) 2026- LHY.</value>", "WinUI English About text still shows the old copyright.");
            AssertContains(winUiEn, "https://github.com/lhy8888/Lhash", "WinUI English About link still points to the old GitHub repo.");
            AssertContains(winUiZh, "LHash</value>", "WinUI Chinese About title still shows the old app name.");
            AssertContains(winUiZh, "LHash: ", "WinUI Chinese About text still shows the old product name.");
            AssertContains(winUiAssembly, "AssemblyTitle(\"LHash\")", "WinUI assembly title still shows the old product name.");
            AssertContains(winUiAssembly, "AssemblyCompany(\"LHY\")", "WinUI assembly company still shows the old publisher.");
            AssertContains(winUiAssembly, "AssemblyCopyright(\"Copyright (C) 2026- LHY.\")", "WinUI assembly copyright still shows the old owner.");
            AssertContains(winUiWap, "Alias=\"LHash.exe\"", "WinUI package execution alias still points at the old executable name.");
            AssertContains(winUiWapDev, "Alias=\"LHashDev.exe\"", "WinUI dev package execution alias still points at the old executable name.");
            AssertContains(winUiWap, "<DisplayName>LHash</DisplayName>", "WinUI package display name still shows the old app name.");
            AssertContains(winUiWap, "Description=\"LHash\"", "WinUI package description still shows the old app name.");
            AssertContains(winUiWap, "<PublisherDisplayName>LHY</PublisherDisplayName>", "WinUI package publisher display name still shows the old owner.");
            AssertContains(winUiWapDev, "<DisplayName>LHash Dev</DisplayName>", "WinUI dev package display name still shows the old app name.");
            AssertContains(winUiWapDev, "Description=\"LHash Dev\"", "WinUI dev package description still shows the old app name.");
            AssertContains(winUiWapDev, "<PublisherDisplayName>LHY</PublisherDisplayName>", "WinUI dev package publisher display name still shows the old owner.");

            AssertContains(uwpEn, "<value>LHash UWP</value>", "UWP English title still shows the old app name.");
            AssertContains(uwpEn, "<value>About LHash UWP</value>", "UWP English About title still shows the old app name.");
            AssertContains(uwpEn, "<value>LHash UWP: Files Hash Calculator</value>", "UWP English About text still shows the old product name.");
            AssertContains(uwpEn, "<value>Copyright (C) 2026- LHY.</value>", "UWP English About text still shows the old copyright.");
            AssertContains(uwpEn, "https://github.com/lhy8888/Lhash", "UWP English About link still points to the old GitHub repo.");
            AssertContains(uwpZh, "LHash UWP</value>", "UWP Chinese About title still shows the old app name.");
            AssertContains(uwpZh, "LHash UWP: ", "UWP Chinese About text still shows the old product name.");
            AssertContains(uwpAssembly, "AssemblyTitle(\"LHashUwp\")", "UWP assembly title still shows the old product name.");
            AssertContains(uwpAssembly, "AssemblyCompany(\"LHY\")", "UWP assembly company still shows the old publisher.");
            AssertContains(uwpAssembly, "AssemblyCopyright(\"Copyright (C) 2026- LHY.\")", "UWP assembly copyright still shows the old owner.");
            AssertContains(uwpManifest, "<DisplayName>LHash UWP</DisplayName>", "UWP manifest display name still shows the old app name.");
            AssertContains(uwpManifest, "Description=\"LHash UWP\"", "UWP manifest description still shows the old app name.");
            AssertContains(uwpManifest, "<PublisherDisplayName>LHY</PublisherDisplayName>", "UWP manifest publisher display name still shows the old owner.");
            AssertContains(uwpManifestDev, "<DisplayName>LHash UWP Dev</DisplayName>", "UWP debug manifest display name still shows the old app name.");
            AssertContains(uwpManifestDev, "Description=\"LHash UWP Dev\"", "UWP debug manifest description still shows the old app name.");
            AssertContains(uwpManifestDev, "<PublisherDisplayName>LHY</PublisherDisplayName>", "UWP debug manifest publisher display name still shows the old owner.");
            AssertContains(uwpWap, "<DisplayName>LHash UWP</DisplayName>", "UWP WAP package display name still shows the old app name.");
            AssertContains(uwpWap, "Description=\"LHash UWP\"", "UWP WAP package description still shows the old app name.");
            AssertContains(uwpWap, "<PublisherDisplayName>LHY</PublisherDisplayName>", "UWP WAP package publisher display name still shows the old owner.");
            AssertContains(uwpWapDev, "<DisplayName>LHash UWP Dev</DisplayName>", "UWP WAP debug package display name still shows the old app name.");
            AssertContains(uwpWapDev, "Description=\"LHash UWP Dev\"", "UWP WAP debug package description still shows the old app name.");
            AssertContains(uwpWapDev, "<PublisherDisplayName>LHY</PublisherDisplayName>", "UWP WAP debug package publisher display name still shows the old owner.");

            AssertContains(legacyShellStrings, "Hash with LHash", "Legacy shell extension menu text still shows the old app name.");
            AssertContains(legacyShellStringsZh, "LHash", "Legacy shell extension Chinese menu text still shows the old app name.");
            AssertContains(wuiShellVerb, "Hash with LHash", "WinUI shell extension verb display name still shows the old app name.");
            AssertContains(wuiShellStrings, "Hash with LHash", "WinUI shell extension English menu text still shows the old app name.");
            AssertContains(wuiShellStringsZh, "LHash", "WinUI shell extension Chinese menu text still shows the old app name.");
            AssertContains(uwpShellVerb, "Hash with LHash UWP", "UWP shell extension verb display name still shows the old app name.");
            AssertContains(uwpShellStrings, "Hash with LHash UWP", "UWP shell extension English menu text still shows the old app name.");
            AssertContains(uwpShellStringsZh, "LHash UWP", "UWP shell extension Chinese menu text still shows the old app name.");

            AssertPngAsset(repoRoot, @"trunk\source\WinUI\Assets\AboutLogo.large.png", 200, 200, 512);
            AssertPngAsset(repoRoot, @"trunk\source\WinUWP\Assets\AboutLogo.large.png", 200, 200, 512);
            AssertNonEmptyFile(repoRoot, @"trunk\source\WinMFC\res\icon1.ico");
            AssertNonEmptyFile(repoRoot, @"trunk\source\WinUI\Assets\fHashWUI.ico");
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
            AssertContains(content, "szData[i] == _T('\\0')", "WM_COPYDATA validation no longer checks for null termination.");
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
            AssertContains(dialogResource, "CONTROL         \"\",IDC_TASK_LIST,\"SysListView32\",LVS_REPORT | LVS_SINGLESEL | WS_TABSTOP | WS_BORDER,8,262,608,72", "Legacy MFC task list is no longer constrained to the compact six-row viewport.");
            AssertContains(ReadRepoFile(repoRoot, @"trunk\source\WinMFC\UIStringsZHCN.cpp"), "m_stringsMap[_T(\"MAINDLG_SETTINGS_ALGORITHMS\")] = _T(\"算法选择\");", "Legacy settings menu no longer labels algorithm controls as 算法选择 in Simplified Chinese.");
            AssertContains(hyperEditHashHeader, "afx_msg void OnDropFiles(HDROP hDropInfo);", "HyperEditHash is missing the drop forwarding declaration.");
            AssertContains(hyperEditHashSource, "ON_WM_DROPFILES()", "HyperEditHash no longer subscribes to WM_DROPFILES.");
            AssertContains(hyperEditHashSource, "parentWnd->SendMessage(WM_DROPFILES, reinterpret_cast<WPARAM>(hDropInfo), 0);", "Dropped files over the enlarged result area are no longer forwarded to the main dialog.");
        }, failures);
        Run("Win32 read failures propagate as errors", () =>
        {
            string winApi = ReadRepoFile(repoRoot, @"trunk\source\OsUtils\OsFileWinApi.cpp");
            string winUwp = ReadRepoFile(repoRoot, @"trunk\source\OsUtils\OsFileWinUwp.cpp");
            string hashEngine = ReadHashEngineImplementation(repoRoot);

            AssertContains(winApi, "return -1;", "OsFileWinApi.cpp no longer returns -1 on ReadFile/WriteFile failure.");
            AssertContains(winUwp, "return -1;", "OsFileWinUwp.cpp no longer returns -1 on ReadFile/WriteFile failure.");
            AssertContains(hashEngine, "fileAttemptState->readFailed = false;", "HashEngine.cpp is missing explicit read failure tracking.");
            AssertContains(hashEngine, "RESULT_ERROR", "HashEngine.cpp is missing the read-failure error path.");
            AssertContains(hashEngine, "Failed to read file while hashing.", "HashEngine.cpp is missing the user-visible read failure message.");
        }, failures);
        Run("Shell extension hardening is present", () =>
        {
            string legacyShell = ReadRepoFile(repoRoot, @"sub-proj\fHashShlExt\fHashShellExt.cpp");
            string wuiShell = ReadRepoFile(repoRoot, @"sub-proj\fHashWUIShellExt\ExplorerCommandVerb.cpp");
            string uwpShell = ReadRepoFile(repoRoot, @"sub-proj\fHashUwpShellExt\ExplorerCommandVerb.cpp");
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
            AssertContains(shellCore, "BOOL bCreated = CreateProcess", "Shared shell-command core launch hardening is missing.");
            AssertContains(shellCore, "#include \"WinCommon/WinHandleGuard.h\"", "Shared shell-command core does not include the shared HANDLE RAII wrappers.");
            AssertContains(shellCore, "WinHandleGuard::UniqueWinHandle threadHandle(pInfo.hThread);", "Shared shell-command core does not wrap thread handles after CreateProcess.");
            AssertContains(shellCore, "WinHandleGuard::UniqueWinHandle processHandle(pInfo.hProcess);", "Shared shell-command core does not wrap process handles after CreateProcess.");
            AssertDoesNotContain(shellCore, "CloseHandle(pInfo.hThread);", "Shared shell-command core still closes thread handles manually.");
            AssertDoesNotContain(shellCore, "CloseHandle(pInfo.hProcess);", "Shared shell-command core still closes process handles manually.");
            AssertContains(shellCore, "if (pszExecName == NULL || pszPath == NULL || cchPath == 0)", "Shared shell-command core does not validate executable-path inputs.");
            AssertContains(shellCore, "if (psia == NULL || ptstrExecCmd == NULL || tstrExecPath.empty())", "Shared shell-command core does not validate command-line inputs.");
            AssertContains(wuiShell, "#include \"WinCommon/ShellExplorerCommandCore.h\"", "WinUI shell extension does not consume the shared hardened shell-command core.");
            AssertContains(wuiShell, "LaunchShellCommandLine(tstrExecPath, tstrExecCmd);", "WinUI shell extension no longer routes detached process launch through the hardened shared helper.");
            AssertContains(uwpShell, "#include \"WinCommon/ShellExplorerCommandCore.h\"", "UWP shell extension does not consume the shared hardened shell-command core.");
            AssertContains(uwpShell, "LaunchShellCommandLine(tstrExecPath, tstrExecCmd);", "UWP shell extension no longer routes detached process launch through the hardened shared helper.");
            AssertContains(mfcDialogAndInput, "DragQueryFile(hDropInfo, index, NULL, 0)", "MFC drag/drop path extraction no longer queries required buffer sizes.");
            AssertContains(windowsUtils, "WinHandleGuard::UniqueFindHandle hFind", "WindowsUtils shell-extension registration helpers do not yet wrap FindFirstFile handles in RAII.");
            AssertContains(windowsUtils, "WinHandleGuard::UniqueModuleHandle hModule", "WindowsUtils shell-extension registration helpers do not yet wrap module handles in RAII.");
            AssertDoesNotContain(windowsUtils, "FreeLibrary(hModule);", "WindowsUtils shell-extension registration helpers still release modules manually.");
            AssertContains(windowsUtils, "SetClipboardData", "Clipboard helper no longer transfers ownership safely.");
        }, failures);
        Run("Filesystem and runtime hardening is present", () =>
        {
            string osFileHeader = ReadRepoFile(repoRoot, @"trunk\source\OsUtils\OsFile.h");
            string osFileWinApi = ReadRepoFile(repoRoot, @"trunk\source\OsUtils\OsFileWinApi.cpp");
            string osFileWinUwp = ReadRepoFile(repoRoot, @"trunk\source\OsUtils\OsFileWinUwp.cpp");
            string hashEngineResult = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngineResult.cpp");
            string checkedArithmetic = ReadRepoFile(repoRoot, @"trunk\source\Common\CheckedArithmetic.h");
            string threadAccess = ReadRepoFile(repoRoot, @"trunk\source\LegacyCompat\ThreadDataExecutionAccess.h");
            string executionContext = ReadRepoFile(repoRoot, @"trunk\source\Runtime\HashExecutionContext.h");
            string progressTracker = ReadRepoFile(repoRoot, @"trunk\source\Common\HashProgressTracker.cpp");
            string digestQueue = ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestQueue.cpp");
            string md5 = ReadRepoFile(repoRoot, @"trunk\source\Algorithms\MD5.cpp");
            string sha1 = ReadRepoFile(repoRoot, @"trunk\source\Algorithms\SHA1.cpp");
            string uiBridgeHeader = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\UIBridgeMFC.h");
            string uiBridge = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\UIBridgeMFC.cpp");
            string nativeRuntimeSource = ReadRepoFile(repoRoot, @"native-runtime-tests\FHash.NativeRuntimeTests\HashEngineRuntimeTests.cpp");

            AssertContains(osFileHeader, "bool isHashTargetAllowed(void *exception = NULL);", "OsFile no longer exposes the hash-target policy hook.");
            AssertContains(osFileWinApi, "FILE_ATTRIBUTE_REPARSE_POINT", "Win32 file hashing no longer checks for reparse points.");
            AssertContains(osFileWinApi, "Refusing to hash a symbolic link, junction, mount point, or other reparse point.", "Win32 file hashing no longer rejects reparse points with an explicit message.");
            AssertContains(osFileWinApi, "HasReparsePointInPathHierarchy", "Win32 file hashing no longer walks ancestor path segments when checking for reparse points.");
            AssertContains(osFileWinApi, "if (!isHashTargetAllowed(exception))", "Win32 file hashing no longer gates file open on the target policy.");
            AssertContains(osFileWinUwp, "FILE_ATTRIBUTE_REPARSE_POINT", "UWP file hashing no longer checks for reparse points.");
            AssertContains(osFileWinUwp, "HasReparsePointInPathHierarchy", "UWP file hashing no longer walks ancestor path segments when checking for reparse points.");
            AssertContains(hashEngineResult, "result.meta.modifiedDate = osFile.getModifiedTimeFormat();", "HashEngine metadata flow no longer relies on the opened file handle.");
            AssertDoesNotContain(hashEngineResult, "GetFileAttributesEx", "HashEngine metadata flow still uses path-based attribute lookup.");

            AssertContains(checkedArithmetic, "TryAddUInt64", "Checked arithmetic helpers no longer expose checked uint64 addition.");
            AssertContains(checkedArithmetic, "TrySubtractUInt64", "Checked arithmetic helpers no longer expose checked uint64 subtraction.");
            AssertContains(checkedArithmetic, "TryMultiplyUInt64", "Checked arithmetic helpers no longer expose checked uint64 multiplication.");
            AssertContains(checkedArithmetic, "SaturatingAddUInt64", "Checked arithmetic helpers no longer expose saturating addition.");
            AssertContains(checkedArithmetic, "ReplaceSizedValueUInt64", "Checked arithmetic helpers no longer expose bounded replace arithmetic.");
            AssertContains(executionContext, "class NullHashProgressSink : public HashProgressSink", "HashExecutionContext no longer exposes a null-object progress sink.");
            AssertContains(executionContext, "HashProgressSink& progressSinkObserver;", "HashExecutionContext no longer models the progress sink as a non-owning observer reference.");
            AssertContains(executionContext, "sink != NULL ? *sink : GetNullHashProgressSink()", "HashExecutionContext no longer provides a null-safe progress sink fallback.");
            AssertDoesNotContain(executionContext, "HashProgressSink *progressSink;", "HashExecutionContext regressed to a raw stored progress sink pointer.");
            AssertContains(executionContext, "SaturatingAddUInt64", "HashExecutionContext no longer uses saturating size accounting.");
            AssertContains(executionContext, "ReplaceSizedValueUInt64", "HashExecutionContext no longer uses checked replace arithmetic.");
            AssertContains(threadAccess, "SaturatingAddUInt64", "Legacy ThreadData size accounting no longer uses saturating arithmetic.");
            AssertContains(threadAccess, "ReplaceSizedValueUInt64", "Legacy ThreadData replacement accounting no longer uses checked arithmetic.");
            AssertContains(progressTracker, "CalculateBoundedProgressValue", "Progress tracking no longer uses bounded progress calculations.");
            AssertContains(digestQueue, "SaturatingAddUInt64(fileSize, static_cast<uint64_t>(bufferLength) - 1)", "Digest queue sizing no longer uses saturating chunk arithmetic.");

            AssertContains(md5, "static const unsigned char PADDING[64]", "MD5 padding is no longer held as immutable shared algorithm state.");
            AssertDoesNotContain(sha1, "static unsigned char workspace[64];", "SHA1 still shares mutable static workspace across concurrent runs.");
            AssertContains(sha1, "unsigned char workspace[64];", "SHA1 no longer uses stack-local transform workspace.");
            AssertContains(uiBridgeHeader, "struct ProgressDispatchState", "MFC UI bridge no longer exposes a throttled progress dispatch state.");
            AssertContains(uiBridge, "kUiProgressDispatchIntervalMs = 80", "MFC UI bridge no longer throttles progress dispatch.");
            AssertContains(uiBridge, "ShouldPostProgressValue(m_totalProgressDispatchState, value)", "MFC UI bridge no longer gates total-progress posts through the throttling helper.");
            AssertContains(nativeRuntimeSource, "HashThreadFunc_ProducesConsistentDigestsAcrossConcurrentRuns", "Native runtime tests no longer cover concurrent digest consistency.");
            AssertContains(nativeRuntimeSource, "std::async(std::launch::async, runSingleRequest)", "Native runtime tests no longer exercise concurrent hashing via async tasks.");
        }, failures);
        Run("BLAKE3 provider and descriptor variants stay covered by hardening gates", () =>
        {
            string registryCore = ReadRepoFile(repoRoot, @"trunk\source\Domain\HashAlgorithmRegistryCore.h");
            string providerHeader = ReadRepoFile(repoRoot, @"trunk\source\Runtime\Hash\BLAKE3HashProvider.h");
            string providerImplementation = ReadRepoFile(repoRoot, @"trunk\source\Runtime\Hash\BLAKE3HashProvider.cpp");
            string runtimeSource = ReadRepoFile(repoRoot, @"native-runtime-tests\FHash.NativeRuntimeTests\HashEngineRuntimeTests.cpp");
            string securityRuntimeSource = ReadRepoFile(repoRoot, @"native-runtime-tests\FHash.NativeRuntimeTests\HashEngineSecurityRuntimeTests.cpp");
            string nativeCoreProject = ReadRepoFile(repoRoot, @"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj");
            string uwpNativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashUwpNative\fHashUwpNative.vcxproj");
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
            AssertContains(nativeCoreProject, "Condition=\"'$(Platform)'=='Win32'\">/arch:SSE2", "Desktop native core no longer enables the Win32 SSE2 BLAKE3 path.");
            AssertContains(nativeCoreProject, "Condition=\"'$(Platform)'=='Win32'\">/arch:AVX", "Desktop native core no longer enables the Win32 SSE4.1-compatible BLAKE3 path.");
            AssertContains(nativeCoreProject, "/arch:AVX2", "Desktop native core no longer enables AVX2 for the dedicated BLAKE3 translation unit.");
            AssertContains(nativeCoreProject, "/arch:AVX512", "Desktop native core no longer enables AVX512 for the dedicated BLAKE3 translation unit.");
            AssertContains(nativeCoreProject, "Condition=\"'$(Platform)'=='Win32'\">BLAKE3_USE_NEON=0;BLAKE3_NO_AVX512;%(PreprocessorDefinitions)</PreprocessorDefinitions>", "Desktop native core no longer keeps Win32 BLAKE3 on the x86 SIMD plus AVX-512-disabled path.");
            AssertContains(uwpNativeProject, @"blake3_sse2.c", "UWP native core no longer compiles the BLAKE3 SSE2 implementation for x64.");
            AssertContains(uwpNativeProject, @"blake3_sse41.c", "UWP native core no longer compiles the BLAKE3 SSE4.1 implementation for x64.");
            AssertContains(uwpNativeProject, @"blake3_avx2.c", "UWP native core no longer compiles the BLAKE3 AVX2 implementation for x64.");
            AssertContains(uwpNativeProject, @"blake3_avx512.c", "UWP native core no longer compiles the BLAKE3 AVX512 implementation for x64.");
            AssertContains(uwpNativeProject, @"blake3_neon.c", "UWP native core no longer compiles the BLAKE3 NEON implementation for ARM64.");
            AssertContains(uwpNativeProject, "Condition=\"'$(Platform)'=='Win32'\">/arch:SSE2", "UWP native core no longer enables the Win32 SSE2 BLAKE3 path.");
            AssertContains(uwpNativeProject, "/arch:AVX2", "UWP native core no longer enables AVX2 for the dedicated BLAKE3 translation unit.");
            AssertContains(uwpNativeProject, "/arch:AVX512", "UWP native core no longer enables AVX512 for the dedicated BLAKE3 translation unit.");
            AssertContains(uwpNativeProject, "Condition=\"'$(Platform)'=='ARM64'\">BLAKE3_USE_NEON=1;BLAKE3_NO_SSE2;BLAKE3_NO_SSE41;BLAKE3_NO_AVX2;BLAKE3_NO_AVX512;%(PreprocessorDefinitions)</PreprocessorDefinitions>", "UWP native core no longer enables the ARM64 NEON BLAKE3 path.");
            AssertContains(securityHarness, "TestJunctionAncestorAttackSurface", "Security regression no longer runs the junction ancestor attack harness.");
            AssertContains(securityHarness, "TestHashStyleOpenSharingViolation", "Security regression no longer runs the sharing-violation harness.");
            AssertContains(securityHarness, "CreateDirectoryJunction", "Security regression no longer builds the runtime junction attack harness.");
            AssertContains(securityHarness, "ERROR_SHARING_VIOLATION = 32", "Security regression no longer validates sharing-violation attack semantics.");
        }, failures);
        Run("DLL search path hardening is present", () =>
        {
            string filesHashApp = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHash.cpp");
            string windowsUtilsHeader = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\WindowsUtils.h");
            string windowsUtils = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\WindowsUtils.cpp");
            string winUiProgram = ReadRepoFile(repoRoot, @"trunk\source\WinUI\Program.cs");
            string winUiWin32Helper = ReadRepoFile(repoRoot, @"trunk\source\WinUI\Win32Helper.cs");
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
            AssertContains(ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashProgressController.cpp"), "taskRowState.state == FILES_HASH_TASK_PENDING ||", "Legacy task list no longer keeps older completed rows stable while updating current rows.");
            AssertContains(ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashProgressController.cpp"), "m_taskListCtrl->EnsureVisible(rowIndex, FALSE);", "Legacy task list no longer keeps the active row visible inside the compact viewport.");
        }, failures);
        Run("Native compiler and linker mitigations are imported consistently", () =>
        {
            string nativeSecurityTargets = ReadRepoFile(repoRoot, @"NativeSecurity.targets");
            string[] vcxProjects = Directory.GetFiles(repoRoot, "*.vcxproj", SearchOption.AllDirectories);

            AssertContains(nativeSecurityTargets, "<BufferSecurityCheck>true</BufferSecurityCheck>", "Shared native security targets do not enable /GS.");
            AssertContains(nativeSecurityTargets, "<SDLCheck>true</SDLCheck>", "Shared native security targets do not enable /sdl.");
            AssertContains(nativeSecurityTargets, "<FHashEnableControlFlowGuard>true</FHashEnableControlFlowGuard>", "Shared native security targets do not enable CFG by default.");
            AssertContains(nativeSecurityTargets, "<FHashEnableControlFlowGuard Condition=\"'$(CLRSupport)'!=''\">false</FHashEnableControlFlowGuard>", "Shared native security targets do not exempt managed CLR bridge projects from CFG.");
            AssertContains(nativeSecurityTargets, "<ControlFlowGuard Condition=\"'$(FHashEnableControlFlowGuard)'=='true'\">Guard</ControlFlowGuard>", "Shared native security targets do not enable CFG for eligible native projects.");
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
        Run("WinMFC context-menu controller preserves elevation and context-menu safety flow", () =>
        {
            string dialog = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashDlg.cpp");
            string controller = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashContextMenuController.cpp");

            AssertContains(dialog, "m_hashContextMenuController.HandleButtonClick(", "WinMFC dialog no longer routes context-menu clicks through the dedicated controller.");
            AssertContains(controller, "WindowsComm::GetWindowsVersion(osvi, bOsVersionInfoEx)", "WinMFC context-menu controller no longer gates elevation by Windows version.");
            AssertContains(controller, "osvi.dwMajorVersion >= 6", "WinMFC context-menu controller no longer restricts elevation to Vista-or-newer Windows versions.");
            AssertContains(controller, "WindowsUtils::ElevateProcess()", "WinMFC context-menu controller no longer uses the hardened elevation helper.");
            AssertContains(controller, "WindowsUtils::RemoveContextMenu(); // Try to delete all items related to fHash", "WinMFC context-menu controller no longer performs the defensive pre-add cleanup.");
            AssertContains(controller, "WindowsUtils::AddContextMenu()", "WinMFC context-menu controller no longer uses the shared context-menu add helper.");
            AssertContains(controller, "SetStatusText(addFailedText);", "WinMFC context-menu controller no longer surfaces add failures to the UI.");
            AssertContains(controller, "SetStatusText(removeFailedText);", "WinMFC context-menu controller no longer surfaces remove failures to the UI.");
        }, failures);
        Run("UWP and WinUI attack surface stays minimal", () =>
        {
            string uwpManifest = ReadRepoFile(repoRoot, @"trunk\source\WinUWP\Package.appxmanifest");
            string uwpHelper = ReadRepoFile(repoRoot, @"trunk\source\WinUWP\UwpHelper.cs");
            string uwpMainPage = ReadRepoFile(repoRoot, @"trunk\source\WinUWP\MainPage.xaml.cs");
            string uwpEn = ReadRepoFile(repoRoot, @"trunk\source\WinUWP\Strings\en-US\Resources.resw");
            string uwpZh = ReadRepoFile(repoRoot, @"trunk\source\WinUWP\Strings\zh-CN\Resources.resw");
            string winUiProject = ReadRepoFile(repoRoot, @"trunk\source\WinUI\fHashWUI.csproj");
            string winUiHelper = ReadRepoFile(repoRoot, @"trunk\source\WinUI\WinUIHelper.cs");
            string winUiMainPage = ReadRepoFile(repoRoot, @"trunk\source\WinUI\MainPage.xaml.cs");
            string winUiEn = ReadRepoFile(repoRoot, @"trunk\source\WinUI\Strings\en-US\Resources.resw");
            string winUiZh = ReadRepoFile(repoRoot, @"trunk\source\WinUI\Strings\zh-CN\Resources.resw");
            string winMfcDlg = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashDlg.cpp");
            string winMfcRes = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\fileshash.rc");
            string winMfcBaseStrings = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\UIStringsBase.cpp");
            string winMfcZhStrings = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\UIStringsZHCN.cpp");

            AssertDoesNotContain(uwpManifest, "internetClient", "UWP manifest still requests Internet client capability.");
            AssertDoesNotContain(uwpManifest, "privateNetworkClientServer", "UWP manifest still requests private network capability.");
            AssertContains(uwpHelper, "Launcher.LaunchUriAsync", "UWP URL launching path is missing.");
            AssertDoesNotContain(uwpHelper, "HttpClient", "UWP helper unexpectedly added an in-app HTTP client.");
            AssertDoesNotContain(winUiProject, "Microsoft.Web.WebView2", "WinUI project still carries an explicit WebView2 package reference.");
            AssertContains(winUiHelper, "Launcher.LaunchUriAsync", "WinUI URL launching path is missing.");
            AssertDoesNotContain(uwpMainPage, "MenuItemGoogle", "UWP UI still exposes a Google hash-search action.");
            AssertDoesNotContain(uwpMainPage, "MenuItemVirusTotal", "UWP UI still exposes a VirusTotal hash-search action.");
            AssertDoesNotContain(uwpEn, "Search Google", "UWP English resources still advertise Google hash search.");
            AssertDoesNotContain(uwpEn, "Search VirusTotal", "UWP English resources still advertise VirusTotal hash search.");
            AssertDoesNotContain(uwpZh, "闂佺懓鍚嬬划搴ㄥ磼?Google", "UWP Chinese resources still advertise Google hash search.");
            AssertDoesNotContain(uwpZh, "闂佺懓鍚嬬划搴ㄥ磼?VirusTotal", "UWP Chinese resources still advertise VirusTotal hash search.");
            AssertDoesNotContain(winUiMainPage, "MenuItemGoogle", "WinUI UI still exposes a Google hash-search action.");
            AssertDoesNotContain(winUiMainPage, "MenuItemVirusTotal", "WinUI UI still exposes a VirusTotal hash-search action.");
            AssertDoesNotContain(winUiEn, "Search Google", "WinUI English resources still advertise Google hash search.");
            AssertDoesNotContain(winUiEn, "Search VirusTotal", "WinUI English resources still advertise VirusTotal hash search.");
            AssertDoesNotContain(winUiZh, "闂佺懓鍚嬬划搴ㄥ磼?Google", "WinUI Chinese resources still advertise Google hash search.");
            AssertDoesNotContain(winUiZh, "闂佺懓鍚嬬划搴ㄥ磼?VirusTotal", "WinUI Chinese resources still advertise VirusTotal hash search.");
            AssertDoesNotContain(winMfcDlg, "Searchgoogle", "WinMFC dialog still exposes a Google hash-search command.");
            AssertDoesNotContain(winMfcDlg, "Searchvirustotal", "WinMFC dialog still exposes a VirusTotal hash-search command.");
            AssertDoesNotContain(winMfcRes, "[Search Google]", "WinMFC menu resources still expose Google hash search.");
            AssertDoesNotContain(winMfcRes, "[Search VirusTotal]", "WinMFC menu resources still expose VirusTotal hash search.");
            AssertDoesNotContain(winMfcBaseStrings, "Search Google", "WinMFC English strings still advertise Google hash search.");
            AssertDoesNotContain(winMfcBaseStrings, "Search VirusTotal", "WinMFC English strings still advertise VirusTotal hash search.");
            AssertDoesNotContain(winMfcZhStrings, "闂佺懓鍚嬬划搴ㄥ磼?Google", "WinMFC Chinese strings still advertise Google hash search.");
            AssertDoesNotContain(winMfcZhStrings, "闂佺懓鍚嬬划搴ㄥ磼?VirusTotal", "WinMFC Chinese strings still advertise VirusTotal hash search.");
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
        string path = Path.Combine(repoRoot, relativePath);
        return File.ReadAllText(path, DetectEncoding(path));
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
        string path = Path.Combine(repoRoot, relativePath);
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
        string path = Path.Combine(repoRoot, relativePath);
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
