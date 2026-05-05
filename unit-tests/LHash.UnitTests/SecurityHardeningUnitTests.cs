namespace LHash.UnitTests;

public sealed class SecurityHardeningUnitTests
{
    [Fact]
    public void FileOpenPaths_RejectReparsePoints_AndReuseOpenedHandleMetadata()
    {
        string osFile = RepositoryTestContext.ReadTextFile(@"trunk\source\OsUtils\OsFile.h");
        string osFilePosixDarwin = RepositoryTestContext.ReadTextFile(@"trunk\source\OsUtils\OsFilePosixDarwin.cpp");
        string winApi = RepositoryTestContext.ReadTextFile(@"trunk\source\OsUtils\OsFileWinApi.cpp");
        string engineResult = RepositoryTestContext.ReadTextFile(@"trunk\source\Common\HashEngineResult.cpp");

        Assert.Contains("bool isHashTargetAllowed(void *exception = NULL);", osFile, StringComparison.Ordinal);
        Assert.False(File.Exists(Path.Combine(RepositoryTestContext.RepoRoot, @"trunk\source\OsUtils\OsFileWinAfx.cpp")));
        Assert.False(File.Exists(Path.Combine(RepositoryTestContext.RepoRoot, @"trunk\source\OsUtils\OsFileWinUwp.cpp")));

        Assert.Contains("static const int kNoFollowFlag = O_NOFOLLOW;", osFilePosixDarwin, StringComparison.Ordinal);
        Assert.Contains("static bool TryGetPathStatus(const std::string& filePath, bool allowMissingPath, struct stat *fileStatus, bool *pathExists)", osFilePosixDarwin, StringComparison.Ordinal);
        Assert.Contains("static bool TryValidatePathPolicy(const std::string& filePath, bool allowMissingPath, struct stat *pathStatus, bool *pathExists, char *errorBuffer)", osFilePosixDarwin, StringComparison.Ordinal);
        Assert.Contains("if (lstat(filePath.c_str(), &pathStatus) != 0)", osFilePosixDarwin, StringComparison.Ordinal);
        Assert.Contains("if (IsSymbolicLink(fileStatus))", osFilePosixDarwin, StringComparison.Ordinal);
        Assert.Contains("Refusing to hash a symbolic link.", osFilePosixDarwin, StringComparison.Ordinal);
        Assert.Contains("if (IsOpenModeCreate(posixFlag))", osFilePosixDarwin, StringComparison.Ordinal);
        Assert.Contains("openFlags = posixFlag | kNoFollowFlag", osFilePosixDarwin, StringComparison.Ordinal);
        Assert.Contains("ValidateOpenedHandleAgainstPathPolicy(*fd, strFilePath, pathStatus, pathExists, pFileExc)", osFilePosixDarwin, StringComparison.Ordinal);
        Assert.Contains("errno == ELOOP", osFilePosixDarwin, StringComparison.Ordinal);
        Assert.Contains("if (fd == NULL || *fd == -1)", osFilePosixDarwin, StringComparison.Ordinal);
        Assert.Contains("if (TryGetCurrentFileStatus(fd, strFilePath, &st))", osFilePosixDarwin, StringComparison.Ordinal);
        Assert.Contains("if (fstat(fileHandle, &openedStatus) != 0)", osFilePosixDarwin, StringComparison.Ordinal);
        Assert.Contains("if (!IsRegularFile(openedStatus))", osFilePosixDarwin, StringComparison.Ordinal);
        Assert.DoesNotContain("Open first, we don't check here.", osFilePosixDarwin, StringComparison.Ordinal);
        Assert.DoesNotContain("if ((statRet = stat(strFilePath.c_str(), &st)) == 0", osFilePosixDarwin, StringComparison.Ordinal);
        Assert.DoesNotContain("if (stat(strFilePath.c_str(), &st) == 0)", osFilePosixDarwin, StringComparison.Ordinal);

        Assert.Contains("FILE_ATTRIBUTE_REPARSE_POINT", winApi, StringComparison.Ordinal);
        Assert.Contains("Refusing to hash a symbolic link, junction, mount point, or other reparse point.", winApi, StringComparison.Ordinal);
        Assert.Contains("HasReparsePointInPathHierarchy", winApi, StringComparison.Ordinal);
        Assert.Contains("PathSegmentHasReparsePoint", winApi, StringComparison.Ordinal);
        Assert.Contains("if (!TryLongPathFix(_filePath, &fixedPath, pFileExc))", winApi, StringComparison.Ordinal);
        Assert.Contains("if (TryRejectReparsePointPath(fixedPath, pFileExc))", winApi, StringComparison.Ordinal);
        Assert.Contains("FILE_FLAG_OPEN_REPARSE_POINT", winApi, StringComparison.Ordinal);
        Assert.Contains("GetFileInformationByHandleEx(", winApi, StringComparison.Ordinal);
        Assert.Contains("GetFinalPathNameByHandle(", winApi, StringComparison.Ordinal);
        Assert.Contains("Cannot verify the final opened file path. Refusing to hash.", winApi, StringComparison.Ordinal);
        Assert.Contains("kWindowsMaxExtendedPath = 32767", winApi, StringComparison.Ordinal);
        Assert.Contains("ERROR_FILENAME_EXCED_RANGE", winApi, StringComparison.Ordinal);
        Assert.DoesNotContain("CreateFileFromAppW", winApi, StringComparison.Ordinal);
        Assert.DoesNotContain("LHASH_UWP_LIB", winApi, StringComparison.Ordinal);
        Assert.DoesNotContain("LHASH_WUI_LIB", winApi, StringComparison.Ordinal);

        Assert.Contains("result.meta.modifiedDate = osFile.getModifiedTimeFormat();", engineResult, StringComparison.Ordinal);
        Assert.DoesNotContain("GetFileAttributesEx", engineResult, StringComparison.Ordinal);
    }

