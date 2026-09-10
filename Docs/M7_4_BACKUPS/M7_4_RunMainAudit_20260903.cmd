@echo off
set "PROGRAMDATA=C:\ProgramData"
set "APPDATA=C:\Users\mongo\AppData\Roaming"
set "LOCALAPPDATA=C:\Users\mongo\AppData\Local"
set "USERPROFILE=C:\Users\mongo"
set "TEMP=C:\Users\mongo\AppData\Local\Temp"
set "TMP=C:\Users\mongo\AppData\Local\Temp"
set "NO_PROXY=localhost,127.0.0.1"
set "UNITY_UPM_TIMEOUT=120"
set "AUDITLOG=C:\Users\mongo\AppData\Local\Temp\147VR_M7_4_MainRuntimeAudit_20260903.log"
set "PATH=C:\Windows\System32;C:\Windows;C:\Program Files\Unity\Hub\Editor\6000.4.4f1\Editor"
"C:\Program Files\Unity\Hub\Editor\6000.4.4f1\Editor\Unity.exe" -batchmode -nographics -projectPath "C:\Users\mongo\UnityProjects\147 VR" -executeMethod M7_4_MainRuntimeAudit.Run -logFile "%AUDITLOG%" -quit
exit /b %ERRORLEVEL%
