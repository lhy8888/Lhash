using System.Runtime.InteropServices;
using System.Text;

internal static partial class Program
{
    private static int Main()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        string repoRoot = FindRepoRoot();
        var failures = new List<string>();

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
        Run("Native compiler and linker mitigations are imported consistently", () =>
        {
            string nativeSecurityTargets = ReadRepoFile(repoRoot, @"NativeSecurity.targets");
            string vendorTargets = ReadRepoFile(repoRoot, @"NativeOpenSslVendor.targets");
            string cmakeLists = ReadRepoFile(repoRoot, @"CMakeLists.txt");
            string[] vcxProjects = Directory.GetFiles(repoRoot, "*.vcxproj", SearchOption.AllDirectories);

            AssertContains(nativeSecurityTargets, "<BufferSecurityCheck>true</BufferSecurityCheck>", "Shared native security targets do not enable /GS.");
            AssertContains(nativeSecurityTargets, "<SDLCheck>true</SDLCheck>", "Shared native security targets do not enable /sdl.");
            AssertContains(nativeSecurityTargets, "<LHashEnableControlFlowGuard>true</LHashEnableControlFlowGuard>", "Shared native security targets do not enable CFG by default.");
            AssertContains(nativeSecurityTargets, "<LHashEnableControlFlowGuard Condition=\"'$(CLRSupport)'!=''\">false</LHashEnableControlFlowGuard>", "Shared native security targets do not exempt managed CLR bridge projects from CFG.");
            AssertContains(nativeSecurityTargets, "<ControlFlowGuard Condition=\"'$(LHashEnableControlFlowGuard)'=='true'\">Guard</ControlFlowGuard>", "Shared native security targets do not enable CFG for eligible native projects.");
            AssertContains(nativeSecurityTargets, "<RandomizedBaseAddress>true</RandomizedBaseAddress>", "Shared native security targets do not enable ASLR.");
            AssertContains(nativeSecurityTargets, "<HighEntropyVA>true</HighEntropyVA>", "Shared native security targets do not enable high-entropy VA.");
            AssertContains(nativeSecurityTargets, "<DataExecutionPrevention>true</DataExecutionPrevention>", "Shared native security targets do not enable DEP.");
            AssertContains(vendorTargets, "FailNonReleaseOpenSslVendor", "Shared OpenSSL vendor targets do not reject non-Release builds.");
            AssertContains(vendorTargets, "Do not link it into non-Release builds.", "Shared OpenSSL vendor targets do not explain the Release-only restriction.");
            AssertContains(cmakeLists, "message(FATAL_ERROR", "CMake does not fail fast when OpenSSL vendor integration is enabled.");
            AssertContains(cmakeLists, "CMake OpenSSL vendor integration is not implemented yet.", "CMake does not document the unsupported OpenSSL vendor path.");
            AssertContains(cmakeLists, "Use the MSBuild Windows release workflow with LHashOpenSslInstallRoot.", "CMake does not direct users to the supported Windows vendor workflow.");
            AssertDoesNotContain(cmakeLists, "target_compile_definitions(lhash_core PRIVATE LHASH_WITH_OPENSSL3_VENDOR=1)", "CMake still wires the unsupported OpenSSL vendor compile definition instead of failing fast.");

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
            AssertContains(controller, "WindowsComm::IsWindowsVistaOrGreater()", "WinMFC context-menu controller no longer gates elevation by the modern Windows-version helper.");
            AssertContains(controller, "WindowsUtils::ElevateProcess()", "WinMFC context-menu controller no longer uses the hardened elevation helper.");
            AssertContains(controller, "WindowsUtils::RemoveContextMenu(); // Try to delete all items related to LHash", "WinMFC context-menu controller no longer performs the defensive pre-add cleanup.");
            AssertContains(controller, "WindowsUtils::AddContextMenu()", "WinMFC context-menu controller no longer uses the shared context-menu add helper.");
            AssertContains(controller, "SetStatusText(addFailedText);", "WinMFC context-menu controller no longer surfaces add failures to the UI.");
            AssertContains(controller, "SetStatusText(removeFailedText);", "WinMFC context-menu controller no longer surfaces remove failures to the UI.");
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
