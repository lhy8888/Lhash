param(
    [Parameter(Mandatory = $true)]
    [string]$FilePath,

    [Parameter(Mandatory = $true)]
    [string]$PfxPath,

    [Parameter(Mandatory = $true)]
    [string]$PfxPassword,

    [string]$TimestampUrl = 'http://timestamp.digicert.com',
    [string]$DigestAlgorithm = 'SHA256'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Find-SignToolPath {
    $command = Get-Command signtool.exe -ErrorAction SilentlyContinue
    if ($command -and $command.Path) {
        return $command.Path
    }

    $searchRoots = @(
        "${env:ProgramFiles(x86)}\\Windows Kits\\10\\bin",
        "${env:ProgramFiles(x86)}\\Microsoft SDKs\\ClickOnce\\SignTool"
    ) | Where-Object { $_ -and (Test-Path $_) }

    foreach ($root in $searchRoots) {
        $candidate = Get-ChildItem $root -Recurse -Filter signtool.exe -ErrorAction SilentlyContinue |
            Sort-Object FullName -Descending |
            Select-Object -First 1
        if ($candidate) {
            return $candidate.FullName
        }
    }

    throw 'Unable to locate signtool.exe. Install the Windows SDK or run on a GitHub Windows runner image that includes SignTool.'
}

if (-not (Test-Path $FilePath)) {
    throw "The file to sign does not exist: $FilePath"
}

if (-not (Test-Path $PfxPath)) {
    throw "The signing certificate file does not exist: $PfxPath"
}

$signToolPath = Find-SignToolPath
$securePassword = ConvertTo-SecureString $PfxPassword -AsPlainText -Force
$cert = Import-PfxCertificate `
    -FilePath $PfxPath `
    -CertStoreLocation Cert:\CurrentUser\My `
    -Password $securePassword `
    -Exportable:$false

if (-not $cert) {
    throw "Unable to import signing certificate: $PfxPath"
}

if ($cert -is [System.Array]) {
    $cert = $cert | Select-Object -First 1
}

try {
    $timestampUrls = @(
        $TimestampUrl,
        'http://timestamp.sectigo.com',
        'http://timestamp.globalsign.com/tsa/r6advanced1'
    )

    $signed = $false
    foreach ($timestampUrl in $timestampUrls) {
        & $signToolPath sign /fd $DigestAlgorithm /td $DigestAlgorithm /tr $timestampUrl /sha1 $cert.Thumbprint /v $FilePath
        if ($LASTEXITCODE -eq 0) {
            $signed = $true
            break
        }

        Write-Warning "Timestamp server $timestampUrl failed, trying next..."
    }

    if (-not $signed) {
        throw 'signtool sign failed: all timestamp servers exhausted.'
    }

    & $signToolPath verify /pa /v $FilePath
    if ($LASTEXITCODE -ne 0) {
        throw "signtool verify failed with exit code $LASTEXITCODE."
    }
}
finally {
    Remove-Item "Cert:\CurrentUser\My\$($cert.Thumbprint)" -Force -ErrorAction SilentlyContinue
}
