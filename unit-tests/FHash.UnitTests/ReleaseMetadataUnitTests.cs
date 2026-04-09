namespace FHash.UnitTests;

public sealed class ReleaseMetadataUnitTests
{
    [Fact]
    public void LegacyVersion_IsUpdatedTo_1_10_1_0_AndAboutDialogDisplays_1_10_1()
    {
        string versionHeader = RepositoryTestContext.ReadTextFile(@"trunk\source\WinMFC\version.h");
        string aboutDialog = RepositoryTestContext.ReadTextFile(@"trunk\source\WinMFC\AboutDlg.cpp");

        Assert.Contains("#define NUM_VERSION_LEGACY 1,10,1,0", versionHeader, StringComparison.Ordinal);
        Assert.Contains("#define STR_VERSION_LEGACY \"1.10.1.0\"", versionHeader, StringComparison.Ordinal);
        Assert.Contains("if (fHashVersion.Right(2) == _T(\".0\"))", aboutDialog, StringComparison.Ordinal);
        Assert.Contains("fHashVersion = fHashVersion.Left(fHashVersion.GetLength() - 2);", aboutDialog, StringComparison.Ordinal);
    }

    [Fact]
    public void PlatformVersionMetadata_IsAlignedTo_1_10_1_0()
    {
        string[] files =
        {
            @"trunk\source\WinUI\Properties\AssemblyInfo.cs",
            @"trunk\source\WinUI\app.manifest",
            @"trunk\fHashWUIWap\version.h",
            @"trunk\fHashWUIWap\Package.appxmanifest",
            @"trunk\fHashWUIWap\Package-DEV.appxmanifest",
            @"trunk\source\WinUWP\Properties\AssemblyInfo.cs",
            @"trunk\source\WinUWP\Package.appxmanifest",
            @"trunk\source\WinUWP\Package-DEBUG.appxmanifest",
            @"trunk\fHashUwpWap\version.h",
            @"trunk\fHashUwpWap\Package.appxmanifest",
            @"trunk\fHashUwpWap\Package-DEBUG.appxmanifest"
        };

        foreach (string relativePath in files)
        {
            string content = RepositoryTestContext.ReadTextFile(relativePath);
            Assert.Contains("1.10.1.0", content, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void WinUiProject_Defines_CsWin32_Method_Manifest_For_Desktop_Windowing()
    {
        string nativeMethods = RepositoryTestContext.ReadTextFile(@"trunk\source\WinUI\NativeMethods.txt");

        Assert.Contains("GetCurrentPackageId", nativeMethods, StringComparison.Ordinal);
        Assert.Contains("GetDpiForWindow", nativeMethods, StringComparison.Ordinal);
        Assert.Contains("GetWindowPlacement", nativeMethods, StringComparison.Ordinal);
        Assert.Contains("ShowWindow", nativeMethods, StringComparison.Ordinal);
        Assert.Contains("GetCursorPos", nativeMethods, StringComparison.Ordinal);
        Assert.Contains("SetForegroundWindow", nativeMethods, StringComparison.Ordinal);
        Assert.Contains("SetWindowSubclass", nativeMethods, StringComparison.Ordinal);
        Assert.Contains("DefSubclassProc", nativeMethods, StringComparison.Ordinal);
        Assert.Contains("WM_GETMINMAXINFO", nativeMethods, StringComparison.Ordinal);
        Assert.Contains("MINMAXINFO", nativeMethods, StringComparison.Ordinal);
    }

    [Fact]
    public void MfcAlgorithmSelectionController_UsesRoomierGridLayout()
    {
        string controller = RepositoryTestContext.ReadTextFile(@"trunk\source\WinMFC\FilesHashAlgorithmSelectionController.cpp");

        Assert.Contains("HASH_ALGORITHM_CHECK_BOX_SPACING_X = 18", controller, StringComparison.Ordinal);
        Assert.Contains("HASH_ALGORITHM_CHECK_BOX_SPACING_Y = 8", controller, StringComparison.Ordinal);
        Assert.Contains("HASH_ALGORITHM_CHECK_BOX_MIN_WIDTH = 84", controller, StringComparison.Ordinal);
        Assert.Contains("HASH_ALGORITHM_CHECK_BOX_MIN_HEIGHT = 18", controller, StringComparison.Ordinal);
        Assert.Contains("HASH_ALGORITHM_LAYOUT_MAX_COLUMNS = 2", controller, StringComparison.Ordinal);
        Assert.Contains("int columnIndex = index % columnCount;", controller, StringComparison.Ordinal);
        Assert.Contains("int rowIndex = index / columnCount;", controller, StringComparison.Ordinal);
    }

    [Fact]
    public void Workflow_AndSigningScript_SupportOptionalAuthenticodeSigning()
    {
        string workflow = RepositoryTestContext.ReadUtf8File(@".github\workflows\windows-build.yml");
        string signingScript = RepositoryTestContext.ReadUtf8File(@"trunk\sign_legacy_exe.ps1");

        Assert.Contains("LHASH_SIGN_PFX_BASE64", workflow, StringComparison.Ordinal);
        Assert.Contains("LHASH_SIGN_PFX_PASSWORD", workflow, StringComparison.Ordinal);
        Assert.Contains("Sign WinUI desktop app (optional)", workflow, StringComparison.Ordinal);
        Assert.Contains("trunk/sign_legacy_exe.ps1", workflow, StringComparison.Ordinal);
        Assert.Contains("Get-AuthenticodeSignature", workflow, StringComparison.Ordinal);

        Assert.Contains("param(", signingScript, StringComparison.Ordinal);
        Assert.Contains("signtool.exe", signingScript, StringComparison.Ordinal);
        Assert.Contains("sign", signingScript, StringComparison.Ordinal);
        Assert.Contains("verify", signingScript, StringComparison.Ordinal);
    }

    [Fact]
    public void Workflow_PublishRelease_RunsRehearsal_OnBranches_AndPublishes_OnTags()
    {
        string workflow = RepositoryTestContext.ReadUtf8File(@".github\workflows\windows-build.yml");

        Assert.Contains("publish-release:", workflow, StringComparison.Ordinal);
        Assert.Contains("if: github.event_name != 'pull_request'", workflow, StringComparison.Ordinal);
        Assert.Contains("pattern: LHash-legacy-*", workflow, StringComparison.Ordinal);
        Assert.Contains("merge-multiple: true", workflow, StringComparison.Ordinal);
        Assert.Contains("Stage release rehearsal bundle", workflow, StringComparison.Ordinal);
        Assert.Contains("release_mode=\"rehearsal\"", workflow, StringComparison.Ordinal);
        Assert.Contains("release_mode=\"tagged-release\"", workflow, StringComparison.Ordinal);
        Assert.Contains("RELEASE_MANIFEST.txt", workflow, StringComparison.Ordinal);
        Assert.Contains("LHash-release-rehearsal", workflow, StringComparison.Ordinal);
        Assert.Contains("tar -czf \"$PWD/LHash-release-rehearsal-$short_sha.tar.gz\"", workflow, StringComparison.Ordinal);
        Assert.Contains("if: startsWith(github.ref, 'refs/tags/v')", workflow, StringComparison.Ordinal);
        Assert.Contains("softprops/action-gh-release@v2", workflow, StringComparison.Ordinal);
        Assert.Contains("release-assets/LHash-legacy-x64-*.zip", workflow, StringComparison.Ordinal);
        Assert.Contains("release-staging/RELEASE_MANIFEST.txt", workflow, StringComparison.Ordinal);
        Assert.DoesNotContain("artifacts/winui-x64-*", workflow, StringComparison.Ordinal);
    }
}
