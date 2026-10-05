[CmdletBinding()]
param(
    [string]$Root = (Resolve-Path (Join-Path $PSScriptRoot "../..")).Path,
    [string]$UnrealEditorCmd,
    [ValidateRange(10, 3600)][int]$StageTimeoutSeconds = 900
)

$ErrorActionPreference = "Stop"
$Root = (Resolve-Path -LiteralPath $Root).Path
$project = Join-Path $Root "Migration/Unreal/FrontRoomsss.uproject"
$savedRoot = Join-Path $Root "Migration/Unreal/Saved/MigrationSmoke"
$runId = [DateTime]::UtcNow.ToString("yyyyMMddTHHmmssfffZ")
$runDirectory = Join-Path $savedRoot $runId
New-Item -ItemType Directory -Path $runDirectory -Force | Out-Null
$report = [ordered]@{
    schema = "frontrooms.unreal.smoke"
    schemaVersion = 1
    startedUtc = [DateTime]::UtcNow.ToString("o")
    finishedUtc = $null
    status = "running"
    engineVersion = $null
    projectAssociation = $null
    stages = @()
    coverage = @("Unity export contract", "Unity deterministic golden chunk exports", "Unity asset SHA-256 integrity", "Windows A/N/S/E/M/P material factory and asset audit", "Win64 DX12/SM6 Nanite + Lumen hardware RT + VSM project configuration", "Unreal editor build", "Unreal contract commandlet", "Unity sidecar collision/anchor/scale/axis/LOD import", "sidecar asset factory applies collision/LOD/anchor metadata", "deterministic hash/state transitions/movement input/imported asset load", "PIE/standalone PlayerStart and pawn spawn", "live possessed movement trace", "Relay Search/Complete transitions and key/door/window interactions", "saved playable runtime map", "HDR calibration", "native HUD and audio event seam", "Win64 FMOD bank contract and rendered event playback")
    pendingCoverage = @("FMOD bank regeneration for three missing StreamOpen/StreamClose/StreamLock events")
    error = $null
}

function Save-SmokeReport {
    $json = $report | ConvertTo-Json -Depth 10
    Set-Content -LiteralPath (Join-Path $runDirectory "report.json") -Value $json -Encoding UTF8
    Set-Content -LiteralPath (Join-Path $savedRoot "latest.json") -Value $json -Encoding UTF8
}

function ConvertTo-NativeArgument([string]$Value) {
    # CommandLineToArgvW quoting, including backslashes immediately before a quote.
    return '"' + [regex]::Replace([regex]::Replace($Value, '(\\*)"', '$1$1\"'), '(\\+)$', '$1$1') + '"'
}

