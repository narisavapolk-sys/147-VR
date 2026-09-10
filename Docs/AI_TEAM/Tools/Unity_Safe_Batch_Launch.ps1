<#
147VR - Unity Safe Batch Launch
Fixes the recurring "Package Manager could not connect to IPC stream after 30s"
failure. Root cause (confirmed 2026-09-06): the calling shell's PATH inherits
...\AppData\Local\npm-cache\_npx\<hash>\node_modules\.bin (from `npx` launchers
like Desktop Commander's `remote` command). That entry breaks Unity's child
UnityPackageManager.exe handshake.

MANDATORY before running this: read Docs/AI_TEAM/LOCK.md. This script still
launches Unity, so the "One Unity Executor at a time" rule applies. This
script does not check or enforce the lock itself - the human/AI running it
must confirm ownership first.

Usage:
  .\Unity_Safe_Batch_Launch.ps1 -LogName "CompileCheck"
  .\Unity_Safe_Batch_Launch.ps1 -LogName "M22Follow" -ExecuteMethod "M22FollowBatchRunner.Run"
  .\Unity_Safe_Batch_Launch.ps1 -LogName "PlayModeCheck" -Quit:$false
#>
param(
  [Parameter(Mandatory=$true)][string]$LogName,
  [string]$ExecuteMethod,
  [bool]$Quit = $true,
  [int]$TimeoutSec = 300,
  [switch]$Force
)

$ErrorActionPreference = "Stop"
$ProjectPath = "C:\Users\mongo\UnityProjects\147 VR"
$UnityExe = "C:\Program Files\Unity\Hub\Editor\6000.4.4f1\Editor\Unity.exe"
$LogDir = Join-Path $ProjectPath "Docs\AI_TEAM\UnityLogs"
New-Item -ItemType Directory -Force -Path $LogDir | Out-Null

$Timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$LogFile = Join-Path $LogDir "$($LogName)_$Timestamp.log"
$SummaryFile = Join-Path $LogDir "$($LogName)_$Timestamp`_SUMMARY.txt"

# 1. Guard: refuse to run if Unity is already using this machine's editor.
#    (Does not know which project another instance has open - if -Force is
#    used, you are personally certifying no conflicting session exists.)
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
if (-not $env:USERPROFILE)   { $env:USERPROFILE = [Environment]::GetFolderPath('UserProfile') }
if (-not $env:PROGRAMDATA)   { $env:PROGRAMDATA = "C:\ProgramData" }
if (-not $env:APPDATA)       { $env:APPDATA = "$env:USERPROFILE\AppData\Roaming" }
if (-not $env:LOCALAPPDATA)  { $env:LOCALAPPDATA = "$env:USERPROFILE\AppData\Local" }
if (-not $env:TEMP)          { $env:TEMP = "$env:USERPROFILE\AppData\Local\Temp" }
if (-not $env:TMP)           { $env:TMP = $env:TEMP }
if (-not $env:NO_PROXY)      { $env:NO_PROXY = "localhost,127.0.0.1" }

# 4. Build the Unity command line.
# Start-Process receives one command-line string here so paths containing spaces are
# explicitly quoted. Passing an unquoted ArgumentList array can make Unity parse
# `147` / `VR` or `Docs` / `AI_TEAM` as separate arguments and fall back to Editor.log.
$argList = '-batchmode -nographics -automated' +
  ' -projectPath "' + $ProjectPath + '"' +
  ' -logFile "' + $LogFile + '"'
if ($ExecuteMethod) { $argList += ' -executeMethod "' + $ExecuteMethod + '"' }
if ($Quit) { $argList += ' -quit' }

Write-Output "LAUNCHING: `"$UnityExe`" $argList"
$proc = Start-Process -FilePath $UnityExe -ArgumentList $argList -PassThru -WindowStyle Hidden

# 5. Poll for completion instead of blocking forever.
$elapsed = 0
$pollInterval = 5
while (-not $proc.HasExited -and $elapsed -lt $TimeoutSec) {
  Start-Sleep -Seconds $pollInterval
  $elapsed += $pollInterval
}
$result = if ($proc.HasExited) { "EXITED code=$($proc.ExitCode)" } else { "STILL_RUNNING after $TimeoutSec s (pid=$($proc.Id))" }

# 6. Summarize the log so the caller doesn't have to grep a 200+ line file.
$ipcOk = $false; $ipcFail = $false; $compileErr = $false
if (Test-Path $LogFile) {
  $ipcOk = [bool](Select-String -Path $LogFile -Pattern "Connected to IPC stream" -Quiet -ErrorAction SilentlyContinue)
  $ipcFail = [bool](Select-String -Path $LogFile -Pattern "Could not connect to IPC stream" -Quiet -ErrorAction SilentlyContinue)
  $compileErr = [bool](Select-String -Path $LogFile -Pattern "error CS" -Quiet -ErrorAction SilentlyContinue)
}

$summary = @"
147VR Unity Safe Batch Launch - Summary
Timestamp: $Timestamp
LogName: $LogName
ExecuteMethod: $ExecuteMethod
Process result: $result
UPM IPC connected: $ipcOk
UPM IPC failed (still): $ipcFail
Compile errors (CS) found: $compileErr
LogFile: $LogFile
"@
Set-Content -Path $SummaryFile -Value $summary
Write-Output $summary
