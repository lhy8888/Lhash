namespace LHash.UnitTests;

public sealed class ReleaseMetadataUnitTests
{
    [Fact]
    public void LegacyVersion_IsUpdatedTo_1_12_5_0_AndAboutDialogDisplays_1_12_5()
    {
        string versionHeader = RepositoryTestContext.ReadTextFile(@"trunk\source\WinMFC\version.h");
        string aboutDialog = RepositoryTestContext.ReadTextFile(@"trunk\source\WinMFC\AboutDlg.cpp");

        Assert.Contains("#define NUM_VERSION_LEGACY 1,12,5,0", versionHeader, StringComparison.Ordinal);
        Assert.Contains("#define STR_VERSION_LEGACY \"1.12.5.0\"", versionHeader, StringComparison.Ordinal);
        Assert.Contains("if (LHashVersion.Right(2) == _T(\".0\"))", aboutDialog, StringComparison.Ordinal);
        Assert.Contains("LHashVersion = LHashVersion.Left(LHashVersion.GetLength() - 2);", aboutDialog, StringComparison.Ordinal);
    }

    [Fact]
    public void ActivePlatformVersionMetadata_IsAlignedTo_1_12_5_0()
    {
        string versionHeader = RepositoryTestContext.ReadTextFile(@"trunk\source\WinMFC\version.h");
        string readme = RepositoryTestContext.ReadTextFile(@"README.md");
        string draft = RepositoryTestContext.ReadTextFile(@"docs\README-trusted-verification-draft.md");

        Assert.Contains("#define STR_VERSION_LEGACY \"1.12.5.0\"", versionHeader, StringComparison.Ordinal);
        Assert.Contains("Current release: [`v1.12.5`](https://github.com/lhy8888/Lhash/releases/tag/v1.12.5)", readme, StringComparison.Ordinal);
        Assert.Contains("Current release: [`v1.12.5`](https://github.com/lhy8888/Lhash/releases/tag/v1.12.5)", draft, StringComparison.Ordinal);
        Assert.Contains("Windows UI mainline: `MFC`", readme, StringComparison.Ordinal);
        Assert.Contains("Windows UI mainline: `MFC`", draft, StringComparison.Ordinal);
        Assert.Contains("Core Build Matrix workflow", readme, StringComparison.Ordinal);
        Assert.Contains("Core Build Matrix workflow", draft, StringComparison.Ordinal);
        Assert.Contains("macOS CLI MVP Build workflow", readme, StringComparison.Ordinal);
        Assert.Contains("macOS CLI MVP Build workflow", draft, StringComparison.Ordinal);
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
        string signingGuide = RepositoryTestContext.ReadUtf8File(@"CODE_SIGNING.md");
        string signingPolicy = RepositoryTestContext.ReadUtf8File(@"CODE_SIGNING_POLICY.md");

        Assert.Contains("LHASH_SIGN_PFX_BASE64", workflow, StringComparison.Ordinal);
        Assert.Contains("LHASH_SIGN_PFX_PASSWORD", workflow, StringComparison.Ordinal);
        Assert.Contains("Sign Windows MFC app with PFX certificate (optional)", workflow, StringComparison.Ordinal);
        Assert.Contains("trunk/sign_legacy_exe.ps1", workflow, StringComparison.Ordinal);
        Assert.Contains("Get-AuthenticodeSignature", workflow, StringComparison.Ordinal);
        Assert.Contains("SignPath Foundation", signingGuide, StringComparison.Ordinal);
        Assert.Contains("Apply for a free SignPath.io subscription", signingGuide, StringComparison.Ordinal);
        Assert.Contains("Publisher unknown", signingGuide, StringComparison.Ordinal);
        Assert.Contains("Code Signing Policy", signingPolicy, StringComparison.Ordinal);
        Assert.Contains("repository owner and release maintainer", signingPolicy, StringComparison.Ordinal);
        Assert.DoesNotContain("LHASH_TRUSTED_SIGNING_", workflow, StringComparison.Ordinal);
        Assert.DoesNotContain("Azure Trusted Signing", signingGuide, StringComparison.Ordinal);
        Assert.DoesNotContain("/p $PfxPassword", signingScript, StringComparison.Ordinal);

        Assert.Contains("param(", signingScript, StringComparison.Ordinal);
        Assert.Contains("signtool.exe", signingScript, StringComparison.Ordinal);
        Assert.Contains("ConvertTo-SecureString", signingScript, StringComparison.Ordinal);
        Assert.Contains("Import-PfxCertificate", signingScript, StringComparison.Ordinal);
        Assert.Contains("Cert:\\CurrentUser\\My", signingScript, StringComparison.Ordinal);
        Assert.Contains("http://timestamp.sectigo.com", signingScript, StringComparison.Ordinal);
        Assert.Contains("http://timestamp.globalsign.com/tsa/r6advanced1", signingScript, StringComparison.Ordinal);
        Assert.Contains("/sha1 $cert.Thumbprint", signingScript, StringComparison.Ordinal);
        Assert.Contains("Remove-Item \"Cert:\\CurrentUser\\My\\$($cert.Thumbprint)\"", signingScript, StringComparison.Ordinal);
        Assert.Contains("sign", signingScript, StringComparison.Ordinal);
        Assert.Contains("verify", signingScript, StringComparison.Ordinal);
    }

