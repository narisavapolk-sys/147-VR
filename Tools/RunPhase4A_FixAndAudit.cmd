@echo off
set "PROJECT=C:\Users\mongo\UnityProjects\147 VR"
set "UNITY=C:\Program Files\Unity\Hub\Editor\6000.4.4f1\Editor\Unity.exe"
set "PROGRAMDATA=C:\ProgramData"
set "APPDATA=C:\Users\mongo\AppData\Roaming"
set "LOCALAPPDATA=C:\Users\mongo\AppData\Local"
set "USERPROFILE=C:\Users\mongo"
set "TEMP=C:\Users\mongo\AppData\Local\Temp"
set "TMP=C:\Users\mongo\AppData\Local\Temp"
set "NO_PROXY=localhost,127.0.0.1"
set "UNITY_UPM_TIMEOUT=120"
cd /d "%PROJECT%"
"%UNITY%" -batchmode -quit -projectPath "%PROJECT%" -executeMethod Phase4A_FixTableSurface.Run -logFile "C:\Users\mongo\AppData\Local\Temp\147VR_PHASE4A_FIX.log"
if errorlevel 1 exit /b %errorlevel%
"%UNITY%" -batchmode -quit -projectPath "%PROJECT%" -executeMethod Phase4A_MarkingAlignmentAudit.Run -logFile "C:\Users\mongo\AppData\Local\Temp\147VR_PHASE4A_AUDIT2.log"
exit /b %errorlevel%
