namespace FHash.UnitTests;

public sealed class HashExtensibilityRegressionUnitTests
{
    [Fact]
    public void NativeRuntimeTests_CoverDescriptorRegistrationRequestSelectionAndResultProjectionForNewAlgorithms()
    {
        string nativeRuntimeSource = RepositoryTestContext.ReadUtf8File(@"native-runtime-tests\FHash.NativeRuntimeTests\HashEngineRuntimeTests.cpp");

        Assert.Contains("HashAlgorithmRegistry_SupportsDescriptorIdRegistrationAndReset", nativeRuntimeSource, StringComparison.Ordinal);
        Assert.Contains("\"blake3\"", nativeRuntimeSource, StringComparison.Ordinal);
        Assert.Contains("TryGetHashAlgorithmDescriptorById(sunjwbase::strtotstr(std::string(\"blake3\")), &registeredDescriptor)", nativeRuntimeSource, StringComparison.Ordinal);
        Assert.Contains("!TryGetHashAlgorithmTypeById(sunjwbase::strtotstr(std::string(\"blake3\")), &digestType)", nativeRuntimeSource, StringComparison.Ordinal);

        Assert.Contains("HashRequest_SelectionStateResolvesByAlgorithmIdForUnknownDigestTypes", nativeRuntimeSource, StringComparison.Ordinal);
        Assert.Contains("\"sha3-256\"", nativeRuntimeSource, StringComparison.Ordinal);
        Assert.Contains("CreateHashRequestAlgorithmSelectionState(request);", nativeRuntimeSource, StringComparison.Ordinal);
        Assert.Contains("TryGetHashAlgorithmIndexById(sunjwbase::strtotstr(std::string(\"sha3-256\")), &sha3Index)", nativeRuntimeSource, StringComparison.Ordinal);
        Assert.Contains("TryGetHashAlgorithmIndexById(sunjwbase::strtotstr(std::string(\"blake3\")), &blake3Index)", nativeRuntimeSource, StringComparison.Ordinal);
        Assert.Contains("selectionState.enabled[static_cast<size_t>(sha3Index)]", nativeRuntimeSource, StringComparison.Ordinal);
        Assert.Contains("selectionState.enabled[static_cast<size_t>(blake3Index)]", nativeRuntimeSource, StringComparison.Ordinal);

        Assert.Contains("HashResult_ProjectsRegistryExtendedDigestValuesWithoutFixedSlots", nativeRuntimeSource, StringComparison.Ordinal);
        Assert.Contains("\"xxh3\"", nativeRuntimeSource, StringComparison.Ordinal);
        Assert.Contains("SetResultDigestById(resultData, NormalizeHashAlgorithmId(sunjwbase::strtotstr(std::string(\"xxh3\"))), sunjwbase::strtotstr(std::string(\"CAFEBABE\")))", nativeRuntimeSource, StringComparison.Ordinal);
        Assert.Contains("HashResult result = ProjectHashResult(resultData);", nativeRuntimeSource, StringComparison.Ordinal);
        Assert.Contains("result.digests.size()", nativeRuntimeSource, StringComparison.Ordinal);
        Assert.Contains("ResolveDigestResultAlgorithmId(result.digests[0])", nativeRuntimeSource, StringComparison.Ordinal);
    }

