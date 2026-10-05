[CmdletBinding()]
param(
    [string]$Root = (Resolve-Path (Join-Path $PSScriptRoot "../..")).Path,
    [string]$UnrealEditor = "D:\UE_5.8\Engine\Binaries\Win64\UnrealEditor.exe",
    [string]$Map = "/Game/FrontRooms/Maps/FrontRoomsRuntime"
)

$ErrorActionPreference = "Stop"
$Root = (Resolve-Path -LiteralPath $Root).Path
$Project = Join-Path $Root "Migration/Unreal/FrontRoomsUE.uproject"
if (-not (Test-Path -LiteralPath $UnrealEditor)) { throw "UnrealEditor was not found: $UnrealEditor" }
if (-not (Test-Path -LiteralPath $Project)) { throw "FrontRoomsUE.uproject was not found: $Project" }

# Keep UE's editor/Zen/UBT state in the workspace. This avoids stale or
# inaccessible per-user caches and skips startup SDK probing for platforms the
# project intentionally does not target. Win64 remains the only build target.
$profile = Join-Path $Root ".ue58-profile"
$env:USERPROFILE = $profile
$env:LOCALAPPDATA = Join-Path $profile "AppData/Local"
$env:APPDATA = Join-Path $profile "AppData/Roaming"
$env:UBA_ROOT = Join-Path $profile "UBA"
$env:UE_SKIP_UBT_SDK_SETUP = "1"
New-Item -ItemType Directory -Path $env:LOCALAPPDATA, $env:APPDATA, $env:UBA_ROOT -Force | Out-Null

Start-Process -FilePath $UnrealEditor -ArgumentList @($Project, "-map=$Map", "-dx12", "-sm6", "-NoSplash") -WorkingDirectory $Root | Out-Null
Write-Host "Opened FrontRoomsUE in Unreal Engine 5.8.3: $Project"
