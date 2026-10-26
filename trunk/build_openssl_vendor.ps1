param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('x64', 'ARM64')]
    [string]$Platform,

    [string]$InstallRoot = '',

    [string]$CombinedLogPath = '',

    [string]$Configuration = 'Release',

    [ValidateSet('optimized', 'portable')]
    [string]$AssemblyMode = 'optimized'
)

$ErrorActionPreference = 'Stop'
$AssemblyMode = $AssemblyMode.ToLowerInvariant()

$openSslVersion = 'openssl-3.5.9'
$openSslSourceDirectory = 'third_party\openssl\3.5.9'

$repoRoot = Split-Path -Parent $PSScriptRoot
$sourceRoot = Join-Path $repoRoot $openSslSourceDirectory
$configValidator = Join-Path $repoRoot 'tools\verify_openssl_build_config.pl'
$configureTarget = if ($Platform -eq 'x64') {
    'VC-WIN64A'
} elseif ($AssemblyMode -eq 'optimized') {
    # Upstream supports MSVC C with clang-cl assembling Windows ARM64 sources.
    'VC-WIN64-CLANGASM-ARM'
} else {
    'VC-WIN64-ARM'
}
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
    'ms\applink.c',
    'Configure',
    'configdata.pm.in',
    'LICENSE.txt',
    'VERSION.dat'
)
foreach ($requiredVendorFile in $requiredVendorFiles) {
    $requiredVendorPath = Join-Path $sourceRoot $requiredVendorFile
    if (-not (Test-Path $requiredVendorPath)) {
        throw "OpenSSL vendor source is incomplete. Missing required build asset: $requiredVendorPath"
    }
}

