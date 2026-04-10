param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('x64', 'Win32', 'ARM64')]
    [string]$Platform,

    [string]$InstallRoot = '',

    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'

$openSslVersion = 'openssl-3.0.20'
$openSslSourceDirectory = 'third_party\openssl\3.0.20'

$repoRoot = Split-Path -Parent $PSScriptRoot
$sourceRoot = Join-Path $repoRoot $openSslSourceDirectory
if (-not (Test-Path $sourceRoot)) {
    throw "OpenSSL vendor source for $openSslVersion was not found at $sourceRoot."
}

$requiredVendorFiles = @(
    'external\perl\MODULES.txt',
    'external\perl\Text-Template-1.56\lib\Text\Template.pm',
    'external\perl\Text-Template-1.56\lib\Text\Template\Preprocess.pm'
)
foreach ($requiredVendorFile in $requiredVendorFiles) {
    $requiredVendorPath = Join-Path $sourceRoot $requiredVendorFile
    if (-not (Test-Path $requiredVendorPath)) {
        throw "OpenSSL vendor source is incomplete. Missing required build asset: $requiredVendorPath"
    }
}

if ([string]::IsNullOrWhiteSpace($InstallRoot)) {
    $InstallRoot = Join-Path $repoRoot ("artifacts\openssl\{0}-{1}" -f $Platform, $Configuration)
}

$libPath = Join-Path $InstallRoot 'lib\libcrypto.lib'
if (Test-Path $libPath) {
    Write-Host "Reusing existing OpenSSL vendor build at $InstallRoot"
    Write-Host "OPENSSL_VENDOR_INSTALL_ROOT=$InstallRoot"
    return
}

$perl = Get-Command perl -ErrorAction SilentlyContinue
if ($null -eq $perl) {
    throw 'Perl is required to build the vendored OpenSSL source. Install Strawberry Perl or make perl available on PATH.'
}

$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
if (-not (Test-Path $vswhere)) {
    throw 'Unable to locate vswhere.exe to resolve the Visual Studio toolchain.'
}

$vsInstallDir = & $vswhere -latest -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath
if ([string]::IsNullOrWhiteSpace($vsInstallDir)) {
    throw 'Unable to locate a Visual Studio installation with VC tools.'
}

$vcvarsall = Join-Path $vsInstallDir 'VC\Auxiliary\Build\vcvarsall.bat'
if (-not (Test-Path $vcvarsall)) {
    throw "Unable to locate vcvarsall.bat at $vcvarsall."
}

$configureTarget = ''
$vcvarsArch = ''
switch ($Platform) {
    'x64' {
        $configureTarget = 'VC-WIN64A'
        $vcvarsArch = 'amd64'
    }
    'Win32' {
        $configureTarget = 'VC-WIN32'
        $vcvarsArch = 'x86'
    }
    'ARM64' {
        $configureTarget = 'VC-WIN64-ARM'
        $vcvarsArch = 'amd64_arm64'
    }
    default {
        throw "Unsupported OpenSSL vendor platform: $Platform"
    }
}

$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("lhash-openssl-build-{0}-{1}" -f $Platform.ToLowerInvariant(), [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force -Path $tempRoot | Out-Null
$buildRoot = Join-Path $tempRoot 'src'

try {
    Copy-Item -Path $sourceRoot -Destination $buildRoot -Recurse -Force
    New-Item -ItemType Directory -Force -Path $InstallRoot | Out-Null
    $openSslDir = Join-Path $InstallRoot 'ssl'

    $configureCommand = @(
        'perl',
        'Configure',
        $configureTarget,
        'no-makedepend',
        'no-shared',
        'no-tests',
        'no-apps',
        'no-docs',
        'no-module',
        'no-ssl',
        'no-asm',
        "--prefix=$InstallRoot",
        "--openssldir=$openSslDir"
    ) -join ' '

    $command = @(
        "call `"$vcvarsall`" $vcvarsArch",
        "cd /d `"$buildRoot`"",
        $configureCommand,
        "nmake /NOLOGO build_libs",
        "nmake /NOLOGO install_dev"
    ) -join ' && '

    & cmd.exe /d /s /c $command
    if ($LASTEXITCODE -ne 0) {
        throw "OpenSSL vendor build failed with exit code $LASTEXITCODE."
    }

    if (-not (Test-Path $libPath)) {
        throw "OpenSSL vendor build did not produce $libPath."
    }

    Write-Host "OPENSSL_VENDOR_INSTALL_ROOT=$InstallRoot"
}
finally {
    Remove-Item -Recurse -Force $tempRoot -ErrorAction SilentlyContinue
}
