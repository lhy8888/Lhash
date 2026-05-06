using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace LHash.UnitTests;

public sealed class NativeRuntimeFrameworkUnitTests
{
    private static readonly string[] ExpectedNativeRuntimeTests = new[]
    {
        "HashThreadFunc_ComputesExpectedDigestsForSingleFile",
        "HashThreadFunc_ProcessesMultipleFilesAndWholeProgress",
        "HashThreadFunc_ComputesStandardMd5AndSha1KnownAnswerVectors",
        "HashThreadFunc_RespectsSelectedAlgorithms",
        "HashThreadFunc_ComputesOfficialOpenSslDigestsForKnownVector",
        "RunHashRequest_OpenSslSha2VariantsStayDistinctWithinOpenSslFamily",
        "RunHashRequest_OpenSslUnknownIdsAreIgnoredAndKnownVariantsStayOrdered",
        "HashThreadFunc_OpenSslVariantsRemainStableAcrossConcurrentRuns",
        "RunHashRequest_OpenSslDigestUpdateFailureProducesExplicitFileError",
        "RunHashRequest_OpenSslDigestFinalizeFailureProducesExplicitFileError",
        "HashThreadFunc_ComputesOfficialBlake3DigestsForKnownVector",
        "HashThreadFunc_ComputesOfficialXXH3DigestsForKnownVector",
        "HashThreadFunc_ComputesOfficialCRC32CDigestForKnownVector",
        "HashThreadFunc_ComputesOfficialCRC32CBoundaryDigestsForKnownVectors",
        "RunHashRequest_Blake3UppercaseFlagRemainsDeterministicAcrossVariants",
        "RunHashRequest_Blake3UnknownIdsAreIgnoredAndKnownVariantsStayOrdered",
        "HashThreadFunc_Blake3VariantsRemainStableAcrossConcurrentRuns",
        "RunHashRequest_XXH3AndCRC32CUnknownIdsAreIgnoredAndKnownVariantsStayOrdered",
        "HashThreadFunc_XXH3AndCRC32CRemainStableAcrossConcurrentRuns",
        "RunHashRequest_XXH3AndCRC32CMultiFileConcurrentMatchesSingleRun",
        "HashThreadFunc_ProducesConsistentDigestsAcrossConcurrentRuns",
        "RunHashRequest_IgnoresUnknownAndDuplicateAlgorithmsInRequest",
        "RunHashRequest_DescriptorOnlyAlgorithmDoesNotBreakSupportedDigests",
        "ThreadDataExecutionAccess_IgnoresUnknownAlgorithmSelection",
        "HashAlgorithmRegistry_SupportsDescriptorIdRegistrationAndReset",
        "HashRequest_AlgorithmIdsDriveSelectionAndDeduplication",
        "HashRequest_SelectionStateResolvesByAlgorithmIdForUnknownDigestTypes",
        "HashResult_ProjectsRegistryExtendedDigestValuesWithoutFixedSlots",
        "HashDigestOperationRegistry_StaysConsistentWithAlgorithmRegistry",
        "HashDigestOperationRegistry_BuildsDescriptorSnapshotFromAlgorithmRegistry",
        "HashDigestOperationRegistry_AllowsNullDescriptorProbeForKnownDigests",
        "HashDigestOperationRegistry_ValidatesDescriptorCompletenessAndUnknownSupport",
        "HashDigestUpdater_CreatesRegistryOrderedOperationsForSelectedAlgorithms",
        "HashDigestUpdater_IgnoresDescriptorOnlyAlgorithmsWithoutBreakingConsistency",
        "HashDigestUpdater_PropagatesParallelUpdateExceptionsAfterAllWorkersComplete",
        "HashThreadFunc_AllowsMetadataOnlyRequestsWithoutEnabledAlgorithms",
        "HashResultSearch_FindsMatchingRuntimeDigests",
        "HashResultSearch_MatchesPathAndDigestForRuntimeResults",
        "HashThreadFunc_ComputesExpectedDigestsForEmptyFile",
        "RunHashRequest_ReportsMissingFileAsErrorResult",
        "RunHashRequest_ContinuesAfterOpenFileErrorInBatch",
        "RunHashRequest_CancelsWhenStopRequestedBeforeStart",
        "RunHashRequest_PropagatesUppercasePreferenceInHashReadyEvent",
        "RunHashRequest_CancelsDuringFileProgressAndSkipsRemainingFiles",
    };

    private static string[] ExtractRegisteredNativeRuntimeTests(string testSource)
    {
        MatchCollection matches = Regex.Matches(testSource, @"tests\.push_back\(\{\s*""([^""]+)""", RegexOptions.CultureInvariant);
        HashSet<string> testNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (Match match in matches)
        {
            if (match.Success)
            {
                testNames.Add(match.Groups[1].Value);
            }
        }

        return testNames.OrderBy(testName => testName, StringComparer.Ordinal).ToArray();
    }