    [Fact]
    public void CoreDescriptorIdSeams_AllowNewAlgorithmsWithoutAddingFixedDigestFields()
    {
        string registryCore = RepositoryTestContext.ReadUtf8File(@"trunk\source\Domain\HashAlgorithmRegistryCore.h");
        string request = RepositoryTestContext.ReadUtf8File(@"trunk\source\Domain\HashRequest.h");
        string result = RepositoryTestContext.ReadUtf8File(@"trunk\source\Domain\HashResult.h");
        string digestMetadataAccess = RepositoryTestContext.ReadUtf8File(@"trunk\source\Common\ResultDigestMetadataAccess.h");
        string digestValueAccess = RepositoryTestContext.ReadUtf8File(@"trunk\source\Common\ResultDigestValueAccess.h");
        string global = RepositoryTestContext.ReadUtf8File(@"trunk\source\Common\Global.h");
        string legacyTypeCompat = RepositoryTestContext.ReadUtf8File(@"trunk\source\LegacyCompat\HashAlgorithmTypeCompat.h");

        Assert.Contains("RegisterHashAlgorithmDescriptor(const HashAlgorithmDescriptor& algorithmDescriptor)", registryCore, StringComparison.Ordinal);
        Assert.Contains("ClearHashAlgorithmDescriptorsForTesting()", registryCore, StringComparison.Ordinal);
        Assert.Contains("ResetHashAlgorithmDescriptorsToDefaultsForTesting()", registryCore, StringComparison.Ordinal);
        Assert.Contains("TryGetHashAlgorithmDescriptorById(const HashAlgorithmId& algorithmId, const HashAlgorithmDescriptor **algorithmDescriptor)", registryCore, StringComparison.Ordinal);
        Assert.DoesNotContain("ResultDigestType type;", registryCore, StringComparison.Ordinal);

        Assert.Contains("std::vector<HashAlgorithmId> algorithmIds;", request, StringComparison.Ordinal);
        Assert.Contains("GetHashRequestNormalizedAlgorithmIds(const HashRequest& request)", request, StringComparison.Ordinal);
        Assert.Contains("if (!IsRegisteredHashAlgorithmId(normalizedAlgorithmId))", request, StringComparison.Ordinal);
        Assert.Contains("TryGetHashAlgorithmIndexById(normalizedAlgorithmIds[algorithmIndex], &registeredIndex)", request, StringComparison.Ordinal);
        Assert.DoesNotContain("std::vector<ResultDigestType> algorithms;", request, StringComparison.Ordinal);

        Assert.Contains("GetResultDigestCount()", digestMetadataAccess, StringComparison.Ordinal);
        Assert.Contains("GetResultDigestMetadataById(const HashAlgorithmId& algorithmId)", digestMetadataAccess, StringComparison.Ordinal);
        Assert.Contains("VisitResultDigestIds(TResultDigestIdVisitor visitor)", digestMetadataAccess, StringComparison.Ordinal);
        Assert.Contains("SetResultDigestById(ResultData& result, const HashAlgorithmId& algorithmId, const sunjwbase::tstring& digestValue)", digestValueAccess, StringComparison.Ordinal);
        Assert.Contains("VisitResultDigestMetadataValues(const ResultData& result, TResultDigestMetadataValueVisitor visitor)", digestValueAccess, StringComparison.Ordinal);
        Assert.Contains("ProjectHashResult(const ResultData& result)", result, StringComparison.Ordinal);
        Assert.Contains("digestResult.algorithmId = GetHashAlgorithmDescriptorId(digestMetadata);", result, StringComparison.Ordinal);

        Assert.Contains("std::vector<HashDigestResult> digests;", global, StringComparison.Ordinal);
        Assert.DoesNotContain("sunjwbase::tstring md5;", global, StringComparison.Ordinal);
        Assert.DoesNotContain("sunjwbase::tstring sha1;", global, StringComparison.Ordinal);
        Assert.DoesNotContain("sunjwbase::tstring sha256;", global, StringComparison.Ordinal);
        Assert.DoesNotContain("sunjwbase::tstring sha512;", global, StringComparison.Ordinal);

        Assert.Contains("TryGetHashAlgorithmTypeById(const HashAlgorithmId& algorithmId, ResultDigestType *digestType)", legacyTypeCompat, StringComparison.Ordinal);
        Assert.Contains("TryGetHashAlgorithmId(ResultDigestType digestType, HashAlgorithmId *algorithmId)", legacyTypeCompat, StringComparison.Ordinal);
    }
}
