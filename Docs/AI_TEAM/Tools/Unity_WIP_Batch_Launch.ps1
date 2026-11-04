<#
147VR-import-wip — Unity Safe Batch Launch (project-specific clone)
Same contract as Unity_Safe_Batch_Launch.ps1, but this wip project lives at
C:\Users\mongo\UnityProjects\147VR-import-wip (the original script hardcodes
"C:\Users\mongo\UnityProjects\147 VR" and must NOT be used for this project).

Fixes inherited from the original: strips npm-cache/_npx PATH entries that
break UnityPackageManager IPC, sets UNITY_UPM_TIMEOUT=120, guards against a
second Unity instance, always logs to an explicit absolute log file.

MANDATORY before running: record ownership in Docs/AI_TEAM/LOCK.md.
One Unity Executor at a time.

Usage:
  .\Unity_WIP_Batch_Launch.ps1 -LogName "Round11_Prep"
  .\Unity_WIP_Batch_Launch.ps1 -LogName "Uv2Ensure" -ExecuteMethod "VR147.EditorTools.ToolRunner.RunCommands" -TimeoutSec 1800
  .\Unity_WIP_Batch_Launch.ps1 -LogName "PlayModePhaseTests" -RunTests -TestFilter "PoolPhysicsPhaseTests" -TimeoutSec 3600
#>
param(
  [Parameter(Mandatory=$true)][string]$LogName,
  [string]$ExecuteMethod,
  [bool]$Quit = $true,
  [int]$TimeoutSec = 900,
  [switch]$Force,
  # Keep graphics context alive (batchmode WITHOUT -nographics). Some native
  # import/bake code paths behave differently with a GPU context present.
  [switch]$Graphics,
  # PlayMode test batch: Unity exits by itself after -runTests (ExitCode = failed test count)
  [switch]$RunTests,
  [string]$TestFilter,
  [string]$TestResults
)

$ErrorActionPreference = "Stop"
$ProjectPath = "C:\Users\mongo\UnityProjects\147VR-import-wip"
$UnityExe = "C:\Program Files\Unity\Hub\Editor\6000.4.4f1\Editor\Unity.exe"
$LogDir = Join-Path $ProjectPath "Docs\AI_TEAM\UnityLogs"
New-Item -ItemType Directory -Force -Path $LogDir | Out-Null

$Timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$LogFile = Join-Path $LogDir "$($LogName)_$Timestamp.log"
$SummaryFile = Join-Path $LogDir "$($LogName)_$Timestamp`_SUMMARY.txt"

# 0. Cleanup previous command queue state so the new batch starts empty-handed.
#    (ToolRunner archives editor_cmd.txt on load; archive left behind is fine.)
$cmdFile = Join-Path $ProjectPath "Logs\editor_cmd.txt"

# 1. Guard: refuse to run if Unity is already using this machine's editor.
if (-not $Force) {
  $existing = Get-Process -Name "Unity" -ErrorAction SilentlyContinue
  if ($existing) {
    Write-Output "ABORT: Unity.exe already running (PID: $($existing.Id -join ',')). Close it, or confirm it is unrelated and rerun with -Force."
    exit 2
  }
}

# 2. Clean PATH contamination that breaks UPM child-process IPC.
$cleanEntries = $env:PATH -split ';' | Where-Object {
  $_ -notmatch 'npm-cache' -and $_ -notmatch '\\_npx\\'
}
$env:PATH = ($cleanEntries -join ';')

# 3. Ensure the environment UPM needs is present and sane.
$env:UNITY_UPM_TIMEOUT = "120"
if (-not $env:USERPROFILE)  { $env:USERPROFILE = [Environment]::GetFolderPath('UserProfile') }
if (-not $env:PROGRAMDATA)  { $env:PROGRAMDATA = "C:\ProgramData" }
if (-not $env:APPDATA)      { $env:APPDATA = "$env:USERPROFILE\AppData\Roaming" }
if (-not $env:LOCALAPPDATA) { $env:LOCALAPPDATA = "$env:USERPROFILE\AppData\Local" }
if (-not $env:TEMP)         { $env:TEMP = "$env:USERPROFILE\AppData\Local\Temp" }
if (-not $env:TMP)          { $env:TMP = $env:TEMP }
if (-not $env:NO_PROXY)     { $env:NO_PROXY = "localhost,127.0.0.1" }

# 4. Build the Unity command line as ONE string so quoted paths never split.
if ($RunTests) {
  # Test batches: keep graphics (some PlayMode tests touch rendering), batchmode only.
  $argList = '-batchmode' +
    ' -projectPath "' + $ProjectPath + '"' +
    ' -logFile "' + $LogFile + '"' +
    ' -runTests -testPlatform PlayMode'
  if ($TestFilter)   { $argList += ' -testFilter "' + $TestFilter + '"' }
  if (-not $TestResults) { $TestResults = Join-Path $LogDir "$($LogName)_$Timestamp`_results.xml" }
  $argList += ' -testResults "' + $TestResults + '"'
} else {
  $argList = '-batchmode' +
    $(if (-not $Graphics) { ' -nographics -automated' } else { ' -automated' }) +
    ' -projectPath "' + $ProjectPath + '"' +
    ' -logFile "' + $LogFile + '"'
  if ($ExecuteMethod) { $argList += ' -executeMethod "' + $ExecuteMethod + '"' }
  if ($Quit) { $argList += ' -quit' }
}

Write-Output "LAUNCHING: `"$UnityExe`" $argList"
Write-Output "LOG: $LogFile"
if ($RunTests) { Write-Output "RESULTS: $TestResults" }
$proc = Start-Process -FilePath $UnityExe -ArgumentList $argList -PassThru -WindowStyle Hidden

# 5. Poll for completion instead of blocking forever.
$elapsed = 0
$pollInterval = 5
while (-not $proc.HasExited -and $elapsed -lt $TimeoutSec) {
  Start-Sleep -Seconds $pollInterval
  $elapsed += $pollInterval
}
$result = if ($proc.HasExited) { "EXITED code=$($proc.ExitCode)" } else { "STILL_RUNNING after $TimeoutSec s (pid=$($proc.Id))" }

# 6. Summarize the log so the caller doesn't have to grep a big file.
$ipcOk = $false; $ipcFail = $false; $compileErr = $false
if (Test-Path $LogFile) {
  $ipcOk = [bool](Select-String -Path $LogFile -Pattern "Connected to IPC stream" -Quiet -ErrorAction SilentlyContinue)
  $ipcFail = [bool](Select-String -Path $LogFile -Pattern "Could not connect to IPC stream" -Quiet -ErrorAction SilentlyContinue)
  $compileErr = [bool](Select-String -Path $LogFile -Pattern "error CS" -Quiet -ErrorAction SilentlyContinue)
}

$summary = @"
147VR WIP Unity Batch — Summary
Timestamp: $Timestamp
LogName:   $LogName
Mode:      $(if ($RunTests) { 'runTests PlayMode' } else { 'executeMethod' })
Executed:  $(if ($RunTests) { "-testFilter '$TestFilter'" } else { $ExecuteMethod })
Process:   $result
UPM IPC connected: $ipcOk
UPM IPC failed (still): $ipcFail
Compile errors (CS) found: $compileErr
LogFile:   $LogFile
TestResults: $TestResults
"@
Set-Content -Path $SummaryFile -Value $summary
Write-Output $summary
