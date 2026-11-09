param(
    [switch]$SkipPublish
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot

if (-not $SkipPublish) {
    & (Join-Path $PSScriptRoot 'publish.ps1')
}

$iscc = @(
    (Join-Path $env:LOCALAPPDATA 'Programs\Inno Setup 6\ISCC.exe'),
    'C:\Program Files (x86)\Inno Setup 6\ISCC.exe',
    'C:\Program Files\Inno Setup 6\ISCC.exe'
) | Where-Object { Test-Path $_ } | Select-Object -First 1

if (-not $iscc) {
    throw 'Inno Setup 6 not found. Install it with: winget install --id JRSoftware.InnoSetup -e --source winget'
}

& $iscc (Join-Path $root 'installer\MenuBarCryptoPrice.iss')
