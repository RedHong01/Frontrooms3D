[CmdletBinding()]
param(
    [string]$Root = (Resolve-Path (Join-Path $PSScriptRoot "../..")),
    [string]$UnrealEditorCmd = "D:\UE_5.8\Engine\Binaries\Win64\UnrealEditor-Cmd.exe"
)

$Root = (Resolve-Path $Root).Path
$project = Join-Path $Root "Migration/Unreal/FrontRoomsUE.uproject"
$settings = Join-Path $Root "Migration/exports/unreal_import_settings.json"
if (-not (Test-Path -LiteralPath $UnrealEditorCmd)) { throw "UnrealEditor-Cmd not found: $UnrealEditorCmd" }
if (-not (Test-Path -LiteralPath $project)) { throw "Unreal project not found: $project" }
& node (Join-Path $Root "Tools/UnrealMigration/export_unreal_import_settings.mjs") $Root
if ($LASTEXITCODE -ne 0) { throw "Could not generate Unreal import settings" }
& $UnrealEditorCmd $project -run=ImportAssets "-importsettings=$settings" -dest=/Game/FrontRooms/UnityImported -nosourcecontrol -unattended -nop4 -nullrhi
if ($LASTEXITCODE -ne 0) { throw "Unreal FBX import failed with exit code $LASTEXITCODE" }
Write-Host "Imported all Unity FBX files into /Game/FrontRooms/UnityImported"
