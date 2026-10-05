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
$audioRoot = Join-Path $contentRoot "FrontRooms/Audio"
$audioSettingsPath = Join-Path $Root "Migration/exports/unreal_audio_import_settings.json"
$materialAssetFactoryPath = Join-Path $Root "Migration/exports/unreal_material_asset_factory.json"
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
    materials = [ordered]@{
        profiles = 0
        sourceFiles = 0
        profileFile = $null
        factoryFile = $null
        factoryTextures = 0
        factorySourceTextures = 0
        factoryImportedTextures = 0
        factoryMissingTextures = @()
        factoryUnresolvedTextures = @()
        channelCounts = [ordered]@{ A = 0; N = 0; S = 0; E = 0; M = 0; P = 0 }
        sourceChannelCounts = [ordered]@{ A = 0; N = 0; S = 0; E = 0; M = 0; P = 0 }
        assetFactory = [ordered]@{
            reportFile = $materialAssetFactoryPath
            status = "not-run"
            requested = 0
            applied = 0
            skipped = 0
            errors = 0
            changed = 0
            channelCounts = [ordered]@{ A = 0; N = 0; S = 0; E = 0; M = 0; P = 0 }
            mismatches = @()
        }
    }
    audio = [ordered]@{ settingsFile = $audioSettingsPath; expected = 0; sourceFiles = 0; importedAssets = 0; missingAssets = @(); fmodBanks = [ordered]@{ expected = 0; staged = 0; copied = 0; hashMismatches = @(); runtimeDll = $false; manifest = (Join-Path $Root "Migration/exports/fmod_bank_manifest.json") } }
    logs = [ordered]@{ meshImportWarnings = 0; meshImportErrors = 0; textureImportWarnings = 0; textureImportErrors = 0 }
    missingSources = [System.Collections.Generic.List[string]]::new()
    missingStaged = [System.Collections.Generic.List[string]]::new()
    stagedHashMismatches = [System.Collections.Generic.List[string]]::new()
    missingImported = [System.Collections.Generic.List[string]]::new()
    status = "running"
}

# The editor material factory writes actual UTexture2D import properties.  A
# source-only audit remains useful before the commandlet runs, so report this
# gate when its output exists and let smoke/pipeline enforce its presence at
# the editor stage.
if (Test-Path -LiteralPath $materialAssetFactoryPath -PathType Leaf) {
    try {
        $assetFactory = Get-Content -Raw -LiteralPath $materialAssetFactoryPath | ConvertFrom-Json
        $report.materials.assetFactory.status = if ([int]$assetFactory.errors -eq 0) { "passed" } else { "failed" }
        $report.materials.assetFactory.requested = [int]$assetFactory.requested
        $report.materials.assetFactory.applied = [int]$assetFactory.applied
        $report.materials.assetFactory.skipped = [int]$assetFactory.skipped
        $report.materials.assetFactory.errors = [int]$assetFactory.errors
        $report.materials.assetFactory.changed = [int]$assetFactory.changed
        foreach ($channel in @('A','N','S','E','M','P')) {
            $report.materials.assetFactory.channelCounts[$channel] = [int]$assetFactory.appliedByChannel.$channel
        }
        $mismatches = @($assetFactory.textures | Where-Object {
            (-not $_.applied) -or
            ([string]$_.actualCompressionSettings -ne [string]$_.compressionSettings) -or
            ([bool]$_.actualSRGB -ne [bool]$_.sRGB) -or
            ([bool]$_.actualNormalMap -ne [bool]$_.normalMap) -or
            ([bool]$_.actualCompressionNoAlpha -ne (([string]$_.channel -eq 'S') -or ([string]$_.channel -eq 'M'))) -or
            ([string]$_.actualTextureGroup -ne [string]$_.textureGroup)
        } | ForEach-Object { [string]$_.ueAsset })
        $report.materials.assetFactory.mismatches = $mismatches
        if ($report.materials.assetFactory.errors -eq 0 -and $mismatches.Count -gt 0) {
            $report.materials.assetFactory.status = "failed"
        }
    } catch {
        $report.materials.assetFactory.status = "invalid"
        $report.materials.assetFactory.mismatches = @($_.Exception.Message)
    }
}

