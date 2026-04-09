namespace FHash.UnitTests;

public sealed class BenchmarkFrameworkUnitTests
{
    [Fact]
    public void NativeBenchmarkWorkflow_ComparesCurrentAgainstPortableBLAKE3Profiles()
    {
        string workflow = RepositoryTestContext.ReadUtf8File(@".github\workflows\native-benchmarks.yml");
        string nativeCoreProject = RepositoryTestContext.ReadUtf8File(@"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj");
        string benchmarkProject = RepositoryTestContext.ReadUtf8File(@"native-benchmarks\FHash.NativeBenchmarks\FHash.NativeBenchmarks.vcxproj");
        string benchmarkSource = RepositoryTestContext.ReadUtf8File(@"native-benchmarks\FHash.NativeBenchmarks\HashEngineBenchmarks.cpp");
        string benchmarkMain = RepositoryTestContext.ReadUtf8File(@"native-benchmarks\FHash.NativeBenchmarks\NativeBenchmarkMain.cpp");
        string benchmarkDoc = RepositoryTestContext.ReadUtf8File(@"docs\NATIVE_BENCHMARKS.md");
        string gitignore = RepositoryTestContext.ReadUtf8File(@".gitignore");

        Assert.Contains("name: Native Benchmarks", workflow, StringComparison.Ordinal);
        Assert.Contains("/p:FHashBlake3SimdProfile=portable", workflow, StringComparison.Ordinal);
        Assert.Contains("/p:FHashBlake3SimdProfile=current", workflow, StringComparison.Ordinal);
        Assert.Contains("native-benchmarks-portable.csv", workflow, StringComparison.Ordinal);
        Assert.Contains("native-benchmarks-current.csv", workflow, StringComparison.Ordinal);
        Assert.Contains("Positive deltas mean the current x64 Release configuration outperformed the portable BLAKE3 control", workflow, StringComparison.Ordinal);

        Assert.Contains("<ProjectName>FHash.NativeBenchmarks</ProjectName>", benchmarkProject, StringComparison.Ordinal);
        Assert.Contains(@"..\..\sub-proj\fHashNativeCore\fHashNativeCore.vcxproj", benchmarkProject, StringComparison.Ordinal);
        Assert.Contains("FHashBuildFlavorSuffix", benchmarkProject, StringComparison.Ordinal);

        Assert.Contains("FHashBlake3SimdProfile", nativeCoreProject, StringComparison.Ordinal);
        Assert.Contains("ExcludedFromBuild Condition=\"'$(FHashBlake3SimdProfile)'=='portable'\"", nativeCoreProject, StringComparison.Ordinal);
        Assert.Contains("BLAKE3_NO_SSE2;BLAKE3_NO_SSE41;BLAKE3_NO_AVX2;BLAKE3_NO_AVX512", nativeCoreProject, StringComparison.Ordinal);

        Assert.Contains("small-single-64k", benchmarkSource, StringComparison.Ordinal);
        Assert.Contains("many-small-256x64k", benchmarkSource, StringComparison.Ordinal);
        Assert.Contains("large-single-128m", benchmarkSource, StringComparison.Ordinal);
        Assert.Contains("\"sha256\"", benchmarkSource, StringComparison.Ordinal);
        Assert.Contains("\"blake3-256\"", benchmarkSource, StringComparison.Ordinal);
        Assert.Contains("\"classic-4\"", benchmarkSource, StringComparison.Ordinal);
        Assert.Contains("\"hybrid-4\"", benchmarkSource, StringComparison.Ordinal);
        Assert.Contains("WriteNativeBenchmarkCsv", benchmarkSource, StringComparison.Ordinal);
        Assert.Contains("--profile", benchmarkMain, StringComparison.Ordinal);
        Assert.Contains("--csv", benchmarkMain, StringComparison.Ordinal);

        Assert.Contains("native-benchmarks/**/x64/", gitignore, StringComparison.Ordinal);
        Assert.Contains("Decision rules", benchmarkDoc, StringComparison.Ordinal);
        Assert.Contains("Do not use the x64 benchmark alone to justify Win32 or ARM64 shipping changes", benchmarkDoc, StringComparison.Ordinal);
    }
}