function Invoke-SmokeStage([string]$Name, [string]$Executable, [string[]]$Arguments) {
    $logPath = Join-Path $runDirectory "$Name.log"
    $stage = [ordered]@{ name = $Name; status = "running"; durationSeconds = 0; exitCode = $null; log = $logPath }
    $report.stages += $stage
    Save-SmokeReport
    Write-Host "SMOKE $Name ..."
    $timer = [Diagnostics.Stopwatch]::StartNew()
    $process = New-Object Diagnostics.Process
    $process.StartInfo = New-Object Diagnostics.ProcessStartInfo
    $process.StartInfo.FileName = $Executable
    $process.StartInfo.Arguments = (($Arguments | ForEach-Object { ConvertTo-NativeArgument $_ }) -join ' ')
    $process.StartInfo.WorkingDirectory = $Root
    $process.StartInfo.UseShellExecute = $false
    $process.StartInfo.CreateNoWindow = $true
    $process.StartInfo.RedirectStandardOutput = $true
    $process.StartInfo.RedirectStandardError = $true
    try {
        if (-not $process.Start()) { throw "Could not start $Name" }
        $stdout = $process.StandardOutput.ReadToEndAsync()
        $stderr = $process.StandardError.ReadToEndAsync()
        if (-not $process.WaitForExit($StageTimeoutSeconds * 1000)) {
            # Terminate only the process tree launched for this stage.
            & "$env:SystemRoot\System32\taskkill.exe" /PID $process.Id /T /F | Out-Null
            $process.WaitForExit()
            Set-Content -LiteralPath $logPath -Value ($stdout.Result + $stderr.Result) -Encoding UTF8
            throw "$Name exceeded $StageTimeoutSeconds seconds"
        }
        $output = $stdout.Result + $stderr.Result
        Set-Content -LiteralPath $logPath -Value $output -Encoding UTF8
        $stage.exitCode = $process.ExitCode
        if ($process.ExitCode -ne 0) {
            Write-Host (($output -split "`r?`n" | Select-Object -Last 25) -join "`n")
            throw "$Name failed with exit code $($process.ExitCode); see $logPath"
        }
        if ($Name -eq "contract" -and $output -notmatch 'FrontRooms contract passed:') { throw "Contract success marker missing; see $logPath" }
        if ($Name -eq "sidecars" -and $output -notmatch 'FrontRooms sidecars passed: 113 Unity prop contracts imported and validated') { throw "Sidecar success marker missing; see $logPath" }
        if ($Name -eq "sidecars" -and $output -notmatch 'FrontRooms sidecar asset factory passed: 113/113 assets, 55 box colliders, 59 LOD values, 490 anchors') { throw "Sidecar asset factory success marker missing; see $logPath" }
        if ($Name -eq "runtime-assets" -and $output -notmatch 'FrontRooms smoke live movement trace passed: possessed=true') { throw "Live possessed movement success marker missing; see $logPath" }
        if ($Name -eq "runtime-assets" -and $output -notmatch 'FrontRooms FMOD Win64 runtime ready: 5 banks loaded') { throw "Native FMOD runtime success marker missing; see $logPath" }
        if ($Name -eq "runtime-assets" -and $output -notmatch 'FrontRooms FMOD playback: event:/Foley/Player/KeyPickup') { throw "Native FMOD playback marker missing; see $logPath" }
        if ($Name -eq "material-assets" -and $output -notmatch 'FrontRooms material factory passed: 167/167 textures, changed=\d+, errors=0') { throw "Material asset factory success marker missing; see $logPath" }
        if ($Name -eq "runtime-assets" -and $output -notmatch 'FrontRooms smoke passed') { throw "Smoke success marker missing; see $logPath" }
        if ($Name -eq "fmod-banks" -and $output -notmatch 'FrontRooms FMOD bank probe passed: 5 banks loaded, 28 events resolved, rendered playback') { throw "FMOD bank probe success marker missing; see $logPath" }
        if ($Name -eq "fmod-banks" -and $output -notmatch 'event:/Mechanism/Door/StreamOpen.*event:/Mechanism/Door/StreamClose.*event:/Mechanism/Door/StreamLock') { throw "FMOD source/bank drift marker missing; see $logPath" }
        $stage.status = "passed"
        Write-Host "SMOKE $Name passed"
    } catch {
        $stage.status = "failed"
        throw
    } finally {
        $timer.Stop()
        $stage.durationSeconds = [Math]::Round($timer.Elapsed.TotalSeconds, 2)
        $process.Dispose()
        Save-SmokeReport
    }
}

