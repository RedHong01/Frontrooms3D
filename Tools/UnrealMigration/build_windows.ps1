[CmdletBinding()]
param(
    [string]$Root = (Resolve-Path (Join-Path $PSScriptRoot "../..")).Path,
    [string]$Output = "",
    [ValidateSet("Development", "Shipping")][string]$Configuration = "Development",
    [switch]$SkipCook
)

$ErrorActionPreference = "Stop"
$Root = (Resolve-Path -LiteralPath $Root).Path
$project = Join-Path $Root "Migration/Unreal/FrontRoomsUE.uproject"
if (-not (Test-Path -LiteralPath $project -PathType Leaf)) { throw "Unreal project not found: $project" }

$engine = & (Join-Path $PSScriptRoot "resolve_unreal_engine.ps1") -Root $Root
$uat = Join-Path (Split-Path -Parent $engine.BuildScript) "RunUAT.bat"
if (-not (Test-Path -LiteralPath $uat -PathType Leaf)) { throw "RunUAT.bat not found: $uat" }

if (-not $Output) {
    $Output = Join-Path $Root "Migration/Unreal/Builds/Windows/$Configuration"
}
$Output = [IO.Path]::GetFullPath($Output)
New-Item -ItemType Directory -Path $Output -Force | Out-Null

# Keep editor and UAT caches inside the repository's disposable profile. This
# also makes the gate usable when the caller has no write access to the default
# Unreal user cache directories.
$profile = Join-Path $Root ".editor-profile"
$local = Join-Path $profile "AppData/Local"
$roaming = Join-Path $profile "AppData/Roaming"
New-Item -ItemType Directory -Path $local, $roaming -Force | Out-Null
$previousUserProfile = $env:USERPROFILE
$previousLocalAppData = $env:LOCALAPPDATA
$previousAppData = $env:APPDATA
$env:USERPROFILE = $profile
$env:LOCALAPPDATA = $local
$env:APPDATA = $roaming
$env:HOME = $profile

try {
    $args = @(
        "BuildCookRun",
        "-project=$project",
        "-noP4",
        "-utf8output",
        "-unattended",
        "-platform=Win64",
        "-clientconfig=$Configuration",
        "-build",
        "-stage",
        "-pak",
        "-archive",
        "-archivedirectory=$Output",
        "-NoUba"
    )
    if (-not $SkipCook) { $args += "-cook" }
    Write-Host "WINDOWS BUILD using Unreal $($engine.Version)"
    Write-Host "WINDOWS BUILD output: $Output"
    & $uat @args
    if ($LASTEXITCODE -ne 0) { throw "Windows BuildCookRun failed with exit code $LASTEXITCODE" }

    $exe = Get-ChildItem -LiteralPath $Output -Recurse -Filter "FrontRooms.exe" -File | Select-Object -First 1
    if (-not $exe) { throw "Windows build completed without FrontRooms.exe under $Output" }
    Write-Host "Windows build passed: $($exe.FullName)"
} finally {
    if ($null -ne $previousUserProfile) { $env:USERPROFILE = $previousUserProfile }
    if ($null -ne $previousLocalAppData) { $env:LOCALAPPDATA = $previousLocalAppData }
    if ($null -ne $previousAppData) { $env:APPDATA = $previousAppData }
}
