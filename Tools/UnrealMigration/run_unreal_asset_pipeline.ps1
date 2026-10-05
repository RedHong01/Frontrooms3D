[CmdletBinding()]
param(
    [string]$Root = (Resolve-Path (Join-Path $PSScriptRoot "../..")).Path,
    [string]$UnrealEditorCmd = "D:\UE_5.8\Engine\Binaries\Win64\UnrealEditor-Cmd.exe",
    [switch]$SkipImport,
    [switch]$WhatIf
)

# One repeatable Windows asset pass. Unity remains the source of truth;
# staging and UE ImportAssets can be rerun after a Mac-side Unity change.
$ErrorActionPreference = "Stop"
$Root = (Resolve-Path -LiteralPath $Root).Path
$project = Join-Path $Root "Migration/Unreal/FrontRoomsss.uproject"
$stage = Join-Path $Root "Tools/UnrealMigration/stage_unity_assets.ps1"
$meshImport = Join-Path $Root "Tools/UnrealMigration/import_unity_fbx.ps1"
$textureImport = Join-Path $Root "Tools/UnrealMigration/import_unity_textures.ps1"
$audioImport = Join-Path $Root "Tools/UnrealMigration/import_unity_audio.ps1"
$audit = Join-Path $Root "Tools/UnrealMigration/audit_unreal_assets.ps1"
$sidecarDirectory = Join-Path $Root "Assets/Resources/Props/Models"
$sidecarReport = Join-Path $Root "Migration/exports/unreal_sidecar_asset_factory.json"
$materialReport = Join-Path $Root "Migration/exports/unreal_material_asset_factory.json"
if (-not (Test-Path -LiteralPath $project)) { throw "Unreal project not found: $project" }
if (-not (Test-Path -LiteralPath $UnrealEditorCmd) -and -not $SkipImport) { throw "UnrealEditor-Cmd not found: $UnrealEditorCmd" }

& node (Join-Path $Root "Tools/UnrealMigration/export_asset_bridge.mjs") $Root
if ($LASTEXITCODE -ne 0) { throw "Asset bridge export failed" }
& node (Join-Path $Root "Tools/UnrealMigration/export_unreal_material_profiles.mjs") $Root
if ($LASTEXITCODE -ne 0) { throw "Material profile export failed" }
& node (Join-Path $Root "Tools/UnrealMigration/export_unreal_material_factory.mjs") $Root
if ($LASTEXITCODE -ne 0) { throw "Material factory export failed" }
& node (Join-Path $Root "Tools/UnrealMigration/export_unreal_audio_import_settings.mjs") $Root
if ($LASTEXITCODE -ne 0) { throw "Audio import settings export failed" }
if ($WhatIf) {
    & $stage -Root $Root -WhatIf
    if ($LASTEXITCODE -ne 0) { throw "Unity asset staging preview failed" }
    Write-Host "FrontRooms Unreal Windows asset pipeline preview complete"
    exit 0
} else {
    & $stage -Root $Root
}
if ($LASTEXITCODE -ne 0) { throw "Unity asset staging failed" }
if (-not $SkipImport -and -not $WhatIf) {
    & $meshImport -Root $Root -UnrealEditorCmd $UnrealEditorCmd
    if ($LASTEXITCODE -ne 0) { throw "FBX import failed" }
    & $textureImport -Root $Root -UnrealEditorCmd $UnrealEditorCmd
    if ($LASTEXITCODE -ne 0) { throw "Texture import failed" }
    & $audioImport -Root $Root -UnrealEditorCmd $UnrealEditorCmd
    if ($LASTEXITCODE -ne 0) { throw "Audio import failed" }
    & $UnrealEditorCmd $project -unattended -nop4 -nosplash -nullrhi -stdout -UTF8Output -run=FrontRoomsMaterialFactory ("-Manifest=" + (Join-Path $Root "Migration/exports/unreal_material_factory.json")) ("-Report=" + $materialReport)
    if ($LASTEXITCODE -ne 0) { throw "Material asset factory failed" }
    & $UnrealEditorCmd $project -unattended -nop4 -nosplash -nullrhi -stdout -UTF8Output -run=FrontRoomsSidecar -ApplyToAssets ("-Sidecars=" + $sidecarDirectory) ("-Report=" + $sidecarReport)
    if ($LASTEXITCODE -ne 0) { throw "Sidecar asset factory failed" }
}
& $audit -Root $Root
if ($LASTEXITCODE -ne 0) { throw "Asset audit failed" }
Write-Host "FrontRooms Unreal Windows asset pipeline passed"
