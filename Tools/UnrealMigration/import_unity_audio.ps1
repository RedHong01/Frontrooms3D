[CmdletBinding()]
param(
    [string]$Root = (Resolve-Path (Join-Path $PSScriptRoot "../..")).Path,
    [string]$UnrealEditorCmd = "D:\UE_5.8\Engine\Binaries\Win64\UnrealEditor-Cmd.exe"
)

$ErrorActionPreference = "Stop"
$Root = (Resolve-Path -LiteralPath $Root).Path
$project = Join-Path $Root "Migration/Unreal/FrontRoomsss.uproject"
$settings = Join-Path $Root "Migration/exports/unreal_audio_import_settings.json"
if (-not (Test-Path -LiteralPath $settings -PathType Leaf)) {
    throw "Audio import settings missing; run node Tools/UnrealMigration/export_unreal_audio_import_settings.mjs first"
}
$engine = & (Join-Path $PSScriptRoot "resolve_unreal_engine.ps1") -Root $Root -UnrealEditorCmd $UnrealEditorCmd
& $engine.EditorCmd $project -run=ImportAssets "-importsettings=$settings" -dest=/Game/FrontRooms/Audio -nosourcecontrol -unattended -nop4 -nullrhi
if ($LASTEXITCODE -ne 0) { throw "Unreal audio import failed with exit code $LASTEXITCODE" }
Write-Host "Imported Unity/FMOD source audio into /Game/FrontRooms/Audio"
