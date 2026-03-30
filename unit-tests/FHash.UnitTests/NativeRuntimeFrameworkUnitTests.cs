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
        Assert.Contains("HashEngineRuntimeTests.cpp", project, StringComparison.Ordinal);
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
        string mainSource = RepositoryTestContext.ReadUtf8File(@"native-runtime-tests\FHash.NativeRuntimeTests\NativeTestMain.cpp");
        string harness = RepositoryTestContext.ReadUtf8File(@"native-runtime-tests\FHash.NativeRuntimeTests\NativeTestHarness.h");

        Assert.Contains("class CapturingProgressSink : public HashProgressSink", testSource, StringComparison.Ordinal);
        Assert.Contains("HashThreadFunc_ComputesExpectedDigestsForSingleFile", testSource, StringComparison.Ordinal);
        Assert.Contains("HashThreadFunc_RespectsSelectedAlgorithms", testSource, StringComparison.Ordinal);
        Assert.Contains("HashResultSearch_FindsMatchingRuntimeDigests", testSource, StringComparison.Ordinal);
        Assert.Contains("RunHashRequest_ReportsMissingFileAsErrorResult", testSource, StringComparison.Ordinal);
        Assert.Contains("RunHashRequest_CancelsWhenStopRequestedBeforeStart", testSource, StringComparison.Ordinal);
        Assert.Contains("HashThreadFunc(&threadData)", testSource, StringComparison.Ordinal);
        Assert.Contains("RunHashRequest(&executionContext, request)", testSource, StringComparison.Ordinal);
        Assert.Contains("CountDigestMatchingHashResults(results, digestQuery)", testSource, StringComparison.Ordinal);
        Assert.Contains("900150983CD24FB0D6963F7D28E17F72", testSource, StringComparison.Ordinal);
        Assert.Contains("BA7816BF8F01CFEA414140DE5DAE2223B00361A396177A9CB410FF61F20015AD", testSource, StringComparison.Ordinal);

        Assert.Contains("struct NativeTestCase", harness, StringComparison.Ordinal);
        Assert.Contains("RunNativeTestCase(const NativeTestCase& testCase)", harness, StringComparison.Ordinal);
        Assert.Contains("RegisterHashEngineRuntimeTests(tests);", mainSource, StringComparison.Ordinal);
        Assert.Contains("All native runtime tests passed", mainSource, StringComparison.Ordinal);
    }
}
