namespace FHash.UnitTests;

public sealed class CommonSeamUnitTests
{
    [Fact]
    public void HashAlgorithmRegistry_DefinesStableCompatibilityOrder()
    {
        string registry = RepositoryTestContext.ReadUtf8File(@"trunk\source\Common\HashAlgorithmRegistry.h");

        RepositoryTestContext.AssertContainsInOrder(
            registry,
            "RESULT_DIGEST_MD5",
            "RESULT_DIGEST_SHA1",
            "RESULT_DIGEST_SHA256",
            "RESULT_DIGEST_SHA512");
        Assert.Contains("struct HashAlgorithmDescriptorRegistry", registry, StringComparison.Ordinal);
        Assert.Contains("GetHashAlgorithmDescriptorRegistry()", registry, StringComparison.Ordinal);
        Assert.Contains("sizeof(algorithmDescriptors) / sizeof(HashAlgorithmDescriptor)", registry, StringComparison.Ordinal);
        Assert.DoesNotContain("compatibilityValueField", registry, StringComparison.Ordinal);
    }

    [Fact]
    public void ThreadDataAccess_UmbrellaShimDependsOnDedicatedSeams()
    {
        string access = RepositoryTestContext.ReadUtf8File(@"trunk\source\Common\ThreadDataAccess.h");

        Assert.Contains("#include \"Common/ThreadDataExecutionAccess.h\"", access, StringComparison.Ordinal);
        Assert.Contains("#include \"Common/ThreadDataInputAccess.h\"", access, StringComparison.Ordinal);
        Assert.Contains("#include \"Common/ThreadDataResultAccess.h\"", access, StringComparison.Ordinal);
        Assert.DoesNotContain("GetThreadDataObserver(const ThreadData& threadData)", access, StringComparison.Ordinal);
        Assert.DoesNotContain("VisitThreadDataResults(const ThreadData& threadData", access, StringComparison.Ordinal);
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

        Assert.Contains("typedef HashAlgorithmDescriptor ResultDigestMetadata;", access, StringComparison.Ordinal);
        Assert.Contains("GetResultDigestCount()", access, StringComparison.Ordinal);
        Assert.Contains("GetResultDigestMetadataAt(int index)", access, StringComparison.Ordinal);
        Assert.Contains("VisitResultDigestMetadata(TResultDigestMetadataVisitor visitor)", access, StringComparison.Ordinal);
        Assert.Contains("VisitResultDigests(TResultDigestVisitor visitor)", access, StringComparison.Ordinal);
        Assert.DoesNotContain("ResetResultDigests(ResultData& result)", access, StringComparison.Ordinal);
    }

    [Fact]
    public void ResultDigestStateAccess_OwnsRegistrySizedStorageSurface()
    {
        string access = RepositoryTestContext.ReadUtf8File(@"trunk\source\Common\ResultDigestStateAccess.h");
        string global = RepositoryTestContext.ReadUtf8File(@"trunk\source\Common\Global.h");

        Assert.Contains("std::vector<sunjwbase::tstring> values;", global, StringComparison.Ordinal);
        Assert.Contains("std::vector<bool> enabled;", global, StringComparison.Ordinal);
        Assert.DoesNotContain("struct ResultDigestCompatibilityFields", global, StringComparison.Ordinal);
        Assert.Contains("GetDigestStorageValue(const ResultDigestStorage& digestStorage, ResultDigestType digestType)", access, StringComparison.Ordinal);
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

        Assert.Contains("GetResultDigest(const ResultData& result, ResultDigestType digestType)", access, StringComparison.Ordinal);
        Assert.Contains("return GetStoredResultDigest(result, digestType);", access, StringComparison.Ordinal);
        Assert.Contains("VisitResultDigestMetadataValues(const ResultData& result, TResultDigestMetadataValueVisitor visitor)", access, StringComparison.Ordinal);
        Assert.Contains("HasAnyResultDigests(const ResultData& result)", access, StringComparison.Ordinal);
        Assert.Contains("SetResultDigest(ResultData& result, ResultDigestType digestType, const sunjwbase::tstring& digestValue)", access, StringComparison.Ordinal);
        Assert.Contains("ResetResultDigests(ResultData& result)", access, StringComparison.Ordinal);
        Assert.DoesNotContain("SetCompatibilityResultDigest(result, digestType, digestValue);", access, StringComparison.Ordinal);
        Assert.DoesNotContain("ClearCompatibilityResultDigest(result, digestType);", access, StringComparison.Ordinal);
    }

