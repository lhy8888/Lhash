namespace FHash.UnitTests;

public sealed class NativeRuntimeFrameworkUnitTests
{
    [Fact]
    public void NativeRuntimeTestProject_BuildsStandaloneAgainstNativeCore()
    {
        string project = RepositoryTestContext.ReadUtf8File(@"native-runtime-tests\FHash.NativeRuntimeTests\FHash.NativeRuntimeTests.vcxproj");
        string filters = RepositoryTestContext.ReadUtf8File(@"native-runtime-tests\FHash.NativeRuntimeTests\FHash.NativeRuntimeTests.vcxproj.filters");
        string solution = RepositoryTestContext.ReadUtf8File(@"trunk\fileshash15.sln");
        string gitignore = RepositoryTestContext.ReadUtf8File(@".gitignore");

        Assert.Contains("<ProjectName>FHash.NativeRuntimeTests</ProjectName>", project, StringComparison.Ordinal);
        Assert.Contains("<ConfigurationType>Application</ConfigurationType>", project, StringComparison.Ordinal);
        Assert.Contains("<UseOfMfc>Static</UseOfMfc>", project, StringComparison.Ordinal);
        Assert.Contains(@"$(ProjectDir)..\..\trunk\source\", project, StringComparison.Ordinal);
        Assert.Contains(@"..\..\sub-proj\fHashNativeCore\fHashNativeCore.vcxproj", project, StringComparison.Ordinal);
        Assert.Contains("<LinkLibraryDependencies>true</LinkLibraryDependencies>", project, StringComparison.Ordinal);
        Assert.Contains("Version.lib;%(AdditionalDependencies)", project, StringComparison.Ordinal);
        Assert.Contains("HashEngineRuntimeTests.cpp", project, StringComparison.Ordinal);
        Assert.Contains("HashEngineSecurityRuntimeTests.cpp", project, StringComparison.Ordinal);
        Assert.Contains("NativeTestMain.cpp", project, StringComparison.Ordinal);
        Assert.Contains("NativeTestHarness.h", project, StringComparison.Ordinal);

        Assert.Contains("Filter Include=\"Source Files\"", filters, StringComparison.Ordinal);
        Assert.Contains("Filter Include=\"Header Files\"", filters, StringComparison.Ordinal);

        Assert.Contains("FHash.NativeRuntimeTests", solution, StringComparison.Ordinal);
        Assert.Contains("native-runtime-tests/**/x64/", gitignore, StringComparison.Ordinal);
    }

    [Fact]
    public void NativeRuntimeTests_ExerciseRealHashEngineExecutionSearchAndCancellation()
    {
        string testSource = RepositoryTestContext.ReadUtf8File(@"native-runtime-tests\FHash.NativeRuntimeTests\HashEngineRuntimeTests.cpp");
        string securityTestSource = RepositoryTestContext.ReadUtf8File(@"native-runtime-tests\FHash.NativeRuntimeTests\HashEngineSecurityRuntimeTests.cpp");
        string mainSource = RepositoryTestContext.ReadUtf8File(@"native-runtime-tests\FHash.NativeRuntimeTests\NativeTestMain.cpp");
        string harness = RepositoryTestContext.ReadUtf8File(@"native-runtime-tests\FHash.NativeRuntimeTests\NativeTestHarness.h");

        Assert.Contains("class CapturingProgressSink : public HashProgressSink", testSource, StringComparison.Ordinal);
        Assert.Contains("HashThreadFunc_ComputesExpectedDigestsForSingleFile", testSource, StringComparison.Ordinal);
        Assert.Contains("HashThreadFunc_ProcessesMultipleFilesAndWholeProgress", testSource, StringComparison.Ordinal);
        Assert.Contains("HashThreadFunc_RespectsSelectedAlgorithms", testSource, StringComparison.Ordinal);
        Assert.Contains("HashThreadFunc_ComputesOfficialBlake3DigestsForKnownVector", testSource, StringComparison.Ordinal);
        Assert.Contains("HashThreadFunc_ComputesOfficialOpenSslDigestsForKnownVector", testSource, StringComparison.Ordinal);
        Assert.Contains("HashThreadFunc_ComputesOfficialXXH3DigestsForKnownVector", testSource, StringComparison.Ordinal);
        Assert.Contains("HashThreadFunc_ComputesOfficialCRC32CDigestForKnownVector", testSource, StringComparison.Ordinal);
        Assert.Contains("HashThreadFunc_ComputesOfficialCRC32CBoundaryDigestsForKnownVectors", testSource, StringComparison.Ordinal);
        Assert.Contains("RunHashRequest_OpenSslSha2VariantsCanCoexistWithLegacySha2", testSource, StringComparison.Ordinal);
        Assert.Contains("RunHashRequest_OpenSslUnknownIdsAreIgnoredAndKnownVariantsStayOrdered", testSource, StringComparison.Ordinal);
        Assert.Contains("HashThreadFunc_OpenSslVariantsRemainStableAcrossConcurrentRuns", testSource, StringComparison.Ordinal);
        Assert.Contains("RunHashRequest_Blake3UppercaseFlagRemainsDeterministicAcrossVariants", testSource, StringComparison.Ordinal);
        Assert.Contains("RunHashRequest_Blake3UnknownIdsAreIgnoredAndKnownVariantsStayOrdered", testSource, StringComparison.Ordinal);
        Assert.Contains("HashThreadFunc_Blake3VariantsRemainStableAcrossConcurrentRuns", testSource, StringComparison.Ordinal);
        Assert.Contains("RunHashRequest_XXH3AndCRC32CUnknownIdsAreIgnoredAndKnownVariantsStayOrdered", testSource, StringComparison.Ordinal);
        Assert.Contains("HashThreadFunc_XXH3AndCRC32CRemainStableAcrossConcurrentRuns", testSource, StringComparison.Ordinal);
        Assert.Contains("RunHashRequest_XXH3AndCRC32CMultiFileConcurrentMatchesSingleRun", testSource, StringComparison.Ordinal);
        Assert.Contains("HashResultSearch_FindsMatchingRuntimeDigests", testSource, StringComparison.Ordinal);
        Assert.Contains("HashResultSearch_MatchesPathAndDigestForRuntimeResults", testSource, StringComparison.Ordinal);
        Assert.Contains("HashThreadFunc_ComputesExpectedDigestsForEmptyFile", testSource, StringComparison.Ordinal);
        Assert.Contains("RunHashRequest_ReportsMissingFileAsErrorResult", testSource, StringComparison.Ordinal);
        Assert.Contains("RunHashRequest_ContinuesAfterOpenFileErrorInBatch", testSource, StringComparison.Ordinal);
        Assert.Contains("RunHashRequest_IgnoresUnknownAndDuplicateAlgorithmsInRequest", testSource, StringComparison.Ordinal);
        Assert.Contains("RunHashRequest_DescriptorOnlyAlgorithmDoesNotBreakSupportedDigests", testSource, StringComparison.Ordinal);
        Assert.Contains("ThreadDataExecutionAccess_IgnoresUnknownAlgorithmSelection", testSource, StringComparison.Ordinal);
        Assert.Contains("HashAlgorithmRegistry_SupportsDescriptorIdRegistrationAndReset", testSource, StringComparison.Ordinal);
        Assert.Contains("HashRequest_AlgorithmIdsDriveSelectionAndDeduplication", testSource, StringComparison.Ordinal);
        Assert.Contains("HashRequest_SelectionStateResolvesByAlgorithmIdForUnknownDigestTypes", testSource, StringComparison.Ordinal);
        Assert.Contains("HashResult_ProjectsRegistryExtendedDigestValuesWithoutFixedSlots", testSource, StringComparison.Ordinal);
        Assert.Contains("HashDigestOperationRegistry_StaysConsistentWithAlgorithmRegistry", testSource, StringComparison.Ordinal);
        Assert.Contains("HashDigestOperationRegistry_BuildsDescriptorSnapshotFromAlgorithmRegistry", testSource, StringComparison.Ordinal);
        Assert.Contains("HashDigestOperationRegistry_AllowsNullDescriptorProbeForKnownDigests", testSource, StringComparison.Ordinal);
        Assert.Contains("HashDigestOperationRegistry_ValidatesDescriptorCompletenessAndUnknownSupport", testSource, StringComparison.Ordinal);
        Assert.Contains("HashDigestUpdater_CreatesRegistryOrderedOperationsForSelectedAlgorithms", testSource, StringComparison.Ordinal);
        Assert.Contains("HashDigestUpdater_IgnoresDescriptorOnlyAlgorithmsWithoutBreakingConsistency", testSource, StringComparison.Ordinal);
        Assert.Contains("RunHashRequest_CancelsWhenStopRequestedBeforeStart", testSource, StringComparison.Ordinal);
        Assert.Contains("RunHashRequest_PropagatesUppercasePreferenceInHashReadyEvent", testSource, StringComparison.Ordinal);
        Assert.Contains("RunHashRequest_CancelsDuringFileProgressAndSkipsRemainingFiles", testSource, StringComparison.Ordinal);
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
        Assert.Contains("384264F676F39536840523F284921CDC68B6846B", testSource, StringComparison.Ordinal);
        Assert.Contains("BDDD813C634239723171EF3FEE98579B94964E3BB1CB3E427262C8C068D52319", testSource, StringComparison.Ordinal);
        Assert.Contains("BA80A53F981C4D0D6A2797B69F12F6E94C212F14685AC4B74B12BB6FDBFFA2D1", testSource, StringComparison.Ordinal);
        Assert.Contains("AA4938119B1DC7B87CBAD0FFD200D0AE", testSource, StringComparison.Ordinal);
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
