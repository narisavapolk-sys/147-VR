$ErrorActionPreference='Stop'
$env:PROGRAMDATA='C:\ProgramData'
$env:APPDATA='C:\Users\mongo\AppData\Roaming'
$env:LOCALAPPDATA='C:\Users\mongo\AppData\Local'
$env:USERPROFILE='C:\Users\mongo'
$env:TEMP='C:\Users\mongo\AppData\Local\Temp'
$env:TMP='C:\Users\mongo\AppData\Local\Temp'
$env:NO_PROXY='localhost,127.0.0.1'
$env:UNITY_UPM_TIMEOUT='120'
$unity='C:\Program Files\Unity\Hub\Editor\6000.4.4f1\Editor\Unity.exe'
$proj='C:\Users\mongo\UnityProjects\147 VR'
$log='C:\Users\mongo\AppData\Local\Temp\147VR_PHASE4A_CORRECT_RETRY.log'
$arg='-batchmode -quit -projectPath "'+$proj+'" -executeMethod Phase4A_CorrectVisualPhysicsIntegration.Run -logFile "'+$log+'"'
$p=Start-Process -FilePath $unity -ArgumentList $arg -PassThru -Wait
Write-Output ('UNITY_EXIT='+$p.ExitCode)
if(Test-Path $log){Get-Content $log -Tail 100}
exit $p.ExitCode