    [Fact]
    public void HandleOwnership_UsesRaiiAcrossWorkerAndShellPaths()
    {
        string handleGuard = RepositoryTestContext.ReadTextFile(@"trunk\source\WinCommon\WinHandleGuard.h");
        string threadLaunch = RepositoryTestContext.ReadTextFile(@"trunk\source\Adapters\ThreadDataBridge\HashThreadLaunch.h");
        string sessionController = RepositoryTestContext.ReadTextFile(@"trunk\source\WinMFC\FilesHashSessionController.h");
        string shellCore = RepositoryTestContext.ReadTextFile(@"trunk\source\WinCommon\ShellExplorerCommandCore.h");
        string legacyShell = RepositoryTestContext.ReadTextFile(@"sub-proj\LHashShlExt\LHashShellExt.cpp");
        string windowsUtils = RepositoryTestContext.ReadTextFile(@"trunk\source\WinMFC\WindowsUtils.cpp");

        Assert.Contains("typedef UniqueHandleBase<HANDLE, HandleCloseTraits> UniqueWinHandle;", handleGuard, StringComparison.Ordinal);
        Assert.Contains("typedef UniqueHandleBase<HANDLE, FindHandleCloseTraits> UniqueFindHandle;", handleGuard, StringComparison.Ordinal);
        Assert.Contains("typedef UniqueHandleBase<HMODULE, ModuleCloseTraits> UniqueModuleHandle;", handleGuard, StringComparison.Ordinal);

        Assert.Contains("static inline WinHandleGuard::UniqueWinHandle StartHashWorkerThread", threadLaunch, StringComparison.Ordinal);
        Assert.Contains("CloseHashWorkerThreadHandle(WinHandleGuard::UniqueWinHandle *threadHandle)", threadLaunch, StringComparison.Ordinal);
        Assert.Contains("RestartHashWorkerThread(WinHandleGuard::UniqueWinHandle *existingThreadHandle", threadLaunch, StringComparison.Ordinal);

        Assert.Contains("WinHandleGuard::UniqueWinHandle m_hWorkThread;", sessionController, StringComparison.Ordinal);
        Assert.Contains("#include \"WinCommon/WinHandleGuard.h\"", shellCore, StringComparison.Ordinal);
        Assert.Contains("WinHandleGuard::UniqueWinHandle threadHandle(pInfo.hThread);", shellCore, StringComparison.Ordinal);
        Assert.Contains("WinHandleGuard::UniqueWinHandle processHandle(pInfo.hProcess);", shellCore, StringComparison.Ordinal);
        Assert.Contains("0, 0, FALSE,", shellCore, StringComparison.Ordinal);
        Assert.DoesNotContain("0, 0, TRUE,", shellCore, StringComparison.Ordinal);

        Assert.Contains("#include \"WinCommon/WinHandleGuard.h\"", legacyShell, StringComparison.Ordinal);
        Assert.Contains("WinHandleGuard::UniqueWinHandle threadHandle(pInfo.hThread);", legacyShell, StringComparison.Ordinal);
        Assert.Contains("WinHandleGuard::UniqueWinHandle processHandle(pInfo.hProcess);", legacyShell, StringComparison.Ordinal);
        Assert.Contains("WinHandleGuard::UniqueWinHandle hProcLHash(OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, FALSE, dwPidLHash));", legacyShell, StringComparison.Ordinal);
        Assert.Contains("TCHAR szPath[MAX_PATH + 1] = {};", legacyShell, StringComparison.Ordinal);
        Assert.Contains("ULONG nChars = static_cast<ULONG>(_countof(szPath));", legacyShell, StringComparison.Ordinal);
        Assert.Contains("std::vector<TCHAR> cmdBuffer(cmdLen, static_cast<TCHAR>(0));", legacyShell, StringComparison.Ordinal);
        Assert.Contains("CreateProcess(tstrLHashPath.c_str(), cmdBuffer.data(),", legacyShell, StringComparison.Ordinal);
        Assert.DoesNotContain("new TCHAR[cmdLen]", legacyShell, StringComparison.Ordinal);
        Assert.DoesNotContain("memset(pszCmd, 0, cmdLen);", legacyShell, StringComparison.Ordinal);
        Assert.DoesNotContain("delete [] pszCmd;", legacyShell, StringComparison.Ordinal);
        Assert.DoesNotContain("TCHAR szPath[MAX_PATH + 1] = { L'0' };", legacyShell, StringComparison.Ordinal);
        Assert.DoesNotContain("ULONG nChars = MAX_PATH;", legacyShell, StringComparison.Ordinal);
        Assert.Contains("_tcsicmp(tstrProcLHashPath.c_str(), m_LHashPath.c_str()) == 0", legacyShell, StringComparison.Ordinal);
        Assert.DoesNotContain("tstrProcLHashPath == m_LHashPath", legacyShell, StringComparison.Ordinal);
        Assert.Contains("0, 0, FALSE,", legacyShell, StringComparison.Ordinal);
        Assert.DoesNotContain("0, 0, TRUE,", legacyShell, StringComparison.Ordinal);

        Assert.Contains("#include \"WinCommon/WinHandleGuard.h\"", windowsUtils, StringComparison.Ordinal);
        Assert.Contains("WinHandleGuard::UniqueFindHandle hFind", windowsUtils, StringComparison.Ordinal);
        Assert.Contains("WinHandleGuard::UniqueModuleHandle hModule", windowsUtils, StringComparison.Ordinal);
        Assert.DoesNotContain("FreeLibrary(hModule);", windowsUtils, StringComparison.Ordinal);
        Assert.Contains("return LoadLibraryEx(pszDllPath, NULL, LOAD_WITH_ALTERED_SEARCH_PATH);", windowsUtils, StringComparison.Ordinal);
        Assert.DoesNotContain("return LoadLibrary(pszDllPath);", windowsUtils, StringComparison.Ordinal);
        Assert.Contains("bool IsAcceptableContextMenuDeleteResult(LONG deleteResult)", windowsUtils, StringComparison.Ordinal);
        Assert.Contains("deleteResult == ERROR_SUCCESS || deleteResult == ERROR_FILE_NOT_FOUND", windowsUtils, StringComparison.Ordinal);
        Assert.Contains("deleteSucceeded = deleteSucceeded && IsAcceptableContextMenuDeleteResult(keyShell.RecurseDeleteKey(CONTEXT_MENU_ITEM_EN_US));", windowsUtils, StringComparison.Ordinal);
        Assert.Contains("deleteSucceeded = deleteSucceeded && IsAcceptableContextMenuDeleteResult(keyShellEx.RecurseDeleteKey(_T(\"LHashShellExt\")));", windowsUtils, StringComparison.Ordinal);
        Assert.DoesNotContain("lResult &= keyShell.RecurseDeleteKey", windowsUtils, StringComparison.Ordinal);
        Assert.DoesNotContain("lResult &= keyShellEx.RecurseDeleteKey", windowsUtils, StringComparison.Ordinal);
        Assert.Contains("GetNativeSystemInfo(&systemInfo);", windowsUtils, StringComparison.Ordinal);
        Assert.Contains("case PROCESSOR_ARCHITECTURE_ARM64:", windowsUtils, StringComparison.Ordinal);
        Assert.DoesNotContain("QueryStringValue(lpszArchKeyName", windowsUtils, StringComparison.Ordinal);
    }

