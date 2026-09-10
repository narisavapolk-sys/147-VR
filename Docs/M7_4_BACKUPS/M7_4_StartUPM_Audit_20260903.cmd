@echo off
set "PROGRAMDATA=C:\ProgramData"
set "APPDATA=C:\Users\mongo\AppData\Roaming"
set "LOCALAPPDATA=C:\Users\mongo\AppData\Local"
set "USERPROFILE=C:\Users\mongo"
set "TEMP=C:\Users\mongo\AppData\Local\Temp"
set "TMP=C:\Users\mongo\AppData\Local\Temp"
set "NO_PROXY=localhost,127.0.0.1"
set "UNITY_UPM_TIMEOUT=120"
cd /d "C:\Users\mongo\UnityProjects\147 VR"
"C:\Program Files\Unity\Hub\Editor\6000.4.4f1\Editor\Data\Resources\PackageManager\Server\UnityPackageManager.exe" server -s 14774 --ipc-path Upm-M74 -l 2
