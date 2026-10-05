[CmdletBinding()]
param(
    [string]$Root = (Resolve-Path (Join-Path $PSScriptRoot "../..")).Path,
    [string]$ReportPath,
    [switch]$Strict
)

$ErrorActionPreference = "Stop"
$Root = (Resolve-Path -LiteralPath $Root).Path
$project = Join-Path $Root "Migration/Unreal/FrontRoomsUE.uproject"
$bridgePath = Join-Path $Root "Migration/exports/asset_bridge.json"
$kitPath = Join-Path $Root "Migration/exports/kit_manifest.json"
$contentRoot = Join-Path $Root "Migration/Unreal/Content"
$stagedRoot = Join-Path $contentRoot "FrontRooms/UnitySource"
$importedRoot = Join-Path $contentRoot "FrontRooms/UnityImported"
$ReportPath = if ($ReportPath) { $ReportPath } else { Join-Path $Root "Migration/exports/unreal_asset_audit.json" }

function Normalize-Rel([string]$path) { return ($path -replace '\\','/').TrimStart('./') }
function Add-Missing([System.Collections.Generic.List[string]]$list, [string]$value) { if ($list.Count -lt 20) { $list.Add($value) } }
function Check-Source([string]$relative, [string]$kind) {
    $source = Join-Path $Root ($relative -replace '/', '\')
    if (-not (Test-Path -LiteralPath $source -PathType Leaf)) { Add-Missing $report.missingSources "${kind}:$relative"; return $false }
    $staged = Join-Path $stagedRoot ($relative.Substring("Assets/".Length) -replace '/', '\')
    if (-not (Test-Path -LiteralPath $staged -PathType Leaf)) { Add-Missing $report.missingStaged "${kind}:$relative"; return $false }
    return $true
}

if (-not (Test-Path -LiteralPath $bridgePath)) { throw "Missing asset bridge: $bridgePath" }
if (-not (Test-Path -LiteralPath $kitPath)) { throw "Missing kit manifest: $kitPath" }
$bridge = Get-Content -Raw -LiteralPath $bridgePath | ConvertFrom-Json
$kit = Get-Content -Raw -LiteralPath $kitPath | ConvertFrom-Json
$report = [ordered]@{
    schema = "frontrooms.unreal.asset-audit"
    schemaVersion = 1
    generatedUtc = [DateTime]::UtcNow.ToString("o")
    project = (Resolve-Path $project).Path
    targetPlatforms = @("Win64")
    expected = [ordered]@{}
    coverage = [ordered]@{}
    sidecars = [ordered]@{ valid = 0; invalid = 0; colliderShapes = 0; lodEntries = 0; warnings = @() }
    materials = [ordered]@{ profiles = 0; sourceFiles = 0; profileFile = $null }
    logs = [ordered]@{ meshImportWarnings = 0; meshImportErrors = 0; textureImportWarnings = 0; textureImportErrors = 0 }
    missingSources = [System.Collections.Generic.List[string]]::new()
    missingStaged = [System.Collections.Generic.List[string]]::new()
    stagedHashMismatches = [System.Collections.Generic.List[string]]::new()
    missingImported = [System.Collections.Generic.List[string]]::new()
    status = "running"
}

$groups = $bridge.files | Group-Object kind | ForEach-Object { $report.expected[$_.Name] = $_.Count }
$meshFiles = @($bridge.files | Where-Object kind -eq "mesh")
$textureFiles = @($bridge.files | Where-Object kind -eq "texture")
$sidecarFiles = @($bridge.files | Where-Object kind -eq "sidecar")
$materialFiles = @($bridge.files | Where-Object kind -eq "unity-material-source")
$unityMatFiles = @($materialFiles | Where-Object { $_.source -like "*.mat" })
$audioFiles = @($bridge.files | Where-Object kind -in @("audio","fmod-bank","video","font"))

foreach ($file in $bridge.files) {
    if (Check-Source $file.source $file.kind) {
        $staged = Join-Path $stagedRoot ($file.source.Substring("Assets/".Length) -replace '/', '\')
        $hash = (Get-FileHash -Algorithm SHA256 -LiteralPath $staged).Hash.ToLowerInvariant()
        if ($hash -ne $file.sha256.ToLowerInvariant()) { Add-Missing $report.stagedHashMismatches "$($file.kind):$($file.source)" }
    }
}

$meshImported = 0
foreach ($file in $meshFiles) {
    $stem = [IO.Path]::GetFileNameWithoutExtension($file.source)
    $category = if ($file.source -like "*/Models/Office/*") { "Office" } else { "Props" }
    $folder = Join-Path $importedRoot "$category/$stem"
    $assets = @(Get-ChildItem -LiteralPath $folder -Filter *.uasset -File -ErrorAction SilentlyContinue)
    if ($assets.Count -gt 0) { $meshImported++ } else { Add-Missing $report.missingImported "mesh:$file.source" }
}
$textureImported = 0
$textureFolder = Join-Path $importedRoot "Textures"
foreach ($file in $textureFiles) {
    $stem = [IO.Path]::GetFileNameWithoutExtension($file.source)
    if (Test-Path -LiteralPath (Join-Path $textureFolder "$stem.uasset") -PathType Leaf) { $textureImported++ } else { Add-Missing $report.missingImported "texture:$file.source" }
}
$report.coverage.meshSources = [ordered]@{ expected = $meshFiles.Count; importedPackages = $meshImported }
$report.coverage.textureSources = [ordered]@{ expected = $textureFiles.Count; importedAssets = $textureImported }
$report.coverage.sidecarSources = [ordered]@{ expected = $sidecarFiles.Count; present = 0 }
$report.coverage.miscSources = [ordered]@{ expected = $audioFiles.Count; present = 0 }

foreach ($file in $sidecarFiles) {
    $source = Join-Path $Root ($file.source -replace '/', '\')
    if (-not (Test-Path -LiteralPath $source -PathType Leaf)) { continue }
    $report.coverage.sidecarSources.present++
    try {
        $sidecar = Get-Content -Raw -LiteralPath $source | ConvertFrom-Json
        $valid = $true
        if (-not $sidecar.name) { $valid = $false }
        foreach ($collider in @($sidecar.colliders)) {
            $report.sidecars.colliderShapes++
            if (@($collider.size).Count -ne 3 -or (@($collider.size) | Where-Object { [double]$_ -le 0 }).Count -gt 0) { $valid = $false }
        }
        $lastDistance = 0.0
        if ($null -ne $sidecar.lodDistances) {
            foreach ($distance in @($sidecar.lodDistances)) {
                if ($null -ne $distance) {
                    $report.sidecars.lodEntries++
                    # The sidecar uses -1 for an uncullable final LOD.
                    if ([double]$distance -ge 0) {
                        if ([double]$distance -lt $lastDistance) { $valid = $false }
                        $lastDistance = [double]$distance
                    }
                }
            }
        }
        if ($null -ne $sidecar.lodRatios) {
            foreach ($ratio in @($sidecar.lodRatios)) {
                # Null means that the optional later LOD is not exported yet.
                if ($null -ne $ratio -and ([double]$ratio -le 0 -or [double]$ratio -gt 1)) { $valid = $false }
            }
        }
        if ($valid) { $report.sidecars.valid++ } else { $report.sidecars.invalid++ ; if ($report.sidecars.warnings.Count -lt 20) { $report.sidecars.warnings += $file.source } }
    } catch { $report.sidecars.invalid++; if ($report.sidecars.warnings.Count -lt 20) { $report.sidecars.warnings += "$($file.source): $($_.Exception.Message)" } }
}

foreach ($file in $audioFiles) {
    $source = Join-Path $Root ($file.source -replace '/', '\')
    if (Test-Path -LiteralPath $source -PathType Leaf) { $report.coverage.miscSources.present++ }
}

$profilePath = Join-Path $Root "Migration/exports/unreal_material_profiles.json"
& node (Join-Path $Root "Tools/UnrealMigration/export_unreal_material_profiles.mjs") $Root | Out-Host
if ($LASTEXITCODE -eq 0 -and (Test-Path -LiteralPath $profilePath)) {
    $profiles = Get-Content -Raw -LiteralPath $profilePath | ConvertFrom-Json
    $report.materials.profiles = @($profiles.profiles).Count
    $report.materials.sourceFiles = $unityMatFiles.Count
    $report.materials.profileFile = $profilePath
}

$meshLog = Join-Path $Root "Migration/Unreal/Saved/batch-import.log"
$textureLog = Join-Path $Root "Migration/Unreal/Saved/texture-import.log"
foreach ($spec in @(@($meshLog, "meshImportWarnings", "meshImportErrors"), @($textureLog, "textureImportWarnings", "textureImportErrors"))) {
    if (Test-Path -LiteralPath $spec[0]) {
        $text = Get-Content -Raw -LiteralPath $spec[0]
        $report.logs[$spec[1]] = ([regex]::Matches($text, "LogStaticMesh: Warning:|LogInterchangeImport: Warning:")).Count
        $report.logs[$spec[2]] = ([regex]::Matches($text, "Error:")).Count
    }
}

$allCoveragePass = ($meshImported -eq $meshFiles.Count -and $textureImported -eq $textureFiles.Count -and
    $report.coverage.sidecarSources.present -eq $sidecarFiles.Count -and $report.coverage.miscSources.present -eq $audioFiles.Count -and
    $report.stagedHashMismatches.Count -eq 0)
$report.status = if ($allCoveragePass -and $report.sidecars.invalid -eq 0 -and $report.materials.profiles -eq $unityMatFiles.Count) { "passed" } else { "failed" }
$json = $report | ConvertTo-Json -Depth 12
New-Item -ItemType Directory -Path (Split-Path -Parent $ReportPath) -Force | Out-Null
Set-Content -LiteralPath $ReportPath -Value $json -Encoding UTF8
Write-Host "FrontRooms Unreal asset audit $($report.status): $meshImported/$($meshFiles.Count) FBX packages, $textureImported/$($textureFiles.Count) textures, $($report.sidecars.valid)/$($sidecarFiles.Count) sidecars, $($report.materials.profiles) material profiles"
Write-Host "Asset audit report: $ReportPath"
if ($report.logs.meshImportWarnings -gt 0) { Write-Warning "Mesh import has $($report.logs.meshImportWarnings) bounds/tangent warnings; sidecar collision/LOD metadata is the remediation source." }
if ($Strict -and $report.logs.meshImportWarnings -gt 0) { exit 2 }
if ($report.status -ne "passed") { exit 1 }