    [Fact]
    public void ResultNetProjection_OwnsSharedManagedProjectionPrimitives()
    {
        string resultNetProjection = RepositoryTestContext.ReadUtf8File(@"trunk\source\Common\ResultNetProjection.h");
        string resultDataProjection = RepositoryTestContext.ReadUtf8File(@"trunk\source\Common\ResultDataProjection.h");
        string hashResultProjection = RepositoryTestContext.ReadUtf8File(@"trunk\source\Common\HashResultProjection.h");

        Assert.Contains("static inline TResultStateNet ConvertResultStateToNet(ResultState resultState)", resultNetProjection, StringComparison.Ordinal);
        Assert.Contains("DispatchResultDigestValueByType(ResultDigestType digestType, TMd5Action onMd5, TSha1Action onSha1, TSha256Action onSha256, TSha512Action onSha512)", resultNetProjection, StringComparison.Ordinal);
        Assert.Contains("static inline TResultDataNet AssignResultDigestToNet(TResultDataNet resultDataNet, ResultDigestType digestType, TResultString digestValue)", resultNetProjection, StringComparison.Ordinal);

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
        string threadResultAccess = RepositoryTestContext.ReadUtf8File(@"trunk\source\Common\ThreadDataResultAccess.h");

        Assert.Contains("NormalizeHashResultPathSearchText(const sunjwbase::tstring& pathText)", hashResultSearch, StringComparison.Ordinal);
        Assert.Contains("NormalizeHashResultDigestSearchText(const sunjwbase::tstring& digestText)", hashResultSearch, StringComparison.Ordinal);
        Assert.Contains("HashResultMatchesPathText(const HashResult& result, const sunjwbase::tstring& pathText)", hashResultSearch, StringComparison.Ordinal);
        Assert.Contains("HashResultMatchesPathAndDigestText(const HashResult& result, const sunjwbase::tstring& pathText, const sunjwbase::tstring& digestText)", hashResultSearch, StringComparison.Ordinal);
        Assert.Contains("VisitHashResults(const HashResultList& resultList, THashResultVisitor visitor)", hashResultSearch, StringComparison.Ordinal);
        Assert.Contains("VisitMatchingHashResults(const HashResultList& resultList, THashResultPredicate predicate, THashResultVisitor visitor)", hashResultSearch, StringComparison.Ordinal);
        Assert.Contains("CountMatchingHashResults(const HashResultList& resultList, THashResultPredicate predicate)", hashResultSearch, StringComparison.Ordinal);
        Assert.Contains("VisitDigestMatchingHashResults(const HashResultList& resultList, const sunjwbase::tstring& digestText, THashResultVisitor visitor)", hashResultSearch, StringComparison.Ordinal);
        Assert.Contains("VisitPathAndDigestMatchingHashResults(const HashResultList& resultList, const sunjwbase::tstring& pathText, const sunjwbase::tstring& digestText, THashResultVisitor visitor)", hashResultSearch, StringComparison.Ordinal);
        Assert.Contains("VisitThreadDataHashResults(const ThreadData& threadData, THashResultVisitor visitor)", threadResultAccess, StringComparison.Ordinal);
        Assert.Contains("VisitThreadDataPathAndDigestMatchingHashResults(const ThreadData& threadData, const sunjwbase::tstring& pathText, const sunjwbase::tstring& digestText, THashResultVisitor visitor)", threadResultAccess, StringComparison.Ordinal);
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
    public void Workflow_RunsIndependentUnitTests_AndGatesNativeBuilds()
    {
        string workflow = RepositoryTestContext.ReadUtf8File(@".github\workflows\windows-build.yml");

        Assert.Contains("unit-tests:", workflow, StringComparison.Ordinal);
        Assert.Contains("dotnet restore unit-tests/FHash.UnitTests/FHash.UnitTests.csproj", workflow, StringComparison.Ordinal);
        Assert.Contains("dotnet test unit-tests/FHash.UnitTests/FHash.UnitTests.csproj --configuration Release --no-restore", workflow, StringComparison.Ordinal);
        Assert.Contains("native-runtime-tests:", workflow, StringComparison.Ordinal);
        Assert.Contains("msbuild native-runtime-tests/FHash.NativeRuntimeTests/FHash.NativeRuntimeTests.vcxproj", workflow, StringComparison.Ordinal);
        Assert.Contains(@"native-runtime-tests\FHash.NativeRuntimeTests\x64\Release\FHash.NativeRuntimeTests.exe", workflow, StringComparison.Ordinal);
        RepositoryTestContext.AssertContainsInOrder(
            workflow,
            "build-legacy-x64:",
            "needs:",
            "- security-regression",
            "- unit-tests",
            "- native-runtime-tests");
    }

    [Fact]
    public void WinUiNativeStack_ReusesNativeCore_InsteadOfRecompilingCoreSources()
    {
        string nativeCoreProject = RepositoryTestContext.ReadUtf8File(@"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj");
        string winUiNativeProject = RepositoryTestContext.ReadUtf8File(@"sub-proj\fHashWUINative\fHashWUINative.vcxproj");
        string clrBridgeProject = RepositoryTestContext.ReadUtf8File(@"sub-proj\fHashClrBridge\fHashClrBridge.vcxproj");
        string workflow = RepositoryTestContext.ReadUtf8File(@".github\workflows\windows-build.yml");

        Assert.Contains("<SolutionDir Condition=\"'$(SolutionDir)'==''\">$(ProjectDir)..\\..\\trunk\\</SolutionDir>", nativeCoreProject, StringComparison.Ordinal);
        Assert.Contains("<FHashRuntimeSuffix Condition=\"'$(FHashDynamicRuntime)'=='true'\">-md</FHashRuntimeSuffix>", nativeCoreProject, StringComparison.Ordinal);
        Assert.Contains(@"$(ProjectDir);$(ProjectDir)..\..\trunk\source\;$(SolutionDir)source\;%(AdditionalIncludeDirectories)", nativeCoreProject, StringComparison.Ordinal);
        Assert.Contains("<UseOfMfc Condition=\"'$(FHashDynamicRuntime)'=='true'\">Dynamic</UseOfMfc>", nativeCoreProject, StringComparison.Ordinal);
        Assert.Contains("<RuntimeLibrary Condition=\"'$(FHashDynamicRuntime)'=='true'\">MultiThreadedDLL</RuntimeLibrary>", nativeCoreProject, StringComparison.Ordinal);
        Assert.Contains(@"$(MSBuildProjectName)$(FHashRuntimeSuffix)", nativeCoreProject, StringComparison.Ordinal);
        Assert.Contains(@"..\..\trunk\source\Common\HashFileRunner.cpp", nativeCoreProject, StringComparison.Ordinal);

        Assert.DoesNotContain(@"..\..\trunk\source\Algorithms\MD5.cpp", winUiNativeProject, StringComparison.Ordinal);
        Assert.DoesNotContain(@"..\..\trunk\source\Algorithms\SHA1.cpp", winUiNativeProject, StringComparison.Ordinal);
        Assert.DoesNotContain(@"..\..\trunk\source\Algorithms\sha256.cpp", winUiNativeProject, StringComparison.Ordinal);
        Assert.DoesNotContain(@"..\..\trunk\source\Algorithms\sha512.cpp", winUiNativeProject, StringComparison.Ordinal);
        Assert.DoesNotContain(@"..\..\trunk\source\Common\HashEngine.cpp", winUiNativeProject, StringComparison.Ordinal);
        Assert.DoesNotContain(@"..\..\trunk\source\Common\HashFileRunner.cpp", winUiNativeProject, StringComparison.Ordinal);
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
            workflow,
            "build-winui-bridge-x64:",
            "& msbuild sub-proj/fHashNativeCore/fHashNativeCore.vcxproj /m /p:Configuration=Release /p:Platform=x64 /p:PlatformToolset=v143 /p:FHashDynamicRuntime=true",
            "& msbuild sub-proj/fHashWUINative/fHashWUINative.vcxproj",
            "& msbuild sub-proj/fHashClrBridge/fHashClrBridge.vcxproj /restore");
    }
}
