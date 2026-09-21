@echo off
setlocal EnableExtensions
set "PROGRAMDATA=C:\ProgramData"
set "APPDATA=C:\Users\mongo\AppData\Roaming"
set "LOCALAPPDATA=C:\Users\mongo\AppData\Local"
set "USERPROFILE=C:\Users\mongo"
set "SystemRoot=C:\WINDOWS"
set "TEMP=C:\Users\mongo\AppData\Local\Temp"
set "TMP=C:\Users\mongo\AppData\Local\Temp"
set "NO_PROXY=localhost,127.0.0.1"
set "PATH=%SystemRoot%\system32;%SystemRoot%;%SystemRoot%\System32\Wbem;%SystemRoot%\System32\WindowsPowerShell\v1.0\;C:\Program Files\nodejs;C:\Program Files\Git\cmd;%PATH%"
set "PROJECT=C:\Users\mongo\UnityProjects\147 VR"
set "UNITY=C:\Program Files\Unity\Hub\Editor\6000.4.4f1\Editor\Unity.exe"
set "LOG=C:\Users\mongo\AppData\Local\Temp\147VR_PHASE4A_MARKING_AUDIT.log"
cd /d "%PROJECT%"
"%UNITY%" -batchmode -quit -projectPath "%PROJECT%" -executeMethod Phase4A_MarkingAlignmentAudit.Run -logFile "%LOG%"
set "RC=%ERRORLEVEL%"
echo UNITY_EXIT=%RC%>>"%LOG%"
exit /b %RC%
