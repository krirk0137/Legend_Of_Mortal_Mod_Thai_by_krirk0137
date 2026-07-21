# Build LomThaiText and install it into the live game.
#   .\install.ps1                      # build + install DLL + install poc\Thai.tsv if absent
#   .\install.ps1 -Tsv path\to\x.tsv   # also overwrite the installed Thai.tsv
#   .\install.ps1 -Uninstall           # remove the plugin folder entirely
param(
    [string]$GameDir = 'C:\Program Files (x86)\Steam\steamapps\common\LegendOfMortal',
    [string]$Tsv,
    [switch]$Uninstall
)
$ErrorActionPreference = 'Stop'
$src  = Join-Path $PSScriptRoot 'LomThaiText'
$dest = Join-Path $GameDir 'BepInEx\plugins\LomThaiText'

if ($Uninstall) {
    if (Test-Path $dest) { Remove-Item -Recurse -Force $dest; Write-Host "removed $dest" }
    else { Write-Host "nothing installed at $dest" }
    return
}

dotnet build $src -c Release -v minimal -p:GameDir="$GameDir"
if ($LASTEXITCODE -ne 0) { throw 'build failed' }

New-Item -ItemType Directory -Force $dest | Out-Null
Copy-Item (Join-Path $src 'bin\Release\LomThaiText.dll') $dest -Force

$destTsv = Join-Path $dest 'Thai.tsv'
if ($Tsv) { Copy-Item $Tsv $destTsv -Force }
elseif (-not (Test-Path $destTsv)) { Copy-Item (Join-Path $src 'poc\Thai.tsv') $destTsv -Force }

Write-Host "installed -> $dest"
Get-ChildItem $dest | Select-Object Name, Length, LastWriteTime | Format-Table -AutoSize
