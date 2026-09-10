$required=@{
PROGRAMDATA='C:\ProgramData'; ALLUSERSPROFILE='C:\ProgramData'; APPDATA='C:\Users\mongo\AppData\Roaming'; LOCALAPPDATA='C:\Users\mongo\AppData\Local'; USERPROFILE='C:\Users\mongo'; TEMP='C:\Users\mongo\AppData\Local\Temp'; TMP='C:\Users\mongo\AppData\Local\Temp'; NO_PROXY='localhost,127.0.0.1'; UNITY_UPM_TIMEOUT='120'
}
foreach($k in $required.Keys){[Environment]::SetEnvironmentVariable($k,$required[$k],'Process')}
$env:PATH=(($env:PATH -split ';') | Where-Object {$_ -notlike '*npm-cache*_npx*'}) -join ';'
$unity='C:\Program Files\Unity\Hub\Editor\6000.4.4f1\Editor\Unity.exe'
$project='C:\Users\mongo\UnityProjects\147 VR'
$log='C:\Users\mongo\UnityProjects\147 VR\Docs\M7_4_V007UnityIntegrate_20260903.log'
$p=Start-Process -FilePath $unity -ArgumentList @('-batchmode','-quit','-projectPath',$project,'-executeMethod','M7_4_V007UnityIntegrate.Run','-logFile',$log) -PassThru -Wait
Write-Output "UNITY_EXIT=$($p.ExitCode)"
exit $p.ExitCode
