[CmdletBinding()]
param(
    [string]$Root = (Resolve-Path (Join-Path $PSScriptRoot "../..")).Path,
    [string]$UnrealEditorCmd
)

$ErrorActionPreference = "Stop"
$project = Join-Path $Root "Migration/Unreal/FrontRoomsUE.uproject"
$association = (Get-Content -Raw -LiteralPath $project | ConvertFrom-Json).EngineAssociation
$candidates = @()
if ($UnrealEditorCmd) {
    $candidates = @($UnrealEditorCmd)
} else {
    $launcherFile = Join-Path $env:ProgramData "Epic/UnrealEngineLauncher/LauncherInstalled.dat"
    if (Test-Path -LiteralPath $launcherFile) {
        $installs = (Get-Content -Raw -LiteralPath $launcherFile | ConvertFrom-Json).InstallationList
        foreach ($install in $installs) {
            if ($install.AppName -eq "UE_$association") {
                $candidates += Join-Path $install.InstallLocation "Engine/Binaries/Win64/UnrealEditor-Cmd.exe"
            }
        }
    }
    $candidates += "D:\UE_$association\Engine\Binaries\Win64\UnrealEditor-Cmd.exe"
    $candidates += Join-Path $env:ProgramFiles "Epic Games/UE_$association/Engine/Binaries/Win64/UnrealEditor-Cmd.exe"
}

$editor = $candidates | Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } | Select-Object -First 1
if (-not $editor) { throw "No installed Unreal editor for project association '$association'. Install that engine or pass -UnrealEditorCmd." }
$editor = (Resolve-Path -LiteralPath $editor).Path
$engineRoot = Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $editor))
$version = Get-Content -Raw -LiteralPath (Join-Path $engineRoot "Build/Build.version") | ConvertFrom-Json
$series = "$($version.MajorVersion).$($version.MinorVersion)"
if ($association -ne $series) {
    throw "Engine mismatch: project associates with $association, but editor is $series. Update the project association deliberately before running smoke/import."
}
$build = Join-Path $engineRoot "Build/BatchFiles/Build.bat"
if (-not (Test-Path -LiteralPath $build -PathType Leaf)) { throw "Missing Unreal build script: $build" }
[pscustomobject]@{
    EditorCmd = $editor
    BuildScript = $build
    Version = "$series.$($version.PatchVersion)"
    Association = $association
}
