namespace FHash.UnitTests;

public sealed class SecurityHardeningUnitTests
{
    [Fact]
    public void FileOpenPaths_RejectReparsePoints_AndReuseOpenedHandleMetadata()
    {
        string osFile = RepositoryTestContext.ReadTextFile(@"trunk\source\OsUtils\OsFile.h");
        string winApi = RepositoryTestContext.ReadTextFile(@"trunk\source\OsUtils\OsFileWinApi.cpp");
        string winUwp = RepositoryTestContext.ReadTextFile(@"trunk\source\OsUtils\OsFileWinUwp.cpp");
        string engineResult = RepositoryTestContext.ReadTextFile(@"trunk\source\Common\HashEngineResult.cpp");

        Assert.Contains("bool isHashTargetAllowed(void *exception = NULL);", osFile, StringComparison.Ordinal);

        Assert.Contains("FILE_ATTRIBUTE_REPARSE_POINT", winApi, StringComparison.Ordinal);
        Assert.Contains("Refusing to hash a symbolic link, junction, mount point, or other reparse point.", winApi, StringComparison.Ordinal);
        Assert.Contains("if (!isHashTargetAllowed(exception))", winApi, StringComparison.Ordinal);

        Assert.Contains("FILE_ATTRIBUTE_REPARSE_POINT", winUwp, StringComparison.Ordinal);
        Assert.Contains("Refusing to hash a symbolic link, junction, mount point, or other reparse point.", winUwp, StringComparison.Ordinal);
        Assert.Contains("if (!isHashTargetAllowed(exception))", winUwp, StringComparison.Ordinal);

        Assert.Contains("result.meta.modifiedDate = osFile.getModifiedTimeFormat();", engineResult, StringComparison.Ordinal);
        Assert.DoesNotContain("GetFileAttributesEx", engineResult, StringComparison.Ordinal);
    }

    [Fact]
    public void HandleOwnership_UsesRaiiAcrossWorkerAndShellPaths()
    {
        string handleGuard = RepositoryTestContext.ReadTextFile(@"trunk\source\WinCommon\WinHandleGuard.h");
        string threadLaunch = RepositoryTestContext.ReadTextFile(@"trunk\source\LegacyCompat\HashThreadLaunch.h");
        string sessionController = RepositoryTestContext.ReadTextFile(@"trunk\source\WinMFC\FilesHashSessionController.h");
        string clrHeader = RepositoryTestContext.ReadTextFile(@"sub-proj\fHashClrBridge\HashMgmtClr.h");
        string clrSource = RepositoryTestContext.ReadTextFile(@"sub-proj\fHashClrBridge\HashMgmtClr.cpp");
        string uwpHeader = RepositoryTestContext.ReadTextFile(@"sub-proj\fHashWinRtBridge\HashMgmt.h");
        string shellCore = RepositoryTestContext.ReadTextFile(@"trunk\source\WinCommon\ShellExplorerCommandCore.h");
        string legacyShell = RepositoryTestContext.ReadTextFile(@"sub-proj\fHashShlExt\fHashShellExt.cpp");
        string windowsUtils = RepositoryTestContext.ReadTextFile(@"trunk\source\WinMFC\WindowsUtils.cpp");

        Assert.Contains("typedef UniqueHandleBase<HANDLE, HandleCloseTraits> UniqueWinHandle;", handleGuard, StringComparison.Ordinal);
        Assert.Contains("typedef UniqueHandleBase<HANDLE, FindHandleCloseTraits> UniqueFindHandle;", handleGuard, StringComparison.Ordinal);
        Assert.Contains("typedef UniqueHandleBase<HMODULE, ModuleCloseTraits> UniqueModuleHandle;", handleGuard, StringComparison.Ordinal);

        Assert.Contains("static inline WinHandleGuard::UniqueWinHandle StartHashWorkerThread", threadLaunch, StringComparison.Ordinal);
        Assert.Contains("CloseHashWorkerThreadHandle(WinHandleGuard::UniqueWinHandle *threadHandle)", threadLaunch, StringComparison.Ordinal);
        Assert.Contains("RestartHashWorkerThread(WinHandleGuard::UniqueWinHandle *existingThreadHandle", threadLaunch, StringComparison.Ordinal);

        Assert.Contains("WinHandleGuard::UniqueWinHandle m_hWorkThread;", sessionController, StringComparison.Ordinal);
        Assert.Contains("WinHandleGuard::UniqueWinHandle *m_pWorkThread;", clrHeader, StringComparison.Ordinal);
        Assert.Contains("WinHandleGuard::UniqueWinHandle m_hWorkThread;", uwpHeader, StringComparison.Ordinal);
        Assert.Contains("m_pWorkThread = new WinHandleGuard::UniqueWinHandle();", clrSource, StringComparison.Ordinal);
        Assert.Contains("CloseHashWorkerThreadHandle(m_pWorkThread);", clrSource, StringComparison.Ordinal);
        Assert.Contains("RestartHashWorkerThread(m_pWorkThread, m_pThreadData, &thredID);", clrSource, StringComparison.Ordinal);

        Assert.Contains("#include \"WinCommon/WinHandleGuard.h\"", shellCore, StringComparison.Ordinal);
        Assert.Contains("WinHandleGuard::UniqueWinHandle threadHandle(pInfo.hThread);", shellCore, StringComparison.Ordinal);
        Assert.Contains("WinHandleGuard::UniqueWinHandle processHandle(pInfo.hProcess);", shellCore, StringComparison.Ordinal);

        Assert.Contains("#include \"WinCommon/WinHandleGuard.h\"", legacyShell, StringComparison.Ordinal);
        Assert.Contains("WinHandleGuard::UniqueWinHandle threadHandle(pInfo.hThread);", legacyShell, StringComparison.Ordinal);
        Assert.Contains("WinHandleGuard::UniqueWinHandle processHandle(pInfo.hProcess);", legacyShell, StringComparison.Ordinal);
        Assert.Contains("WinHandleGuard::UniqueWinHandle hProcfHash(OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, FALSE, dwPidfHash));", legacyShell, StringComparison.Ordinal);

        Assert.Contains("#include \"WinCommon/WinHandleGuard.h\"", windowsUtils, StringComparison.Ordinal);
        Assert.Contains("WinHandleGuard::UniqueFindHandle hFind", windowsUtils, StringComparison.Ordinal);
        Assert.Contains("WinHandleGuard::UniqueModuleHandle hModule", windowsUtils, StringComparison.Ordinal);
        Assert.DoesNotContain("FreeLibrary(hModule);", windowsUtils, StringComparison.Ordinal);
    }

