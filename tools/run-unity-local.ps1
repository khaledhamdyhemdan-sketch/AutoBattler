# Requires Windows PowerShell 5.1+ or PowerShell 7+ and Unity 6000.3.0f1.
# Usage from the repository root:
#   powershell -ExecutionPolicy Bypass -File tools/run-unity-local.ps1
#   powershell -ExecutionPolicy Bypass -File tools/run-unity-local.ps1 -BuildApk
#
# Never stores signing credentials and never publishes a release automatically.
[CmdletBinding()]
param(
    [string]$UnityEditor = '',
    [switch]$BuildApk
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$project = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$versionFile = Join-Path $project 'ProjectSettings/ProjectVersion.txt'
$scene = Join-Path $project 'Assets/Chronicles/Chronicles_Main.unity'
if (-not (Test-Path $versionFile) -or -not (Test-Path $scene)) {
    throw "This directory is not a complete Chronicles Unity project."
}
$version = (Select-String -Path $versionFile -Pattern '^m_EditorVersion: (.+)$').Matches.Groups[1].Value.Trim()
if ($version -ne '6000.3.0f1') {
    throw "Expected Unity 6000.3.0f1; project requests $version. Do not silently upgrade the project."
}
if (-not $UnityEditor) { $UnityEditor = $env:UNITY_EDITOR_EXE }
if (-not $UnityEditor) {
    $candidates = @(
        "C:\Program Files\Unity\Hub\Editor\$version\Editor\Unity.exe",
        "C:\Program Files\Unity Hub\Editor\$version\Editor\Unity.exe"
    )
    foreach ($candidate in $candidates) {
        if (Test-Path $candidate) {
            $UnityEditor = $candidate
            break
        }
    }
}
if (-not $UnityEditor -or -not (Test-Path $UnityEditor)) {
    throw "Unity $version not found. Install that version with Android Build Support in Unity Hub or set UNITY_EDITOR_EXE."
}
$reportDirectory = Join-Path $project 'Builds/Chronicles/Validation'
New-Item -ItemType Directory -Path $reportDirectory -Force | Out-Null

function Invoke-UnityStep {
    param([string]$Title, [string]$Method, [string]$LogName)
    $log = Join-Path $reportDirectory $LogName
    Write-Host "Running $Title..." -ForegroundColor Cyan
    $args = @(
        '-batchmode', '-nographics', '-quit',
        '-projectPath', $project,
        '-executeMethod', $Method,
        '-logFile', $log
    )
    & $UnityEditor @args
    $result = $LASTEXITCODE
    if ($result -ne 0) {
        Write-Host "FAILED: $Title (exit $result). Check $log" -ForegroundColor Red
        if (Test-Path $log) {
            Get-Content -Path $log -Tail 35 | Write-Host
        }
        throw "Unity could not complete $Title. APK build will not proceed."
    }
    if (Select-String -Path $log -Pattern 'Offline suite FAILED|error CS[0-9]+|Compilation failed|Aborting batchmode due to failure' -Quiet) {
        throw "Unity reported errors in $Title; inspect $log. APK build will not proceed."
    }
    Write-Host "Completed $Title. Log: $log" -ForegroundColor Green
}

Invoke-UnityStep -Title 'catalog, progression, economy and English checks' `
    -Method 'MythicChronicles.Editor.ChroniclesAllChecks.Run' `
    -LogName 'chronicles-editor-tests.log'

if ($BuildApk) {
    Invoke-UnityStep -Title 'Android development APK build' `
        -Method 'MythicChronicles.Editor.ChroniclesAndroidBuild.BuildApk' `
        -LogName 'chronicles-android-build.log'
    $apk = Join-Path $project 'Builds/Chronicles/Chronicles_Android.apk'
    if (-not (Test-Path $apk)) {
        throw "Unity returned success but the APK was not found at $apk."
    }
    Write-Host "APK exists at: $apk" -ForegroundColor Green
    Write-Warning "An APK file alone is not a tested release. Install it on a device and test gameplay/performance."
}
Write-Host 'Unity checks completed. No server deployment or store publishing performed.'