    private static void AssertNativeRuntimeRegistrationMatches(string testSource)
    {
        string[] nativeRegisteredTests = ExtractRegisteredNativeRuntimeTests(testSource);
        string[] expectedRegisteredTests = ExpectedNativeRuntimeTests
            .OrderBy(testName => testName, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(expectedRegisteredTests, nativeRegisteredTests);
    }

    [Fact]
    public void NativeRuntimeTestProject_BuildsStandaloneAgainstNativeCore()
    {
        string project = RepositoryTestContext.ReadUtf8File(@"native-runtime-tests\LHash.NativeRuntimeTests\LHash.NativeRuntimeTests.vcxproj");
        string filters = RepositoryTestContext.ReadUtf8File(@"native-runtime-tests\LHash.NativeRuntimeTests\LHash.NativeRuntimeTests.vcxproj.filters");
        string solution = RepositoryTestContext.ReadUtf8File(@"trunk\fileshash15.sln");
        string gitignore = RepositoryTestContext.ReadUtf8File(@".gitignore");
        string nativeUtf8Targets = RepositoryTestContext.ReadUtf8File(@"NativeUtf8.targets");

        Assert.Contains("<ProjectName>LHash.NativeRuntimeTests</ProjectName>", project, StringComparison.Ordinal);
        Assert.Contains("<ConfigurationType>Application</ConfigurationType>", project, StringComparison.Ordinal);
        Assert.Contains("<UseOfMfc>Static</UseOfMfc>", project, StringComparison.Ordinal);
        Assert.Contains(@"$(ProjectDir)..\..\trunk\source\", project, StringComparison.Ordinal);
        Assert.Contains(@"$(ProjectDir)..\..\third_party\blake3\1.8.4\c", project, StringComparison.Ordinal);
        Assert.Contains(@"$(ProjectDir)..\..\third_party\xxhash\0.8.3", project, StringComparison.Ordinal);
        Assert.Contains(@"$(ProjectDir)..\..\third_party\crc32c\1.1.2\include", project, StringComparison.Ordinal);
        Assert.Contains(@"..\..\sub-proj\LHashNativeCore\LHashNativeCore.vcxproj", project, StringComparison.Ordinal);
        Assert.Contains("<LinkLibraryDependencies>true</LinkLibraryDependencies>", project, StringComparison.Ordinal);
        Assert.Contains("Version.lib;%(AdditionalDependencies)", project, StringComparison.Ordinal);
        Assert.Contains("HashEngineRuntimeTests.cpp", project, StringComparison.Ordinal);
        Assert.Contains("HashEngineSecurityRuntimeTests.cpp", project, StringComparison.Ordinal);
        Assert.Contains("NativeTestMain.cpp", project, StringComparison.Ordinal);
        Assert.Contains("NativeTestHarness.h", project, StringComparison.Ordinal);

        Assert.Contains("Filter Include=\"Source Files\"", filters, StringComparison.Ordinal);
        Assert.Contains("Filter Include=\"Header Files\"", filters, StringComparison.Ordinal);

        Assert.Contains("LHash.NativeRuntimeTests", solution, StringComparison.Ordinal);
        Assert.Contains("native-runtime-tests/**/x64/", gitignore, StringComparison.Ordinal);
        Assert.Contains("NativeOpenSslVendor.targets", nativeUtf8Targets, StringComparison.Ordinal);
    }

    [Fact]
    public void NativeRuntimeTests_ExerciseRealHashEngineExecutionSearchAndCancellation()
    {
        string testSource = RepositoryTestContext.ReadUtf8File(@"native-runtime-tests\LHash.NativeRuntimeTests\HashEngineRuntimeTests.cpp");
        string securityTestSource = RepositoryTestContext.ReadUtf8File(@"native-runtime-tests\LHash.NativeRuntimeTests\HashEngineSecurityRuntimeTests.cpp");
        string mainSource = RepositoryTestContext.ReadUtf8File(@"native-runtime-tests\LHash.NativeRuntimeTests\NativeTestMain.cpp");
        string harness = RepositoryTestContext.ReadUtf8File(@"native-runtime-tests\LHash.NativeRuntimeTests\NativeTestHarness.h");

        Assert.Contains("class CapturingProgressSink : public HashProgressSink", testSource, StringComparison.Ordinal);
        AssertNativeRuntimeRegistrationMatches(testSource);
        Assert.Contains("RunHashThreadData(threadData)", testSource, StringComparison.Ordinal);
        Assert.Contains("RunHashRequest(&executionContext, request)", testSource, StringComparison.Ordinal);
        Assert.Contains("E1BE4D7A8AB5560AA4199EEA339849BA8E293D55CA0A81006726D184519E647F", testSource, StringComparison.Ordinal);
        Assert.Contains("CreateOfficialXXH3SanityInput", testSource, StringComparison.Ordinal);
        Assert.Contains("CreateAscendingByteInput", testSource, StringComparison.Ordinal);
        Assert.Contains("54247382A8D6B94D", testSource, StringComparison.Ordinal);
        Assert.Contains("20EFC49FF02422EA54247382A8D6B94D", testSource, StringComparison.Ordinal);
        Assert.Contains("46DD794E", testSource, StringComparison.Ordinal);
        Assert.Contains("8A9136AA", testSource, StringComparison.Ordinal);
        Assert.Contains("62A8AB43", testSource, StringComparison.Ordinal);
        Assert.Contains("113FDB5C", testSource, StringComparison.Ordinal);
        Assert.Contains("D9963A56", testSource, StringComparison.Ordinal);
        Assert.Contains("CreateAlgorithmId(\"blake3-1024\")", testSource, StringComparison.Ordinal);
        Assert.Contains("CreateAlgorithmId(\"openssl-sha3\")", testSource, StringComparison.Ordinal);
        Assert.Contains("CreateAlgorithmId(\"xxh3\")", testSource, StringComparison.Ordinal);
        Assert.Contains("CreateAlgorithmId(\"crc32c-64\")", testSource, StringComparison.Ordinal);
        Assert.Contains("ScopedOpenSslEvpFailureInjection", testSource, StringComparison.Ordinal);
        Assert.Contains("CountDigestMatchingHashResults(results, digestQuery)", testSource, StringComparison.Ordinal);
        Assert.Contains("VisitPathAndDigestMatchingHashResults", testSource, StringComparison.Ordinal);
        Assert.Contains("std::atomic<bool> *stopRequestedFlag_", testSource, StringComparison.Ordinal);
        Assert.Contains("HashJobState& jobState, HashCancellationState& cancellationState", testSource, StringComparison.Ordinal);
        Assert.Contains("ConfigureStopOnEvent(&cancellationState.stopRequested, PROGRESS_EVENT_FILE_PROGRESS, 1)", testSource, StringComparison.Ordinal);
        Assert.Contains("900150983CD24FB0D6963F7D28E17F72", testSource, StringComparison.Ordinal);
        Assert.Contains("BA7816BF8F01CFEA414140DE5DAE2223B00361A396177A9CB410FF61F20015AD", testSource, StringComparison.Ordinal);
        Assert.Contains("CB00753F45A35E8BB5A03D699AC65007272C32AB0EDED1631A8B605A43FF5BED", testSource, StringComparison.Ordinal);
        Assert.Contains("D41D8CD98F00B204E9800998ECF8427E", testSource, StringComparison.Ordinal);
        Assert.Contains("5D41402ABC4B2A76B9719D911017C592", testSource, StringComparison.Ordinal);
        Assert.Contains("DDAF35A193617ABACC417349AE20413112E6FA4E89A97EA20A9EEEE64B55D39A", testSource, StringComparison.Ordinal);
        Assert.Contains("3A985DA74FE225B2045C172D6BD390BD855F086E3E9D525B46BFE24511431532", testSource, StringComparison.Ordinal);
        Assert.Contains("EC01498288516FC926459F58E2C6AD8DF9B473CB0FC08C2596DA7CF0E49BE4B2", testSource, StringComparison.Ordinal);
        Assert.Contains("B751850B1A57168A5693CD924B6B096E08F621827444F70D884F5D0240D2712E", testSource, StringComparison.Ordinal);
        Assert.Contains("BA80A53F981C4D0D6A2797B69F12F6E94C212F14685AC4B74B12BB6FDBFFA2D1", testSource, StringComparison.Ordinal);
        Assert.Contains("508C5E8C327C14E2E1A72BA34EEB452F37458B209ED63A294D999B4C86675982", testSource, StringComparison.Ordinal);
        Assert.Contains("5881092DD818BF5CF8A3DDB793FBCBA74097D5C526A6D35F97B83351940F2CC8", testSource, StringComparison.Ordinal);
        Assert.Contains("483366601360A8771C6863080CC4114D8DB44530F8F1E1EE4F94EA37E78B5739", testSource, StringComparison.Ordinal);
        Assert.Contains("OsFile_RejectsLeafPathsNestedUnderDirectoryJunctions", securityTestSource, StringComparison.Ordinal);
        Assert.Contains("OsFile_ReportsSharingViolationsForLockedFiles", securityTestSource, StringComparison.Ordinal);
        Assert.Contains("mklink /J", securityTestSource, StringComparison.Ordinal);
        Assert.Contains("FILE_ATTRIBUTE_REPARSE_POINT", securityTestSource, StringComparison.Ordinal);
        Assert.Contains("sunjwbase::OsFile::ERR_MSG_BUFFER_LEN", securityTestSource, StringComparison.Ordinal);
        Assert.Contains("openReadScan(openError)", securityTestSource, StringComparison.Ordinal);

        Assert.Contains("struct NativeTestCase", harness, StringComparison.Ordinal);
        Assert.Contains("RunNativeTestCase(const NativeTestCase& testCase)", harness, StringComparison.Ordinal);
        Assert.Contains("RegisterHashEngineRuntimeTests(tests);", mainSource, StringComparison.Ordinal);
        Assert.Contains("RegisterHashEngineSecurityRuntimeTests(tests);", mainSource, StringComparison.Ordinal);
        Assert.Contains("All native runtime tests passed", mainSource, StringComparison.Ordinal);
    }
}