    [Fact]
    public void Workflow_PublishRelease_RunsManualRehearsal_And_Publishes_OnTags()
    {
        string workflow = RepositoryTestContext.ReadUtf8File(@".github\workflows\windows-build.yml");

        Assert.Contains("publish-release:", workflow, StringComparison.Ordinal);
        Assert.Contains("if: github.event_name == 'workflow_dispatch' || startsWith(github.ref, 'refs/tags/v')", workflow, StringComparison.Ordinal);
        Assert.Contains("build-windows-arm64:", workflow, StringComparison.Ordinal);
        Assert.Contains("prepare-openssl-vendor-arm64:", workflow, StringComparison.Ordinal);
        Assert.Contains("pattern: LHash-windows-*", workflow, StringComparison.Ordinal);
        Assert.Contains("merge-multiple: true", workflow, StringComparison.Ordinal);
        Assert.Contains("Stage release rehearsal bundle", workflow, StringComparison.Ordinal);
        Assert.Contains("release_mode=\"rehearsal\"", workflow, StringComparison.Ordinal);
        Assert.Contains("release_mode=\"tagged-release\"", workflow, StringComparison.Ordinal);
        Assert.Contains("RELEASE_MANIFEST.txt", workflow, StringComparison.Ordinal);
        Assert.Contains("lhash_application_version=$app_version", workflow, StringComparison.Ordinal);
        Assert.Contains("Expected packaged Windows ARM64 release zip was not downloaded into release-assets.", workflow, StringComparison.Ordinal);
        Assert.Contains("windows_arm64=$(basename $(ls \"$artifact_root\"/LHash-windows-arm64-*.zip | head -n 1))", workflow, StringComparison.Ordinal);
        Assert.Contains("LHash-release-rehearsal", workflow, StringComparison.Ordinal);
        Assert.Contains("tar -czf \"$PWD/LHash-release-rehearsal-$short_sha.tar.gz\"", workflow, StringComparison.Ordinal);
        Assert.Contains("if: startsWith(github.ref, 'refs/tags/v')", workflow, StringComparison.Ordinal);
        Assert.Contains("softprops/action-gh-release@v2", workflow, StringComparison.Ordinal);
        Assert.Contains("release-assets/LHash-windows-x64-*.zip", workflow, StringComparison.Ordinal);
        Assert.Contains("release-assets/LHash-windows-arm64-*.zip", workflow, StringComparison.Ordinal);
        Assert.Contains("release-staging/RELEASE_MANIFEST.txt", workflow, StringComparison.Ordinal);
        Assert.Contains("release-staging/SHA256SUMS.txt", workflow, StringComparison.Ordinal);
        Assert.Contains("shasum -a 256 LHash-windows-x64-*.zip LHash-windows-arm64-*.zip", workflow, StringComparison.Ordinal);
        Assert.DoesNotContain("artifacts/winui-x64-*", workflow, StringComparison.Ordinal);
    }

