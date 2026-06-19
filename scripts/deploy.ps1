param(
    [string]$GameRoot = "C:\Program Files (x86)\Steam\steamapps\common\Nuclear Option",
    [string]$Configuration = "Release",
    [switch]$ClearHarmonyCache
)

$ErrorActionPreference = "Stop"

$RepoRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $RepoRoot "MissileCamera.csproj"

if (Get-Process -Name "NuclearOption" -ErrorAction SilentlyContinue) {
    Write-Error "Close Nuclear Option before deploy."
}

dotnet build $project -c $Configuration --verbosity minimal
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$dll = Join-Path $RepoRoot "bin\$Configuration\net48\MissileCamera.dll"
if (-not (Test-Path $dll)) {
    Write-Error "Build output missing: $dll"
}

$pluginRoot = Join-Path $GameRoot "BepInEx\plugins\MissileCamera"
New-Item -ItemType Directory -Path $pluginRoot -Force | Out-Null

Copy-Item -Force $dll (Join-Path $pluginRoot "MissileCamera.dll")

if ($ClearHarmonyCache) {
    $cache = Join-Path $GameRoot "BepInEx\cache\harmony_interop_cache.dat"
    if (Test-Path $cache) {
        Remove-Item -Force $cache
        Write-Host "Removed harmony_interop_cache.dat"
    }
}

Get-Item (Join-Path $pluginRoot "MissileCamera.dll") | Format-List FullName, Length, LastWriteTime
Write-Host "MissileCamera (BepInEx) deployed to $pluginRoot"