    [Fact]
    public void RuntimeUses_CheckedArithmetic_ForSizesAndProgress()
    {
        string global = RepositoryTestContext.ReadTextFile(@"trunk\source\Common\HashTypes.h");
        string checkedArithmetic = RepositoryTestContext.ReadTextFile(@"trunk\source\Common\CheckedArithmetic.h");
        string executionContext = RepositoryTestContext.ReadTextFile(@"trunk\source\Runtime\HashExecutionContext.h");
        string threadAccess = RepositoryTestContext.ReadTextFile(@"trunk\source\Adapters\ThreadDataBridge\ThreadDataExecutionAccess.h");
        string progressTracker = RepositoryTestContext.ReadTextFile(@"trunk\source\Common\HashProgressTracker.cpp");
        string digestQueue = RepositoryTestContext.ReadTextFile(@"trunk\source\Common\HashDigestQueue.cpp");
        string successfulCompletion = RepositoryTestContext.ReadTextFile(@"trunk\source\Common\HashSuccessfulFileCompletionWorkflow.cpp");

        Assert.Contains("std::atomic<uint64_t> countedSize;", global, StringComparison.Ordinal);
        Assert.Contains("TryAddUInt64", checkedArithmetic, StringComparison.Ordinal);
        Assert.Contains("TrySubtractUInt64", checkedArithmetic, StringComparison.Ordinal);
        Assert.Contains("TryMultiplyUInt64", checkedArithmetic, StringComparison.Ordinal);
        Assert.Contains("SaturatingAddUInt64", checkedArithmetic, StringComparison.Ordinal);
        Assert.Contains("ReplaceSizedValueUInt64", checkedArithmetic, StringComparison.Ordinal);
        Assert.Contains("CalculateBoundedProgressValue", checkedArithmetic, StringComparison.Ordinal);
        Assert.Contains("CalculateIndexedProgressValue", checkedArithmetic, StringComparison.Ordinal);

        Assert.Contains("SaturatingAddUInt64", executionContext, StringComparison.Ordinal);
        Assert.Contains("ReplaceSizedValueUInt64", executionContext, StringComparison.Ordinal);
        Assert.Contains("countedSize.load(std::memory_order_relaxed)", executionContext, StringComparison.Ordinal);
        Assert.Contains("countedSize.store(0, std::memory_order_relaxed);", executionContext, StringComparison.Ordinal);
        Assert.Contains("countedSize.compare_exchange_weak(", executionContext, StringComparison.Ordinal);
        Assert.Contains("SaturatingAddUInt64", threadAccess, StringComparison.Ordinal);
        Assert.Contains("ReplaceSizedValueUInt64", threadAccess, StringComparison.Ordinal);
        Assert.Contains("countedSize.load(std::memory_order_relaxed)", threadAccess, StringComparison.Ordinal);
        Assert.Contains("countedSize.store(0, std::memory_order_relaxed);", threadAccess, StringComparison.Ordinal);
        Assert.Contains("countedSize.compare_exchange_weak(", threadAccess, StringComparison.Ordinal);
        Assert.Contains("CalculateBoundedProgressValue", progressTracker, StringComparison.Ordinal);
        Assert.Contains("SaturatingAddUInt64(fileSize, static_cast<uint64_t>(bufferLength) - 1)", digestQueue, StringComparison.Ordinal);
        Assert.Contains("CalculateIndexedProgressValue", successfulCompletion, StringComparison.Ordinal);
    }