    [Fact]
    public void NativeProjects_AndResourceChain_Are_Migrating_To_Utf8()
    {
        string utf8Targets = RepositoryTestContext.ReadUtf8File(@"NativeUtf8.targets");
        string legacyProject = RepositoryTestContext.ReadUtf8File(@"trunk\fileshash.vcxproj");
        string nativeCoreProject = RepositoryTestContext.ReadUtf8File(@"sub-proj\LHashNativeCore\LHashNativeCore.vcxproj");
        string runtimeTestsProject = RepositoryTestContext.ReadUtf8File(@"native-runtime-tests\LHash.NativeRuntimeTests\LHash.NativeRuntimeTests.vcxproj");
        string benchmarksProject = RepositoryTestContext.ReadUtf8File(@"native-benchmarks\LHash.NativeBenchmarks\LHash.NativeBenchmarks.vcxproj");
        string shellProject = RepositoryTestContext.ReadUtf8File(@"sub-proj\LHashShlExt\LHashShlExt.vcxproj");
        string legacyRc = RepositoryTestContext.ReadTextFile(@"trunk\source\WinMFC\fileshash.rc");
        string legacyRc2 = RepositoryTestContext.ReadTextFile(@"trunk\source\WinMFC\res\fileshash.rc2");
        string shellRc = RepositoryTestContext.ReadTextFile(@"sub-proj\LHashShlExt\LHashShlExt.rc");
        Assert.Contains("<AdditionalOptions>/utf-8 %(AdditionalOptions)</AdditionalOptions>", utf8Targets, StringComparison.Ordinal);
        Assert.Contains("<AdditionalOptions>/c65001 %(AdditionalOptions)</AdditionalOptions>", utf8Targets, StringComparison.Ordinal);

        Assert.Contains("NativeUtf8.targets", legacyProject, StringComparison.Ordinal);
        Assert.Contains("NativeUtf8.targets", nativeCoreProject, StringComparison.Ordinal);
        Assert.Contains("NativeUtf8.targets", runtimeTestsProject, StringComparison.Ordinal);
        Assert.Contains("NativeUtf8.targets", benchmarksProject, StringComparison.Ordinal);
        Assert.Contains("NativeUtf8.targets", shellProject, StringComparison.Ordinal);

        Assert.DoesNotContain("/source-charset:.936", legacyProject, StringComparison.Ordinal);
        Assert.DoesNotContain("/execution-charset:.936", legacyProject, StringComparison.Ordinal);
        Assert.DoesNotContain("/c936", legacyProject, StringComparison.Ordinal);
        Assert.DoesNotContain("/source-charset:.936", nativeCoreProject, StringComparison.Ordinal);
        Assert.DoesNotContain("/execution-charset:.936", nativeCoreProject, StringComparison.Ordinal);
        Assert.DoesNotContain("/source-charset:.936", runtimeTestsProject, StringComparison.Ordinal);
        Assert.DoesNotContain("/execution-charset:.936", runtimeTestsProject, StringComparison.Ordinal);
        Assert.DoesNotContain("/source-charset:.936", benchmarksProject, StringComparison.Ordinal);
        Assert.DoesNotContain("/execution-charset:.936", benchmarksProject, StringComparison.Ordinal);
        Assert.DoesNotContain("/source-charset:.936", shellProject, StringComparison.Ordinal);
        Assert.DoesNotContain("/execution-charset:.936", shellProject, StringComparison.Ordinal);
        Assert.DoesNotContain("/c936", shellProject, StringComparison.Ordinal);

        Assert.Contains("#pragma code_page(65001)", legacyRc, StringComparison.Ordinal);
        Assert.Contains("BLOCK \"080404b0\"", legacyRc2, StringComparison.Ordinal);
        Assert.Contains("VALUE \"Translation\", 0x804, 1200", legacyRc2, StringComparison.Ordinal);
        Assert.Contains("#pragma code_page(65001)", shellRc, StringComparison.Ordinal);
        Assert.Contains("BLOCK \"080404b0\"", shellRc, StringComparison.Ordinal);
        Assert.Contains("VALUE \"Translation\", 0x804, 1200", shellRc, StringComparison.Ordinal);
    }

