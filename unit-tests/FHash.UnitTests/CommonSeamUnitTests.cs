namespace FHash.UnitTests;

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
        string registryTypeCompat = RepositoryTestContext.ReadUtf8File(@"trunk\source\LegacyCompat\HashAlgorithmTypeCompat.h");
        string legacyDigestType = RepositoryTestContext.ReadUtf8File(@"trunk\source\LegacyCompat\ResultDigestTypeCompat.h");
        string global = RepositoryTestContext.ReadUtf8File(@"trunk\source\Common\Global.h");
        string legacyRegistryShimPath = Path.Combine(RepositoryTestContext.RepoRoot, @"trunk\source\Common\HashAlgorithmRegistry.h");

        Assert.False(File.Exists(legacyRegistryShimPath));
        RepositoryTestContext.AssertContainsInOrder(
            registryCore,
            "RegisterHashAlgorithmDescriptorUnlocked({ \"md5\", \"MD5 (Deprecated)\", true, false });",
            "RegisterHashAlgorithmDescriptorUnlocked({ \"sha1\", \"SHA1 (Deprecated)\", true, false });",
            "RegisterHashAlgorithmDescriptorUnlocked({ \"openssl-sha-256\", \"SHA-256\", true, true });",
            "RegisterHashAlgorithmDescriptorUnlocked({ \"openssl-sha-512\", \"SHA-512\", true, true });",
            "RegisterHashAlgorithmDescriptorUnlocked({ \"blake3-256\", \"BLAKE3-256\", true, false });",
            "RegisterHashAlgorithmDescriptorUnlocked({ \"blake3-512\", \"BLAKE3-512\", true, false });",
            "RegisterHashAlgorithmDescriptorUnlocked({ \"blake3-xof\", \"BLAKE3 XOF\", true, false });");
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
    public void ArchivedSha256Source_DropsDuplicateExtractAndStringMacros()
    {
        string sha256Source = RepositoryTestContext.ReadUtf8File(@"archive\legacy-algorithms\trunk\source\Algorithms\sha256.cpp");

        Assert.DoesNotContain("mutils_word8", sha256Source, StringComparison.Ordinal);
        Assert.Equal(1, CountOccurrences(sha256Source, "#ifndef EXTRACT_UCHAR"));
        Assert.Equal(1, CountOccurrences(sha256Source, "#define STRING2INT("));
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
                !path.Contains(@"\refactor-tests\", StringComparison.OrdinalIgnoreCase) &&
                !path.Contains(@"\archive\", StringComparison.OrdinalIgnoreCase))
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

        Assert.Contains("kDefaultHashBufferLength = 1u * 1024u * 1024u;", digestBufferPlanHeader, StringComparison.Ordinal);
        Assert.Contains("HashDigestBufferPlan CreateDefaultHashDigestBufferPlan();", digestBufferPlanHeader, StringComparison.Ordinal);
        Assert.DoesNotContain("CreateHashDigestBufferPlan(const HashRequest& request, HashDigestExecutionMode digestExecutionMode);", digestBufferPlanHeader, StringComparison.Ordinal);
        Assert.Contains("HashDigestBufferPlan CreateDefaultHashDigestBufferPlan()", digestBufferPlan, StringComparison.Ordinal);
        Assert.Contains("digestBufferPlan.preferredBufferLength = kDefaultHashBufferLength;", digestBufferPlan, StringComparison.Ordinal);
        Assert.DoesNotContain("1048576", digestBufferPlan, StringComparison.Ordinal);
        Assert.Contains("executionPlan->digestBufferPlan = CreateDefaultHashDigestBufferPlan();", jobExecutionPlan, StringComparison.Ordinal);
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
        string access = RepositoryTestContext.ReadUtf8File(@"trunk\source\LegacyCompat\ThreadDataAccess.h");
        string legacyCommonShimPath = Path.Combine(RepositoryTestContext.RepoRoot, @"trunk\source\Common\ThreadDataAccess.h");

        Assert.False(File.Exists(legacyCommonShimPath));
        Assert.Contains("#include \"LegacyCompat/ThreadDataExecutionAccess.h\"", access, StringComparison.Ordinal);
        Assert.Contains("#include \"LegacyCompat/ThreadDataInputAccess.h\"", access, StringComparison.Ordinal);
        Assert.Contains("#include \"LegacyCompat/ThreadDataResultAccess.h\"", access, StringComparison.Ordinal);
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
        string typeCompat = RepositoryTestContext.ReadUtf8File(@"trunk\source\LegacyCompat\ResultDigestTypeMetadataCompat.h");

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
        string typeCompat = RepositoryTestContext.ReadUtf8File(@"trunk\source\LegacyCompat\ResultDigestTypeStateCompat.h");
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
        string typeCompat = RepositoryTestContext.ReadUtf8File(@"trunk\source\LegacyCompat\ResultDigestTypeValueCompat.h");

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
        Assert.DoesNotContain("#include \"LegacyCompat/HashAlgorithmTypeCompat.h\"", resultNetProjection, StringComparison.Ordinal);
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
        string legacyThreadResultAccess = RepositoryTestContext.ReadUtf8File(@"trunk\source\LegacyCompat\ThreadDataResultAccess.h");
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
        string hashResultRender = RepositoryTestContext.ReadUtf8File(@"trunk\source\Common\HashResultRender.h");
        string mfcHeader = RepositoryTestContext.ReadUtf8File(@"trunk\source\WinMFC\UIBridgeMFC.h");
        string bridgeMacHeader = RepositoryTestContext.ReadUtf8File(@"trunk\source\OSXUI\UIBridgeMacSwift.h");

        Assert.Contains("#include \"Common/HashResult.h\"", hashResultRender, StringComparison.Ordinal);
        Assert.Contains("GetHashResultSizeDisplayInfo(const HashResult& result)", hashResultRender, StringComparison.Ordinal);
        Assert.Contains("VisitRenderableHashResultMetaLines(const HashResult& result, TResultMetaLineVisitor visitor)", hashResultRender, StringComparison.Ordinal);
        Assert.Contains("VisitHashResultDigestDisplayValues(const HashResult& result, bool uppercase, TResultDigestDisplayVisitor visitor)", hashResultRender, StringComparison.Ordinal);
        Assert.Contains("#include \"Common/HashResultRender.h\"", mfcHeader, StringComparison.Ordinal);
        Assert.DoesNotContain("#include \"Common/HashResultCompatibility.h\"", mfcHeader, StringComparison.Ordinal);
        Assert.DoesNotContain("#include \"Common/HashResultCompatibility.h\"", bridgeMacHeader, StringComparison.Ordinal);
    }

    [Fact]
    public void HashResultCompatibility_IsRemoved_AfterRealtimeAndHistoryConsumersSwitchToHashResult()
    {
        string compatibilityPath = Path.Combine(RepositoryTestContext.RepoRoot, @"trunk\source\Common\HashResultCompatibility.h");
        string searchHeader = RepositoryTestContext.ReadTextFile(@"trunk\source\WinMFC\FilesHashSearchController.h");
        string searchSource = RepositoryTestContext.ReadTextFile(@"trunk\source\WinMFC\FilesHashSearchController.cpp");
        string bridgeMacHeader = RepositoryTestContext.ReadTextFile(@"trunk\source\OSXUI\UIBridgeMacSwift.h");
        string bridgeMacSource = RepositoryTestContext.ReadTextFile(@"trunk\source\OSXUI\UIBridgeMacSwift.mm");
        string hashBridgeMac = RepositoryTestContext.ReadTextFile(@"trunk\source\OSXUI\HashBridge.mm");

        Assert.False(File.Exists(compatibilityPath));
        Assert.Contains("void AppendResult(const HashResult& result);", searchHeader, StringComparison.Ordinal);
        Assert.Contains("VisitThreadDataHashResults(*m_threadData, [&](const HashResult& result)", searchSource, StringComparison.Ordinal);
        Assert.Contains("VisitThreadDataPathAndDigestMatchingHashResults(*m_threadData, tstrFileToFind, tstrHashToFind, [&](const HashResult& result)", searchSource, StringComparison.Ordinal);
        Assert.DoesNotContain("AppendResult(ProjectHashResult(result));", searchSource, StringComparison.Ordinal);
        Assert.DoesNotContain("#include \"Common/HashResultCompatibility.h\"", bridgeMacHeader, StringComparison.Ordinal);
        Assert.Contains("static ResultDataSwift *ConvertHashResultToSwift(const HashResult& result);", bridgeMacHeader, StringComparison.Ordinal);
        Assert.DoesNotContain("CreateCompatibilityResultData(result);", bridgeMacSource, StringComparison.Ordinal);
        Assert.Contains("ConvertHashResultToSwift(const HashResult& result)", bridgeMacSource, StringComparison.Ordinal);
        Assert.Contains("VisitThreadDataHashResults(*_thrdData, [&](const HashResult& result)", hashBridgeMac, StringComparison.Ordinal);
        Assert.Contains("ConvertHashResultToSwift(result);", hashBridgeMac, StringComparison.Ordinal);
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
        Assert.Contains("dotnet restore unit-tests/FHash.UnitTests/FHash.UnitTests.csproj", workflow, StringComparison.Ordinal);
        Assert.Contains("dotnet test unit-tests/FHash.UnitTests/FHash.UnitTests.csproj --configuration Release --no-restore", workflow, StringComparison.Ordinal);
        Assert.Contains("prepare-openssl-vendor-x64:", workflow, StringComparison.Ordinal);
        Assert.Contains("Restore cached OpenSSL vendor x64", workflow, StringComparison.Ordinal);
        Assert.Contains("actions/cache@v4", workflow, StringComparison.Ordinal);
        Assert.Contains("name: FHash-openssl-vendor-x64", workflow, StringComparison.Ordinal);
        Assert.Contains("Download OpenSSL vendor x64 artifact", workflow, StringComparison.Ordinal);
        Assert.Contains("native-runtime-tests:", workflow, StringComparison.Ordinal);
        Assert.Contains("msbuild native-runtime-tests/FHash.NativeRuntimeTests/FHash.NativeRuntimeTests.vcxproj", workflow, StringComparison.Ordinal);
        Assert.Contains(@"native-runtime-tests\FHash.NativeRuntimeTests\x64\Release\FHash.NativeRuntimeTests.exe", workflow, StringComparison.Ordinal);
        RepositoryTestContext.AssertContainsInOrder(
            workflow,
            "build-legacy-x64:",
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
    public void TrunkRoot_ArchivesLegacySolutionWrappers_And_PlatformTrees_While_KeepingSharedSecurityTargetsAsSingleAuthority()
    {
        string[] archivedRelativePaths =
        [
            @"archive\legacy-projects\trunk\fHash.xcworkspace\contents.xcworkspacedata",
            @"archive\legacy-projects\trunk\fHashMacUI.xcodeproj\project.pbxproj",
            @"archive\legacy-projects\trunk\fhashwui17.sln",
            @"archive\legacy-projects\trunk\fhashwui18.slnx",
            @"archive\legacy-projects\trunk\fileshashuwp17.sln",
            @"archive\legacy-projects\trunk\package_macos_dmg.sh",
            @"archive\legacy-projects\trunk\package_win_mfc64.py",
            @"archive\legacy-platforms\trunk\fHashWUIWap\version.h",
            @"archive\legacy-platforms\trunk\fHashUwpWap\version.h",
            @"archive\legacy-platforms\trunk\source\WinUWP\Package.appxmanifest",
            @"archive\legacy-platforms\trunk\source\OSXUI\UIBridgeMacSwift.h",
            @"archive\legacy-platforms\sub-proj\fHashWinRtBridge\fHashWinRtBridge.vcxproj",
            @"archive\legacy-platforms\sub-proj\fHashUwpNative\fHashUwpNative.vcxproj",
            @"archive\legacy-platforms\sub-proj\fHashUwpShellExt\fHashUwpShellExt.vcxproj",
            @"archive\legacy-platforms\sub-proj\fHashWUIShellExt\fHashWUIShellExt.vcxproj"
        ];

        string[] removedFromTrunkRoot =
        [
            @"trunk\fHash.xcworkspace",
            @"trunk\fHashMacUI.xcodeproj",
            @"trunk\fhashwui17.sln",
            @"trunk\fhashwui18.slnx",
            @"trunk\fileshashuwp17.sln",
            @"trunk\package_macos_dmg.sh",
            @"trunk\package_win_mfc64.py",
            @"trunk\fHashWUIWap",
            @"trunk\fHashUwpWap",
            @"trunk\source\WinUWP",
            @"trunk\source\OSXUI",
            @"sub-proj\fHashWinRtBridge",
            @"sub-proj\fHashUwpNative",
            @"sub-proj\fHashUwpShellExt",
            @"sub-proj\fHashWUIShellExt"
        ];

        foreach (string relativePath in archivedRelativePaths)
        {
            string fullPath = Path.Combine(RepositoryTestContext.RepoRoot, relativePath);
            Assert.True(File.Exists(fullPath) || Directory.Exists(fullPath), $"{relativePath} should exist in archive after the trunk-root cleanup.");
        }

        foreach (string relativePath in removedFromTrunkRoot)
        {
            string fullPath = Path.Combine(RepositoryTestContext.RepoRoot, relativePath);
            Assert.False(File.Exists(fullPath) || Directory.Exists(fullPath), $"{relativePath} should no longer live in trunk root after archiving.");
        }

        string archiveReadme = RepositoryTestContext.ReadUtf8File(@"archive\README.md");
        string legacyProject = RepositoryTestContext.ReadUtf8File(@"trunk\fileshash.vcxproj");

        Assert.Contains("historical project shells and packaging scripts", archiveReadme, StringComparison.Ordinal);
        Assert.Contains("trunk/fileshash15.sln", archiveReadme, StringComparison.Ordinal);
        Assert.Contains("legacy-platforms/trunk/fHashWUIWap", archiveReadme, StringComparison.Ordinal);
        Assert.Contains("legacy-platforms/sub-proj/fHashWinRtBridge", archiveReadme, StringComparison.Ordinal);
        Assert.Contains("sub-proj/fHashClrBridge", archiveReadme, StringComparison.Ordinal);
        Assert.Contains("NativeSecurity.targets", legacyProject, StringComparison.Ordinal);
        Assert.DoesNotContain("<RandomizedBaseAddress>false</RandomizedBaseAddress>", legacyProject, StringComparison.Ordinal);
        Assert.DoesNotContain("<RandomizedBaseAddress>true</RandomizedBaseAddress>", legacyProject, StringComparison.Ordinal);
        Assert.DoesNotContain("<DataExecutionPrevention />", legacyProject, StringComparison.Ordinal);
        Assert.DoesNotContain("<DataExecutionPrevention>true</DataExecutionPrevention>", legacyProject, StringComparison.Ordinal);
    }

    [Fact]
    public void WinUiNativeStack_ReusesNativeCore_InsteadOfRecompilingCoreSources()
    {
        string nativeCoreProject = RepositoryTestContext.ReadUtf8File(@"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj");
        string winUiNativeProject = RepositoryTestContext.ReadUtf8File(@"sub-proj\fHashWUINative\fHashWUINative.vcxproj");
        string clrBridgeProject = RepositoryTestContext.ReadUtf8File(@"sub-proj\fHashClrBridge\fHashClrBridge.vcxproj");
        string previewWorkflow = RepositoryTestContext.ReadUtf8File(@".github\workflows\winui-preview-build.yml");

        Assert.Contains("<SolutionDir Condition=\"'$(SolutionDir)'==''\">$(ProjectDir)..\\..\\trunk\\</SolutionDir>", nativeCoreProject, StringComparison.Ordinal);
        Assert.Contains("<FHashRuntimeSuffix Condition=\"'$(FHashDynamicRuntime)'=='true'\">-md</FHashRuntimeSuffix>", nativeCoreProject, StringComparison.Ordinal);
        Assert.Contains(@"$(ProjectDir);$(ProjectDir)..\..\trunk\source\;$(SolutionDir)source\", nativeCoreProject, StringComparison.Ordinal);
        Assert.Contains(@"$(ProjectDir)..\..\third_party\blake3\1.8.4\c", nativeCoreProject, StringComparison.Ordinal);
        Assert.Contains("<UseOfMfc Condition=\"'$(FHashDynamicRuntime)'=='true'\">Dynamic</UseOfMfc>", nativeCoreProject, StringComparison.Ordinal);
        Assert.Contains("<RuntimeLibrary Condition=\"'$(FHashDynamicRuntime)'=='true'\">MultiThreadedDLL</RuntimeLibrary>", nativeCoreProject, StringComparison.Ordinal);
        Assert.Contains(@"$(MSBuildProjectName)$(FHashRuntimeSuffix)", nativeCoreProject, StringComparison.Ordinal);
        Assert.Contains(@"..\..\trunk\source\Common\HashFileRunner.cpp", nativeCoreProject, StringComparison.Ordinal);
        Assert.Contains(@"..\..\trunk\source\Common\HashScheduler.cpp", nativeCoreProject, StringComparison.Ordinal);
        Assert.Contains(@"..\..\trunk\source\Runtime\Hash\BLAKE3HashProvider.cpp", nativeCoreProject, StringComparison.Ordinal);

        Assert.DoesNotContain(@"..\..\trunk\source\Algorithms\MD5.cpp", winUiNativeProject, StringComparison.Ordinal);
        Assert.DoesNotContain(@"..\..\trunk\source\Algorithms\SHA1.cpp", winUiNativeProject, StringComparison.Ordinal);
        Assert.DoesNotContain(@"..\..\trunk\source\Algorithms\sha256.cpp", winUiNativeProject, StringComparison.Ordinal);
        Assert.DoesNotContain(@"..\..\trunk\source\Algorithms\sha512.cpp", winUiNativeProject, StringComparison.Ordinal);
        Assert.DoesNotContain(@"..\..\trunk\source\Common\HashEngine.cpp", winUiNativeProject, StringComparison.Ordinal);
        Assert.DoesNotContain(@"..\..\trunk\source\Common\HashFileRunner.cpp", winUiNativeProject, StringComparison.Ordinal);
        Assert.DoesNotContain(@"..\..\trunk\source\Common\HashScheduler.cpp", winUiNativeProject, StringComparison.Ordinal);
        Assert.DoesNotContain(@"..\..\trunk\source\Common\HashEnginePreparation.cpp", winUiNativeProject, StringComparison.Ordinal);
        Assert.DoesNotContain(@"..\..\trunk\source\Common\HashEngineResult.cpp", winUiNativeProject, StringComparison.Ordinal);
        Assert.DoesNotContain(@"..\..\trunk\source\Common\strhelper.cpp", winUiNativeProject, StringComparison.Ordinal);
        Assert.DoesNotContain(@"..\..\trunk\source\OsUtils\OsFileWinApi.cpp", winUiNativeProject, StringComparison.Ordinal);
        Assert.DoesNotContain(@"..\..\trunk\source\OsUtils\OsThreadWinApi.cpp", winUiNativeProject, StringComparison.Ordinal);
        Assert.DoesNotContain(@"..\..\trunk\source\WinCommon\WindowsComm.cpp", winUiNativeProject, StringComparison.Ordinal);

        Assert.Contains(@"..\..\trunk\source\WinCommon\AdvTaskbar.cpp", winUiNativeProject, StringComparison.Ordinal);
        Assert.Contains(@"..\..\trunk\source\WinCommon\ClipboardHelper.cpp", winUiNativeProject, StringComparison.Ordinal);
        Assert.Contains(@"..\..\trunk\source\WinCommon\FileVersionHelper.cpp", winUiNativeProject, StringComparison.Ordinal);

        Assert.Contains("fHashWUINative.lib;fHashNativeCore.lib;Version.lib;%(AdditionalDependencies)", clrBridgeProject, StringComparison.Ordinal);
        Assert.Contains(@"$(ProjectDir)..\fHashNativeCore\$(Platform)\$(Configuration)\fHashNativeCore-md\", clrBridgeProject, StringComparison.Ordinal);
        Assert.Contains(@"$(ProjectDir)..\fHashNativeCore\$(Platform)\$(Configuration)\fHashNativeCore\", clrBridgeProject, StringComparison.Ordinal);

        RepositoryTestContext.AssertContainsInOrder(
            previewWorkflow,
            "build-winui-bridge-x64:",
            "& msbuild sub-proj/fHashNativeCore/fHashNativeCore.vcxproj /m /p:Configuration=Release /p:Platform=x64 /p:PlatformToolset=v143 /p:FHashDynamicRuntime=true",
            "& msbuild sub-proj/fHashWUINative/fHashWUINative.vcxproj",
            "& msbuild sub-proj/fHashClrBridge/fHashClrBridge.vcxproj /restore");
    }

    [Fact]
    public void WinUiProject_UsesStableWindowsAppSdkPackage()
    {
        string winUiProject = RepositoryTestContext.ReadUtf8File(@"trunk\source\WinUI\fHashWUI.csproj");

        Assert.Contains("<PackageReference Include=\"Microsoft.WindowsAppSDK\" Version=\"1.8.260317003\" />", winUiProject, StringComparison.Ordinal);
        Assert.DoesNotContain("2.0.0-experimental", winUiProject, StringComparison.Ordinal);
    }

    [Fact]
    public void SourceLayerEntryPoints_ExposeDomainRuntimeAndLegacyCompatContracts()
    {
        string domainContracts = RepositoryTestContext.ReadUtf8File(@"trunk\source\Domain\HashDomainContracts.h");
        string runtimeContracts = RepositoryTestContext.ReadUtf8File(@"trunk\source\Runtime\HashRuntimeContracts.h");
        string legacyContracts = RepositoryTestContext.ReadUtf8File(@"trunk\source\LegacyCompat\LegacyCompatibility.h");

        Assert.Contains("#include \"Domain/HashAlgorithmRegistryCore.h\"", domainContracts, StringComparison.Ordinal);
        Assert.Contains("#include \"Domain/HashRequest.h\"", domainContracts, StringComparison.Ordinal);
        Assert.Contains("#include \"Domain/HashResult.h\"", domainContracts, StringComparison.Ordinal);
        Assert.Contains("#include \"Domain/ProgressEvent.h\"", domainContracts, StringComparison.Ordinal);

        Assert.Contains("#include \"Runtime/HashExecutionContext.h\"", runtimeContracts, StringComparison.Ordinal);
        Assert.Contains("#include \"Runtime/HashProgressSink.h\"", runtimeContracts, StringComparison.Ordinal);
        Assert.Contains("#include \"Common/HashEngine.h\"", runtimeContracts, StringComparison.Ordinal);
        Assert.Contains("#include \"Runtime/HashDigestOperationRegistryRuntime.h\"", runtimeContracts, StringComparison.Ordinal);
        Assert.Contains("#include \"Common/HashSchedulerPlan.h\"", runtimeContracts, StringComparison.Ordinal);

        Assert.Contains("#include \"LegacyCompat/LegacyThreadData.h\"", legacyContracts, StringComparison.Ordinal);
        Assert.Contains("#include \"LegacyCompat/ThreadDataAccess.h\"", legacyContracts, StringComparison.Ordinal);
        Assert.Contains("#include \"LegacyCompat/HashRequestProjection.h\"", legacyContracts, StringComparison.Ordinal);
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