    [Fact]
    public void DigestExecution_RemainsThreadLocal_AndUiProgress_IsThrottled()
    {
        string md5 = RepositoryTestContext.ReadTextFile(@"trunk\source\Algorithms\MD5.cpp");
        string sha1 = RepositoryTestContext.ReadTextFile(@"trunk\source\Algorithms\SHA1.cpp");
        string strhelper = RepositoryTestContext.ReadTextFile(@"trunk\source\Common\strhelper.cpp");
        string digestQueue = RepositoryTestContext.ReadTextFile(@"trunk\source\Common\HashDigestQueue.cpp");
        string fileAttemptWorkflow = RepositoryTestContext.ReadTextFile(@"trunk\source\Common\HashFileAttemptWorkflow.cpp");
        string uiBridge = RepositoryTestContext.ReadTextFile(@"trunk\source\WinMFC\UIBridgeMFC.cpp");
        string uiBridgeHeader = RepositoryTestContext.ReadTextFile(@"trunk\source\WinMFC\UIBridgeMFC.h");
        string filesHashDialog = RepositoryTestContext.ReadTextFile(@"trunk\source\WinMFC\FilesHashDlg.cpp");
        string progressController = RepositoryTestContext.ReadTextFile(@"trunk\source\WinMFC\FilesHashProgressController.cpp");
        string nativeRuntimeTests = RepositoryTestContext.ReadTextFile(@"native-runtime-tests\LHash.NativeRuntimeTests\HashEngineRuntimeTests.cpp");

        Assert.Contains("static const unsigned char PADDING[64]", md5, StringComparison.Ordinal);
        Assert.DoesNotContain("static unsigned char PADDING[64]", md5, StringComparison.Ordinal);
        Assert.Contains("void MD5Init (MD5_CTX *mdContext)", md5, StringComparison.Ordinal);
        Assert.Contains("void MD5InitSeededLegacy (MD5_CTX *mdContext, uint32_t pseudoRandomNumber)", md5, StringComparison.Ordinal);
        Assert.DoesNotContain("void MD5Init (MD5_CTX *mdContext, uint32_t pseudoRandomNumber)", md5, StringComparison.Ordinal);
        Assert.DoesNotContain("static unsigned char workspace[64];", sha1, StringComparison.Ordinal);
        Assert.Contains("unsigned char workspace[64];", sha1, StringComparison.Ordinal);
        Assert.DoesNotContain("HashFile(", sha1, StringComparison.Ordinal);
        Assert.DoesNotContain("fopen(", sha1, StringComparison.Ordinal);
        Assert.DoesNotContain("fread(", sha1, StringComparison.Ordinal);
        Assert.DoesNotContain("ferror(", sha1, StringComparison.Ordinal);
        Assert.DoesNotContain("uint32_t ulFileSize", sha1, StringComparison.Ordinal);
        Assert.DoesNotContain("ftell(fIn)", sha1, StringComparison.Ordinal);
        Assert.DoesNotContain("fseek(fIn, 0, SEEK_END)", sha1, StringComparison.Ordinal);
        Assert.Contains("std::wstring_convert<std::codecvt_utf8<wchar_t>>", strhelper, StringComparison.Ordinal);
        Assert.Contains("size_t iconvResult = iconv(cd, inleft > 0 ? &in : NULL, &inleft, &out, &outleft);", strhelper, StringComparison.Ordinal);
        Assert.Contains("if (errno == E2BIG)", strhelper, StringComparison.Ordinal);
        Assert.DoesNotContain("setlocale(LC_ALL", strhelper, StringComparison.Ordinal);
        Assert.DoesNotContain("wcstombs(", strhelper, StringComparison.Ordinal);
        Assert.DoesNotContain("mbstowcs(", strhelper, StringComparison.Ordinal);
        Assert.DoesNotContain("\"UTF-8\", \"ASCII\"", strhelper, StringComparison.Ordinal);
        Assert.DoesNotContain("\"ASCII\", \"UTF-8\"", strhelper, StringComparison.Ordinal);

        Assert.Contains("struct ProgressDispatchState", uiBridgeHeader, StringComparison.Ordinal);
        Assert.Contains("ShouldPostProgressValue", uiBridgeHeader, StringComparison.Ordinal);
        Assert.Contains("InterlockedCompareExchange(&m_refreshPending, 1, 0) == 0", uiBridgeHeader, StringComparison.Ordinal);
        Assert.Contains("InterlockedExchange(&m_refreshPending, 0);", uiBridgeHeader, StringComparison.Ordinal);
        Assert.Contains("InterlockedCompareExchange(&m_taskUpdatePending, 1, 0) == 0", uiBridgeHeader, StringComparison.Ordinal);
        Assert.Contains("DrainPendingTaskUpdates(std::vector<FilesHashTaskUpdate>& taskUpdates)", uiBridgeHeader, StringComparison.Ordinal);
        Assert.Contains("kUiProgressDispatchIntervalMs = 80", uiBridge, StringComparison.Ordinal);
        Assert.Contains("ShouldPostProgressValue(m_totalProgressDispatchState, value)", uiBridge, StringComparison.Ordinal);
        Assert.Contains("PostThreadInfoMessage(WP_PROG_WHOLE, value);", uiBridge, StringComparison.Ordinal);
        Assert.DoesNotContain("new FilesHashTaskUpdate(taskUpdate)", uiBridge, StringComparison.Ordinal);
        Assert.Contains("taskUpdates.swap(m_pendingTaskUpdates);", uiBridge, StringComparison.Ordinal);
        Assert.Contains("m_hashProgressController.ApplyTaskUpdates(taskUpdates);", filesHashDialog, StringComparison.Ordinal);
        Assert.Contains("ON_NOTIFY(LVN_GETDISPINFO, IDC_TASK_LIST, &CFilesHashDlg::OnTaskListGetDispInfo)", filesHashDialog, StringComparison.Ordinal);
        Assert.Contains("m_taskListCtrl->SetRedraw(FALSE);", progressController, StringComparison.Ordinal);
        Assert.Contains("m_taskListCtrl->SetRedraw(TRUE);", progressController, StringComparison.Ordinal);
        Assert.Contains("m_taskListCtrl->SetItemCountEx(static_cast<int>(m_taskRows.size()), LVSICF_NOINVALIDATEALL | LVSICF_NOSCROLL);", progressController, StringComparison.Ordinal);
        Assert.DoesNotContain("m_taskListCtrl->InsertItem(", progressController, StringComparison.Ordinal);
        Assert.DoesNotContain("m_taskListCtrl->SetItemText(", progressController, StringComparison.Ordinal);
        Assert.Contains("std::this_thread::yield();", fileAttemptWorkflow, StringComparison.Ordinal);
        Assert.DoesNotContain("Sleep(3);", fileAttemptWorkflow, StringComparison.Ordinal);
        Assert.Contains("vector<unique_ptr<DigestDataBuffer>> digestBufferPool;", digestQueue, StringComparison.Ordinal);
        Assert.Contains("queue<size_t> availableBufferIndices;", digestQueue, StringComparison.Ordinal);
        Assert.DoesNotContain("make_unique<DigestDataBuffer>(preferredBufferLength)", digestQueue, StringComparison.Ordinal);

        Assert.Contains("std::async(std::launch::async, runSingleRequest)", nativeRuntimeTests, StringComparison.Ordinal);
    }

