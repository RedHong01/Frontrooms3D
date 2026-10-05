[CmdletBinding()]
param(
    [string]$Root = (Resolve-Path (Join-Path $PSScriptRoot "../..")),
    [string]$UnrealEditorCmd = "D:\UE_5.6\Engine\Binaries\Win64\UnrealEditor-Cmd.exe"
)

$Root = (Resolve-Path $Root).Path
$project = Join-Path $Root "Migration/Unreal/FrontRoomsss.uproject"
$settings = Join-Path $Root "Migration/exports/unreal_texture_import_settings.json"
if (-not (Test-Path -LiteralPath $UnrealEditorCmd)) { throw "UnrealEditor-Cmd not found: $UnrealEditorCmd" }
& node (Join-Path $Root "Tools/UnrealMigration/export_unreal_texture_settings.mjs") $Root
if ($LASTEXITCODE -ne 0) { throw "Could not generate texture import settings" }
& $UnrealEditorCmd $project -run=ImportAssets "-importsettings=$settings" -dest=/Game/FrontRooms/UnityImported/Textures -nosourcecontrol -unattended -nop4 -nullrhi
if ($LASTEXITCODE -ne 0) { throw "Unreal texture import failed with exit code $LASTEXITCODE" }
Write-Host "Imported Unity surface textures into /Game/FrontRooms/UnityImported/Textures"