if ([string]::IsNullOrWhiteSpace($InstallRoot)) {
    $InstallRoot = Join-Path $repoRoot ("artifacts\openssl\{0}\{1}-{2}-{3}" -f $openSslVersion, $Platform, $Configuration, $AssemblyMode)
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
$pdbPath = Join-Path $InstallRoot 'lib\ossl_static.pdb'
$configurationHeaderPath = Join-Path $InstallRoot 'include\openssl\configuration.h'
$versionHeaderPath = Join-Path $InstallRoot 'include\openssl\opensslv.h'
$buildIdentityPath = Join-Path $InstallRoot 'lhash-openssl-build-identity.txt'
$buildConfigPath = Join-Path $InstallRoot 'lhash-openssl-build-config.json'
$buildScriptHash = (Get-FileHash -LiteralPath $PSCommandPath -Algorithm SHA256).Hash
$validatorHash = (Get-FileHash -LiteralPath $configValidator -Algorithm SHA256).Hash
$expectedBuildIdentity = '{0}|{1}|{2}|{3}|{4}|{5}|{6}' -f $openSslVersion, $Platform, $Configuration, $AssemblyMode, $configureTarget, $buildScriptHash, $validatorHash
$hasMatchingBuildIdentity = (Test-Path $buildIdentityPath -PathType Leaf) -and
    (([string](Get-Content -LiteralPath $buildIdentityPath -Raw)).Trim() -eq $expectedBuildIdentity)
$hasMatchingBuildConfig = $false
if (Test-Path $buildConfigPath -PathType Leaf) {
    try {
        $savedConfig = Get-Content -LiteralPath $buildConfigPath -Raw | ConvertFrom-Json
        $wantAssembly = $AssemblyMode -eq 'optimized'
        $hasMatchingBuildConfig = $savedConfig.schema_version -eq 1 -and
            $savedConfig.openssl_version -eq '3.5.9' -and
            $savedConfig.assembly_mode -eq $AssemblyMode -and $savedConfig.target -eq $configureTarget -and
            ($savedConfig.sha256_asm -is [bool]) -and ($savedConfig.sha512_asm -is [bool]) -and
            ($savedConfig.assembly_disabled -is [bool]) -and
            $savedConfig.sha256_asm -eq $wantAssembly -and $savedConfig.sha512_asm -eq $wantAssembly -and
            $savedConfig.assembly_disabled -eq (-not $wantAssembly)
    } catch { $hasMatchingBuildConfig = $false }
}

# Older versioned libraries and incomplete installs must never satisfy this cache.
if ($hasMatchingBuildIdentity -and $hasMatchingBuildConfig -and
    (Test-Path $libPath -PathType Leaf) -and (Test-Path $pdbPath -PathType Leaf) -and
    (Test-Path $configurationHeaderPath -PathType Leaf) -and (Test-Path $versionHeaderPath -PathType Leaf)) {
    Reset-CombinedOpenSslLog -Path $CombinedLogPath
    Add-Content -Path $CombinedLogPath -Value ("Reusing existing OpenSSL vendor build at {0}" -f $InstallRoot) -Encoding UTF8
    Add-Content -Path $CombinedLogPath -Value ("OPENSSL_VENDOR_INSTALL_ROOT={0}" -f $InstallRoot) -Encoding UTF8
    Write-Host "Reusing existing OpenSSL vendor build at $InstallRoot"
    Write-Host "OPENSSL_VENDOR_INSTALL_ROOT=$InstallRoot"
    exit 0
}
elseif (Test-Path $libPath) {
    if (-not (Test-Path $pdbPath -PathType Leaf)) {
        Write-Host "Existing OpenSSL vendor build at $InstallRoot is missing ossl_static.pdb; rebuilding to restore full debug companion assets."
    }
    else {
        Write-Host "Existing OpenSSL vendor build at $InstallRoot has a different build identity or incomplete headers; rebuilding for $openSslVersion ($Platform, $Configuration)."
    }
}

$perl = Get-Command perl -ErrorAction SilentlyContinue
if ($null -eq $perl) {
    throw 'Perl is required to build the vendored OpenSSL source. Install Strawberry Perl or make perl available on PATH.'
}

$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
if (-not (Test-Path $vswhere)) {
    throw 'Unable to locate vswhere.exe to resolve the Visual Studio toolchain.'
}

$requiredVcComponent = if ($Platform -eq 'ARM64') {
    'Microsoft.VisualStudio.Component.VC.Tools.ARM64'
}
else {
    'Microsoft.VisualStudio.Component.VC.Tools.x86.x64'
}
$vsInstallDir = & $vswhere -latest -products * -requires $requiredVcComponent -property installationPath
if ([string]::IsNullOrWhiteSpace($vsInstallDir)) {
    throw "Unable to locate a Visual Studio installation with $requiredVcComponent."
}

$vcvarsall = Join-Path $vsInstallDir 'VC\Auxiliary\Build\vcvarsall.bat'
if (-not (Test-Path $vcvarsall)) {
    throw "Unable to locate vcvarsall.bat at $vcvarsall."
}

$vcvarsArch = ''
# PROCESSOR_ARCHITEW6432 identifies the native host even when PowerShell runs
# under emulation. It is absent in a native process (including Windows ARM64).
$hostArchitecture = $env:PROCESSOR_ARCHITEW6432
if ([string]::IsNullOrWhiteSpace($hostArchitecture)) {
    $hostArchitecture = $env:PROCESSOR_ARCHITECTURE
}
switch ($Platform) {
    'x64' {
        $vcvarsArch = 'amd64'
    }
    'ARM64' {
        $vcvarsArch = if ($hostArchitecture -eq 'ARM64') { 'arm64' } else { 'amd64_arm64' }
    }
    default {
        throw "Unsupported OpenSSL vendor platform: $Platform"
    }
}
Write-Host "OpenSSL toolchain: host=$hostArchitecture target=$configureTarget vcvarsall=$vcvarsArch assembly_mode=$AssemblyMode"

# Do not silently fall back to C when an assembler is missing. portable must be
# requested explicitly, and has a different install/build identity.
$assemblerDirectory = ''
if ($AssemblyMode -eq 'optimized') {
    $assemblerName = if ($Platform -eq 'x64') { 'nasm.exe' } else { 'clang-cl.exe' }
    $assembler = Get-Command $assemblerName -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1
    $assemblerPath = if ($null -ne $assembler) { $assembler.Source } else { '' }
    if ([string]::IsNullOrWhiteSpace($assemblerPath)) {
        $candidates = if ($Platform -eq 'x64') {
            @((Join-Path $env:ProgramFiles 'NASM\nasm.exe'),
              (Join-Path ${env:ProgramFiles(x86)} 'NASM\nasm.exe'))
        } else {
            @((Join-Path $vsInstallDir 'VC\Tools\Llvm\x64\bin\clang-cl.exe'),
              (Join-Path $vsInstallDir 'VC\Tools\Llvm\bin\clang-cl.exe'),
              (Join-Path $env:ProgramFiles 'LLVM\bin\clang-cl.exe'))
        }
        foreach ($candidate in $candidates) {
            if (Test-Path -LiteralPath $candidate -PathType Leaf) { $assemblerPath = $candidate; break }
        }
    }
    if ([string]::IsNullOrWhiteSpace($assemblerPath)) {
        throw "Optimized $Platform OpenSSL requires $assemblerName on PATH (NASM for x64; LLVM clang-cl for ARM64). Install it, or explicitly select -AssemblyMode portable."
    }
    $assemblerDirectory = Split-Path -Parent $assemblerPath
    if ($Platform -eq 'x64') {
        $assemblerVersion = & $assemblerPath -v
        if ($LASTEXITCODE -ne 0 -or $assemblerVersion -notmatch 'NASM version (\d+)\.(\d+)' -or
            ([int]$Matches[1] -lt 2) -or ([int]$Matches[1] -eq 2 -and [int]$Matches[2] -lt 9)) {
            throw 'Optimized x64 OpenSSL requires NASM 2.09 or later.'
        }
    } else {
        $assemblerVersion = & $assemblerPath --version
        if ($LASTEXITCODE -ne 0) { throw "Unable to run $assemblerPath." }
    }
    Write-Host "OpenSSL assembler: $assemblerPath ($($assemblerVersion -join ' '))"
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
        $(if ($assemblerDirectory) { "set `"PATH=$assemblerDirectory;%PATH%`"" } else { '' }),
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

    $stepTimer = [System.Diagnostics.Stopwatch]::StartNew()
    Write-Host "Starting OpenSSL vendor $StepName ($Platform)."
    $process = Start-Process -FilePath 'cmd.exe' -ArgumentList '/d', '/s', '/c', "`"$commandScriptPath`"" -NoNewWindow -PassThru -RedirectStandardOutput $LogPath -RedirectStandardError $stderrLogPath
    # Start-Process -Wait waits for every descendant on Windows, including
    # compiler helper processes that can outlive a completed nmake command.
    # The command script runs each build command synchronously. Wait for that
    # command, and keep its handle open so PS 5.1 can still retrieve ExitCode.
    [void]$process.Handle
    while (-not $process.WaitForExit(30000)) {
        $progressMessage = "OpenSSL vendor $StepName ($Platform): command still running after $([int]$stepTimer.Elapsed.TotalSeconds)s."
        if ($env:GITHUB_ACTIONS -eq 'true') {
            Write-Host "::notice::$progressMessage"
        }
        else {
            Write-Host $progressMessage
        }
        if (Test-Path $LogPath) {
            Get-Content -LiteralPath $LogPath -Tail 1 | Write-Host
        }
    }
    $process.WaitForExit()
    $process.Refresh()
    $stepTimer.Stop()
    Write-Host "Finished OpenSSL vendor $StepName ($Platform) in $([int]$stepTimer.Elapsed.TotalSeconds)s; exit code $($process.ExitCode)."

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
    # Only a fully completed build may be reused on the next invocation.
    if (Test-Path $buildIdentityPath) {
        Remove-Item -LiteralPath $buildIdentityPath -Force
    }
    if (Test-Path $buildConfigPath) {
        Remove-Item -LiteralPath $buildConfigPath -Force
    }
    Copy-Item -Path $sourceRoot -Destination $buildRoot -Recurse -Force
    New-Item -ItemType Directory -Force -Path $InstallRoot | Out-Null
    Reset-CombinedOpenSslLog -Path $CombinedLogPath
    $openSslDir = Join-Path $InstallRoot 'ssl'
    $includeInstallRoot = Join-Path $InstallRoot 'include'
    $libInstallRoot = Join-Path $InstallRoot 'lib'
    $configureLog = Join-Path $InstallRoot 'configure.log'
    $generatedLog = Join-Path $InstallRoot 'build-generated.log'
    $buildLog = Join-Path $InstallRoot 'build-libs.log'
    $configCheckLog = Join-Path $InstallRoot 'verify-build-config.log'
    $generatedConfigPath = Join-Path $buildRoot 'lhash-openssl-build-config.json'

    $configureCommand = @(
        'perl',
        'Configure',
        $configureTarget,
        'no-makedepend',
        'no-shared',
        'no-tests',
        'no-module',
        'no-ssl',
        $(if ($AssemblyMode -eq 'portable') { 'no-asm' }),
        "`"--prefix=$InstallRoot`"",
        "`"--openssldir=$openSslDir`""
    ) -join ' '

    Invoke-OpenSslBuildStep -StepName 'configure' -WorkingDirectory $buildRoot -CommandLine $configureCommand -LogPath $configureLog -CombinedLogPath $CombinedLogPath
    $verifyCommand = 'perl "{0}" {1} {2} > "{3}"' -f $configValidator, $AssemblyMode, $configureTarget, $generatedConfigPath
    Invoke-OpenSslBuildStep -StepName 'configuration verification' -WorkingDirectory $buildRoot -CommandLine $verifyCommand -LogPath $configCheckLog -CombinedLogPath $CombinedLogPath
    Invoke-OpenSslBuildStep -StepName 'generated-header build' -WorkingDirectory $buildRoot -CommandLine 'nmake /NOLOGO build_generated' -LogPath $generatedLog -CombinedLogPath $CombinedLogPath
    Invoke-OpenSslBuildStep -StepName 'libcrypto build' -WorkingDirectory $buildRoot -CommandLine 'nmake /NOLOGO build_libs' -LogPath $buildLog -CombinedLogPath $CombinedLogPath

    New-Item -ItemType Directory -Force -Path $includeInstallRoot | Out-Null
    New-Item -ItemType Directory -Force -Path $libInstallRoot | Out-Null

    Copy-Item -Path (Join-Path $buildRoot 'include\*') -Destination $includeInstallRoot -Recurse -Force
    Copy-Item -Path (Join-Path $buildRoot 'libcrypto.lib') -Destination (Join-Path $libInstallRoot 'libcrypto.lib') -Force

    $builtPdbPath = Join-Path $buildRoot 'ossl_static.pdb'
    if (Test-Path $builtPdbPath) {
        Copy-Item -Path $builtPdbPath -Destination (Join-Path $libInstallRoot 'ossl_static.pdb') -Force
    }
    else {
        Add-Content -Path $CombinedLogPath -Value "ossl_static.pdb was not produced by the OpenSSL vendor build; downstream linkers may emit LNK4099 warnings." -Encoding UTF8
        Write-Warning "OpenSSL vendor build did not produce ossl_static.pdb."
    }

    $generatedConfigurationHeader = Join-Path $includeInstallRoot 'openssl\configuration.h'
    if (-not (Test-Path $generatedConfigurationHeader)) {
        throw "OpenSSL vendor build did not produce the generated public header $generatedConfigurationHeader."
    }

    if (-not (Test-Path $libPath)) {
        throw "OpenSSL vendor build did not produce $libPath."
    }
    if (-not (Test-Path $versionHeaderPath)) {
        throw "OpenSSL vendor build did not produce the public version header $versionHeaderPath."
    }

    Copy-Item -LiteralPath $generatedConfigPath -Destination $buildConfigPath -Force
    Get-Content -LiteralPath $buildConfigPath | Add-Content -Path $CombinedLogPath -Encoding UTF8
    Set-Content -LiteralPath $buildIdentityPath -Value $expectedBuildIdentity -Encoding UTF8
    Add-Content -Path $CombinedLogPath -Value "OPENSSL_VENDOR_INSTALL_ROOT=$InstallRoot" -Encoding UTF8
    Write-Host "OPENSSL_VENDOR_INSTALL_ROOT=$InstallRoot"
}
finally {
    Remove-Item -Recurse -Force $tempRoot -ErrorAction SilentlyContinue
}
