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
            AssertContains(workflow, "LHash-legacy-x64", "CI artifact naming no longer uses the LHash bundle name.");
            AssertDoesNotContain(workflow, "fHash-legacy-x64", "CI artifact naming still references the old fHash bundle name.");
            AssertDoesNotContain(workflow, "fHash64.exe", "CI packaging still searches for the legacy fHash64.exe output.");

            AssertContains(mfcRc, "IDD_MAIN_DIALOG DIALOGEX 0, 0, 624, 399", "Legacy MFC main dialog is no longer using the requested larger default size.");
            AssertContains(mfcRc, "EDITTEXT        IDE_TXTMAIN,7,8,610,283", "Legacy MFC result text area no longer matches the current SHA512-friendly layout.");
            AssertContains(mfcRc, "DEFPUSHBUTTON   \"BUTTON_OPEN\",IDC_OPEN,511,295,106,18", "Legacy MFC open button no longer matches the latest main window layout.");
            AssertContains(mfcRc, "PUSHBUTTON      \"BUTTON_CLEAN\",IDC_CLEAN,511,333,106,18", "Legacy MFC clear button is no longer aligned with the open and exit buttons.");
            AssertContains(mfcRc, "LTEXT           \"UPPER_HASH\",IDC_STATIC_UPPER,8,315,70,8,SS_NOTIFY", "Legacy MFC uppercase hash label no longer matches the tightened checkbox layout.");
            AssertContains(mfcRc, "CONTROL         \"\",IDC_CHECKUP,\"Button\",BS_AUTOCHECKBOX | WS_TABSTOP,80,314,11,10", "Legacy MFC uppercase checkbox is no longer positioned close to its label.");

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
            AssertContains(winUiAssembly, "AssemblyTitle(\"LHashWUI\")", "WinUI assembly title still shows the old product name.");
            AssertContains(winUiAssembly, "AssemblyCompany(\"LHY\")", "WinUI assembly company still shows the old publisher.");
            AssertContains(winUiAssembly, "AssemblyCopyright(\"Copyright (C) 2026- LHY.\")", "WinUI assembly copyright still shows the old owner.");
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
            AssertContains(dialogAndInitialization, "btnFind->ShowWindow(SW_HIDE);", "Legacy MFC verify button is still visible in the main dialog.");
            AssertContains(dialogAndSession, "ChangeWindowMessageFilterEx", "Elevated drag-and-drop compatibility handling is missing.");
            AssertContains(dialogAndSession, "ChangeWindowMessageFilter", "Legacy message-filter compatibility fallback is missing.");
            AssertContains(dialogAndSession, "AllowMessageForWindow(pWnd->GetSafeHwnd(), WM_DROPFILES);", "WM_DROPFILES is no longer allowed through the window message filter.");
            AssertContains(dialogAndSession, "AllowMessageForWindow(pWnd->GetSafeHwnd(), WM_COPYDATA);", "WM_COPYDATA is no longer allowed through the window message filter.");
            AssertContains(dialogAndSession, "AllowMessageForWindow(pWnd->GetSafeHwnd(), 0x0049);", "WM_COPYGLOBALDATA is no longer allowed through the window message filter.");
            AssertDoesNotContain(dialogAndSession, "PCHANGEFILTERSTRUCT", "Drag-and-drop compatibility still depends on SDK-specific ChangeWindowMessageFilterEx declarations.");
            AssertContains(dialogResource, "PUSHBUTTON      \"BUTTON_FIND\",IDC_FIND,0,0,0,0", "Legacy MFC verify button still consumes visible layout space.");
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
            AssertContains(legacyShell, "CloseHandle(pInfo.hThread);", "Legacy shell extension does not close thread handles after CreateProcess.");
            AssertContains(legacyShell, "CloseHandle(pInfo.hProcess);", "Legacy shell extension does not close process handles after CreateProcess.");
            AssertContains(legacyShell, "CopyDraggedPath", "Legacy shell extension still relies on fixed-size drag/drop buffers.");
            AssertContains(legacyShell, "DragQueryFile(hDrop, index, NULL, 0)", "Legacy shell extension no longer queries drag/drop path lengths before copying.");
            AssertContains(shellCore, "BOOL bCreated = CreateProcess", "Shared shell-command core launch hardening is missing.");
            AssertContains(shellCore, "CloseHandle(pInfo.hThread);", "Shared shell-command core does not close thread handles.");
            AssertContains(shellCore, "CloseHandle(pInfo.hProcess);", "Shared shell-command core does not close process handles.");
            AssertContains(shellCore, "if (pszExecName == NULL || pszPath == NULL || cchPath == 0)", "Shared shell-command core does not validate executable-path inputs.");
            AssertContains(shellCore, "if (psia == NULL || ptstrExecCmd == NULL || tstrExecPath.empty())", "Shared shell-command core does not validate command-line inputs.");
            AssertContains(wuiShell, "#include \"WinCommon/ShellExplorerCommandCore.h\"", "WinUI shell extension does not consume the shared hardened shell-command core.");
            AssertContains(wuiShell, "LaunchShellCommandLine(tstrExecPath, tstrExecCmd);", "WinUI shell extension no longer routes detached process launch through the hardened shared helper.");
            AssertContains(uwpShell, "#include \"WinCommon/ShellExplorerCommandCore.h\"", "UWP shell extension does not consume the shared hardened shell-command core.");
            AssertContains(uwpShell, "LaunchShellCommandLine(tstrExecPath, tstrExecCmd);", "UWP shell extension no longer routes detached process launch through the hardened shared helper.");
            AssertContains(mfcDialogAndInput, "DragQueryFile(hDropInfo, index, NULL, 0)", "MFC drag/drop path extraction no longer queries required buffer sizes.");
            AssertContains(windowsUtils, "FreeLibrary(hModule);", "WindowsUtils shell-extension registration helpers still leak module handles.");
            AssertContains(windowsUtils, "SetClipboardData", "Clipboard helper no longer transfers ownership safely.");
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
            ReadRepoFile(repoRoot, @"trunk\source\Common\HashSchedulerDispatch.cpp"),
            ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestExecution.cpp"),
            ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestQueue.cpp"),
            ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestPipeline.cpp"),
            ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestSinglePass.cpp"),
            ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestUpdater.cpp"),
            ReadRepoFile(repoRoot, @"trunk\source\Common\HashProgressTracker.cpp"),
            ReadRepoFile(repoRoot, @"trunk\source\Common\HashEnginePreparation.cpp"),
            ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngineResult.cpp"),
            ReadRepoFile(repoRoot, @"trunk\source\Common\HashResultPublisher.cpp"));
    }
}
