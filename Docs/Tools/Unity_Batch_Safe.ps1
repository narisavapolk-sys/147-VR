# 147VR Unity Batch Safe Launcher
# Safe Unity 6 batch entrypoint.
# -NoQuit is an explicit lifecycle-owner escape hatch for reviewed runners.
# It only suppresses the launcher-added -quit argument.
# Test runs intentionally omit -quit so
# Unity Test Framework controls the PlayMode lifecycle and result shutdown.
param(
  [string]$ProjectPath = "C:\Users\mongo\UnityProjects\147 VR",
  [string]$UnityExe = "C:\Program Files\Unity\Hub\Editor\6000.4.4f1\Editor\Unity.exe",
  [string]$ExecuteMethod = "",
  [string]$ExtraArgs = "",
  [int]$TimeoutSeconds = 600,
  [switch]$NoQuit
)
$ErrorActionPreference = "Stop"
$unityProcs = Get-Process -Name "Unity","UnityPackageManager" -ErrorAction SilentlyContinue
if ($unityProcs) {
  Write-Host "[BLOCKED] Unity or UnityPackageManager already running (PID: $($unityProcs.Id -join ',')). Close it before running this script." -ForegroundColor Red
  exit 2
}
$env:PROGRAMDATA = "C:\ProgramData"
$env:ALLUSERSPROFILE = "C:\ProgramData"
$env:APPDATA = "C:\Users\mongo\AppData\Roaming"
$env:LOCALAPPDATA = "C:\Users\mongo\AppData\Local"
$env:USERPROFILE = "C:\Users\mongo"
$env:TEMP = "C:\Users\mongo\AppData\Local\Temp"
$env:TMP = "C:\Users\mongo\AppData\Local\Temp"
$env:NO_PROXY = "localhost,127.0.0.1"
$env:UNITY_UPM_TIMEOUT = "120"
$env:PATH = (($env:PATH -split ';') | Where-Object { $_ -notmatch '_npx' -and $_ -notmatch 'npm-cache' }) -join ';'
$stamp = Get-Date -Format "yyyyMMdd_HHmmss"
$logDir = Join-Path $ProjectPath "Docs\UnityBatchLogs"
New-Item -ItemType Directory -Force -Path $logDir | Out-Null
$logFile = Join-Path $logDir "UnityBatch_$stamp.log"
$argList = @(
  "-batchmode","-nographics",
  "-projectPath", "`"$ProjectPath`"",
  "-logFile", "`"$logFile`""
)
if ($ExecuteMethod -ne "") { $argList += @("-executeMethod", $ExecuteMethod) }
if ($ExtraArgs -ne "") { $argList += ($ExtraArgs -split ' ') }
$isTestRun = $ExtraArgs -match '(^|\s)-runTests(\s|$)'
if (-not $isTestRun -and -not $NoQuit) { $argList += "-quit" }
Write-Host "[INFO] Launching Unity with repaired environment..."
Write-Host "[INFO] Log: $logFile"
if ($isTestRun) { Write-Host "[INFO] Unity Test Framework run: lifecycle-owned shutdown (no forced -quit)." }
if ($NoQuit) { Write-Host "[INFO] -NoQuit set: Unity owns its own shutdown (no forced -quit)." }
$proc = Start-Process -FilePath $UnityExe -ArgumentList $argList -PassThru -NoNewWindow
$finished = $proc.WaitForExit($TimeoutSeconds * 1000)
if (-not $finished) {
  Write-Host "[TIMEOUT] Unity did not exit within $TimeoutSeconds s. Left running (PID $($proc.Id)) for manual inspection." -ForegroundColor Yellow
  exit 3
}
$exitCode = $proc.ExitCode
Write-Host "[INFO] Unity exit code: $exitCode"
$logText = Get-Content $logFile -Raw -ErrorAction SilentlyContinue
if ($null -eq $logText) { $logText = "" }
if ($logText -match "Could not connect to IPC stream" -or $logText -match "Received undefined") {
  Write-Host "[BLOCKED-TOOLING] UPM IPC/environment blocker signature found in log." -ForegroundColor Red
  exit 4
}
elseif ($logText -match "error CS\d" -or $logText -match "Compilation failed") {
  Write-Host "[FAIL-COMPILE] Real compiler errors found in log. Inspect $logFile for 'error CS' lines." -ForegroundColor Red
  exit 5
}
elseif ($exitCode -eq 0) {
  Write-Host "[PASS] Unity batch completed cleanly. No known UPM blocker or compile error signature found." -ForegroundColor Green
  exit 0
}
else {
  Write-Host "[UNKNOWN] Unity exited with code $exitCode and no recognized failure signature. Inspect $logFile manually." -ForegroundColor Yellow
  exit 1
}