    [Fact]
    public void RuntimeUses_CheckedArithmetic_ForSizesAndProgress()
    {
        string checkedArithmetic = RepositoryTestContext.ReadTextFile(@"trunk\source\Common\CheckedArithmetic.h");
        string executionContext = RepositoryTestContext.ReadTextFile(@"trunk\source\Runtime\HashExecutionContext.h");
        string threadAccess = RepositoryTestContext.ReadTextFile(@"trunk\source\LegacyCompat\ThreadDataExecutionAccess.h");
        string progressTracker = RepositoryTestContext.ReadTextFile(@"trunk\source\Common\HashProgressTracker.cpp");
        string digestQueue = RepositoryTestContext.ReadTextFile(@"trunk\source\Common\HashDigestQueue.cpp");
        string successfulCompletion = RepositoryTestContext.ReadTextFile(@"trunk\source\Common\HashSuccessfulFileCompletionWorkflow.cpp");

        Assert.Contains("TryAddUInt64", checkedArithmetic, StringComparison.Ordinal);
        Assert.Contains("TrySubtractUInt64", checkedArithmetic, StringComparison.Ordinal);
        Assert.Contains("TryMultiplyUInt64", checkedArithmetic, StringComparison.Ordinal);
        Assert.Contains("SaturatingAddUInt64", checkedArithmetic, StringComparison.Ordinal);
        Assert.Contains("ReplaceSizedValueUInt64", checkedArithmetic, StringComparison.Ordinal);
        Assert.Contains("CalculateBoundedProgressValue", checkedArithmetic, StringComparison.Ordinal);
        Assert.Contains("CalculateIndexedProgressValue", checkedArithmetic, StringComparison.Ordinal);

        Assert.Contains("SaturatingAddUInt64", executionContext, StringComparison.Ordinal);
        Assert.Contains("ReplaceSizedValueUInt64", executionContext, StringComparison.Ordinal);
        Assert.Contains("SaturatingAddUInt64", threadAccess, StringComparison.Ordinal);
        Assert.Contains("ReplaceSizedValueUInt64", threadAccess, StringComparison.Ordinal);
        Assert.Contains("CalculateBoundedProgressValue", progressTracker, StringComparison.Ordinal);
        Assert.Contains("SaturatingAddUInt64(fileSize, static_cast<uint64_t>(bufferLength) - 1)", digestQueue, StringComparison.Ordinal);
        Assert.Contains("CalculateIndexedProgressValue", successfulCompletion, StringComparison.Ordinal);
    }

    [Fact]
    public void DigestExecution_RemainsThreadLocal_AndUiProgress_IsThrottled()
    {
        string sha1 = RepositoryTestContext.ReadTextFile(@"trunk\source\Algorithms\SHA1.cpp");
        string uiBridge = RepositoryTestContext.ReadTextFile(@"trunk\source\WinMFC\UIBridgeMFC.cpp");
        string uiBridgeHeader = RepositoryTestContext.ReadTextFile(@"trunk\source\WinMFC\UIBridgeMFC.h");
        string nativeRuntimeTests = RepositoryTestContext.ReadTextFile(@"native-runtime-tests\FHash.NativeRuntimeTests\HashEngineRuntimeTests.cpp");

        Assert.DoesNotContain("static unsigned char workspace[64];", sha1, StringComparison.Ordinal);
        Assert.Contains("unsigned char workspace[64];", sha1, StringComparison.Ordinal);

        Assert.Contains("struct ProgressDispatchState", uiBridgeHeader, StringComparison.Ordinal);
        Assert.Contains("ShouldPostProgressValue", uiBridgeHeader, StringComparison.Ordinal);
        Assert.Contains("kUiProgressDispatchIntervalMs = 80", uiBridge, StringComparison.Ordinal);
        Assert.Contains("ShouldPostProgressValue(m_totalProgressDispatchState, value)", uiBridge, StringComparison.Ordinal);
        Assert.Contains("PostThreadInfoMessage(WP_PROG_WHOLE, value);", uiBridge, StringComparison.Ordinal);

        Assert.Contains("HashThreadFunc_ProducesConsistentDigestsAcrossConcurrentRuns", nativeRuntimeTests, StringComparison.Ordinal);
        Assert.Contains("std::async(std::launch::async, runSingleRequest)", nativeRuntimeTests, StringComparison.Ordinal);
    }
}
