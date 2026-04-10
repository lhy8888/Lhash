param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('x64', 'Win32', 'ARM64')]
    [string]$Platform,

    [string]$InstallRoot = '',

    [string]$CombinedLogPath = '',

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

$requiredVendorDirectories = @(
    'apps',
    'demos',
    'doc',
    'external',
    'fuzz',
    'ms',
    'ssl',
    'test'
)
foreach ($requiredVendorDirectory in $requiredVendorDirectories) {
    $requiredVendorDirectoryPath = Join-Path $sourceRoot $requiredVendorDirectory
    if (-not (Test-Path $requiredVendorDirectoryPath)) {
        throw "OpenSSL vendor source is incomplete. Missing required upstream directory: $requiredVendorDirectoryPath"
    }
}

$requiredVendorFiles = @(
    'external\perl\MODULES.txt',
    'external\perl\Text-Template-1.56\lib\Text\Template.pm',
    'external\perl\Text-Template-1.56\lib\Text\Template\Preprocess.pm',
    'ms\applink.c'
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

if ([string]::IsNullOrWhiteSpace($CombinedLogPath)) {
    $CombinedLogPath = Join-Path $InstallRoot 'build-openssl-vendor.log'
}

function Reset-CombinedOpenSslLog {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    $parent = Split-Path -Parent $Path
    if (-not [string]::IsNullOrWhiteSpace($parent)) {
        New-Item -ItemType Directory -Force -Path $parent | Out-Null
    }

    Set-Content -Path $Path -Value '' -Encoding UTF8
}

function Append-OpenSslStepLog {
    param(
        [Parameter(Mandatory = $true)]
        [string]$CombinedLogPath,

        [Parameter(Mandatory = $true)]
        [string]$StepName,

        [Parameter(Mandatory = $true)]
        [string]$LogPath
    )

    Add-Content -Path $CombinedLogPath -Value ("==== {0} ({1}) ====" -f $StepName, $LogPath) -Encoding UTF8
    if (Test-Path $LogPath) {
        Get-Content -Path $LogPath | Add-Content -Path $CombinedLogPath -Encoding UTF8
    }
    else {
        Add-Content -Path $CombinedLogPath -Value '<missing log file>' -Encoding UTF8
    }

    Add-Content -Path $CombinedLogPath -Value '' -Encoding UTF8
}

$libPath = Join-Path $InstallRoot 'lib\libcrypto.lib'
if (Test-Path $libPath) {
    Reset-CombinedOpenSslLog -Path $CombinedLogPath
    Add-Content -Path $CombinedLogPath -Value ("Reusing existing OpenSSL vendor build at {0}" -f $InstallRoot) -Encoding UTF8
    Add-Content -Path $CombinedLogPath -Value ("OPENSSL_VENDOR_INSTALL_ROOT={0}" -f $InstallRoot) -Encoding UTF8
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

function Invoke-OpenSslBuildStep {
    param(
        [Parameter(Mandatory = $true)]
        [string]$StepName,

        [Parameter(Mandatory = $true)]
        [string]$WorkingDirectory,

        [Parameter(Mandatory = $true)]
        [string]$CommandLine,

        [Parameter(Mandatory = $true)]
        [string]$LogPath,

        [Parameter(Mandatory = $true)]
        [string]$CombinedLogPath
    )

    $safeStepName = ($StepName -replace '[^A-Za-z0-9]+', '-').Trim('-').ToLowerInvariant()
    $commandScriptPath = Join-Path $tempRoot ("openssl-{0}.cmd" -f $safeStepName)
    $stderrLogPath = "{0}.stderr" -f $LogPath
    $commandScript = @(
        '@echo off',
        "call `"$vcvarsall`" $vcvarsArch",
        'if errorlevel 1 exit /b %errorlevel%',
        "cd /d `"$WorkingDirectory`"",
        'if errorlevel 1 exit /b %errorlevel%',
        $CommandLine,
        'exit /b %errorlevel%'
    ) -join "`r`n"

    Set-Content -Path $commandScriptPath -Value $commandScript -Encoding ASCII

    if (Test-Path $LogPath) {
        Remove-Item $LogPath -Force
    }

    if (Test-Path $stderrLogPath) {
        Remove-Item $stderrLogPath -Force
    }

    $process = Start-Process -FilePath 'cmd.exe' -ArgumentList '/d', '/s', '/c', "`"$commandScriptPath`"" -NoNewWindow -Wait -PassThru -RedirectStandardOutput $LogPath -RedirectStandardError $stderrLogPath

    if (Test-Path $stderrLogPath) {
        if (-not (Test-Path $LogPath)) {
            New-Item -ItemType File -Force -Path $LogPath | Out-Null
        }

        if ((Get-Item $stderrLogPath).Length -gt 0) {
            Add-Content -Path $LogPath -Value '' -Encoding UTF8
            Get-Content -Path $stderrLogPath | Add-Content -Path $LogPath -Encoding UTF8
        }

        Remove-Item $stderrLogPath -Force
    }

    if (-not (Test-Path $LogPath)) {
        New-Item -ItemType File -Force -Path $LogPath | Out-Null
    }

    Append-OpenSslStepLog -CombinedLogPath $CombinedLogPath -StepName $StepName -LogPath $LogPath

    if ($process.ExitCode -ne 0) {
        Write-Host ("OpenSSL vendor {0} failed. Emitting {1}:" -f $StepName, $LogPath)
        Get-Content -Path $LogPath
        throw "OpenSSL vendor $StepName failed with exit code $($process.ExitCode). See $CombinedLogPath for details."
    }
}

try {
    Copy-Item -Path $sourceRoot -Destination $buildRoot -Recurse -Force
    New-Item -ItemType Directory -Force -Path $InstallRoot | Out-Null
    Reset-CombinedOpenSslLog -Path $CombinedLogPath
    $openSslDir = Join-Path $InstallRoot 'ssl'
    $includeInstallRoot = Join-Path $InstallRoot 'include'
    $libInstallRoot = Join-Path $InstallRoot 'lib'
    $configureLog = Join-Path $InstallRoot 'configure.log'
    $generatedLog = Join-Path $InstallRoot 'build-generated.log'
    $buildLog = Join-Path $InstallRoot 'build-libs.log'

    $configureCommand = @(
        'perl',
        'Configure',
        $configureTarget,
        'no-makedepend',
        'no-shared',
        'no-tests',
        'no-module',
        'no-ssl',
        'no-asm',
        "--prefix=$InstallRoot",
        "--openssldir=$openSslDir"
    ) -join ' '

    Invoke-OpenSslBuildStep -StepName 'configure' -WorkingDirectory $buildRoot -CommandLine $configureCommand -LogPath $configureLog -CombinedLogPath $CombinedLogPath
    Invoke-OpenSslBuildStep -StepName 'generated-header build' -WorkingDirectory $buildRoot -CommandLine 'nmake /NOLOGO build_generated' -LogPath $generatedLog -CombinedLogPath $CombinedLogPath
    Invoke-OpenSslBuildStep -StepName 'libcrypto build' -WorkingDirectory $buildRoot -CommandLine 'nmake /NOLOGO build_libs' -LogPath $buildLog -CombinedLogPath $CombinedLogPath

    New-Item -ItemType Directory -Force -Path $includeInstallRoot | Out-Null
    New-Item -ItemType Directory -Force -Path $libInstallRoot | Out-Null

    Copy-Item -Path (Join-Path $buildRoot 'include\*') -Destination $includeInstallRoot -Recurse -Force
    Copy-Item -Path (Join-Path $buildRoot 'libcrypto.lib') -Destination (Join-Path $libInstallRoot 'libcrypto.lib') -Force

    $generatedConfigurationHeader = Join-Path $includeInstallRoot 'openssl\configuration.h'
    if (-not (Test-Path $generatedConfigurationHeader)) {
        throw "OpenSSL vendor build did not produce the generated public header $generatedConfigurationHeader."
    }

    if (-not (Test-Path $libPath)) {
        throw "OpenSSL vendor build did not produce $libPath."
    }

    Add-Content -Path $CombinedLogPath -Value "OPENSSL_VENDOR_INSTALL_ROOT=$InstallRoot" -Encoding UTF8
    Write-Host "OPENSSL_VENDOR_INSTALL_ROOT=$InstallRoot"
}
finally {
    Remove-Item -Recurse -Force $tempRoot -ErrorAction SilentlyContinue
}