try {
    $node = (Get-Command node -ErrorAction Stop).Source
    Invoke-SmokeStage "exports" $node @((Join-Path $Root "Tools/UnrealMigration/validate_exports.mjs"), $Root)
    Invoke-SmokeStage "nanite-config" $node @((Join-Path $Root "Tools/UnrealMigration/validate_nanite_config.mjs"), $Root)
    Invoke-SmokeStage "source-assets" $node @((Join-Path $Root "Tools/UnrealMigration/test_asset_bridge.mjs"), $Root)
    $windowsPowerShell = (Get-Command powershell.exe -CommandType Application -ErrorAction Stop).Source
    Invoke-SmokeStage "asset-audit" $windowsPowerShell @("-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass", "-File", (Join-Path $Root "Tools/UnrealMigration/audit_unreal_assets.ps1"), "-Root", $Root)
    $engine = & (Join-Path $PSScriptRoot "resolve_unreal_engine.ps1") -Root $Root -UnrealEditorCmd $UnrealEditorCmd
    $report.engineVersion = $engine.Version
    $report.projectAssociation = $engine.Association
    Write-Host "SMOKE using Unreal $($engine.Version) ($($engine.EditorCmd))"
    # Editor-Cmd.exe lives in Engine/Binaries/Win64; resolve the UE root before
    # locating the bundled Python runtime used by the FMOD bank probe.
    $engineRoot = Split-Path (Split-Path (Split-Path $engine.EditorCmd -Parent) -Parent) -Parent
    $fmodPython = Join-Path $engineRoot "Binaries/ThirdParty/Python3/Win64/python.exe"
    Invoke-SmokeStage "golden-chunks" $fmodPython @((Join-Path $Root "Tools/UnrealMigration/validate_golden_chunks.py"), "--root", $Root, "--report", (Join-Path $runDirectory "unity-golden-chunk-validation.json"))
    $fmodRender = Join-Path $runDirectory "fmod-footstep.wav"
    Invoke-SmokeStage "fmod-banks" $fmodPython @((Join-Path $Root "Tools/audio/fmod_bank_probe.py"), "--root", $Root, "--write-manifest", "--render", $fmodRender, "--allow-missing-event", "event:/Mechanism/Door/StreamOpen", "--allow-missing-event", "event:/Mechanism/Door/StreamClose", "--allow-missing-event", "event:/Mechanism/Door/StreamLock")
    # Keep UnrealBuildTool's generated environment cache inside the run directory.
    # Shared machine caches can be owned by another Windows install context and
    # are disposable; isolating this cache keeps the smoke gate reproducible.
    # Keep the profile root short because UE's DerivedDataCache appends several
    # nested segments and rejects paths longer than its cache-key budget.
    $smokeUserProfile = Join-Path $Root ".smoke-profile"
    $smokeLocalAppData = Join-Path $smokeUserProfile "AppData/Local"
    $smokeRoamingAppData = Join-Path $smokeUserProfile "AppData/Roaming"
    # UE5.8 keeps the UBA executor available even with -NoUBA (the flag
    # disables detouring but does not remove the executor). Point its storage
    # at the workspace so the Windows smoke gate never touches C:\ProgramData.
    $smokeUbaRoot = Join-Path $Root ".smoke-profile/UBA"
    New-Item -ItemType Directory -Path $smokeUbaRoot -Force | Out-Null
    New-Item -ItemType Directory -Path $smokeLocalAppData, $smokeRoamingAppData -Force | Out-Null
    $previousUserProfile = $env:USERPROFILE
    $previousLocalAppData = $env:LOCALAPPDATA
    $previousAppData = $env:APPDATA
    $previousUbaRoot = $env:UBA_ROOT
    $env:USERPROFILE = $smokeUserProfile
    $env:LOCALAPPDATA = $smokeLocalAppData
    $env:APPDATA = $smokeRoamingAppData
    $env:UBA_ROOT = $smokeUbaRoot
    # Use a fresh worker PowerShell for the .bat invocation; no cmd string interpolation.
    $worker = Join-Path $runDirectory "build.ps1"
    @'
param([string]$BuildScript, [string]$Project)
# The sandbox cannot write C:\ProgramData\Epic\UnrealBuildAccelerator.
# Keep this smoke gate local and deterministic; normal developer builds may
# opt into UBA outside the gate.
& $BuildScript FrontRoomsEditor Win64 Development $Project -NoHotReloadFromIDE -NoUBA
exit $LASTEXITCODE
'@ | Set-Content -LiteralPath $worker -Encoding UTF8
    Invoke-SmokeStage "build" $windowsPowerShell @("-NoProfile", "-NonInteractive", "-File", $worker, "-BuildScript", $engine.BuildScript, "-Project", $project)
    $commonArgs = @($project, "-unattended", "-nop4", "-nosplash", "-nullrhi", "-NoZenStore", "-stdout", "-UTF8Output")
    Invoke-SmokeStage "contract" $engine.EditorCmd ($commonArgs + @("-run=FrontRoomsContract", "-abslog=$(Join-Path $runDirectory 'contract-engine.log')"))
    $materialManifest = Join-Path $Root "Migration/exports/unreal_material_factory.json"
    $materialReport = Join-Path $runDirectory "material-assets-report.json"
    Invoke-SmokeStage "material-assets" $engine.EditorCmd ($commonArgs + @("-run=FrontRoomsMaterialFactory", "-Manifest=$materialManifest", "-Report=$materialReport", "-abslog=$(Join-Path $runDirectory 'material-assets-engine.log')"))
    $sidecarReport = Join-Path $runDirectory "sidecar-report.json"
    $sidecarDirectory = Join-Path $Root "Assets/Resources/Props/Models"
    Invoke-SmokeStage "sidecars" $engine.EditorCmd ($commonArgs + @("-run=FrontRoomsSidecar", "-ApplyToAssets", "-Sidecars=$sidecarDirectory", "-Report=$sidecarReport", "-abslog=$(Join-Path $runDirectory 'sidecar-engine.log')"))
    Invoke-SmokeStage "runtime-assets" $engine.EditorCmd ($commonArgs + @("-run=FrontRoomsSmoke", "-abslog=$(Join-Path $runDirectory 'smoke-engine.log')"))
    $report.status = "passed"
} catch {
    $report.status = "failed"
    $report.error = $_.Exception.Message
    Write-Host "SMOKE FAILED: $($report.error)"
} finally {
    if ($null -ne $previousUserProfile) { $env:USERPROFILE = $previousUserProfile }
    if ($null -ne $previousLocalAppData) { $env:LOCALAPPDATA = $previousLocalAppData }
    if ($null -ne $previousAppData) { $env:APPDATA = $previousAppData }
    if ($null -ne $previousUbaRoot) { $env:UBA_ROOT = $previousUbaRoot } else { Remove-Item Env:UBA_ROOT -ErrorAction SilentlyContinue }
    $report.finishedUtc = [DateTime]::UtcNow.ToString("o")
    Save-SmokeReport
    Write-Host "SMOKE report: $(Join-Path $runDirectory 'report.json')"
}
if ($report.status -ne "passed") { exit 1 }
Write-Host "FrontRooms migration smoke passed (Unreal $($report.engineVersion))."

