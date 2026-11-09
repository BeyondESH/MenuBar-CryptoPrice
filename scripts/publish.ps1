$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$dist = Join-Path $root 'dist'
$settings = Join-Path $dist 'settings'

New-Item -ItemType Directory -Force -Path $dist | Out-Null
if (Test-Path $settings) {
    Remove-Item -Recurse -Force $settings
}
New-Item -ItemType Directory -Force -Path $settings | Out-Null

dotnet publish (Join-Path $root 'src\MenuBarCryptoPrice\MenuBarCryptoPrice.csproj') `
  -c Release -r win-x64 --self-contained true -o $dist

dotnet publish (Join-Path $root 'src\MenuBarCryptoPrice.Settings.WinUI\MenuBarCryptoPrice.Settings.WinUI.csproj') `
  -c Release -r win-x64 --self-contained true -o $settings

Write-Output "Published to $dist"
