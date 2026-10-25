param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string]$Version,

    [string]$RepoRoot = ''
)

$ErrorActionPreference = 'Stop'

try {

if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $RepoRoot = Split-Path -Parent $PSScriptRoot
}

$sourceRoot = Join-Path $RepoRoot ("third_party\openssl\{0}" -f $Version)
if (-not (Test-Path $sourceRoot)) {
    throw "OpenSSL vendor source directory was not found at $sourceRoot."
}

# The reviewed manifest is outside the upstream tree. It is generated from the
# release tarball, whose SHA-256 and download URL are recorded in its header.
# Keep the inventory exact: a marker-only scan cannot detect arbitrary edits.
$manifestPath = Join-Path $RepoRoot ("third_party\openssl\OPENSSL_{0}_FILES.sha256" -f $Version.Replace('.', '_'))
if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
    throw "Missing trusted OpenSSL file manifest: $manifestPath"
}
$expectedFiles = [System.Collections.Generic.Dictionary[string,string]]::new([System.StringComparer]::Ordinal)
foreach ($line in [System.IO.File]::ReadLines($manifestPath)) {
    if ($line.StartsWith('#') -or [string]::IsNullOrWhiteSpace($line)) { continue }
    if ($line -cnotmatch '^([0-9a-f]{64})  ([^\\]+)$') {
        throw "Malformed OpenSSL file manifest entry: $line"
    }
    $relativePath = $Matches[2]
    if ($relativePath.StartsWith('/') -or $relativePath.Split('/') -contains '..' -or $expectedFiles.ContainsKey($relativePath)) {
        throw "Unsafe or duplicate OpenSSL file manifest path: $relativePath"
    }
    $expectedFiles.Add($relativePath, $Matches[1])
}
if ($expectedFiles.Count -eq 0) { throw 'The OpenSSL file manifest is empty.' }
$resolvedSourceRoot = (Resolve-Path -LiteralPath $sourceRoot).ProviderPath.TrimEnd([char[]]'\/')
$actualFiles = @(Get-ChildItem -LiteralPath $sourceRoot -Recurse -Force)
foreach ($entry in $actualFiles) {
    if (($entry.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw "OpenSSL upstream tree must not contain links: $($entry.FullName)"
    }
    if ($entry.PSIsContainer) { continue }
    $relativePath = $entry.FullName.Substring($resolvedSourceRoot.Length + 1).Replace('\', '/')
    if (-not $expectedFiles.ContainsKey($relativePath)) {
        throw "Unexpected file in OpenSSL upstream tree: $relativePath"
    }
    $actualHash = (Get-FileHash -LiteralPath $entry.FullName -Algorithm SHA256).Hash
    if ($actualHash -ine $expectedFiles[$relativePath]) {
        throw "OpenSSL upstream file has changed: $relativePath"
    }
    [void]$expectedFiles.Remove($relativePath)
}
if ($expectedFiles.Count -ne 0) {
    throw ("Missing OpenSSL upstream files: {0}" -f (($expectedFiles.Keys | Select-Object -First 10) -join ', '))
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
# OpenSSL 3.5.9's upstream contribution guide contains this literal AI-credit
# example. Allow only that complete line in that document; keep all other
# local-marker checks, including ChatGPT references in other files.
$upstreamContributingPath = Join-Path (Resolve-Path -LiteralPath $sourceRoot).ProviderPath 'CONTRIBUTING.md'
$upstreamAssistedByExample = '        Assisted-by: ChatGPT:gpt-4o'
$forbiddenMatches = Get-ChildItem -Path $sourceRoot -Recurse -File |
    Where-Object {
        $_.Extension -in $textExtensions -or $_.Name -in $textFileNames
    } |
    Select-String -Pattern $forbiddenPattern |
    Where-Object {
        -not ($Version -eq '3.5.9' -and
            $_.Path -ceq $upstreamContributingPath -and
            $_.Line -ceq $upstreamAssistedByExample)
    }

if ($null -ne $forbiddenMatches) {
    $matchSummary = $forbiddenMatches | ForEach-Object { "$($_.Path):$($_.LineNumber): $($_.Line.Trim())" }
    throw ("OpenSSL vendor pristine check failed. Unexpected local marker text found in upstream source tree:`n{0}" -f ($matchSummary -join "`n"))
}

Write-Host "OpenSSL vendor source $Version is pristine."

}
catch {
    if ($env:GITHUB_ACTIONS -eq 'true') {
        # Preserve the precise failure in public job annotations without allowing
        # multiline/vendor text to be interpreted as additional workflow commands.
        $annotation = $_.Exception.Message.Replace('%', '%25').Replace("`r", '%0D').Replace("`n", '%0A')
        Write-Host "::error::$annotation"
    }
    throw
}