$groups = $bridge.files | Group-Object kind | ForEach-Object { $report.expected[$_.Name] = $_.Count }
$meshFiles = @($bridge.files | Where-Object kind -eq "mesh")
$textureFiles = @($bridge.files | Where-Object kind -eq "texture")
$sidecarFiles = @($bridge.files | Where-Object kind -eq "sidecar")
$materialFiles = @($bridge.files | Where-Object kind -eq "unity-material-source")
$unityMatFiles = @($materialFiles | Where-Object { $_.source -like "*.mat" })
$audioFiles = @($bridge.files | Where-Object kind -in @("audio","fmod-bank","video","font"))
$fmodBankFiles = @($bridge.files | Where-Object kind -eq "fmod-bank")
$report.audio.fmodBanks.expected = $fmodBankFiles.Count

foreach ($file in $bridge.files) {
    if (Check-Source $file.source $file.kind) {
        $staged = Join-Path $stagedRoot ($file.source.Substring("Assets/".Length) -replace '/', '\')
        $hash = (Get-FileHash -Algorithm SHA256 -LiteralPath $staged).Hash.ToLowerInvariant()
        if ($hash -ne $file.sha256.ToLowerInvariant()) { Add-Missing $report.stagedHashMismatches "$($file.kind):$($file.source)" }
    }
}

# Banks are staged twice: UnitySource preserves the original bridge bytes,
# while Content/FrontRooms/Audio/FMOD/Banks is the runtime payload used by the
# Win64 package. Compare both against the bridge hash so the native FMOD probe
# cannot accidentally exercise a different bank set.
foreach ($file in $fmodBankFiles) {
    $name = [IO.Path]::GetFileName($file.source)
    $stagedBank = Join-Path $contentRoot "FrontRooms/Audio/FMOD/Banks/$name"
    $sourceBank = Join-Path $Root ($file.source -replace '/', '\\')
    if (Test-Path -LiteralPath $stagedBank -PathType Leaf) {
        $report.audio.fmodBanks.copied++
        if ((Get-FileHash -Algorithm SHA256 -LiteralPath $stagedBank).Hash.ToLowerInvariant() -ne $file.sha256.ToLowerInvariant()) {
            $report.audio.fmodBanks.hashMismatches += $name
        }
    }
    $unitySourceBank = Join-Path $stagedRoot ($file.source.Substring("Assets/".Length) -replace '/', '\\')
    if (Test-Path -LiteralPath $unitySourceBank -PathType Leaf) { $report.audio.fmodBanks.staged++ }
}
$fmodDll = Join-Path $Root "Assets/Plugins/FMOD/platforms/win/lib/x86_64/fmodstudio.dll"
$report.audio.fmodBanks.runtimeDll = Test-Path -LiteralPath $fmodDll -PathType Leaf

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

if (Test-Path -LiteralPath $audioSettingsPath -PathType Leaf) {
    $audioSettings = Get-Content -Raw -LiteralPath $audioSettingsPath | ConvertFrom-Json
    $audioGroups = @($audioSettings.ImportGroups)
    $report.audio.expected = $audioGroups.Count
    foreach ($group in $audioGroups) {
        $destination = [string]$group.DestinationPath
        $package = [IO.Path]::GetFileName($destination.TrimEnd('/'))
        $relative = ($destination -replace '^/Game/','') -replace '/','\\'
        $assetPath = Join-Path (Join-Path $contentRoot $relative) ($package + '.uasset')
        if (Test-Path -LiteralPath $assetPath -PathType Leaf) {
            $report.audio.importedAssets++
        } elseif ($report.audio.missingAssets.Count -lt 20) {
            $report.audio.missingAssets += $destination
        }
    }
    $report.audio.sourceFiles = $audioGroups.Count
} else {
    $report.audio.missingAssets += "settings:$audioSettingsPath"
}

$profilePath = Join-Path $Root "Migration/exports/unreal_material_profiles.json"
& node (Join-Path $Root "Tools/UnrealMigration/export_unreal_material_profiles.mjs") $Root | Out-Host
if ($LASTEXITCODE -eq 0 -and (Test-Path -LiteralPath $profilePath)) {
    $profiles = Get-Content -Raw -LiteralPath $profilePath | ConvertFrom-Json
    $report.materials.profiles = @($profiles.profiles).Count
    $report.materials.sourceFiles = $unityMatFiles.Count
    $report.materials.profileFile = $profilePath
}
$factoryPath = Join-Path $Root "Migration/exports/unreal_material_factory.json"
& node (Join-Path $Root "Tools/UnrealMigration/export_unreal_material_factory.mjs") $Root | Out-Host
if ($LASTEXITCODE -eq 0 -and (Test-Path -LiteralPath $factoryPath)) {
    $factory = Get-Content -Raw -LiteralPath $factoryPath | ConvertFrom-Json
    $report.materials.factoryFile = $factoryPath
    $report.materials.factoryTextures = [int]$factory.textureCount
    $report.materials.factorySourceTextures = [int]$factory.sourceTextureCount
    foreach ($channel in @('A','N','S','E','M','P')) {
        $report.materials.channelCounts[$channel] = [int]$factory.channelCounts.$channel
        $report.materials.sourceChannelCounts[$channel] = [int]$factory.sourceChannelCounts.$channel
    }
    $report.materials.factoryUnresolvedTextures = @($factory.unresolvedTextures)
    foreach ($texture in @($factory.textures)) {
        $assetName = [IO.Path]::GetFileNameWithoutExtension(([string]$texture.source)) + '.uasset'
        $assetPath = Join-Path $textureFolder $assetName
        if (Test-Path -LiteralPath $assetPath -PathType Leaf) {
            $report.materials.factoryImportedTextures++
        } elseif ($report.materials.factoryMissingTextures.Count -lt 20) {
            $report.materials.factoryMissingTextures += [string]$texture.source
        }
    }
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
    $report.audio.importedAssets -eq $report.audio.expected -and $report.audio.expected -gt 0 -and
    $report.audio.fmodBanks.expected -eq 5 -and $report.audio.fmodBanks.staged -eq 5 -and $report.audio.fmodBanks.copied -eq 5 -and
    $report.audio.fmodBanks.hashMismatches.Count -eq 0 -and $report.audio.fmodBanks.runtimeDll -and
    $report.stagedHashMismatches.Count -eq 0 -and
    $report.materials.factorySourceTextures -eq $textureFiles.Count -and
    $report.materials.factoryTextures -gt 0 -and
    $report.materials.factoryImportedTextures -eq $report.materials.factoryTextures -and
    $report.materials.factoryUnresolvedTextures.Count -eq 0)
$report.status = if ($allCoveragePass -and $report.sidecars.invalid -eq 0 -and $report.materials.profiles -eq $unityMatFiles.Count) { "passed" } else { "failed" }
$json = $report | ConvertTo-Json -Depth 12
New-Item -ItemType Directory -Path (Split-Path -Parent $ReportPath) -Force | Out-Null
Set-Content -LiteralPath $ReportPath -Value $json -Encoding UTF8
Write-Host "FrontRooms Unreal asset audit $($report.status): $meshImported/$($meshFiles.Count) FBX packages, $textureImported/$($textureFiles.Count) textures, $($report.sidecars.valid)/$($sidecarFiles.Count) sidecars, $($report.materials.profiles) material profiles, $($report.audio.importedAssets)/$($report.audio.expected) audio assets"
Write-Host "Asset audit report: $ReportPath"
if ($report.logs.meshImportWarnings -gt 0) { Write-Warning "Mesh import has $($report.logs.meshImportWarnings) bounds/tangent warnings; sidecar collision/LOD metadata is the remediation source." }
if ($Strict -and $report.logs.meshImportWarnings -gt 0) { exit 2 }
if ($report.status -ne "passed") { exit 1 }