    [Fact]
    public void MfcProgressDispatchState_ResetsWhenANewJobStarts()
    {
        string uiBridge = RepositoryTestContext.ReadTextFile(@"trunk\source\WinMFC\UIBridgeMFC.cpp");
        string uiBridgeHeader = RepositoryTestContext.ReadTextFile(@"trunk\source\WinMFC\UIBridgeMFC.h");

        Assert.Contains("static inline void ResetProgressDispatchState", uiBridgeHeader, StringComparison.Ordinal);
        Assert.Contains("ResetProgressDispatchState(m_fileProgressDispatchState);", uiBridge, StringComparison.Ordinal);
        Assert.Contains("ResetProgressDispatchState(m_totalProgressDispatchState);", uiBridge, StringComparison.Ordinal);
        Assert.Contains("case PROGRESS_EVENT_FILE_STARTED:", uiBridge, StringComparison.Ordinal);
    }

    [Fact]
    public void Blake3Integration_UsesOfficialProviderProfiles_AndCoversUppercaseOrderingAndConcurrency()
    {
        string providerHeader = RepositoryTestContext.ReadTextFile(@"trunk\source\Runtime\Hash\BLAKE3HashProvider.h");
        string providerImplementation = RepositoryTestContext.ReadTextFile(@"trunk\source\Runtime\Hash\BLAKE3HashProvider.cpp");
        string runtimeTests = RepositoryTestContext.ReadTextFile(@"native-runtime-tests\LHash.NativeRuntimeTests\HashEngineRuntimeTests.cpp");
        string nativeCoreProject = RepositoryTestContext.ReadTextFile(@"sub-proj\LHashNativeCore\LHashNativeCore.vcxproj");
        string securityHarness = RepositoryTestContext.ReadTextFile(@"security-tests\SecurityRegression\WindowsSecurityRuntimeHarness.cs");

        Assert.Contains("BLAKE3_256_OUTPUT_BYTES = BLAKE3_OUT_LEN", providerHeader, StringComparison.Ordinal);
        Assert.Contains("BLAKE3_512_OUTPUT_BYTES = 64", providerHeader, StringComparison.Ordinal);
        Assert.Contains("BLAKE3_XOF_OUTPUT_BYTES = 128", providerHeader, StringComparison.Ordinal);
        Assert.Contains("blake3_hasher_init", providerImplementation, StringComparison.Ordinal);
        Assert.Contains("blake3_hasher_update", providerImplementation, StringComparison.Ordinal);
        Assert.Contains("blake3_hasher_finalize", providerImplementation, StringComparison.Ordinal);
        Assert.DoesNotContain("static blake3_hasher", providerImplementation, StringComparison.Ordinal);

        Assert.Contains("RunHashRequest_Blake3UppercaseFlagRemainsDeterministicAcrossVariants", runtimeTests, StringComparison.Ordinal);
        Assert.Contains("RunHashRequest_Blake3UnknownIdsAreIgnoredAndKnownVariantsStayOrdered", runtimeTests, StringComparison.Ordinal);
        Assert.Contains("HashThreadFunc_Blake3VariantsRemainStableAcrossConcurrentRuns", runtimeTests, StringComparison.Ordinal);
        Assert.Contains("CreateAlgorithmId(\"blake3-1024\")", runtimeTests, StringComparison.Ordinal);
        Assert.Contains("std::async(std::launch::async, runSingleRequest)", runtimeTests, StringComparison.Ordinal);

        Assert.Contains(@"blake3_sse2.c", nativeCoreProject, StringComparison.Ordinal);
        Assert.Contains(@"blake3_sse41.c", nativeCoreProject, StringComparison.Ordinal);
        Assert.Contains(@"blake3_avx2.c", nativeCoreProject, StringComparison.Ordinal);
        Assert.Contains(@"blake3_avx512.c", nativeCoreProject, StringComparison.Ordinal);
        Assert.Contains("LHashBlake3SimdProfile", nativeCoreProject, StringComparison.Ordinal);
        Assert.Contains("Condition=\"'$(LHashBlake3SimdProfile)'=='portable'\">BLAKE3_USE_NEON=0;BLAKE3_NO_SSE2;BLAKE3_NO_SSE41;BLAKE3_NO_AVX2;BLAKE3_NO_AVX512;%(PreprocessorDefinitions)</PreprocessorDefinitions>", nativeCoreProject, StringComparison.Ordinal);
        Assert.Contains("ExcludedFromBuild Condition=\"'$(LHashBlake3SimdProfile)'=='portable'\"", nativeCoreProject, StringComparison.Ordinal);
        Assert.DoesNotContain("Condition=\"'$(Platform)'=='Win32'\">/arch:SSE2", nativeCoreProject, StringComparison.Ordinal);
        Assert.DoesNotContain("Condition=\"'$(Platform)'=='Win32'\">/arch:AVX", nativeCoreProject, StringComparison.Ordinal);
        Assert.Contains("/arch:AVX2", nativeCoreProject, StringComparison.Ordinal);
        Assert.Contains("/arch:AVX512", nativeCoreProject, StringComparison.Ordinal);
        Assert.Contains("TestJunctionAncestorAttackSurface", securityHarness, StringComparison.Ordinal);
        Assert.Contains("TestHashStyleOpenSharingViolation", securityHarness, StringComparison.Ordinal);
        Assert.Contains("CreateDirectoryJunction", securityHarness, StringComparison.Ordinal);
        Assert.Contains("ERROR_SHARING_VIOLATION = 32", securityHarness, StringComparison.Ordinal);
    }

