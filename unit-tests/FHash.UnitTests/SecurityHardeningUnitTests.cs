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
        Assert.Contains("HasReparsePointInPathHierarchy", winApi, StringComparison.Ordinal);
        Assert.Contains("PathSegmentHasReparsePoint", winApi, StringComparison.Ordinal);
        Assert.Contains("if (!isHashTargetAllowed(exception))", winApi, StringComparison.Ordinal);

        Assert.Contains("FILE_ATTRIBUTE_REPARSE_POINT", winUwp, StringComparison.Ordinal);
        Assert.Contains("Refusing to hash a symbolic link, junction, mount point, or other reparse point.", winUwp, StringComparison.Ordinal);
        Assert.Contains("HasReparsePointInPathHierarchy", winUwp, StringComparison.Ordinal);
        Assert.Contains("PathSegmentHasReparsePoint", winUwp, StringComparison.Ordinal);
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
        Assert.Contains("0, 0, FALSE,", shellCore, StringComparison.Ordinal);
        Assert.DoesNotContain("0, 0, TRUE,", shellCore, StringComparison.Ordinal);

        Assert.Contains("#include \"WinCommon/WinHandleGuard.h\"", legacyShell, StringComparison.Ordinal);
        Assert.Contains("WinHandleGuard::UniqueWinHandle threadHandle(pInfo.hThread);", legacyShell, StringComparison.Ordinal);
        Assert.Contains("WinHandleGuard::UniqueWinHandle processHandle(pInfo.hProcess);", legacyShell, StringComparison.Ordinal);
        Assert.Contains("WinHandleGuard::UniqueWinHandle hProcfHash(OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, FALSE, dwPidfHash));", legacyShell, StringComparison.Ordinal);
        Assert.Contains("0, 0, FALSE,", legacyShell, StringComparison.Ordinal);
        Assert.DoesNotContain("0, 0, TRUE,", legacyShell, StringComparison.Ordinal);

        Assert.Contains("#include \"WinCommon/WinHandleGuard.h\"", windowsUtils, StringComparison.Ordinal);
        Assert.Contains("WinHandleGuard::UniqueFindHandle hFind", windowsUtils, StringComparison.Ordinal);
        Assert.Contains("WinHandleGuard::UniqueModuleHandle hModule", windowsUtils, StringComparison.Ordinal);
        Assert.DoesNotContain("FreeLibrary(hModule);", windowsUtils, StringComparison.Ordinal);
        Assert.Contains("bool IsAcceptableContextMenuDeleteResult(LONG deleteResult)", windowsUtils, StringComparison.Ordinal);
        Assert.Contains("deleteResult == ERROR_SUCCESS || deleteResult == ERROR_FILE_NOT_FOUND", windowsUtils, StringComparison.Ordinal);
        Assert.Contains("deleteSucceeded = deleteSucceeded && IsAcceptableContextMenuDeleteResult(keyShell.RecurseDeleteKey(CONTEXT_MENU_ITEM_EN_US));", windowsUtils, StringComparison.Ordinal);
        Assert.Contains("deleteSucceeded = deleteSucceeded && IsAcceptableContextMenuDeleteResult(keyShellEx.RecurseDeleteKey(_T(\"LHashShellExt\")));", windowsUtils, StringComparison.Ordinal);
        Assert.DoesNotContain("lResult &= keyShell.RecurseDeleteKey", windowsUtils, StringComparison.Ordinal);
        Assert.DoesNotContain("lResult &= keyShellEx.RecurseDeleteKey", windowsUtils, StringComparison.Ordinal);
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
        string md5 = RepositoryTestContext.ReadTextFile(@"trunk\source\Algorithms\MD5.cpp");
        string sha1 = RepositoryTestContext.ReadTextFile(@"trunk\source\Algorithms\SHA1.cpp");
        string uiBridge = RepositoryTestContext.ReadTextFile(@"trunk\source\WinMFC\UIBridgeMFC.cpp");
        string uiBridgeHeader = RepositoryTestContext.ReadTextFile(@"trunk\source\WinMFC\UIBridgeMFC.h");
        string nativeRuntimeTests = RepositoryTestContext.ReadTextFile(@"native-runtime-tests\FHash.NativeRuntimeTests\HashEngineRuntimeTests.cpp");

        Assert.Contains("static const unsigned char PADDING[64]", md5, StringComparison.Ordinal);
        Assert.DoesNotContain("static unsigned char PADDING[64]", md5, StringComparison.Ordinal);
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
        string runtimeTests = RepositoryTestContext.ReadTextFile(@"native-runtime-tests\FHash.NativeRuntimeTests\HashEngineRuntimeTests.cpp");
        string nativeCoreProject = RepositoryTestContext.ReadTextFile(@"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj");
        string uwpNativeProject = RepositoryTestContext.ReadTextFile(@"sub-proj\fHashUwpNative\fHashUwpNative.vcxproj");
        string securityProgram = RepositoryTestContext.ReadTextFile(@"security-tests\SecurityRegression\Program.cs");
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
        Assert.Contains("FHashBlake3SimdProfile", nativeCoreProject, StringComparison.Ordinal);
        Assert.Contains("Condition=\"'$(FHashBlake3SimdProfile)'=='portable'\">BLAKE3_USE_NEON=0;BLAKE3_NO_SSE2;BLAKE3_NO_SSE41;BLAKE3_NO_AVX2;BLAKE3_NO_AVX512;%(PreprocessorDefinitions)</PreprocessorDefinitions>", nativeCoreProject, StringComparison.Ordinal);
        Assert.Contains("ExcludedFromBuild Condition=\"'$(FHashBlake3SimdProfile)'=='portable'\"", nativeCoreProject, StringComparison.Ordinal);
        Assert.Contains("Condition=\"'$(Platform)'=='Win32'\">/arch:SSE2", nativeCoreProject, StringComparison.Ordinal);
        Assert.Contains("Condition=\"'$(Platform)'=='Win32'\">/arch:AVX", nativeCoreProject, StringComparison.Ordinal);
        Assert.Contains("/arch:AVX2", nativeCoreProject, StringComparison.Ordinal);
        Assert.Contains("/arch:AVX512", nativeCoreProject, StringComparison.Ordinal);
        Assert.Contains(@"blake3_neon.c", uwpNativeProject, StringComparison.Ordinal);
        Assert.Contains("Condition=\"'$(Platform)'=='ARM64'\">BLAKE3_USE_NEON=1", uwpNativeProject, StringComparison.Ordinal);
        Assert.Contains("Condition=\"'$(Platform)'=='Win32'\">/arch:SSE2", uwpNativeProject, StringComparison.Ordinal);
        Assert.Contains("/arch:AVX512", uwpNativeProject, StringComparison.Ordinal);

        Assert.Contains("Windows junction attack harness reproduces ancestor reparse-point traversal", securityProgram, StringComparison.Ordinal);
        Assert.Contains("Windows hash-style open harness reproduces sharing violations for locked files", securityProgram, StringComparison.Ordinal);
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
        string runtimeTests = RepositoryTestContext.ReadTextFile(@"native-runtime-tests\FHash.NativeRuntimeTests\HashEngineRuntimeTests.cpp");
        string nativeCoreProject = RepositoryTestContext.ReadTextFile(@"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj");
        string uwpNativeProject = RepositoryTestContext.ReadTextFile(@"sub-proj\fHashUwpNative\fHashUwpNative.vcxproj");
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
        Assert.Contains(@"third_party\xxhash\0.8.3\xxhash.c", uwpNativeProject, StringComparison.Ordinal);
        Assert.Contains(@"third_party\crc32c\1.1.2\src\crc32c_arm64.cc", uwpNativeProject, StringComparison.Ordinal);
    }

    [Fact]
    public void OpenSslEvpIntegration_UsesOfficialVendorAndKeepsLegacySha2Distinct()
    {
        string registryCore = RepositoryTestContext.ReadTextFile(@"trunk\source\Domain\HashAlgorithmRegistryCore.h");
        string digestRegistry = RepositoryTestContext.ReadTextFile(@"trunk\source\Common\HashDigestOperationRegistry.cpp");
        string providerHeader = RepositoryTestContext.ReadTextFile(@"trunk\source\Runtime\Hash\OpenSslEvpHashProvider.h");
        string providerImplementation = RepositoryTestContext.ReadTextFile(@"trunk\source\Runtime\Hash\OpenSslEvpHashProvider.cpp");
        string runtimeTests = RepositoryTestContext.ReadTextFile(@"native-runtime-tests\FHash.NativeRuntimeTests\HashEngineRuntimeTests.cpp");
        string nativeCoreProject = RepositoryTestContext.ReadTextFile(@"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj");
        string uwpNativeProject = RepositoryTestContext.ReadTextFile(@"sub-proj\fHashUwpNative\fHashUwpNative.vcxproj");
        string workflow = RepositoryTestContext.ReadTextFile(@".github\workflows\windows-build.yml");
        string vendorTargets = RepositoryTestContext.ReadTextFile(@"NativeOpenSslVendor.targets");
        string vendorScript = RepositoryTestContext.ReadTextFile(@"trunk\build_openssl_vendor.ps1");
        string vendorNote = RepositoryTestContext.ReadTextFile(@"third_party\openssl\3.0.20\README.LHash.md");
        string licenseException = RepositoryTestContext.ReadTextFile(@"LICENSE-OPENSSL-EXCEPTION.md");

        Assert.Contains("{ \"openssl-sha-256\", \"SHA-256\", true, false }", registryCore, StringComparison.Ordinal);
        Assert.Contains("{ \"openssl-sha-384\", \"SHA-384\", true, false }", registryCore, StringComparison.Ordinal);
        Assert.Contains("{ \"openssl-sha-512\", \"SHA-512\", true, false }", registryCore, StringComparison.Ordinal);
        Assert.Contains("{ \"openssl-sha3-256\", \"SHA3-256\", true, false }", registryCore, StringComparison.Ordinal);
        Assert.Contains("{ \"openssl-sha3-384\", \"SHA3-384\", true, false }", registryCore, StringComparison.Ordinal);
        Assert.Contains("{ \"openssl-sha3-512\", \"SHA3-512\", true, false }", registryCore, StringComparison.Ordinal);
        Assert.Contains("{ \"openssl-blake2b-512\", \"BLAKE2b-512\", true, false }", registryCore, StringComparison.Ordinal);
        Assert.Contains("{ \"openssl-blake2s-256\", \"BLAKE2s-256\", true, false }", registryCore, StringComparison.Ordinal);
        Assert.Contains("{ \"openssl-shake128-256\", \"SHAKE128-256\", true, false }", registryCore, StringComparison.Ordinal);
        Assert.Contains("{ \"openssl-shake256-512\", \"SHAKE256-512\", true, false }", registryCore, StringComparison.Ordinal);
        Assert.Contains("{ \"sha256\", \"SHA256\", true, true }", registryCore, StringComparison.Ordinal);
        Assert.Contains("{ \"sha512\", \"SHA512\", true, true }", registryCore, StringComparison.Ordinal);

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
        Assert.Contains("EVP_DigestInit_ex2", providerImplementation, StringComparison.Ordinal);
        Assert.Contains("OSSL_DIGEST_PARAM_SIZE", providerImplementation, StringComparison.Ordinal);
        Assert.Contains("OSSL_PARAM_construct_size_t", providerImplementation, StringComparison.Ordinal);
        Assert.Contains("EVP_DigestUpdate", providerImplementation, StringComparison.Ordinal);
        Assert.Contains("EVP_DigestFinal_ex", providerImplementation, StringComparison.Ordinal);
        Assert.Contains("EVP_DigestFinalXOF", providerImplementation, StringComparison.Ordinal);
        Assert.DoesNotContain("static EVP_MD_CTX", providerImplementation, StringComparison.Ordinal);
        Assert.Contains("OPENSSL_SHA_384_OUTPUT_BYTES = 48", providerHeader, StringComparison.Ordinal);
        Assert.Contains("OPENSSL_SHA3_384_OUTPUT_BYTES = 48", providerHeader, StringComparison.Ordinal);
        Assert.Contains("OPENSSL_BLAKE2B_512_OUTPUT_BYTES = 64", providerHeader, StringComparison.Ordinal);
        Assert.Contains("OPENSSL_BLAKE2S_256_OUTPUT_BYTES = 32", providerHeader, StringComparison.Ordinal);
        Assert.Contains("OPENSSL_SHAKE256_512_OUTPUT_BYTES = 64", providerHeader, StringComparison.Ordinal);

        Assert.Contains(@"Runtime\Hash\OpenSslEvpHashProvider.cpp", nativeCoreProject, StringComparison.Ordinal);
        Assert.Contains(@"Runtime\Hash\OpenSslEvpHashProvider.cpp", uwpNativeProject, StringComparison.Ordinal);
        Assert.Contains("FHashOpenSslInstallRoot", vendorTargets, StringComparison.Ordinal);
        Assert.Contains("FHASH_WITH_OPENSSL3_VENDOR=1", vendorTargets, StringComparison.Ordinal);
        Assert.Contains("libcrypto.lib", vendorTargets, StringComparison.Ordinal);
        Assert.Contains("openssl-3.0.20", vendorScript, StringComparison.Ordinal);
        Assert.Contains("VC-WIN64A", vendorScript, StringComparison.Ordinal);
        Assert.Contains("VC-WIN64-ARM", vendorScript, StringComparison.Ordinal);
        Assert.Contains("build_openssl_vendor.ps1", workflow, StringComparison.Ordinal);
        Assert.Contains("/p:FHashOpenSslInstallRoot=$openSslRoot", workflow, StringComparison.Ordinal);

        Assert.Contains("OpenSSL Linking Exception", licenseException, StringComparison.Ordinal);
        Assert.Contains("Upstream tag: openssl-3.0.20", vendorNote, StringComparison.Ordinal);
        Assert.Contains("OpenSSL-backed algorithm descriptors are exposed through the registry with", vendorNote, StringComparison.Ordinal);
        Assert.Contains("legacy `sha256` / `sha512` ids and labels untouched", vendorNote, StringComparison.Ordinal);

        Assert.Contains("HashThreadFunc_ComputesOfficialOpenSslDigestsForKnownVector", runtimeTests, StringComparison.Ordinal);
        Assert.Contains("RunHashRequest_OpenSslSha2VariantsCanCoexistWithLegacySha2", runtimeTests, StringComparison.Ordinal);
        Assert.Contains("RunHashRequest_OpenSslUnknownIdsAreIgnoredAndKnownVariantsStayOrdered", runtimeTests, StringComparison.Ordinal);
        Assert.Contains("HashThreadFunc_OpenSslVariantsRemainStableAcrossConcurrentRuns", runtimeTests, StringComparison.Ordinal);
        Assert.Contains("CreateAlgorithmId(\"openssl-sha-256\")", runtimeTests, StringComparison.Ordinal);
        Assert.Contains("CreateAlgorithmId(\"openssl-blake2b-512\")", runtimeTests, StringComparison.Ordinal);
        Assert.Contains("CreateAlgorithmId(\"openssl-blake2s-256\")", runtimeTests, StringComparison.Ordinal);
        Assert.Contains("CreateAlgorithmId(\"openssl-sha3\")", runtimeTests, StringComparison.Ordinal);
    }

    [Fact]
    public void HashExecutionContext_UsesANonOwningSinkReferenceWithNullFallback()
    {
        string executionContext = RepositoryTestContext.ReadTextFile(@"trunk\source\Runtime\HashExecutionContext.h");

        Assert.Contains("class NullHashProgressSink : public HashProgressSink", executionContext, StringComparison.Ordinal);
        Assert.Contains("HashProgressSink& GetNullHashProgressSink()", executionContext, StringComparison.Ordinal);
        Assert.Contains("sink != NULL ? *sink : GetNullHashProgressSink()", executionContext, StringComparison.Ordinal);
        Assert.Contains("HashProgressSink& progressSinkObserver;", executionContext, StringComparison.Ordinal);
        Assert.DoesNotContain("HashProgressSink *progressSink;", executionContext, StringComparison.Ordinal);
    }
}
