$ErrorActionPreference = 'Stop'
$UnityPath = 'C:\Program Files\Unity\Hub\Editor\6000.4.4f1\Editor\Unity.exe'
$ProjectPath = 'C:\Users\mongo\UnityProjects\147 VR'
$LogDir = 'C:\Users\mongo\AppData\Local\Temp'
$CompileLog = Join-Path $LogDir '147VR_M74_COMPILE_STEP.log'
$env:PROGRAMDATA = 'C:\ProgramData'
$env:APPDATA = 'C:\Users\mongo\AppData\Roaming'
$env:LOCALAPPDATA = 'C:\Users\mongo\AppData\Local'
$env:USERPROFILE = 'C:\Users\mongo'
$env:TEMP = 'C:\Users\mongo\AppData\Local\Temp'
$env:TMP = 'C:\Users\mongo\AppData\Local\Temp'
$env:NO_PROXY = 'localhost,127.0.0.1'
$env:UNITY_UPM_TIMEOUT = '120'
function Wait-UnityExit {
    $deadline = (Get-Date).AddMinutes(3)
    while ((Get-Process Unity -ErrorAction SilentlyContinue) -and (Get-Date) -lt $deadline) { Start-Sleep -Seconds 3 }
    $unity = Get-Process Unity -ErrorAction SilentlyContinue
    if ($unity) { $unity | Stop-Process -Force -ErrorAction SilentlyContinue; Start-Sleep -Seconds 3 }
}
function Clear-Locks {
    Remove-Item (Join-Path $ProjectPath 'Library\ArtifactDB-lock') -Force -ErrorAction SilentlyContinue
    Remove-Item (Join-Path $ProjectPath 'Library\SourceAssetDB-lock') -Force -ErrorAction SilentlyContinue
    Remove-Item (Join-Path $ProjectPath 'Library\ilpp.pid') -Force -ErrorAction SilentlyContinue
}
Write-Host '=== M7.4 TWO-STEP HEADLESS ==='
Write-Host '[A] Unity import/compile'
Clear-Locks
if (Test-Path $CompileLog) { Remove-Item $CompileLog -Force -ErrorAction SilentlyContinue }
& $UnityPath -batchmode -nographics -silent-crashes -projectPath $ProjectPath -logFile $CompileLog -quit
$compileCode = $LASTEXITCODE
Wait-UnityExit
$compileText = if (Test-Path $CompileLog) { Get-Content $CompileLog -Raw } else { '' }
if ($compileText -match 'error CS\d+|Compilation failed|Unhandled Exception|Fatal Error') { throw 'Compile/import log contains a fatal compilation error.' }
Write-Host "[A PASS] Import/compile completed. Unity exit code=$compileCode; no fatal compilation error detected."
Clear-Locks
Start-Sleep -Seconds 5
$attempt = 0
while ($true) {
    $attempt++
    $AuditLog = Join-Path $LogDir ("147VR_M74_AUDIT_STEP_{0:yyyyMMdd_HHmmss}.log" -f (Get-Date))
    Write-Host "[B] Execute M7.4 One-Shot attempt $attempt"
    & $UnityPath -batchmode -nographics -silent-crashes -projectPath $ProjectPath -executeMethod M7_4_OneShotFixAndAudit.Run -logFile $AuditLog
    $auditCode = $LASTEXITCODE
    Wait-UnityExit
    if (Test-Path $AuditLog) {
        $text = Get-Content $AuditLog -Raw
        if ($text -match '\[M7\.4 ONE-SHOT PASS\]') { Write-Host '>>> [M7.4 TWO-STEP PASS] <<<'; exit 0 }
        Write-Host "[B] One-Shot exit=$auditCode; retrying."
    } else { Write-Host '[B] Audit log missing; retrying.' }
    Clear-Locks
    Start-Sleep -Seconds 5
}