    [Fact]
    public void XXH3AndCRC32CIntegration_UsesOfficialProviders_AndCoversVectorsOrderingAndConcurrency()
    {
        string xxh3ProviderHeader = RepositoryTestContext.ReadTextFile(@"trunk\source\Runtime\Hash\XXHash3HashProvider.h");
        string xxh3ProviderImplementation = RepositoryTestContext.ReadTextFile(@"trunk\source\Runtime\Hash\XXHash3HashProvider.cpp");
        string crc32cProviderHeader = RepositoryTestContext.ReadTextFile(@"trunk\source\Runtime\Hash\CRC32CHashProvider.h");
        string crc32cProviderImplementation = RepositoryTestContext.ReadTextFile(@"trunk\source\Runtime\Hash\CRC32CHashProvider.cpp");
        string runtimeTests = RepositoryTestContext.ReadTextFile(@"native-runtime-tests\LHash.NativeRuntimeTests\HashEngineRuntimeTests.cpp");
        string nativeCoreProject = RepositoryTestContext.ReadTextFile(@"sub-proj\LHashNativeCore\LHashNativeCore.vcxproj");
        string crc32cArm64Check = RepositoryTestContext.ReadTextFile(@"third_party\crc32c\1.1.2\src\crc32c_arm64_check.h");

        Assert.Contains("XXH3_64_OUTPUT_BYTES = sizeof(XXH64_hash_t)", xxh3ProviderHeader, StringComparison.Ordinal);
        Assert.Contains("XXH3_128_OUTPUT_BYTES = sizeof(XXH128_hash_t)", xxh3ProviderHeader, StringComparison.Ordinal);
        Assert.Contains("XXH3_64bits_reset", xxh3ProviderImplementation, StringComparison.Ordinal);
        Assert.Contains("XXH3_64bits_update", xxh3ProviderImplementation, StringComparison.Ordinal);
        Assert.Contains("XXH64_canonicalFromHash", xxh3ProviderImplementation, StringComparison.Ordinal);
        Assert.Contains("XXH3_128bits_digest", xxh3ProviderImplementation, StringComparison.Ordinal);
        Assert.DoesNotContain("static XXH3_state_t", xxh3ProviderImplementation, StringComparison.Ordinal);

        Assert.Contains("CRC32C_OUTPUT_BYTES = sizeof(uint32_t)", crc32cProviderHeader, StringComparison.Ordinal);
        Assert.Contains("crc32c_extend", crc32cProviderImplementation, StringComparison.Ordinal);
        Assert.DoesNotContain("static uint32_t", crc32cProviderImplementation, StringComparison.Ordinal);
        Assert.Contains("PF_ARM_V8_CRC32_INSTRUCTIONS_AVAILABLE", crc32cArm64Check, StringComparison.Ordinal);
        Assert.Contains("PF_ARM_V8_CRYPTO_INSTRUCTIONS_AVAILABLE", crc32cArm64Check, StringComparison.Ordinal);

        Assert.Contains("HashThreadFunc_ComputesOfficialXXH3DigestsForKnownVector", runtimeTests, StringComparison.Ordinal);
        Assert.Contains("HashThreadFunc_ComputesOfficialCRC32CDigestForKnownVector", runtimeTests, StringComparison.Ordinal);
        Assert.Contains("HashThreadFunc_ComputesOfficialCRC32CBoundaryDigestsForKnownVectors", runtimeTests, StringComparison.Ordinal);
        Assert.Contains("RunHashRequest_XXH3AndCRC32CUnknownIdsAreIgnoredAndKnownVariantsStayOrdered", runtimeTests, StringComparison.Ordinal);
        Assert.Contains("HashThreadFunc_XXH3AndCRC32CRemainStableAcrossConcurrentRuns", runtimeTests, StringComparison.Ordinal);
        Assert.Contains("RunHashRequest_XXH3AndCRC32CMultiFileConcurrentMatchesSingleRun", runtimeTests, StringComparison.Ordinal);
        Assert.Contains("std::async(std::launch::async, runSingleRequest)", runtimeTests, StringComparison.Ordinal);
        Assert.Contains("CreateAlgorithmId(\"xxh3\")", runtimeTests, StringComparison.Ordinal);
        Assert.Contains("CreateAlgorithmId(\"crc32c-64\")", runtimeTests, StringComparison.Ordinal);
        Assert.Contains("8A9136AA", runtimeTests, StringComparison.Ordinal);
        Assert.Contains("62A8AB43", runtimeTests, StringComparison.Ordinal);
        Assert.Contains("113FDB5C", runtimeTests, StringComparison.Ordinal);
        Assert.Contains("D9963A56", runtimeTests, StringComparison.Ordinal);

        Assert.Contains(@"third_party\xxhash\0.8.3\xxhash.c", nativeCoreProject, StringComparison.Ordinal);
        Assert.Contains(@"third_party\crc32c\1.1.2\src\crc32c.cc", nativeCoreProject, StringComparison.Ordinal);
        Assert.Contains(@"third_party\crc32c\1.1.2\src\crc32c_arm64.cc", nativeCoreProject, StringComparison.Ordinal);
    }

