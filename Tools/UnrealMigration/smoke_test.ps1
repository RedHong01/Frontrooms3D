[CmdletBinding()]
param(
    [string]$Root = (Resolve-Path (Join-Path $PSScriptRoot "../..")).Path,
    [string]$UnrealEditorCmd,
    [ValidateRange(10, 3600)][int]$StageTimeoutSeconds = 900
)

$ErrorActionPreference = "Stop"
$Root = (Resolve-Path -LiteralPath $Root).Path
$project = Join-Path $Root "Migration/Unreal/FrontRoomsUE.uproject"
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
    coverage = @("Unity export contract", "Unity asset SHA-256 integrity", "Unreal editor build", "Unreal contract commandlet", "deterministic hash/state transitions/imported asset load")
    pendingCoverage = @("playable generated map", "movement/sprint", "HUD/input", "rendering/audio", "Complete and Relay Search transitions")
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
        if ($Name -eq "runtime-assets" -and $output -notmatch 'FrontRooms smoke passed') { throw "Smoke success marker missing; see $logPath" }
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
    Invoke-SmokeStage "source-assets" $node @((Join-Path $Root "Tools/UnrealMigration/test_asset_bridge.mjs"), $Root)
    $engine = & (Join-Path $PSScriptRoot "resolve_unreal_engine.ps1") -Root $Root -UnrealEditorCmd $UnrealEditorCmd
    $report.engineVersion = $engine.Version
    $report.projectAssociation = $engine.Association
    Write-Host "SMOKE using Unreal $($engine.Version) ($($engine.EditorCmd))"
    # Use a fresh worker PowerShell for the .bat invocation; no cmd string interpolation.
    $worker = Join-Path $runDirectory "build.ps1"
    @'
param([string]$BuildScript, [string]$Project)
& $BuildScript FrontRoomsEditor Win64 Development $Project -NoHotReloadFromIDE
exit $LASTEXITCODE
'@ | Set-Content -LiteralPath $worker -Encoding UTF8
    $windowsPowerShell = (Get-Command powershell.exe -CommandType Application -ErrorAction Stop).Source
    Invoke-SmokeStage "build" $windowsPowerShell @("-NoProfile", "-NonInteractive", "-File", $worker, "-BuildScript", $engine.BuildScript, "-Project", $project)
    $commonArgs = @($project, "-unattended", "-nop4", "-nosplash", "-nullrhi", "-stdout", "-UTF8Output")
    Invoke-SmokeStage "contract" $engine.EditorCmd ($commonArgs + @("-run=FrontRoomsContract", "-abslog=$(Join-Path $runDirectory 'contract-engine.log')"))
    Invoke-SmokeStage "runtime-assets" $engine.EditorCmd ($commonArgs + @("-run=FrontRoomsSmoke", "-abslog=$(Join-Path $runDirectory 'smoke-engine.log')"))
    $report.status = "passed"
} catch {
    $report.status = "failed"
    $report.error = $_.Exception.Message
    Write-Host "SMOKE FAILED: $($report.error)"
} finally {
    $report.finishedUtc = [DateTime]::UtcNow.ToString("o")
    Save-SmokeReport
    Write-Host "SMOKE report: $(Join-Path $runDirectory 'report.json')"
}
if ($report.status -ne "passed") { exit 1 }
Write-Host "FrontRooms migration smoke passed (Unreal $($report.engineVersion))."
