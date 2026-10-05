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
$profile = Join-Path $Root ".ue58-profile"
$local = Join-Path $profile "AppData/Local"
$roaming = Join-Path $profile "AppData/Roaming"
$ubaRoot = Join-Path $profile "UBA"
New-Item -ItemType Directory -Path $local, $roaming -Force | Out-Null
$null = New-Item -ItemType Directory -Path $ubaRoot -Force
$previousUserProfile = $env:USERPROFILE
$previousLocalAppData = $env:LOCALAPPDATA
$previousAppData = $env:APPDATA
$previousUbaRoot = $env:UBA_ROOT
$previousUebpEngineSavedFolder = $env:uebp_EngineSavedFolder
$previousUebpLogFolder = $env:uebp_LogFolder
$previousUebpFinalLogFolder = $env:uebp_FinalLogFolder
$env:USERPROFILE = $profile
$env:LOCALAPPDATA = $local
$env:APPDATA = $roaming
$env:HOME = $profile
$env:UBA_ROOT = $ubaRoot
# Installed UE builds resolve AutomationTool's commandlet logs under the
# engine directory by default. That directory is read-only for a normal
# developer account, so redirect both the commandlet scratch area and logs to
# the disposable workspace profile used by this migration gate.
$automationSaved = Join-Path $profile "AutomationTool/Saved"
$automationLogs = Join-Path $profile "AutomationTool/Logs"
New-Item -ItemType Directory -Path $automationSaved, $automationLogs -Force | Out-Null
$env:uebp_EngineSavedFolder = $automationSaved
$env:uebp_LogFolder = $automationLogs
$env:uebp_FinalLogFolder = $automationLogs

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
        "-NoUba",
        # UE 5.8's cooker otherwise re-enables Zen from the engine default
        # even when ProjectPackagingSettings disables it. Keep this explicit so
        # the Win64 package can be built without a local Zen service.
        "-AdditionalCookerOptions=-SkipZenStore"
    )
    if ($SkipCook) { $args += "-skipcook" } else { $args += "-cook" }
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
    if ($null -ne $previousUbaRoot) { $env:UBA_ROOT = $previousUbaRoot } else { Remove-Item Env:UBA_ROOT -ErrorAction SilentlyContinue }
    if ($null -ne $previousUebpEngineSavedFolder) { $env:uebp_EngineSavedFolder = $previousUebpEngineSavedFolder } else { Remove-Item Env:uebp_EngineSavedFolder -ErrorAction SilentlyContinue }
    if ($null -ne $previousUebpLogFolder) { $env:uebp_LogFolder = $previousUebpLogFolder } else { Remove-Item Env:uebp_LogFolder -ErrorAction SilentlyContinue }
    if ($null -ne $previousUebpFinalLogFolder) { $env:uebp_FinalLogFolder = $previousUebpFinalLogFolder } else { Remove-Item Env:uebp_FinalLogFolder -ErrorAction SilentlyContinue }
}
