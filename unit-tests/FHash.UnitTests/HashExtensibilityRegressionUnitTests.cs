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
        Assert.Contains("TryGetHashAlgorithmDescriptorById(const HashAlgorithmId& algorithmId, HashAlgorithmDescriptor *algorithmDescriptor)", registryCore, StringComparison.Ordinal);
        Assert.DoesNotContain("ResultDigestType type;", registryCore, StringComparison.Ordinal);

        Assert.Contains("std::vector<HashAlgorithmId> algorithmIds;", request, StringComparison.Ordinal);
        Assert.Contains("GetHashRequestNormalizedAlgorithmIds(const HashRequest& request)", request, StringComparison.Ordinal);
        Assert.Contains("if (!IsRegisteredHashAlgorithmId(normalizedAlgorithmId))", request, StringComparison.Ordinal);
        Assert.Contains("TryGetHashAlgorithmIndexById(normalizedAlgorithmIds[algorithmIndex], &registeredIndex)", request, StringComparison.Ordinal);
        Assert.DoesNotContain("std::vector<ResultDigestType> algorithms;", request, StringComparison.Ordinal);

        Assert.Contains("GetResultDigestCount()", digestMetadataAccess, StringComparison.Ordinal);
        Assert.Contains("GetResultDigestMetadataById(const HashAlgorithmId& algorithmId)", digestMetadataAccess, StringComparison.Ordinal);
        Assert.Contains("GetResultDigestMetadataSnapshot()", digestMetadataAccess, StringComparison.Ordinal);
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

    [Fact]
    public void Blake3Integration_VendorsOfficialFixedVersion_AndAddsThreeDescriptorVariants()
    {
        string registryCore = RepositoryTestContext.ReadUtf8File(@"trunk\source\Domain\HashAlgorithmRegistryCore.h");
        string threadAccess = RepositoryTestContext.ReadUtf8File(@"trunk\source\LegacyCompat\ThreadDataExecutionAccess.h");
        string providerHeader = RepositoryTestContext.ReadUtf8File(@"trunk\source\Runtime\Hash\BLAKE3HashProvider.h");
        string providerImplementation = RepositoryTestContext.ReadUtf8File(@"trunk\source\Runtime\Hash\BLAKE3HashProvider.cpp");
        string nativeCoreProject = RepositoryTestContext.ReadUtf8File(@"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj");
        string uwpNativeProject = RepositoryTestContext.ReadUtf8File(@"archive\legacy-platforms\sub-proj\fHashUwpNative\fHashUwpNative.vcxproj");
        string upstreamNote = RepositoryTestContext.ReadUtf8File(@"third_party\blake3\1.8.4\README.LHash.md");
        string upstreamHeader = RepositoryTestContext.ReadUtf8File(@"third_party\blake3\1.8.4\c\blake3.h");
        string nativeRuntimeSource = RepositoryTestContext.ReadUtf8File(@"native-runtime-tests\FHash.NativeRuntimeTests\HashEngineRuntimeTests.cpp");

        Assert.Contains("bool enabledByDefault;", registryCore, StringComparison.Ordinal);
        Assert.Contains("{ \"blake3-256\", \"BLAKE3-256\", true, false }", registryCore, StringComparison.Ordinal);
        Assert.Contains("{ \"blake3-512\", \"BLAKE3-512\", true, false }", registryCore, StringComparison.Ordinal);
        Assert.Contains("{ \"blake3-xof\", \"BLAKE3 XOF\", true, false }", registryCore, StringComparison.Ordinal);
        Assert.Contains("IsHashAlgorithmDescriptorEnabledByDefault", registryCore, StringComparison.Ordinal);

        Assert.Contains("IsHashAlgorithmDescriptorEnabledByDefault(algorithmDescriptor)", threadAccess, StringComparison.Ordinal);

        Assert.Contains("BLAKE3_256_OUTPUT_BYTES = BLAKE3_OUT_LEN", providerHeader, StringComparison.Ordinal);
        Assert.Contains("BLAKE3_512_OUTPUT_BYTES = 64", providerHeader, StringComparison.Ordinal);
        Assert.Contains("BLAKE3_XOF_OUTPUT_BYTES = 128", providerHeader, StringComparison.Ordinal);
        Assert.Contains("blake3_hasher_finalize(&hasher", providerImplementation, StringComparison.Ordinal);

        Assert.Contains(@"third_party\blake3\1.8.4\c", nativeCoreProject, StringComparison.Ordinal);
        Assert.Contains(@"Runtime\Hash\BLAKE3HashProvider.cpp", nativeCoreProject, StringComparison.Ordinal);
        Assert.Contains(@"third_party\blake3\1.8.4\c\blake3.c", nativeCoreProject, StringComparison.Ordinal);
        Assert.Contains(@"third_party\blake3\1.8.4\c\blake3_sse2.c", nativeCoreProject, StringComparison.Ordinal);
        Assert.Contains(@"third_party\blake3\1.8.4\c\blake3_sse41.c", nativeCoreProject, StringComparison.Ordinal);
        Assert.Contains(@"third_party\blake3\1.8.4\c\blake3_avx2.c", nativeCoreProject, StringComparison.Ordinal);
        Assert.Contains(@"third_party\blake3\1.8.4\c\blake3_avx512.c", nativeCoreProject, StringComparison.Ordinal);
        Assert.Contains("FHashBlake3SimdProfile", nativeCoreProject, StringComparison.Ordinal);
        Assert.Contains("Condition=\"'$(FHashBlake3SimdProfile)'=='portable'\">BLAKE3_USE_NEON=0;BLAKE3_NO_SSE2;BLAKE3_NO_SSE41;BLAKE3_NO_AVX2;BLAKE3_NO_AVX512;%(PreprocessorDefinitions)</PreprocessorDefinitions>", nativeCoreProject, StringComparison.Ordinal);
        Assert.Contains(@"third_party\blake3\1.8.4\c", uwpNativeProject, StringComparison.Ordinal);
        Assert.Contains(@"Runtime\Hash\BLAKE3HashProvider.cpp", uwpNativeProject, StringComparison.Ordinal);
        Assert.Contains(@"third_party\blake3\1.8.4\c\blake3_neon.c", uwpNativeProject, StringComparison.Ordinal);
        Assert.Contains("Condition=\"'$(Platform)'=='ARM64'\">BLAKE3_USE_NEON=1;BLAKE3_NO_SSE2;BLAKE3_NO_SSE41;BLAKE3_NO_AVX2;BLAKE3_NO_AVX512;%(PreprocessorDefinitions)</PreprocessorDefinitions>", uwpNativeProject, StringComparison.Ordinal);

        Assert.Contains("Upstream tag: 1.8.4", upstreamNote, StringComparison.Ordinal);
        Assert.Contains("b97a24f8754819755ef78d8016c0391c65c943c5", upstreamNote, StringComparison.Ordinal);
        Assert.Contains("BLAKE3_VERSION_STRING \"1.8.4\"", upstreamHeader, StringComparison.Ordinal);

        Assert.Contains("HashThreadFunc_ComputesOfficialBlake3DigestsForKnownVector", nativeRuntimeSource, StringComparison.Ordinal);
        Assert.Contains("\"blake3-256\"", nativeRuntimeSource, StringComparison.Ordinal);
        Assert.Contains("\"blake3-512\"", nativeRuntimeSource, StringComparison.Ordinal);
        Assert.Contains("\"blake3-xof\"", nativeRuntimeSource, StringComparison.Ordinal);
    }

    [Fact]
    public void XXH3AndCRC32CIntegration_VendorsOfficialFixedVersions_AndAddsRuntimeCoverage()
    {
        string registryCore = RepositoryTestContext.ReadUtf8File(@"trunk\source\Domain\HashAlgorithmRegistryCore.h");
        string xxh3ProviderHeader = RepositoryTestContext.ReadUtf8File(@"trunk\source\Runtime\Hash\XXHash3HashProvider.h");
        string xxh3ProviderImplementation = RepositoryTestContext.ReadUtf8File(@"trunk\source\Runtime\Hash\XXHash3HashProvider.cpp");
        string crc32cProviderHeader = RepositoryTestContext.ReadUtf8File(@"trunk\source\Runtime\Hash\CRC32CHashProvider.h");
        string crc32cProviderImplementation = RepositoryTestContext.ReadUtf8File(@"trunk\source\Runtime\Hash\CRC32CHashProvider.cpp");
        string nativeCoreProject = RepositoryTestContext.ReadUtf8File(@"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj");
        string uwpNativeProject = RepositoryTestContext.ReadUtf8File(@"archive\legacy-platforms\sub-proj\fHashUwpNative\fHashUwpNative.vcxproj");
        string xxhashNote = RepositoryTestContext.ReadUtf8File(@"third_party\xxhash\0.8.3\README.LHash.md");
        string crc32cNote = RepositoryTestContext.ReadUtf8File(@"third_party\crc32c\1.1.2\README.LHash.md");
        string crc32cArm64Check = RepositoryTestContext.ReadUtf8File(@"third_party\crc32c\1.1.2\src\crc32c_arm64_check.h");
        string nativeRuntimeSource = RepositoryTestContext.ReadUtf8File(@"native-runtime-tests\FHash.NativeRuntimeTests\HashEngineRuntimeTests.cpp");

        Assert.Contains("{ \"xxh3-64\", \"XXH3-64\", true, false }", registryCore, StringComparison.Ordinal);
        Assert.Contains("{ \"xxh3-128\", \"XXH3-128\", true, false }", registryCore, StringComparison.Ordinal);
        Assert.Contains("{ \"crc32c\", \"CRC32C\", true, false }", registryCore, StringComparison.Ordinal);

        Assert.Contains("XXH3_64_OUTPUT_BYTES = sizeof(XXH64_hash_t)", xxh3ProviderHeader, StringComparison.Ordinal);
        Assert.Contains("XXH3_128_OUTPUT_BYTES = sizeof(XXH128_hash_t)", xxh3ProviderHeader, StringComparison.Ordinal);
        Assert.Contains("XXH3_64bits_reset", xxh3ProviderImplementation, StringComparison.Ordinal);
        Assert.Contains("XXH3_128bits_update", xxh3ProviderImplementation, StringComparison.Ordinal);
        Assert.Contains("XXH128_canonicalFromHash", xxh3ProviderImplementation, StringComparison.Ordinal);
        Assert.DoesNotContain("static XXH3_state_t", xxh3ProviderImplementation, StringComparison.Ordinal);

        Assert.Contains("CRC32C_OUTPUT_BYTES = sizeof(uint32_t)", crc32cProviderHeader, StringComparison.Ordinal);
        Assert.Contains("crc32c_extend", crc32cProviderImplementation, StringComparison.Ordinal);
        Assert.DoesNotContain("static uint32_t", crc32cProviderImplementation, StringComparison.Ordinal);

        Assert.Contains(@"third_party\xxhash\0.8.3\xxhash.c", nativeCoreProject, StringComparison.Ordinal);
        Assert.Contains(@"third_party\crc32c\1.1.2\src\crc32c.cc", nativeCoreProject, StringComparison.Ordinal);
        Assert.Contains(@"third_party\crc32c\1.1.2\src\crc32c_sse42.cc", nativeCoreProject, StringComparison.Ordinal);
        Assert.Contains(@"third_party\crc32c\1.1.2\src\crc32c_arm64.cc", nativeCoreProject, StringComparison.Ordinal);
        Assert.Contains(@"Runtime\Hash\XXHash3HashProvider.cpp", nativeCoreProject, StringComparison.Ordinal);
        Assert.Contains(@"Runtime\Hash\CRC32CHashProvider.cpp", nativeCoreProject, StringComparison.Ordinal);
        Assert.Contains(@"third_party\xxhash\0.8.3\xxhash.c", uwpNativeProject, StringComparison.Ordinal);
        Assert.Contains(@"third_party\crc32c\1.1.2\src\crc32c.cc", uwpNativeProject, StringComparison.Ordinal);
        Assert.Contains(@"third_party\crc32c\1.1.2\src\crc32c_arm64.cc", uwpNativeProject, StringComparison.Ordinal);

        Assert.Contains("Upstream tag: v0.8.3", xxhashNote, StringComparison.Ordinal);
        Assert.Contains("e626a72bc2321cd320e953a0ccf1584cad60f363", xxhashNote, StringComparison.Ordinal);
        Assert.Contains("Upstream tag: 1.1.2", crc32cNote, StringComparison.Ordinal);
        Assert.Contains("02e65f4fd3065d27b2e29324800ca6d04df16126", crc32cNote, StringComparison.Ordinal);
        Assert.Contains("PF_ARM_V8_CRC32_INSTRUCTIONS_AVAILABLE", crc32cArm64Check, StringComparison.Ordinal);
        Assert.Contains("PF_ARM_V8_CRYPTO_INSTRUCTIONS_AVAILABLE", crc32cArm64Check, StringComparison.Ordinal);

        Assert.Contains("HashThreadFunc_ComputesOfficialXXH3DigestsForKnownVector", nativeRuntimeSource, StringComparison.Ordinal);
        Assert.Contains("HashThreadFunc_ComputesOfficialCRC32CDigestForKnownVector", nativeRuntimeSource, StringComparison.Ordinal);
        Assert.Contains("HashThreadFunc_ComputesOfficialCRC32CBoundaryDigestsForKnownVectors", nativeRuntimeSource, StringComparison.Ordinal);
        Assert.Contains("RunHashRequest_XXH3AndCRC32CUnknownIdsAreIgnoredAndKnownVariantsStayOrdered", nativeRuntimeSource, StringComparison.Ordinal);
        Assert.Contains("HashThreadFunc_XXH3AndCRC32CRemainStableAcrossConcurrentRuns", nativeRuntimeSource, StringComparison.Ordinal);
        Assert.Contains("RunHashRequest_XXH3AndCRC32CMultiFileConcurrentMatchesSingleRun", nativeRuntimeSource, StringComparison.Ordinal);
        Assert.Contains("54247382A8D6B94D", nativeRuntimeSource, StringComparison.Ordinal);
        Assert.Contains("20EFC49FF02422EA54247382A8D6B94D", nativeRuntimeSource, StringComparison.Ordinal);
        Assert.Contains("46DD794E", nativeRuntimeSource, StringComparison.Ordinal);
        Assert.Contains("8A9136AA", nativeRuntimeSource, StringComparison.Ordinal);
        Assert.Contains("62A8AB43", nativeRuntimeSource, StringComparison.Ordinal);
        Assert.Contains("113FDB5C", nativeRuntimeSource, StringComparison.Ordinal);
        Assert.Contains("D9963A56", nativeRuntimeSource, StringComparison.Ordinal);
    }

    [Fact]
    public void OpenSslEvpIntegration_VendorsOfficialFixedVersion_AndOwnsTheOnlyActiveSha256AndSha512Ids()
    {
        string registryCore = RepositoryTestContext.ReadUtf8File(@"trunk\source\Domain\HashAlgorithmRegistryCore.h");
        string digestRegistry = RepositoryTestContext.ReadUtf8File(@"trunk\source\Common\HashDigestOperationRegistry.cpp");
        string providerHeader = RepositoryTestContext.ReadUtf8File(@"trunk\source\Runtime\Hash\OpenSslEvpHashProvider.h");
        string providerImplementation = RepositoryTestContext.ReadUtf8File(@"trunk\source\Runtime\Hash\OpenSslEvpHashProvider.cpp");
        string nativeCoreProject = RepositoryTestContext.ReadUtf8File(@"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj");
        string uwpNativeProject = RepositoryTestContext.ReadUtf8File(@"archive\legacy-platforms\sub-proj\fHashUwpNative\fHashUwpNative.vcxproj");
        string workflow = RepositoryTestContext.ReadUtf8File(@".github\workflows\windows-build.yml");
        string upstreamNote = RepositoryTestContext.ReadUtf8File(@"third_party\openssl\3.0.20\README.LHash.md");
        string licenseException = RepositoryTestContext.ReadUtf8File(@"LICENSE-OPENSSL-EXCEPTION.md");
        string nativeRuntimeSource = RepositoryTestContext.ReadUtf8File(@"native-runtime-tests\FHash.NativeRuntimeTests\HashEngineRuntimeTests.cpp");

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

        Assert.Contains("GetOpenSslSha256AlgorithmId()", digestRegistry, StringComparison.Ordinal);
        Assert.Contains("GetOpenSslSha384AlgorithmId()", digestRegistry, StringComparison.Ordinal);
        Assert.Contains("GetOpenSslSha512AlgorithmId()", digestRegistry, StringComparison.Ordinal);
        Assert.Contains("GetOpenSslSha3_256AlgorithmId()", digestRegistry, StringComparison.Ordinal);
        Assert.Contains("GetOpenSslSha3_384AlgorithmId()", digestRegistry, StringComparison.Ordinal);
        Assert.Contains("GetOpenSslSha3_512AlgorithmId()", digestRegistry, StringComparison.Ordinal);
        Assert.Contains("GetOpenSslBlake2b_512AlgorithmId()", digestRegistry, StringComparison.Ordinal);
        Assert.Contains("GetOpenSslBlake2s_256AlgorithmId()", digestRegistry, StringComparison.Ordinal);
        Assert.Contains("GetOpenSslShake128_256AlgorithmId()", digestRegistry, StringComparison.Ordinal);
        Assert.Contains("GetOpenSslShake256_512AlgorithmId()", digestRegistry, StringComparison.Ordinal);
        Assert.Contains("SHA256\", \"SHA-256\", \"SHA2-256", digestRegistry, StringComparison.Ordinal);
        Assert.Contains("BLAKE2b512", digestRegistry, StringComparison.Ordinal);
        Assert.Contains("SHAKE256", digestRegistry, StringComparison.Ordinal);

        Assert.Contains("OPENSSL_SHA_256_OUTPUT_BYTES = 32", providerHeader, StringComparison.Ordinal);
        Assert.Contains("OPENSSL_SHA_384_OUTPUT_BYTES = 48", providerHeader, StringComparison.Ordinal);
        Assert.Contains("OPENSSL_SHA_512_OUTPUT_BYTES = 64", providerHeader, StringComparison.Ordinal);
        Assert.Contains("OPENSSL_SHA3_256_OUTPUT_BYTES = 32", providerHeader, StringComparison.Ordinal);
        Assert.Contains("OPENSSL_SHA3_384_OUTPUT_BYTES = 48", providerHeader, StringComparison.Ordinal);
        Assert.Contains("OPENSSL_SHA3_512_OUTPUT_BYTES = 64", providerHeader, StringComparison.Ordinal);
        Assert.Contains("OPENSSL_BLAKE2B_512_OUTPUT_BYTES = 64", providerHeader, StringComparison.Ordinal);
        Assert.Contains("OPENSSL_BLAKE2S_256_OUTPUT_BYTES = 32", providerHeader, StringComparison.Ordinal);
        Assert.Contains("OPENSSL_SHAKE128_256_OUTPUT_BYTES = 32", providerHeader, StringComparison.Ordinal);
        Assert.Contains("OPENSSL_SHAKE256_512_OUTPUT_BYTES = 64", providerHeader, StringComparison.Ordinal);
        Assert.Contains("EVP_MD_fetch", providerImplementation, StringComparison.Ordinal);
        Assert.Contains("EVP_DigestInit_ex2", providerImplementation, StringComparison.Ordinal);
        Assert.Contains("OSSL_DIGEST_PARAM_SIZE", providerImplementation, StringComparison.Ordinal);
        Assert.Contains("EVP_DigestUpdate", providerImplementation, StringComparison.Ordinal);
        Assert.Contains("EVP_DigestFinal_ex", providerImplementation, StringComparison.Ordinal);
        Assert.Contains("EVP_DigestFinalXOF", providerImplementation, StringComparison.Ordinal);

        Assert.Contains(@"Runtime\Hash\OpenSslEvpHashProvider.cpp", nativeCoreProject, StringComparison.Ordinal);
        Assert.Contains(@"Runtime\Hash\OpenSslEvpHashProvider.cpp", uwpNativeProject, StringComparison.Ordinal);
        Assert.Contains("FHashOpenSslInstallRoot", workflow, StringComparison.Ordinal);
        Assert.Contains("build_openssl_vendor.ps1", workflow, StringComparison.Ordinal);
        Assert.Contains("openssl-vendor-x64", workflow, StringComparison.Ordinal);

        Assert.Contains("Upstream tag: openssl-3.0.20", upstreamNote, StringComparison.Ordinal);
        Assert.Contains("5aada9c299a3b28fc82348f4e2b93805fa0a0e9c", upstreamNote, StringComparison.Ordinal);
        Assert.Contains("OpenSSL Linking Exception", licenseException, StringComparison.Ordinal);

        Assert.Contains("HashThreadFunc_ComputesOfficialOpenSslDigestsForKnownVector", nativeRuntimeSource, StringComparison.Ordinal);
        Assert.Contains("RunHashRequest_OpenSslSha2VariantsStayDistinctWithinOpenSslFamily", nativeRuntimeSource, StringComparison.Ordinal);
        Assert.Contains("RunHashRequest_OpenSslUnknownIdsAreIgnoredAndKnownVariantsStayOrdered", nativeRuntimeSource, StringComparison.Ordinal);
        Assert.Contains("HashThreadFunc_OpenSslVariantsRemainStableAcrossConcurrentRuns", nativeRuntimeSource, StringComparison.Ordinal);
        Assert.Contains("CB00753F45A35E8BB5A03D699AC65007272C32AB0EDED1631A8B605A43FF5BED", nativeRuntimeSource, StringComparison.Ordinal);
        Assert.Contains("EC01498288516FC926459F58E2C6AD8DF9B473CB0FC08C2596DA7CF0E49BE4B2", nativeRuntimeSource, StringComparison.Ordinal);
        Assert.Contains("BA80A53F981C4D0D6A2797B69F12F6E94C212F14685AC4B74B12BB6FDBFFA2D17D87C5392AAB792DC252D5DE4533CC9518D38AA8DBF1925AB92386EDD4009923", nativeRuntimeSource, StringComparison.Ordinal);
        Assert.Contains("508C5E8C327C14E2E1A72BA34EEB452F37458B209ED63A294D999B4C86675982", nativeRuntimeSource, StringComparison.Ordinal);
        Assert.Contains("BA7816BF8F01CFEA414140DE5DAE2223B00361A396177A9CB410FF61F20015AD", nativeRuntimeSource, StringComparison.Ordinal);
        Assert.Contains("B751850B1A57168A5693CD924B6B096E08F621827444F70D884F5D0240D2712E", nativeRuntimeSource, StringComparison.Ordinal);
        Assert.Contains("483366601360A8771C6863080CC4114D8DB44530F8F1E1EE4F94EA37E78B5739", nativeRuntimeSource, StringComparison.Ordinal);
    }
}
