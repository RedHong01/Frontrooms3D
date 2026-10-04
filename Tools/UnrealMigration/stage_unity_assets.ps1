[CmdletBinding(SupportsShouldProcess)]
param(
    [string]$Root = (Resolve-Path (Join-Path $PSScriptRoot "../..")),
    [string]$Destination = "Migration/Unreal/Content/FrontRooms/UnitySource"
)

$Root = (Resolve-Path $Root).Path
$manifestPath = Join-Path $Root "Migration/exports/asset_bridge.json"
if (-not (Test-Path -LiteralPath $manifestPath)) {
    throw "Missing $manifestPath. Run: node Tools/UnrealMigration/export_asset_bridge.mjs"
}
$manifest = Get-Content -Raw -LiteralPath $manifestPath | ConvertFrom-Json
$destinationRoot = Join-Path $Root $Destination
foreach ($file in $manifest.files) {
    $source = Join-Path $Root ($file.source -replace '/', '\')
    $relativeFromAssets = $file.source.Substring("Assets/".Length) -replace '/', '\'
    $destinationPath = Join-Path $Root (Join-Path $Destination $relativeFromAssets)
    $parent = Split-Path -Parent $destinationPath
    New-Item -ItemType Directory -Force -Path $parent | Out-Null
    if ($PSCmdlet.ShouldProcess($destinationPath, "Copy $source")) {
        Copy-Item -LiteralPath $source -Destination $destinationPath -Force
    }
}
Write-Host "Staged $($manifest.files.Count) Unity assets under $destinationRoot"