    [Fact]
    public void OpenSslVendorPipeline_AndLinkingException_Are_WiredIntoTheMaintainedBuild()
    {
        string workflow = RepositoryTestContext.ReadUtf8File(@".github\workflows\windows-build.yml");
        string vendorTargets = RepositoryTestContext.ReadUtf8File(@"NativeOpenSslVendor.targets");
        string vendorScript = RepositoryTestContext.ReadUtf8File(@"trunk\build_openssl_vendor.ps1");
        string sourceInfo = RepositoryTestContext.ReadUtf8File(@"third_party\openssl\OPENSSL_3_5_6_SOURCE_INFO.txt");
        string vendorPolicy = RepositoryTestContext.ReadUtf8File(@"third_party\openssl\POLICY.md");
        string vendorModules = RepositoryTestContext.ReadUtf8File(@"third_party\openssl\3.5.6\external\perl\MODULES.txt");
        string textTemplate = RepositoryTestContext.ReadUtf8File(@"third_party\openssl\3.5.6\external\perl\Text-Template-1.56\lib\Text\Template.pm");
        string textTemplatePreprocess = RepositoryTestContext.ReadUtf8File(@"third_party\openssl\3.5.6\external\perl\Text-Template-1.56\lib\Text\Template\Preprocess.pm");
        string appLink = RepositoryTestContext.ReadUtf8File(@"third_party\openssl\3.5.6\ms\applink.c");
        string appsConfig = RepositoryTestContext.ReadUtf8File(@"third_party\openssl\3.5.6\apps\openssl.cnf");
        string demosReadme = RepositoryTestContext.ReadUtf8File(@"third_party\openssl\3.5.6\demos\README.txt");
        string sslBuildInfo = RepositoryTestContext.ReadUtf8File(@"third_party\openssl\3.5.6\ssl\build.info");
        string testBuildInfo = RepositoryTestContext.ReadUtf8File(@"third_party\openssl\3.5.6\test\build.info");
        string exceptionNote = RepositoryTestContext.ReadUtf8File(@"LICENSE-OPENSSL-EXCEPTION.md");
        string readme = RepositoryTestContext.ReadTextFile(@"README.md");

        Assert.Contains("build_openssl_vendor.ps1", workflow, StringComparison.Ordinal);
        Assert.Contains("LHashOpenSslInstallRoot", workflow, StringComparison.Ordinal);
        Assert.Contains("openssl-vendor-x64", workflow, StringComparison.Ordinal);
        Assert.Contains("build-openssl-vendor-x64.log", workflow, StringComparison.Ordinal);
        Assert.Contains("prepare-openssl-vendor-x64:", workflow, StringComparison.Ordinal);
        Assert.DoesNotContain("build-winui-bridge-x64:", workflow, StringComparison.Ordinal);
        Assert.Contains("Verify pristine OpenSSL vendor source", workflow, StringComparison.Ordinal);
        Assert.Contains("tools/verify_openssl_vendor_pristine.ps1", workflow, StringComparison.Ordinal);
        Assert.Contains("Restore cached OpenSSL vendor x64", workflow, StringComparison.Ordinal);
        Assert.Contains("actions/cache@v4", workflow, StringComparison.Ordinal);
        Assert.Contains("name: LHash-openssl-vendor-x64", workflow, StringComparison.Ordinal);
        Assert.Contains("Download OpenSSL vendor x64 artifact", workflow, StringComparison.Ordinal);
        Assert.Contains("-CombinedLogPath $openSslLogPath", workflow, StringComparison.Ordinal);
        Assert.Contains("artifacts/openssl-vendor-x64/*.log", workflow, StringComparison.Ordinal);
        Assert.Contains("openssl_vendor_version=3.5.6", workflow, StringComparison.Ordinal);
        Assert.Contains("openssl_vendor_source=third_party/openssl/3.5.6", workflow, StringComparison.Ordinal);
        Assert.Contains("openssl_vendor_policy=pristine-upstream-source", workflow, StringComparison.Ordinal);
        Assert.Contains("openssl_vendor_local_patches=none", workflow, StringComparison.Ordinal);

        Assert.Contains("LHashOpenSslInstallRoot", vendorTargets, StringComparison.Ordinal);
        Assert.Contains("LHashOpenSslIncludeDir", vendorTargets, StringComparison.Ordinal);
        Assert.Contains("LHashOpenSslLibDir", vendorTargets, StringComparison.Ordinal);
        Assert.Contains("LHASH_WITH_OPENSSL3_VENDOR=1", vendorTargets, StringComparison.Ordinal);
        Assert.Contains("FailNonReleaseOpenSslVendor", vendorTargets, StringComparison.Ordinal);
        Assert.Contains("Do not link it into non-Release builds.", vendorTargets, StringComparison.Ordinal);
        Assert.DoesNotContain("FHashOpenSslInstallRoot", vendorTargets, StringComparison.Ordinal);
        Assert.DoesNotContain("FHASH_WITH_OPENSSL3_VENDOR=1", vendorTargets, StringComparison.Ordinal);
        Assert.Contains("libcrypto.lib", vendorTargets, StringComparison.Ordinal);
        Assert.Contains("/p:LHashOpenSslInstallRoot=$openSslRoot", workflow, StringComparison.Ordinal);
        Assert.Contains("VC-WIN64A", vendorScript, StringComparison.Ordinal);
        Assert.DoesNotContain("VC-WIN32", vendorScript, StringComparison.Ordinal);
        Assert.Contains("VC-WIN64-ARM", vendorScript, StringComparison.Ordinal);
        Assert.Contains("ValidateSet('x64', 'ARM64')", vendorScript, StringComparison.Ordinal);
        Assert.Contains("openssl-3.5.6", vendorScript, StringComparison.Ordinal);
        Assert.Contains("external\\perl\\MODULES.txt", vendorScript, StringComparison.Ordinal);
        Assert.Contains("configdata.pm.in", vendorScript, StringComparison.Ordinal);
        Assert.Contains("LICENSE.txt", vendorScript, StringComparison.Ordinal);
        Assert.Contains("VERSION.dat", vendorScript, StringComparison.Ordinal);
        Assert.Contains("Text-Template-1.56\\lib\\Text\\Template.pm", vendorScript, StringComparison.Ordinal);
        Assert.Contains("ms\\applink.c", vendorScript, StringComparison.Ordinal);
        Assert.Contains("Reset-CombinedOpenSslLog", vendorScript, StringComparison.Ordinal);
        Assert.Contains("Append-OpenSslStepLog", vendorScript, StringComparison.Ordinal);
        Assert.Contains("OpenSSL vendor {0} failed. Emitting {1}:", vendorScript, StringComparison.Ordinal);
        Assert.Contains("Invoke-OpenSslBuildStep -StepName 'configure'", vendorScript, StringComparison.Ordinal);
        Assert.Contains("Invoke-OpenSslBuildStep -StepName 'generated-header build'", vendorScript, StringComparison.Ordinal);
        Assert.Contains("Invoke-OpenSslBuildStep -StepName 'libcrypto build'", vendorScript, StringComparison.Ordinal);
        Assert.Contains("$pdbPath = Join-Path $InstallRoot 'lib\\ossl_static.pdb'", vendorScript, StringComparison.Ordinal);
        Assert.Contains("missing ossl_static.pdb; rebuilding to restore full debug companion assets", vendorScript, StringComparison.Ordinal);
        Assert.Contains("Copy-Item -Path $builtPdbPath -Destination (Join-Path $libInstallRoot 'ossl_static.pdb') -Force", vendorScript, StringComparison.Ordinal);
        Assert.Contains("'no-shared'", vendorScript, StringComparison.Ordinal);
        Assert.Contains("'no-tests'", vendorScript, StringComparison.Ordinal);
        Assert.Contains("'no-module'", vendorScript, StringComparison.Ordinal);
        Assert.Contains("'no-ssl'", vendorScript, StringComparison.Ordinal);
        Assert.Contains("'no-asm'", vendorScript, StringComparison.Ordinal);
        Assert.DoesNotContain("'no-apps'", vendorScript, StringComparison.Ordinal);
        Assert.DoesNotContain("'no-docs'", vendorScript, StringComparison.Ordinal);
        Assert.Contains("Copy-Item -Path (Join-Path $buildRoot 'include\\*')", vendorScript, StringComparison.Ordinal);
        Assert.DoesNotContain("install_dev", vendorScript, StringComparison.Ordinal);
        Assert.Contains("'apps'", vendorScript, StringComparison.Ordinal);
        Assert.Contains("'demos'", vendorScript, StringComparison.Ordinal);
        Assert.Contains("'doc'", vendorScript, StringComparison.Ordinal);
        Assert.Contains("'fuzz'", vendorScript, StringComparison.Ordinal);
        Assert.Contains("'ssl'", vendorScript, StringComparison.Ordinal);
        Assert.Contains("'test'", vendorScript, StringComparison.Ordinal);

        Assert.Contains("version=openssl-3.5.6", sourceInfo, StringComparison.Ordinal);
        Assert.Contains("source=https://www.openssl.org/source/openssl-3.5.6.tar.gz", sourceInfo, StringComparison.Ordinal);
        Assert.Contains("source_policy=pristine upstream tarball extraction", sourceInfo, StringComparison.Ordinal);
        Assert.Contains("vendor_directory=third_party/openssl/3.5.6", sourceInfo, StringComparison.Ordinal);
        Assert.Contains("local_patches=none", sourceInfo, StringComparison.Ordinal);
        Assert.Contains("imported_for=LHash Windows x64/ARM64 static libcrypto vendor build", sourceInfo, StringComparison.Ordinal);
        Assert.Contains("pristine upstream OpenSSL source tree", vendorPolicy, StringComparison.Ordinal);
        Assert.Contains("temporary build copy", vendorPolicy, StringComparison.Ordinal);
        Assert.Contains("third_party/openssl/patches/<version>/", vendorPolicy, StringComparison.Ordinal);
        Assert.Contains("Text-Template-1.56/lib", vendorModules, StringComparison.Ordinal);
        Assert.Contains("package Text::Template;", textTemplate, StringComparison.Ordinal);
        Assert.Contains("package Text::Template::Preprocess;", textTemplatePreprocess, StringComparison.Ordinal);
        Assert.Contains("#define APPLINK_MAX 22", appLink, StringComparison.Ordinal);
        Assert.Contains("[ req ]", appsConfig, StringComparison.Ordinal);
        Assert.Contains("OpenSSL Demonstration Applications", demosReadme, StringComparison.Ordinal);
        Assert.Contains("SOURCE[../libssl]=", sslBuildInfo, StringComparison.Ordinal);
        Assert.Contains("$INITSRC=../ms/applink.c", testBuildInfo, StringComparison.Ordinal);
        Assert.Contains("PATCH=6", RepositoryTestContext.ReadUtf8File(@"third_party\openssl\3.5.6\VERSION.dat"), StringComparison.Ordinal);
        Assert.Contains("OpenSSL Linking Exception", exceptionNote, StringComparison.Ordinal);
        Assert.Contains("GPL-2.0-only with an OpenSSL linking exception", readme, StringComparison.Ordinal);
    }
}

