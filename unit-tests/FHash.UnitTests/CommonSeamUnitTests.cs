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
}
