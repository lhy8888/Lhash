param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string]$Version,

    [string]$RepoRoot = ''
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $RepoRoot = Split-Path -Parent $PSScriptRoot
}

$sourceRoot = Join-Path $RepoRoot ("third_party\openssl\{0}" -f $Version)
if (-not (Test-Path $sourceRoot)) {
    throw "OpenSSL vendor source directory was not found at $sourceRoot."
}

function Assert-PathExists {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [Parameter(Mandatory = $true)]
        [string]$Description
    )

    if (-not (Test-Path $Path)) {
        throw ("OpenSSL vendor pristine check failed. Missing {0}: {1}" -f $Description, $Path)
    }
}

function Assert-PathDoesNotExist {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [Parameter(Mandatory = $true)]
        [string]$Description
    )

    if (Test-Path $Path) {
        throw ("OpenSSL vendor pristine check failed. Unexpected {0}: {1}" -f $Description, $Path)
    }
}

$requiredDirectories = @(
    'apps',
    'demos',
    'doc',
    'external',
    'fuzz',
    'ms',
    'ssl',
    'test'
)

foreach ($requiredDirectory in $requiredDirectories) {
    Assert-PathExists -Path (Join-Path $sourceRoot $requiredDirectory) -Description 'required upstream directory'
}

$requiredFiles = @(
    'external\perl\MODULES.txt',
    'external\perl\Text-Template-1.56\lib\Text\Template.pm',
    'external\perl\Text-Template-1.56\lib\Text\Template\Preprocess.pm',
    'ms\applink.c',
    'Configure',
    'configdata.pm.in',
    'LICENSE.txt',
    'VERSION.dat'
)

foreach ($requiredFile in $requiredFiles) {
    Assert-PathExists -Path (Join-Path $sourceRoot $requiredFile) -Description 'required upstream file'
}

$forbiddenFiles = @(
    'README.LHash.md',
    'OPENSSL_SOURCE_INFO.txt',
    'PATCHES.md'
)

foreach ($forbiddenFile in $forbiddenFiles) {
    Assert-PathDoesNotExist -Path (Join-Path $sourceRoot $forbiddenFile) -Description 'local vendor marker file'
}

$textExtensions = @(
    '.txt',
    '.md',
    '.c',
    '.h',
    '.cc',
    '.cpp',
    '.cxx',
    '.in',
    '.conf',
    '.tmpl',
    '.pl',
    '.pm',
    '.sh',
    '.rb',
    '.py',
    '.ps1',
    '.json',
    '.yml',
    '.yaml',
    '.cmake',
    '.com',
    '.dat'
)

$textFileNames = @(
    'Configure',
    'config',
    'COPYING',
    'COPYRIGHT',
    'INSTALL',
    'NEWS',
    'README',
    'SUPPORT'
)

# Only match markers that are introduced by our local vendor integration.
# Do not match upstream OpenSSL "lhash" implementation identifiers.
$forbiddenPattern = '(fHash|Codex|ChatGPT|local patch|LHash vendor|LHash integration|LHash-specific)'
$forbiddenMatches = Get-ChildItem -Path $sourceRoot -Recurse -File |
    Where-Object {
        $_.Extension -in $textExtensions -or $_.Name -in $textFileNames
    } |
    Select-String -Pattern $forbiddenPattern

if ($null -ne $forbiddenMatches) {
    $matchSummary = $forbiddenMatches | ForEach-Object { "$($_.Path):$($_.LineNumber): $($_.Line.Trim())" }
    throw ("OpenSSL vendor pristine check failed. Unexpected local marker text found in upstream source tree:`n{0}" -f ($matchSummary -join "`n"))
}

Write-Host "OpenSSL vendor source $Version is pristine."
