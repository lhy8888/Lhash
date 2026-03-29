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
    public void ResultDigestStateAccess_OwnsStorageAndCompatibilitySurface()
    {
        string access = RepositoryTestContext.ReadUtf8File(@"trunk\source\Common\ResultDigestStateAccess.h");

        Assert.Contains("GetDigestStorageValue(const ResultDigestStorage& digestStorage, ResultDigestType digestType)", access, StringComparison.Ordinal);
        Assert.Contains("GetResultDigestState(const ResultData& result)", access, StringComparison.Ordinal);
        Assert.Contains("GetResultDigestStorage(const ResultData& result)", access, StringComparison.Ordinal);
        Assert.Contains("GetResultDigestCompatibilityFields(const ResultData& result)", access, StringComparison.Ordinal);
        Assert.Contains("GetCompatibilityResultDigest(const ResultData& result, ResultDigestType digestType)", access, StringComparison.Ordinal);
        Assert.DoesNotContain("HasAnyResultDigests(const ResultData& result)", access, StringComparison.Ordinal);
    }

    [Fact]
    public void ResultDigestValueAccess_OwnsReadWriteAndAggregateSurface()
    {
        string access = RepositoryTestContext.ReadUtf8File(@"trunk\source\Common\ResultDigestValueAccess.h");

        Assert.Contains("GetResultDigest(const ResultData& result, ResultDigestType digestType)", access, StringComparison.Ordinal);
        Assert.Contains("VisitResultDigestMetadataValues(const ResultData& result, TResultDigestMetadataValueVisitor visitor)", access, StringComparison.Ordinal);
        Assert.Contains("HasAnyResultDigests(const ResultData& result)", access, StringComparison.Ordinal);
        Assert.Contains("SetResultDigest(ResultData& result, ResultDigestType digestType, const sunjwbase::tstring& digestValue)", access, StringComparison.Ordinal);
        Assert.Contains("ResetResultDigests(ResultData& result)", access, StringComparison.Ordinal);
    }

    [Fact]
    public void Workflow_RunsIndependentUnitTests_AndGatesNativeBuilds()
    {
        string workflow = RepositoryTestContext.ReadUtf8File(@".github\workflows\windows-build.yml");

        Assert.Contains("unit-tests:", workflow, StringComparison.Ordinal);
        Assert.Contains("dotnet restore unit-tests/FHash.UnitTests/FHash.UnitTests.csproj", workflow, StringComparison.Ordinal);
        Assert.Contains("dotnet test unit-tests/FHash.UnitTests/FHash.UnitTests.csproj --configuration Release --no-restore", workflow, StringComparison.Ordinal);
        RepositoryTestContext.AssertContainsInOrder(
            workflow,
            "build-legacy-x64:",
            "needs:",
            "- security-regression",
            "- unit-tests");
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

        Assert.DoesNotContain(@"..\..\trunk\source\Algorithms\MD5.cpp", winUiNativeProject, StringComparison.Ordinal);
        Assert.DoesNotContain(@"..\..\trunk\source\Algorithms\SHA1.cpp", winUiNativeProject, StringComparison.Ordinal);
        Assert.DoesNotContain(@"..\..\trunk\source\Algorithms\sha256.cpp", winUiNativeProject, StringComparison.Ordinal);
        Assert.DoesNotContain(@"..\..\trunk\source\Algorithms\sha512.cpp", winUiNativeProject, StringComparison.Ordinal);
        Assert.DoesNotContain(@"..\..\trunk\source\Common\HashEngine.cpp", winUiNativeProject, StringComparison.Ordinal);
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
