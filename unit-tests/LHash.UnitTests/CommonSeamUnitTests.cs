namespace LHash.UnitTests;

public sealed class CommonSeamUnitTests
{
    [Fact]
    public void CoreCommonSurface_ContainsNoThreadDataLeakage()
    {
        string commonRoot = Path.Combine(RepositoryTestContext.RepoRoot, @"trunk\source\Common");

        List<string> threadDataLeakFiles = [];
        string[] candidates = Directory.GetFiles(commonRoot, "*.*", SearchOption.AllDirectories)
            .Where(path => path.EndsWith(".h", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".cpp", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        foreach (string candidate in candidates)
        {
            string contents = RepositoryTestContext.ReadTextFile(Path.GetRelativePath(RepositoryTestContext.RepoRoot, candidate).Replace('/', '\\'));
            if (!contents.Contains("ThreadData", StringComparison.Ordinal))
            {
                continue;
            }

            string relativePath = Path.GetRelativePath(RepositoryTestContext.RepoRoot, candidate).Replace('/', '\\');
            threadDataLeakFiles.Add(relativePath);
        }

        Assert.Empty(threadDataLeakFiles);
    }

    [Fact]
    public void LegacyCompatibilityForwardingShims_AreRemovedFromCommonBoundary()
    {
        string[] removedShimPaths =
        [
            @"trunk\source\Common\ThreadDataAccess.h",
            @"trunk\source\Common\ThreadDataExecutionAccess.h",
            @"trunk\source\Common\ThreadDataInputAccess.h",
            @"trunk\source\Common\ThreadDataResultAccess.h",
            @"trunk\source\Common\HashRequestProjection.h",
            @"trunk\source\Common\HashThreadEntryProjection.h",
            @"trunk\source\Common\HashThreadEntry.h",
            @"trunk\source\Common\HashThreadEntry.cpp",
            @"trunk\source\Common\HashThreadLaunch.h",
            @"trunk\source\Common\ManagedHashMgmtAccess.h"
        ];

        foreach (string relativePath in removedShimPaths)
        {
            string fullPath = Path.Combine(RepositoryTestContext.RepoRoot, relativePath);
            Assert.False(File.Exists(fullPath), $"{relativePath} should be removed from Common after the LegacyCompat boundary cleanup.");
        }
    }

    [Fact]
    public void NativeVcxprojToolset_IsUnifiedToV143()
    {
        string[] vcxProjects = Directory.GetFiles(RepositoryTestContext.RepoRoot, "*.vcxproj", SearchOption.AllDirectories);
        Assert.NotEmpty(vcxProjects);

        foreach (string projectPath in vcxProjects)
        {
            string relativePath = Path.GetRelativePath(RepositoryTestContext.RepoRoot, projectPath).Replace('/', '\\');
            string projectContents = RepositoryTestContext.ReadTextFile(relativePath);

            Assert.DoesNotContain("<PlatformToolset>v141</PlatformToolset>", projectContents, StringComparison.Ordinal);
            Assert.DoesNotContain("<PlatformToolset>v145</PlatformToolset>", projectContents, StringComparison.Ordinal);
            Assert.Contains("<PlatformToolset>v143</PlatformToolset>", projectContents, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void HashAlgorithmRegistry_DefinesStableCompatibilityOrder()
    {
        string registryCore = RepositoryTestContext.ReadUtf8File(@"trunk\source\Domain\HashAlgorithmRegistryCore.h");
        string registryTypeCompat = RepositoryTestContext.ReadUtf8File(@"trunk\source\Adapters\ThreadDataBridge\HashAlgorithmTypeCompat.h");
        string legacyDigestType = RepositoryTestContext.ReadUtf8File(@"trunk\source\Adapters\ThreadDataBridge\ResultDigestTypeCompat.h");
        string global = RepositoryTestContext.ReadUtf8File(@"trunk\source\Common\Global.h");
        string legacyRegistryShimPath = Path.Combine(RepositoryTestContext.RepoRoot, @"trunk\source\Common\HashAlgorithmRegistry.h");

        Assert.False(File.Exists(legacyRegistryShimPath));
        RepositoryTestContext.AssertContainsInOrder(
            registryCore,
            "RegisterHashAlgorithmDescriptorUnlocked({ \"md5\", \"MD5 (Deprecated)\", true, false });",
            "RegisterHashAlgorithmDescriptorUnlocked({ \"sha1\", \"SHA1 (Deprecated)\", true, false });",
            "RegisterHashAlgorithmDescriptorUnlocked({ \"openssl-sha-256\", \"SHA-256\", true, true });",
            "RegisterHashAlgorithmDescriptorUnlocked({ \"openssl-sha-384\", \"SHA-384\", true, false });",
            "RegisterHashAlgorithmDescriptorUnlocked({ \"openssl-sha-512\", \"SHA-512\", true, true });",
            "RegisterHashAlgorithmDescriptorUnlocked({ \"openssl-sha3-256\", \"SHA3-256\", true, true });",
            "RegisterHashAlgorithmDescriptorUnlocked({ \"openssl-sha3-384\", \"SHA3-384\", true, false });",
            "RegisterHashAlgorithmDescriptorUnlocked({ \"openssl-sha3-512\", \"SHA3-512\", true, false });",
            "RegisterHashAlgorithmDescriptorUnlocked({ \"openssl-blake2b-512\", \"BLAKE2b-512\", true, false });",
            "RegisterHashAlgorithmDescriptorUnlocked({ \"openssl-blake2s-256\", \"BLAKE2s-256\", true, false });",
            "RegisterHashAlgorithmDescriptorUnlocked({ \"openssl-shake128-256\", \"SHAKE128-256\", true, false });",
            "RegisterHashAlgorithmDescriptorUnlocked({ \"openssl-shake256-512\", \"SHAKE256-512\", true, false });",
            "RegisterHashAlgorithmDescriptorUnlocked({ \"blake3-256\", \"BLAKE3-256\", true, false });",
            "RegisterHashAlgorithmDescriptorUnlocked({ \"blake3-512\", \"BLAKE3-512\", true, false });",
            "RegisterHashAlgorithmDescriptorUnlocked({ \"blake3-xof\", \"BLAKE3 XOF\", true, false });",
            "RegisterHashAlgorithmDescriptorUnlocked({ \"xxh3-64\", \"XXH3-64\", true, false });",
            "RegisterHashAlgorithmDescriptorUnlocked({ \"xxh3-128\", \"XXH3-128\", true, false });",
            "RegisterHashAlgorithmDescriptorUnlocked({ \"crc32c\", \"CRC32C\", true, false });");
        Assert.Contains("RegisterHashAlgorithmDescriptorUnlocked({ \"openssl-sha-256\", \"SHA-256\", true, true });", registryCore, StringComparison.Ordinal);
        Assert.Contains("RegisterHashAlgorithmDescriptorUnlocked({ \"openssl-sha-512\", \"SHA-512\", true, true });", registryCore, StringComparison.Ordinal);
        Assert.Contains("struct HashAlgorithmDescriptorRegistry", registryCore, StringComparison.Ordinal);
        Assert.Contains("GetHashAlgorithmDescriptorRegistry()", registryCore, StringComparison.Ordinal);
        Assert.Contains("GetHashAlgorithmDescriptorSnapshot()", registryCore, StringComparison.Ordinal);
        Assert.Contains("GetMutableHashAlgorithmDescriptorStorage()", registryCore, StringComparison.Ordinal);
        Assert.Contains("GetHashAlgorithmDescriptorRegistryMutex()", registryCore, StringComparison.Ordinal);
        Assert.Contains("std::lock_guard<std::mutex>", registryCore, StringComparison.Ordinal);
        Assert.Contains("RegisterHashAlgorithmDescriptor(const HashAlgorithmDescriptor& algorithmDescriptor)", registryCore, StringComparison.Ordinal);
        Assert.Contains("EnsureDefaultHashAlgorithmDescriptorsRegistered()", registryCore, StringComparison.Ordinal);
        Assert.Contains("typedef sunjwbase::tstring HashAlgorithmId;", registryCore, StringComparison.Ordinal);
        Assert.Contains("bool requiresDigestOperations;", registryCore, StringComparison.Ordinal);
        Assert.Contains("bool enabledByDefault;", registryCore, StringComparison.Ordinal);
        Assert.Contains("DoesHashAlgorithmDescriptorRequireDigestOperations(const HashAlgorithmDescriptor& algorithmDescriptor)", registryCore, StringComparison.Ordinal);
        Assert.Contains("IsHashAlgorithmDescriptorEnabledByDefault(const HashAlgorithmDescriptor& algorithmDescriptor)", registryCore, StringComparison.Ordinal);
        Assert.Contains("NormalizeHashAlgorithmId(const HashAlgorithmId& algorithmId)", registryCore, StringComparison.Ordinal);
        Assert.Contains("TryGetHashAlgorithmDescriptorById(const HashAlgorithmId& algorithmId, HashAlgorithmDescriptor *algorithmDescriptor)", registryCore, StringComparison.Ordinal);
        Assert.Contains("ClearHashAlgorithmDescriptorsForTesting()", registryCore, StringComparison.Ordinal);
        Assert.Contains("ResetHashAlgorithmDescriptorsToDefaultsForTesting()", registryCore, StringComparison.Ordinal);
        Assert.Contains("enum ResultDigestType", legacyDigestType, StringComparison.Ordinal);
        Assert.Contains("RESULT_DIGEST_UNKNOWN = -1", legacyDigestType, StringComparison.Ordinal);
        Assert.DoesNotContain("enum ResultDigestType", global, StringComparison.Ordinal);
        Assert.Contains("GetUnknownHashAlgorithmDescriptor()", registryCore, StringComparison.Ordinal);
        Assert.Contains("TryGetHashAlgorithmTypeById(const HashAlgorithmId& algorithmId, ResultDigestType *digestType)", registryTypeCompat, StringComparison.Ordinal);
        Assert.Contains("TryGetHashAlgorithmIndex(ResultDigestType digestType, int *algorithmIndex)", registryTypeCompat, StringComparison.Ordinal);
        Assert.Contains("TryGetHashAlgorithmDescriptor(ResultDigestType digestType, HashAlgorithmDescriptor *algorithmDescriptor)", registryTypeCompat, StringComparison.Ordinal);
        Assert.DoesNotContain("return algorithmDescriptors[0];", registryCore, StringComparison.Ordinal);
        Assert.DoesNotContain("compatibilityValueField", registryCore, StringComparison.Ordinal);
        Assert.DoesNotContain("ResultDigestType type;", registryCore, StringComparison.Ordinal);
        Assert.DoesNotContain("const HashAlgorithmDescriptor **algorithmDescriptor", registryCore, StringComparison.Ordinal);
    }

    [Fact]
    public void RegistryUnlockedHelpers_DoNotReenterTheirOwnMutexes()
    {
        string registryCore = RepositoryTestContext.ReadUtf8File(@"trunk\source\Domain\HashAlgorithmRegistryCore.h");
        string digestRegistry = RepositoryTestContext.ReadUtf8File(@"trunk\source\Common\HashDigestOperationRegistry.cpp");

        Assert.DoesNotContain(
            "RegisterHashAlgorithmDescriptorUnlocked(const HashAlgorithmDescriptor& algorithmDescriptor)\r\n{\r\n\tif (!IsHashAlgorithmDescriptorValid(algorithmDescriptor))\r\n\t{\r\n\t\treturn false;\r\n\t}\r\n\r\n\tstd::lock_guard<std::mutex> lock(GetHashAlgorithmDescriptorRegistryMutex());",
            registryCore,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "RegisterHashDigestOperationDescriptorUnlocked(const HashDigestOperationDescriptor& operationDescriptor)\r\n\t{\r\n\t\tHashAlgorithmId normalizedAlgorithmId = ResolveHashDigestOperationDescriptorAlgorithmId(operationDescriptor);\r\n\t\tif (normalizedAlgorithmId.empty())\r\n\t\t{\r\n\t\t\treturn false;\r\n\t\t}\r\n\r\n\t\tstd::lock_guard<std::mutex> lock(GetHashDigestOperationRegistryMutex());",
            digestRegistry,
            StringComparison.Ordinal);
        Assert.Contains("BuildRegistryOrderedHashDigestOperationDescriptorSnapshot(", digestRegistry, StringComparison.Ordinal);
        Assert.Contains("GetRegisteredHashAlgorithmDescriptors()", digestRegistry, StringComparison.Ordinal);
    }

    [Fact]
    public void DigestContextFinalization_SkipsDescriptorOnlyAlgorithms()
    {
        string hashDigestContextOps = RepositoryTestContext.ReadUtf8File(@"trunk\source\Common\HashDigestContextOps.cpp");

        Assert.Contains("TryGetHashAlgorithmDescriptorById(algorithmId, &algorithmDescriptor)", hashDigestContextOps, StringComparison.Ordinal);
        Assert.Contains("!DoesHashAlgorithmDescriptorRequireDigestOperations(algorithmDescriptor)", hashDigestContextOps, StringComparison.Ordinal);
        Assert.Contains("return true;", hashDigestContextOps, StringComparison.Ordinal);
    }

    [Fact]
    public void LegacyMd5Source_SeparatesStandardInit_FromSeededLegacyVariant()
    {
        string md5Header = RepositoryTestContext.ReadUtf8File(@"trunk\source\Algorithms\MD5.h");
        string md5Source = RepositoryTestContext.ReadUtf8File(@"trunk\source\Algorithms\MD5.cpp");

        Assert.Contains("void MD5Init (MD5_CTX *mdContext);", md5Header, StringComparison.Ordinal);
        Assert.Contains("void MD5InitSeededLegacy (MD5_CTX *mdContext, uint32_t pseudoRandomNumber);", md5Header, StringComparison.Ordinal);
        Assert.DoesNotContain("void MD5Init (MD5_CTX *mdContext, uint32_t pseudoRandomNumber = 0);", md5Header, StringComparison.Ordinal);
        Assert.Contains("void MD5Init (MD5_CTX *mdContext)", md5Source, StringComparison.Ordinal);
        Assert.Contains("void MD5InitSeededLegacy (MD5_CTX *mdContext, uint32_t pseudoRandomNumber)", md5Source, StringComparison.Ordinal);
        Assert.Contains("MD5Init(mdContext);", md5Source, StringComparison.Ordinal);
        Assert.DoesNotContain("void MD5Init (MD5_CTX *mdContext, uint32_t pseudoRandomNumber)", md5Source, StringComparison.Ordinal);
        Assert.DoesNotContain("Added support for randomizing initialization constants", md5Header, StringComparison.Ordinal);
        Assert.DoesNotContain("Added support for randomizing initialization constants", md5Source, StringComparison.Ordinal);
    }

    [Fact]
    public void LegacySeededMd5Init_IsNotUsedByNormalSourcePaths()
    {
        string[] candidateFiles = Directory.GetFiles(RepositoryTestContext.RepoRoot, "*.*", SearchOption.AllDirectories)
            .Where(path =>
                (path.EndsWith(".h", StringComparison.OrdinalIgnoreCase) ||
                 path.EndsWith(".hpp", StringComparison.OrdinalIgnoreCase) ||
                 path.EndsWith(".c", StringComparison.OrdinalIgnoreCase) ||
                 path.EndsWith(".cc", StringComparison.OrdinalIgnoreCase) ||
                 path.EndsWith(".cpp", StringComparison.OrdinalIgnoreCase)) &&
                !path.Contains(@"\unit-tests\", StringComparison.OrdinalIgnoreCase) &&
                !path.Contains(@"\security-tests\", StringComparison.OrdinalIgnoreCase) &&
                !path.Contains(@"\refactor-tests\", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        List<string> invalidCallSites = [];
        foreach (string candidate in candidateFiles)
        {
            string relativePath = Path.GetRelativePath(RepositoryTestContext.RepoRoot, candidate).Replace('/', '\\');
            if (relativePath.Equals(@"trunk\source\Algorithms\MD5.h", StringComparison.OrdinalIgnoreCase) ||
                relativePath.Equals(@"trunk\source\Algorithms\MD5.cpp", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string contents = RepositoryTestContext.ReadTextFile(relativePath);
            if (contents.Contains("MD5InitSeededLegacy(", StringComparison.Ordinal))
            {
                invalidCallSites.Add(relativePath);
            }
        }

        Assert.Empty(invalidCallSites);
    }

    [Fact]
    public void LegacySha1FileHashHelper_IsRemoved_FromActiveSource()
    {
        string sha1Header = RepositoryTestContext.ReadUtf8File(@"trunk\source\Algorithms\SHA1.h");
        string sha1Source = RepositoryTestContext.ReadUtf8File(@"trunk\source\Algorithms\SHA1.cpp");

        Assert.DoesNotContain("HashFile(", sha1Header, StringComparison.Ordinal);
        Assert.DoesNotContain("HashFile(", sha1Source, StringComparison.Ordinal);
        Assert.DoesNotContain("fopen(", sha1Source, StringComparison.Ordinal);
        Assert.DoesNotContain("fread(", sha1Source, StringComparison.Ordinal);
        Assert.DoesNotContain("ferror(", sha1Source, StringComparison.Ordinal);
    }

    [Fact]
    public void HashDigestBufferPlan_UsesExplicitDefaultFactory_AndNamedConstant()
    {
        string digestBufferPlanHeader = RepositoryTestContext.ReadUtf8File(@"trunk\source\Common\HashDigestBufferPlan.h");
        string digestBufferPlan = RepositoryTestContext.ReadUtf8File(@"trunk\source\Common\HashDigestBufferPlan.cpp");
        string jobExecutionPlan = RepositoryTestContext.ReadUtf8File(@"trunk\source\Common\HashJobExecutionPlan.cpp");

        Assert.Contains("kDefaultHashBufferLength = 4u * 1024u * 1024u;", digestBufferPlanHeader, StringComparison.Ordinal);
        Assert.Contains("HashDigestBufferPlan CreateDefaultHashDigestBufferPlan();", digestBufferPlanHeader, StringComparison.Ordinal);
        Assert.DoesNotContain("CreateHashDigestBufferPlan(const HashRequest& request, HashDigestExecutionMode digestExecutionMode);", digestBufferPlanHeader, StringComparison.Ordinal);
        Assert.Contains("HashDigestBufferPlan CreateDefaultHashDigestBufferPlan()", digestBufferPlan, StringComparison.Ordinal);
        Assert.Contains("digestBufferPlan.preferredBufferLength = kDefaultHashBufferLength;", digestBufferPlan, StringComparison.Ordinal);
        Assert.DoesNotContain("1048576", digestBufferPlan, StringComparison.Ordinal);
        Assert.Contains("executionPlan->digestBufferPlan = CreateDefaultHashDigestBufferPlan();", jobExecutionPlan, StringComparison.Ordinal);
    }

    [Fact]
    public void HashPerformanceHotPaths_UseLightweightYieldSnapshotRefreshAndCachedDigestDefinitions()
    {
        string fileAttemptWorkflow = RepositoryTestContext.ReadUtf8File(@"trunk\source\Common\HashFileAttemptWorkflow.cpp");
        string preScanSizeProbe = RepositoryTestContext.ReadUtf8File(@"trunk\source\Common\HashPreScanSizeProbe.cpp");
        string digestQueue = RepositoryTestContext.ReadUtf8File(@"trunk\source\Common\HashDigestQueue.cpp");
        string bridgeHeader = RepositoryTestContext.ReadUtf8File(@"trunk\source\WinMFC\UIBridgeMFC.h");
        string bridgeImplementation = RepositoryTestContext.ReadUtf8File(@"trunk\source\WinMFC\UIBridgeMFC.cpp");
        string dialog = RepositoryTestContext.ReadUtf8File(@"trunk\source\WinMFC\FilesHashDlg.cpp");
        string lifecycleController = RepositoryTestContext.ReadUtf8File(@"trunk\source\WinMFC\FilesHashLifecycleController.cpp");
        string taskListResource = RepositoryTestContext.ReadUtf8File(@"trunk\source\WinMFC\fileshash.rc");
        string progressController = RepositoryTestContext.ReadUtf8File(@"trunk\source\WinMFC\FilesHashProgressController.cpp");
        string progressControllerHeader = RepositoryTestContext.ReadUtf8File(@"trunk\source\WinMFC\FilesHashProgressController.h");
        string providerImplementation = RepositoryTestContext.ReadUtf8File(@"trunk\source\Runtime\Hash\OpenSslEvpHashProvider.cpp");

        Assert.Contains("SwitchToThread();", fileAttemptWorkflow, StringComparison.Ordinal);
        Assert.DoesNotContain("Sleep(3);", fileAttemptWorkflow, StringComparison.Ordinal);

        Assert.Contains("#include \"OsUtils/OsFile.h\"", preScanSizeProbe, StringComparison.Ordinal);
        Assert.DoesNotContain("#include <Windows.h>", preScanSizeProbe, StringComparison.Ordinal);
        Assert.DoesNotContain("#if defined (_WIN32)", preScanSizeProbe, StringComparison.Ordinal);
        Assert.DoesNotContain("GetFileAttributesEx(path, GetFileExInfoStandard, &fileAttributes)", preScanSizeProbe, StringComparison.Ordinal);
        Assert.DoesNotContain("fileAttributes.nFileSizeHigh", preScanSizeProbe, StringComparison.Ordinal);
        Assert.Contains("sunjwbase::OsFile osFile(path);", preScanSizeProbe, StringComparison.Ordinal);
        Assert.Contains("if (!osFile.openReadScan())", preScanSizeProbe, StringComparison.Ordinal);
        Assert.Contains("int64_t fileLength = osFile.getLength();", preScanSizeProbe, StringComparison.Ordinal);
        Assert.Contains("if (fileLength <= 0)", preScanSizeProbe, StringComparison.Ordinal);
        Assert.Contains("return static_cast<uint64_t>(fileLength);", preScanSizeProbe, StringComparison.Ordinal);

        Assert.Contains("InterlockedCompareExchange(&m_refreshPending, 1, 0) == 0", bridgeHeader, StringComparison.Ordinal);
        Assert.Contains("InterlockedExchange(&m_refreshPending, 0);", bridgeHeader, StringComparison.Ordinal);
        Assert.Contains("InterlockedCompareExchange(&m_taskUpdatePending, 1, 0) == 0", bridgeHeader, StringComparison.Ordinal);
        Assert.Contains("void DrainPendingTaskUpdates(std::vector<FilesHashTaskUpdate>& taskUpdates);", bridgeHeader, StringComparison.Ordinal);
        Assert.Contains("MarkMainTextRefreshHandled();", lifecycleController, StringComparison.Ordinal);
        Assert.Contains("m_pendingTaskUpdates.push_back(taskUpdate);", bridgeImplementation, StringComparison.Ordinal);
        Assert.Contains("m_pendingTaskUpdateIndices[taskUpdate.path] = m_pendingTaskUpdates.size();", bridgeImplementation, StringComparison.Ordinal);
        Assert.Contains("taskUpdates.swap(m_pendingTaskUpdates);", bridgeImplementation, StringComparison.Ordinal);
        Assert.DoesNotContain("new FilesHashTaskUpdate(taskUpdate)", bridgeImplementation, StringComparison.Ordinal);
        Assert.Contains("m_uiBridgeMFC->DrainPendingTaskUpdates(taskUpdates);", dialog, StringComparison.Ordinal);
        Assert.Contains("m_hashProgressController.ApplyTaskUpdates(taskUpdates);", dialog, StringComparison.Ordinal);
        Assert.Contains("ON_NOTIFY(LVN_GETDISPINFO, IDC_TASK_LIST, &CFilesHashDlg::OnTaskListGetDispInfo)", dialog, StringComparison.Ordinal);
        Assert.Contains("void CFilesHashDlg::OnTaskListGetDispInfo", dialog, StringComparison.Ordinal);
        Assert.Contains("_tcsncpy_s(pDispInfo->item.pszText, pDispInfo->item.cchTextMax, displayText, _TRUNCATE);", dialog, StringComparison.Ordinal);
        Assert.Contains("LVS_OWNERDATA", taskListResource, StringComparison.Ordinal);

        Assert.Contains("void ApplyTaskUpdates(const std::vector<FilesHashTaskUpdate>& taskUpdates);", progressControllerHeader, StringComparison.Ordinal);
        Assert.Contains("int GetTaskRowCount() const;", progressControllerHeader, StringComparison.Ordinal);
        Assert.Contains("bool TryGetTaskRowDisplayText(int rowIndex, int subItem, CString *displayText) const;", progressControllerHeader, StringComparison.Ordinal);
        Assert.Contains("m_taskListCtrl->SetRedraw(FALSE);", progressController, StringComparison.Ordinal);
        Assert.Contains("m_taskListCtrl->SetRedraw(TRUE);", progressController, StringComparison.Ordinal);
        Assert.Contains("int ensureVisibleRowIndex = -1;", progressController, StringComparison.Ordinal);
        Assert.Contains("m_taskListCtrl->SetItemCountEx(static_cast<int>(m_taskRows.size()), LVSICF_NOINVALIDATEALL | LVSICF_NOSCROLL);", progressController, StringComparison.Ordinal);
        Assert.Contains("m_taskListCtrl->RedrawItems(rowIndex, rowIndex);", progressController, StringComparison.Ordinal);
        Assert.DoesNotContain("m_taskListCtrl->InsertItem(", progressController, StringComparison.Ordinal);
        Assert.DoesNotContain("m_taskListCtrl->SetItemText(", progressController, StringComparison.Ordinal);
        Assert.Contains("RefreshTaskRow(rowIndex, ensureVisible);", progressController, StringComparison.Ordinal);
        Assert.Contains("if (ensureVisible)", progressController, StringComparison.Ordinal);
        Assert.Contains("m_taskListCtrl->EnsureVisible(rowIndex, FALSE);", progressController, StringComparison.Ordinal);

        Assert.Contains("GetCachedDigestImplementation", providerImplementation, StringComparison.Ordinal);
        Assert.Contains("digestImplementationCache.digestImplementations", providerImplementation, StringComparison.Ordinal);
        Assert.DoesNotContain("EVP_MD_free(digestImplementation);", providerImplementation, StringComparison.Ordinal);

        Assert.Contains("vector<unique_ptr<DigestDataBuffer>> digestBufferPool;", digestQueue, StringComparison.Ordinal);
        Assert.Contains("queue<size_t> availableBufferIndices;", digestQueue, StringComparison.Ordinal);
        Assert.Contains("queue<size_t> queuedBufferIndices;", digestQueue, StringComparison.Ordinal);
        Assert.Contains("availableBufferIndices.push(bufferIndex);", digestQueue, StringComparison.Ordinal);
        Assert.Contains("queuedBufferIndices.push(bufferIndex);", digestQueue, StringComparison.Ordinal);
        Assert.DoesNotContain("make_unique<DigestDataBuffer>(preferredBufferLength)", digestQueue, StringComparison.Ordinal);
    }

    [Fact]
    public void PosixStringHelpers_AvoidGlobalLocaleAndAsciiBridgeFallbacks()
    {
        string strhelper = RepositoryTestContext.ReadUtf8File(@"trunk\source\Common\strhelper.cpp");

        Assert.Contains("std::wstring_convert<std::codecvt_utf8<wchar_t>>", strhelper, StringComparison.Ordinal);
        Assert.Contains("size_t iconvResult = iconv(cd, inleft > 0 ? &in : NULL, &inleft, &out, &outleft);", strhelper, StringComparison.Ordinal);
        Assert.Contains("if (errno == E2BIG)", strhelper, StringComparison.Ordinal);
        Assert.DoesNotContain("setlocale(LC_ALL", strhelper, StringComparison.Ordinal);
        Assert.DoesNotContain("wcstombs(", strhelper, StringComparison.Ordinal);
        Assert.DoesNotContain("mbstowcs(", strhelper, StringComparison.Ordinal);
        Assert.DoesNotContain("return striconv(wstrtostr(wstr), \"UTF-8\", \"ASCII\");", strhelper, StringComparison.Ordinal);
        Assert.DoesNotContain("return strtowstr(striconv(str, \"ASCII\", \"UTF-8\"));", strhelper, StringComparison.Ordinal);
    }

    [Fact]
    public void LegacyThreadDataAccess_OwnsDedicatedThreadDataSessionSurface()
    {
        string access = RepositoryTestContext.ReadUtf8File(@"trunk\source\Adapters\ThreadDataBridge\ThreadDataAccess.h");
        string legacyCommonShimPath = Path.Combine(RepositoryTestContext.RepoRoot, @"trunk\source\Common\ThreadDataAccess.h");

        Assert.False(File.Exists(legacyCommonShimPath));
        Assert.Contains("#include \"Adapters/ThreadDataBridge/ThreadDataExecutionAccess.h\"", access, StringComparison.Ordinal);
        Assert.Contains("#include \"Adapters/ThreadDataBridge/ThreadDataInputAccess.h\"", access, StringComparison.Ordinal);
        Assert.Contains("#include \"Adapters/ThreadDataBridge/ThreadDataResultAccess.h\"", access, StringComparison.Ordinal);
        Assert.Contains("ResetThreadDataForNewSession(ThreadData& threadData)", access, StringComparison.Ordinal);
        Assert.Contains("ResetThreadDataInputFiles(threadData);", access, StringComparison.Ordinal);
        Assert.Contains("ClearThreadDataResults(threadData);", access, StringComparison.Ordinal);
    }

    [Fact]
    public void ResultDigestAccess_UmbrellaShimDependsOnDedicatedSeams()
    {
        string access = RepositoryTestContext.ReadUtf8File(@"trunk\source\Common\ResultDigestAccess.h");

        Assert.Contains("#include \"Common/ResultDigestMetadataAccess.h\"", access, StringComparison.Ordinal);
        Assert.Contains("#include \"Common/ResultDigestStateAccess.h\"", access, StringComparison.Ordinal);
        Assert.Contains("#include \"Common/ResultDigestValueAccess.h\"", access, StringComparison.Ordinal);
        Assert.DoesNotContain("GetResultDigestState(const ResultData& result)", access, StringComparison.Ordinal);
        Assert.DoesNotContain("GetResultDigest(const ResultData& result, ResultDigestType digestType)", access, StringComparison.Ordinal);
    }

    [Fact]
    public void ResultDigestMetadataAccess_OwnsMetadataTraversalSurface()
    {
        string access = RepositoryTestContext.ReadUtf8File(@"trunk\source\Common\ResultDigestMetadataAccess.h");
        string typeCompat = RepositoryTestContext.ReadUtf8File(@"trunk\source\Adapters\ThreadDataBridge\ResultDigestTypeMetadataCompat.h");

        Assert.Contains("typedef HashAlgorithmDescriptor ResultDigestMetadata;", access, StringComparison.Ordinal);
        Assert.Contains("GetResultDigestCount()", access, StringComparison.Ordinal);
        Assert.Contains("GetResultDigestMetadataAt(int index)", access, StringComparison.Ordinal);
        Assert.Contains("GetResultDigestMetadataSnapshot()", access, StringComparison.Ordinal);
        Assert.Contains("VisitResultDigestMetadata(TResultDigestMetadataVisitor visitor)", access, StringComparison.Ordinal);
        Assert.Contains("VisitResultDigestIds(TResultDigestIdVisitor visitor)", access, StringComparison.Ordinal);
        Assert.Contains("VisitResultDigests(TResultDigestVisitor visitor)", typeCompat, StringComparison.Ordinal);
        Assert.DoesNotContain("ResetResultDigests(ResultData& result)", access, StringComparison.Ordinal);
    }

    [Fact]
    public void ResultDigestStateAccess_OwnsRegistrySizedStorageSurface()
    {
        string access = RepositoryTestContext.ReadUtf8File(@"trunk\source\Common\ResultDigestStateAccess.h");
        string typeCompat = RepositoryTestContext.ReadUtf8File(@"trunk\source\Adapters\ThreadDataBridge\ResultDigestTypeStateCompat.h");
        string global = RepositoryTestContext.ReadUtf8File(@"trunk\source\Common\Global.h");

        Assert.Contains("#include \"Common/Global.h\"", access, StringComparison.Ordinal);
        Assert.Contains("std::vector<sunjwbase::tstring> values;", global, StringComparison.Ordinal);
        Assert.Contains("std::vector<bool> enabled;", global, StringComparison.Ordinal);
        Assert.DoesNotContain("struct ResultDigestCompatibilityFields", global, StringComparison.Ordinal);
        Assert.Contains("GetDigestStorageValueById(const ResultDigestStorage& digestStorage, const HashAlgorithmId& algorithmId)", access, StringComparison.Ordinal);
        Assert.Contains("TryResolveDigestStorageIndexById(const HashAlgorithmId& algorithmId, size_t *digestIndex)", access, StringComparison.Ordinal);
        Assert.Contains("TryResolveDigestStorageIndex(ResultDigestType digestType, size_t *digestIndex)", typeCompat, StringComparison.Ordinal);
        Assert.Contains("TryGetMutableDigestStorageValueById(ResultDigestStorage& digestStorage, const HashAlgorithmId& algorithmId, sunjwbase::tstring **digestValue)", access, StringComparison.Ordinal);
        Assert.Contains("TryGetMutableStoredResultDigestById(ResultData& result, const HashAlgorithmId& algorithmId, sunjwbase::tstring **digestValue)", access, StringComparison.Ordinal);
        Assert.DoesNotContain("GetInvalidDigestStorageScratch()", access, StringComparison.Ordinal);
        Assert.DoesNotContain("GetMutableDigestStorageValueById(ResultDigestStorage& digestStorage, const HashAlgorithmId& algorithmId)", access, StringComparison.Ordinal);
        Assert.Contains("EnsureDigestStorageSize(ResultDigestStorage& digestStorage)", access, StringComparison.Ordinal);
        Assert.Contains("GetResultDigestState(const ResultData& result)", access, StringComparison.Ordinal);
        Assert.Contains("GetResultDigestStorage(const ResultData& result)", access, StringComparison.Ordinal);
        Assert.DoesNotContain("GetResultDigestCompatibilityFields(const ResultData& result)", access, StringComparison.Ordinal);
        Assert.DoesNotContain("GetCompatibilityResultDigest(const ResultData& result, ResultDigestType digestType)", access, StringComparison.Ordinal);
        Assert.DoesNotContain("HasAnyResultDigests(const ResultData& result)", access, StringComparison.Ordinal);
    }

    [Fact]
    public void ResultDigestValueAccess_OwnsReadWriteAndAggregateSurface()
    {
        string access = RepositoryTestContext.ReadUtf8File(@"trunk\source\Common\ResultDigestValueAccess.h");
        string typeCompat = RepositoryTestContext.ReadUtf8File(@"trunk\source\Adapters\ThreadDataBridge\ResultDigestTypeValueCompat.h");

        Assert.Contains("GetResultDigestById(const ResultData& result, const HashAlgorithmId& algorithmId)", access, StringComparison.Ordinal);
        Assert.Contains("return GetStoredResultDigestById(result, algorithmId);", access, StringComparison.Ordinal);
        Assert.Contains("TryGetMutableResultDigestById(ResultData& result, const HashAlgorithmId& algorithmId, sunjwbase::tstring **digestValue)", access, StringComparison.Ordinal);
        Assert.Contains("TryGetMutableResultDigest(ResultData& result, ResultDigestType digestType, sunjwbase::tstring **digestValue)", typeCompat, StringComparison.Ordinal);
        Assert.Contains("VisitResultDigestMetadataValues(const ResultData& result, TResultDigestMetadataValueVisitor visitor)", access, StringComparison.Ordinal);
        Assert.Contains("HasAnyResultDigests(const ResultData& result)", access, StringComparison.Ordinal);
        Assert.Contains("SetResultDigestById(ResultData& result, const HashAlgorithmId& algorithmId, const sunjwbase::tstring& digestValue)", access, StringComparison.Ordinal);
        Assert.Contains("SetResultDigest(ResultData& result, ResultDigestType digestType, const sunjwbase::tstring& digestValue)", typeCompat, StringComparison.Ordinal);
        Assert.Contains("ResetResultDigests(ResultData& result)", access, StringComparison.Ordinal);
        Assert.DoesNotContain("SetCompatibilityResultDigest(result, digestType, digestValue);", access, StringComparison.Ordinal);
        Assert.DoesNotContain("ClearCompatibilityResultDigest(result, digestType);", access, StringComparison.Ordinal);
        Assert.DoesNotContain("GetMutableResultDigest(ResultData& result, const HashAlgorithmId& algorithmId)", access, StringComparison.Ordinal);
    }

    [Fact]
    public void MfcAlgorithmSelection_ResetChecks_UsesRegistryDefaultEnablement()
    {
        string controller = RepositoryTestContext.ReadUtf8File(@"trunk\source\WinMFC\FilesHashAlgorithmSelectionController.cpp");

        Assert.Contains("ResetThreadDataHashAlgorithms(*m_threadData);", controller, StringComparison.Ordinal);
        Assert.Contains("IsHashAlgorithmDescriptorEnabledByDefault(algorithmDescriptor) ? BST_CHECKED : BST_UNCHECKED", controller, StringComparison.Ordinal);
        Assert.DoesNotContain("checkBox->SetCheck(BST_CHECKED);", controller, StringComparison.Ordinal);
    }

    [Fact]
    public void ResultNetProjection_OwnsSharedManagedProjectionPrimitives()
    {
        string resultNetProjection = RepositoryTestContext.ReadUtf8File(@"trunk\source\Common\ResultNetProjection.h");
        string resultDataProjection = RepositoryTestContext.ReadUtf8File(@"trunk\source\Common\ResultDataProjection.h");
        string hashResultProjection = RepositoryTestContext.ReadUtf8File(@"trunk\source\Common\HashResultProjection.h");

        Assert.Contains("static inline TResultStateNet ConvertResultStateToNet(ResultState resultState)", resultNetProjection, StringComparison.Ordinal);
        Assert.Contains("DispatchResultDigestValueById(const HashAlgorithmId& algorithmId, TMd5Action onMd5, TSha1Action onSha1, TSha256Action onSha256, TSha512Action onSha512)", resultNetProjection, StringComparison.Ordinal);
        Assert.Contains("static inline TResultDataNet AssignResultDigestToNetById(TResultDataNet resultDataNet, const HashAlgorithmId& algorithmId, TResultString digestValue)", resultNetProjection, StringComparison.Ordinal);
        Assert.DoesNotContain("#include \"Adapters/ThreadDataBridge/HashAlgorithmTypeCompat.h\"", resultNetProjection, StringComparison.Ordinal);
        Assert.DoesNotContain("DispatchResultDigestValueByType(ResultDigestType digestType, TMd5Action onMd5, TSha1Action onSha1, TSha256Action onSha256, TSha512Action onSha512)", resultNetProjection, StringComparison.Ordinal);
        Assert.DoesNotContain("static inline TResultDataNet AssignResultDigestToNet(TResultDataNet resultDataNet, ResultDigestType digestType, TResultString digestValue)", resultNetProjection, StringComparison.Ordinal);

        Assert.Contains("#include \"Common/ResultNetProjection.h\"", resultDataProjection, StringComparison.Ordinal);
        Assert.DoesNotContain("static inline TResultStateNet ConvertResultStateToNet(ResultState resultState)", resultDataProjection, StringComparison.Ordinal);
        Assert.DoesNotContain("DispatchResultDigestValueByType(ResultDigestType digestType, TMd5Action onMd5, TSha1Action onSha1, TSha256Action onSha256, TSha512Action onSha512)", resultDataProjection, StringComparison.Ordinal);
        Assert.DoesNotContain("static inline TResultDataNet AssignResultDigestToNet(TResultDataNet resultDataNet, ResultDigestType digestType, TResultString digestValue)", resultDataProjection, StringComparison.Ordinal);

        Assert.Contains("#include \"Common/ResultNetProjection.h\"", hashResultProjection, StringComparison.Ordinal);
        Assert.DoesNotContain("#include \"Common/ResultDataProjection.h\"", hashResultProjection, StringComparison.Ordinal);
    }

    [Fact]
    public void HashResultSearch_OwnsSharedTraversalAndMatchingPrimitives()
    {
        string hashResultSearch = RepositoryTestContext.ReadUtf8File(@"trunk\source\Common\HashResultSearch.h");
        string legacyThreadResultAccess = RepositoryTestContext.ReadUtf8File(@"trunk\source\Adapters\ThreadDataBridge\ThreadDataResultAccess.h");
        string legacyCommonShimPath = Path.Combine(RepositoryTestContext.RepoRoot, @"trunk\source\Common\ThreadDataResultAccess.h");

        Assert.Contains("NormalizeHashResultPathSearchText(const sunjwbase::tstring& pathText)", hashResultSearch, StringComparison.Ordinal);
        Assert.Contains("NormalizeHashResultDigestSearchText(const sunjwbase::tstring& digestText)", hashResultSearch, StringComparison.Ordinal);
        Assert.Contains("HashResultMatchesPathText(const HashResult& result, const sunjwbase::tstring& pathText)", hashResultSearch, StringComparison.Ordinal);
        Assert.Contains("HashResultMatchesPathAndDigestText(const HashResult& result, const sunjwbase::tstring& pathText, const sunjwbase::tstring& digestText)", hashResultSearch, StringComparison.Ordinal);
        Assert.Contains("VisitHashResults(const HashResultList& resultList, THashResultVisitor visitor)", hashResultSearch, StringComparison.Ordinal);
        Assert.Contains("VisitMatchingHashResults(const HashResultList& resultList, THashResultPredicate predicate, THashResultVisitor visitor)", hashResultSearch, StringComparison.Ordinal);
        Assert.Contains("CountMatchingHashResults(const HashResultList& resultList, THashResultPredicate predicate)", hashResultSearch, StringComparison.Ordinal);
        Assert.Contains("VisitDigestMatchingHashResults(const HashResultList& resultList, const sunjwbase::tstring& digestText, THashResultVisitor visitor)", hashResultSearch, StringComparison.Ordinal);
        Assert.Contains("VisitPathAndDigestMatchingHashResults(const HashResultList& resultList, const sunjwbase::tstring& pathText, const sunjwbase::tstring& digestText, THashResultVisitor visitor)", hashResultSearch, StringComparison.Ordinal);
        Assert.False(File.Exists(legacyCommonShimPath));
        Assert.Contains("VisitThreadDataHashResults(const ThreadData& threadData, THashResultVisitor visitor)", legacyThreadResultAccess, StringComparison.Ordinal);
        Assert.Contains("VisitThreadDataPathAndDigestMatchingHashResults(const ThreadData& threadData, const sunjwbase::tstring& pathText, const sunjwbase::tstring& digestText, THashResultVisitor visitor)", legacyThreadResultAccess, StringComparison.Ordinal);
    }

    [Fact]
    public void HashResultRender_OwnsSharedRealtimeRenderPrimitives()
    {
        string resultRender = RepositoryTestContext.ReadUtf8File(@"trunk\source\Common\ResultDataRender.h");
        string hashResultRender = RepositoryTestContext.ReadUtf8File(@"trunk\source\Common\HashResultRender.h");
        string mfcHeader = RepositoryTestContext.ReadUtf8File(@"trunk\source\WinMFC\UIBridgeMFC.h");
        Assert.Contains("ResultSizeDisplayInfo GetResultSizeDisplayInfo(uint64_t resultSize)", resultRender, StringComparison.Ordinal);
        Assert.Contains("return GetResultSizeDisplayInfo(GetResultSize(result));", resultRender, StringComparison.Ordinal);
        Assert.Contains("#include \"Common/HashResult.h\"", hashResultRender, StringComparison.Ordinal);
        Assert.Contains("GetHashResultSizeDisplayInfo(const HashResult& result)", hashResultRender, StringComparison.Ordinal);
        Assert.Contains("return GetResultSizeDisplayInfo(result.meta.size);", hashResultRender, StringComparison.Ordinal);
        Assert.Contains("VisitRenderableHashResultMetaLines(const HashResult& result, TResultMetaLineVisitor visitor)", hashResultRender, StringComparison.Ordinal);
        Assert.Contains("VisitHashResultDigestDisplayValues(const HashResult& result, bool uppercase, TResultDigestDisplayVisitor visitor)", hashResultRender, StringComparison.Ordinal);
        Assert.Contains("#include \"Common/HashResultRender.h\"", mfcHeader, StringComparison.Ordinal);
        Assert.DoesNotContain("#include \"Common/HashResultCompatibility.h\"", mfcHeader, StringComparison.Ordinal);
    }

    [Fact]
    public void HashResultCompatibility_IsRemoved_AfterRealtimeAndHistoryConsumersSwitchToHashResult()
    {
        string compatibilityPath = Path.Combine(RepositoryTestContext.RepoRoot, @"trunk\source\Common\HashResultCompatibility.h");
        string searchHeader = RepositoryTestContext.ReadTextFile(@"trunk\source\WinMFC\FilesHashSearchController.h");
        string searchSource = RepositoryTestContext.ReadTextFile(@"trunk\source\WinMFC\FilesHashSearchController.cpp");
        Assert.False(File.Exists(compatibilityPath));
        Assert.Contains("void AppendResult(const HashResult& result);", searchHeader, StringComparison.Ordinal);
        Assert.Contains("VisitThreadDataHashResults(*m_threadData, [&](const HashResult& result)", searchSource, StringComparison.Ordinal);
        Assert.Contains("VisitThreadDataPathAndDigestMatchingHashResults(*m_threadData, tstrFileToFind, tstrHashToFind, [&](const HashResult& result)", searchSource, StringComparison.Ordinal);
        Assert.DoesNotContain("AppendResult(ProjectHashResult(result));", searchSource, StringComparison.Ordinal);
    }

    [Fact]
    public void ResultDataCompatibilityShims_NowLayerOnHashResultProjectionAndSearchSeams()
    {
        string resultDataProjection = RepositoryTestContext.ReadTextFile(@"trunk\source\Common\ResultDataProjection.h");
        string resultDataSearch = RepositoryTestContext.ReadTextFile(@"trunk\source\Common\ResultDataSearch.h");

        Assert.Contains("#include \"Common/HashResultProjection.h\"", resultDataProjection, StringComparison.Ordinal);
        Assert.Contains("AssignHashResultCoreToNet<TResultDataNet, TResultStateNet>(resultDataNet, ProjectHashResult(result), convertString);", resultDataProjection, StringComparison.Ordinal);
        Assert.Contains("AssignHashResultDigestsToNet(resultDataNet, ProjectHashResult(result), convertString);", resultDataProjection, StringComparison.Ordinal);
        Assert.Contains("VisitProjectedHashResults<TResultDataNet, TResultStateNet>(resultList, convertString, visitor);", resultDataProjection, StringComparison.Ordinal);
        Assert.Contains("VisitProjectedDigestMatchingHashResults<TResultDataNet, TResultStateNet>(resultList, digestText, convertString, visitor);", resultDataProjection, StringComparison.Ordinal);
        Assert.Contains("CreateProjectedDigestMatchingHashResults<TResultDataNet, TResultStateNet, TResultArray>(resultList, digestText, createResultArray, convertString, setProjectedResult);", resultDataProjection, StringComparison.Ordinal);
        Assert.Contains("return CountDigestMatchingHashResults(resultList, digestText);", resultDataSearch, StringComparison.Ordinal);
    }

    [Fact]
    public void Workflow_RunsIndependentUnitTests_And_PreparesNativeOpenSslVendor_Once()
    {
        string workflow = RepositoryTestContext.ReadUtf8File(@".github\workflows\windows-build.yml");

        Assert.Contains("unit-tests:", workflow, StringComparison.Ordinal);
        Assert.Contains("dotnet restore unit-tests/LHash.UnitTests/LHash.UnitTests.csproj", workflow, StringComparison.Ordinal);
        Assert.Contains("dotnet test unit-tests/LHash.UnitTests/LHash.UnitTests.csproj --configuration Release --no-restore", workflow, StringComparison.Ordinal);
        Assert.Contains("prepare-openssl-vendor-x64:", workflow, StringComparison.Ordinal);
        Assert.Contains("Restore cached OpenSSL vendor x64", workflow, StringComparison.Ordinal);
        Assert.Contains("actions/cache@v4", workflow, StringComparison.Ordinal);
        Assert.Contains("name: LHash-openssl-vendor-x64", workflow, StringComparison.Ordinal);
        Assert.Contains("Download OpenSSL vendor x64 artifact", workflow, StringComparison.Ordinal);
        Assert.Contains("native-runtime-tests:", workflow, StringComparison.Ordinal);
        Assert.Contains("msbuild native-runtime-tests/LHash.NativeRuntimeTests/LHash.NativeRuntimeTests.vcxproj", workflow, StringComparison.Ordinal);
        Assert.Contains(@"native-runtime-tests\LHash.NativeRuntimeTests\x64\Release\LHash.NativeRuntimeTests.exe", workflow, StringComparison.Ordinal);
        RepositoryTestContext.AssertContainsInOrder(
            workflow,
            "build-windows-x64:",
            "needs:",
            "- security-regression",
            "- unit-tests",
            "- prepare-openssl-vendor-x64");
        Assert.DoesNotContain("prepare-openssl-vendor-x64:\r\n    needs:", workflow, StringComparison.Ordinal);
        Assert.DoesNotContain("build-uwp-bridge-x64:", workflow, StringComparison.Ordinal);
        Assert.DoesNotContain("build-wui-shell-ext-x64:", workflow, StringComparison.Ordinal);
        Assert.DoesNotContain("build-uwp-shell-ext-x64:", workflow, StringComparison.Ordinal);
    }

    [Fact]
    public void SourceLayerEntryPoints_ExposeDomainRuntimeAndLegacyCompatContracts()
    {
        string domainContracts = RepositoryTestContext.ReadUtf8File(@"trunk\source\Domain\HashDomainContracts.h");
        string runtimeContracts = RepositoryTestContext.ReadUtf8File(@"trunk\source\Runtime\HashRuntimeContracts.h");
        string legacyContracts = RepositoryTestContext.ReadUtf8File(@"trunk\source\Adapters\ThreadDataBridge\LegacyCompatibility.h");

        Assert.Contains("#include \"Domain/HashAlgorithmRegistryCore.h\"", domainContracts, StringComparison.Ordinal);
        Assert.Contains("#include \"Domain/HashRequest.h\"", domainContracts, StringComparison.Ordinal);
        Assert.Contains("#include \"Domain/HashResult.h\"", domainContracts, StringComparison.Ordinal);
        Assert.Contains("#include \"Domain/ProgressEvent.h\"", domainContracts, StringComparison.Ordinal);

        Assert.Contains("#include \"Runtime/HashExecutionContext.h\"", runtimeContracts, StringComparison.Ordinal);
        Assert.Contains("#include \"Runtime/HashProgressSink.h\"", runtimeContracts, StringComparison.Ordinal);
        Assert.Contains("#include \"Common/HashEngine.h\"", runtimeContracts, StringComparison.Ordinal);
        Assert.Contains("#include \"Runtime/HashDigestOperationRegistryRuntime.h\"", runtimeContracts, StringComparison.Ordinal);
        Assert.Contains("#include \"Common/HashSchedulerPlan.h\"", runtimeContracts, StringComparison.Ordinal);

        Assert.Contains("#include \"Adapters/ThreadDataBridge/LegacyThreadData.h\"", legacyContracts, StringComparison.Ordinal);
        Assert.Contains("#include \"Adapters/ThreadDataBridge/ThreadDataAccess.h\"", legacyContracts, StringComparison.Ordinal);
        Assert.Contains("#include \"Adapters/ThreadDataBridge/HashRequestProjection.h\"", legacyContracts, StringComparison.Ordinal);
    }

    [Fact]
    public void HashRequest_FileAccess_UsesCheckedIndexing()
    {
        string hashRequest = RepositoryTestContext.ReadUtf8File(@"trunk\source\Domain\HashRequest.h");

        Assert.Contains("assert(fileIndex < request.files.size());", hashRequest, StringComparison.Ordinal);
        Assert.Contains("return request.files.at(fileIndex);", hashRequest, StringComparison.Ordinal);
        Assert.DoesNotContain("return request.files[fileIndex];", hashRequest, StringComparison.Ordinal);
    }

    private static int CountOccurrences(string content, string needle)
    {
        int count = 0;
        int startIndex = 0;
        while (true)
        {
            int index = content.IndexOf(needle, startIndex, StringComparison.Ordinal);
            if (index < 0)
            {
                return count;
            }

            count++;
            startIndex = index + needle.Length;
        }
    }
}