    [Fact]
    public void OpenSslEvpIntegration_UsesOfficialVendorAndDefinesTheOnlyActiveSha256AndSha512Variants()
    {
        string registryCore = RepositoryTestContext.ReadTextFile(@"trunk\source\Domain\HashAlgorithmRegistryCore.h");
        string digestRegistry = RepositoryTestContext.ReadTextFile(@"trunk\source\Common\HashDigestOperationRegistry.cpp");
        string providerHeader = RepositoryTestContext.ReadTextFile(@"trunk\source\Runtime\Hash\OpenSslEvpHashProvider.h");
        string providerImplementation = RepositoryTestContext.ReadTextFile(@"trunk\source\Runtime\Hash\OpenSslEvpHashProvider.cpp");
        string runtimeTests = RepositoryTestContext.ReadTextFile(@"native-runtime-tests\LHash.NativeRuntimeTests\HashEngineRuntimeTests.cpp");
        string nativeCoreProject = RepositoryTestContext.ReadTextFile(@"sub-proj\LHashNativeCore\LHashNativeCore.vcxproj");
        string workflow = RepositoryTestContext.ReadTextFile(@".github\workflows\windows-build.yml");
        string vendorTargets = RepositoryTestContext.ReadTextFile(@"NativeOpenSslVendor.targets");
        string vendorScript = RepositoryTestContext.ReadTextFile(@"trunk\build_openssl_vendor.ps1");
        string sourceInfo = RepositoryTestContext.ReadTextFile(@"third_party\openssl\OPENSSL_3_5_6_SOURCE_INFO.txt");
        string vendorPolicy = RepositoryTestContext.ReadTextFile(@"third_party\openssl\POLICY.md");
        string licenseException = RepositoryTestContext.ReadTextFile(@"LICENSE-OPENSSL-EXCEPTION.md");

        Assert.Contains("{ \"openssl-sha-256\", \"SHA-256\", true, true }", registryCore, StringComparison.Ordinal);
        Assert.Contains("{ \"openssl-sha-384\", \"SHA-384\", true, false }", registryCore, StringComparison.Ordinal);
        Assert.Contains("{ \"openssl-sha-512\", \"SHA-512\", true, true }", registryCore, StringComparison.Ordinal);
        Assert.Contains("{ \"openssl-sha3-256\", \"SHA3-256\", true, true }", registryCore, StringComparison.Ordinal);
        Assert.Contains("{ \"openssl-sha3-384\", \"SHA3-384\", true, false }", registryCore, StringComparison.Ordinal);
        Assert.Contains("{ \"openssl-sha3-512\", \"SHA3-512\", true, false }", registryCore, StringComparison.Ordinal);
        Assert.Contains("{ \"openssl-blake2b-512\", \"BLAKE2b-512\", true, false }", registryCore, StringComparison.Ordinal);
        Assert.Contains("{ \"openssl-blake2s-256\", \"BLAKE2s-256\", true, false }", registryCore, StringComparison.Ordinal);
        Assert.Contains("{ \"openssl-shake128-256\", \"SHAKE128-256\", true, false }", registryCore, StringComparison.Ordinal);
        Assert.Contains("{ \"openssl-shake256-512\", \"SHAKE256-512\", true, false }", registryCore, StringComparison.Ordinal);
        Assert.Contains("{ \"md5\", \"MD5 (Deprecated)\", true, false }", registryCore, StringComparison.Ordinal);
        Assert.Contains("{ \"sha1\", \"SHA1 (Deprecated)\", true, false }", registryCore, StringComparison.Ordinal);

        Assert.Contains("InitializeOpenSslSha256DigestContext", digestRegistry, StringComparison.Ordinal);
        Assert.Contains("InitializeOpenSslSha384DigestContext", digestRegistry, StringComparison.Ordinal);
        Assert.Contains("InitializeOpenSslSha512DigestContext", digestRegistry, StringComparison.Ordinal);
        Assert.Contains("InitializeOpenSslSha3_256DigestContext", digestRegistry, StringComparison.Ordinal);
        Assert.Contains("InitializeOpenSslSha3_384DigestContext", digestRegistry, StringComparison.Ordinal);
        Assert.Contains("InitializeOpenSslSha3_512DigestContext", digestRegistry, StringComparison.Ordinal);
        Assert.Contains("InitializeOpenSslBlake2b_512DigestContext", digestRegistry, StringComparison.Ordinal);
        Assert.Contains("InitializeOpenSslBlake2s_256DigestContext", digestRegistry, StringComparison.Ordinal);
        Assert.Contains("InitializeOpenSslShake128_256DigestContext", digestRegistry, StringComparison.Ordinal);
        Assert.Contains("InitializeOpenSslShake256_512DigestContext", digestRegistry, StringComparison.Ordinal);
        Assert.Contains("SetDigestStorageValueById(", digestRegistry, StringComparison.Ordinal);

        Assert.Contains("EVP_MD_fetch", providerImplementation, StringComparison.Ordinal);
        Assert.Contains("GetCachedDigestImplementation", providerImplementation, StringComparison.Ordinal);
        Assert.Contains("EVP_DigestInit_ex2", providerImplementation, StringComparison.Ordinal);
        Assert.Contains("OSSL_DIGEST_PARAM_SIZE", providerImplementation, StringComparison.Ordinal);
        Assert.Contains("OSSL_PARAM_construct_size_t", providerImplementation, StringComparison.Ordinal);
        Assert.Contains("EVP_DigestUpdate", providerImplementation, StringComparison.Ordinal);
        Assert.Contains("EVP_DigestUpdate(hashContext.mdContext, data, dataLen) != 1", providerImplementation, StringComparison.Ordinal);
        Assert.Contains("if (hashContext.updateFailed)", providerImplementation, StringComparison.Ordinal);
        Assert.Contains("hashContext.updateFailed = true;", providerImplementation, StringComparison.Ordinal);
        Assert.Contains("EVP_DigestFinal_ex", providerImplementation, StringComparison.Ordinal);
        Assert.Contains("EVP_DigestFinalXOF", providerImplementation, StringComparison.Ordinal);
        Assert.Contains("ConfigureOpenSslEvpFailureInjection", providerImplementation, StringComparison.Ordinal);
        Assert.DoesNotContain("static EVP_MD_CTX", providerImplementation, StringComparison.Ordinal);
        Assert.Contains("updateFailed(false)", providerHeader, StringComparison.Ordinal);
        Assert.Contains("bool updateFailed;", providerHeader, StringComparison.Ordinal);
        Assert.Contains("struct OpenSslEvpHashFinalizeResult", providerHeader, StringComparison.Ordinal);
        Assert.Contains("OPENSSL_SHA_384_OUTPUT_BYTES = 48", providerHeader, StringComparison.Ordinal);
        Assert.Contains("OPENSSL_SHA3_384_OUTPUT_BYTES = 48", providerHeader, StringComparison.Ordinal);
        Assert.Contains("OPENSSL_BLAKE2B_512_OUTPUT_BYTES = 64", providerHeader, StringComparison.Ordinal);
        Assert.Contains("OPENSSL_BLAKE2S_256_OUTPUT_BYTES = 32", providerHeader, StringComparison.Ordinal);
        Assert.Contains("OPENSSL_SHAKE256_512_OUTPUT_BYTES = 64", providerHeader, StringComparison.Ordinal);

        Assert.Contains(@"Runtime\Hash\OpenSslEvpHashProviderStub.cpp", nativeCoreProject, StringComparison.Ordinal);
        Assert.Contains(@"Runtime\Hash\OpenSslEvpHashProvider.cpp", nativeCoreProject, StringComparison.Ordinal);
        Assert.Contains("ExcludedFromBuild Condition=\"'$(LHashOpenSslInstallRoot)'!=''\">true</ExcludedFromBuild>", nativeCoreProject, StringComparison.Ordinal);
        Assert.Contains("ExcludedFromBuild Condition=\"'$(LHashOpenSslInstallRoot)'==''\">true</ExcludedFromBuild>", nativeCoreProject, StringComparison.Ordinal);
        Assert.Contains("LHashOpenSslInstallRoot", vendorTargets, StringComparison.Ordinal);
        Assert.Contains("LHashOpenSslIncludeDir", vendorTargets, StringComparison.Ordinal);
        Assert.Contains("LHashOpenSslLibDir", vendorTargets, StringComparison.Ordinal);
        Assert.Contains("LHASH_WITH_OPENSSL3_VENDOR=1", vendorTargets, StringComparison.Ordinal);
        Assert.DoesNotContain("FHashOpenSslInstallRoot", vendorTargets, StringComparison.Ordinal);
        Assert.DoesNotContain("FHASH_WITH_OPENSSL3_VENDOR=1", vendorTargets, StringComparison.Ordinal);
        Assert.Contains("libcrypto.lib", vendorTargets, StringComparison.Ordinal);
        Assert.Contains("ValidateSet('x64', 'ARM64')", vendorScript, StringComparison.Ordinal);
        Assert.DoesNotContain("VC-WIN32", vendorScript, StringComparison.Ordinal);
        Assert.Contains("openssl-3.5.6", vendorScript, StringComparison.Ordinal);
        Assert.Contains("VC-WIN64A", vendorScript, StringComparison.Ordinal);
        Assert.Contains("VC-WIN64-ARM", vendorScript, StringComparison.Ordinal);
        Assert.Contains("build_openssl_vendor.ps1", workflow, StringComparison.Ordinal);
        Assert.Contains("Verify pristine OpenSSL vendor source", workflow, StringComparison.Ordinal);
        Assert.Contains("tools/verify_openssl_vendor_pristine.ps1", workflow, StringComparison.Ordinal);
        Assert.Contains("openssl_vendor_version=3.5.6", workflow, StringComparison.Ordinal);
        Assert.Contains("openssl_vendor_source=third_party/openssl/3.5.6", workflow, StringComparison.Ordinal);
        Assert.Contains("openssl_vendor_policy=pristine-upstream-source", workflow, StringComparison.Ordinal);
        Assert.Contains("openssl_vendor_local_patches=none", workflow, StringComparison.Ordinal);
        Assert.Contains("/p:LHashOpenSslInstallRoot=$openSslRoot", workflow, StringComparison.Ordinal);

        Assert.Contains("OpenSSL Linking Exception", licenseException, StringComparison.Ordinal);
        Assert.Contains("version=openssl-3.5.6", sourceInfo, StringComparison.Ordinal);
        Assert.Contains("source_policy=pristine upstream tarball extraction", sourceInfo, StringComparison.Ordinal);
        Assert.Contains("vendor_directory=third_party/openssl/3.5.6", sourceInfo, StringComparison.Ordinal);
        Assert.Contains("imported_for=LHash Windows x64/ARM64 static libcrypto vendor build", sourceInfo, StringComparison.Ordinal);
        Assert.Contains("pristine upstream OpenSSL source tree", vendorPolicy, StringComparison.Ordinal);
        Assert.Contains("temporary build copy", vendorPolicy, StringComparison.Ordinal);
        Assert.Contains("third_party/openssl/patches/<version>/", vendorPolicy, StringComparison.Ordinal);

        Assert.Contains("HashThreadFunc_ComputesOfficialOpenSslDigestsForKnownVector", runtimeTests, StringComparison.Ordinal);
        Assert.Contains("RunHashRequest_OpenSslSha2VariantsStayDistinctWithinOpenSslFamily", runtimeTests, StringComparison.Ordinal);
        Assert.Contains("RunHashRequest_OpenSslUnknownIdsAreIgnoredAndKnownVariantsStayOrdered", runtimeTests, StringComparison.Ordinal);
        Assert.Contains("HashThreadFunc_OpenSslVariantsRemainStableAcrossConcurrentRuns", runtimeTests, StringComparison.Ordinal);
        Assert.Contains("RunHashRequest_OpenSslDigestUpdateFailureProducesExplicitFileError", runtimeTests, StringComparison.Ordinal);
        Assert.Contains("RunHashRequest_OpenSslDigestFinalizeFailureProducesExplicitFileError", runtimeTests, StringComparison.Ordinal);
        Assert.Contains("CreateAlgorithmId(\"openssl-sha-256\")", runtimeTests, StringComparison.Ordinal);
        Assert.Contains("CreateAlgorithmId(\"openssl-blake2b-512\")", runtimeTests, StringComparison.Ordinal);
        Assert.Contains("CreateAlgorithmId(\"openssl-blake2s-256\")", runtimeTests, StringComparison.Ordinal);
        Assert.Contains("CreateAlgorithmId(\"openssl-sha3\")", runtimeTests, StringComparison.Ordinal);
    }

    [Fact]
    public void HashExecutionContext_UsesANonOwningSinkReferenceWithNullFallback()
    {
        string executionContext = RepositoryTestContext.ReadTextFile(@"trunk\source\Runtime\HashExecutionContext.h");

        Assert.Contains("HashExecutionContext is a non-owning synchronous execution context.", executionContext, StringComparison.Ordinal);
        Assert.Contains("The caller must keep the sink, jobState, and cancellationState alive until", executionContext, StringComparison.Ordinal);
        Assert.Contains("class NullHashProgressSink : public HashProgressSink", executionContext, StringComparison.Ordinal);
        Assert.Contains("HashProgressSink& GetNullHashProgressSink()", executionContext, StringComparison.Ordinal);
        Assert.Contains("sink != NULL ? sink : &GetNullHashProgressSink()", executionContext, StringComparison.Ordinal);
        Assert.Contains("HashProgressSink *progressSinkObserver;", executionContext, StringComparison.Ordinal);
        Assert.DoesNotContain("HashProgressSink& progressSinkObserver;", executionContext, StringComparison.Ordinal);
        Assert.DoesNotContain("HashProgressSink *progressSink;", executionContext, StringComparison.Ordinal);
    }
}
