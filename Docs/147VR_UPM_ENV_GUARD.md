# 147 VR — UPM Environment Guard

Status: ACTIVE — 2026-08-31
Purpose: prevent recurrence of the verified UPM child-process environment failure.

## Required launch environment
PROGRAMDATA=C:\ProgramData
ALLUSERSPROFILE=C:\ProgramData
APPDATA=C:\Users\mongo\AppData\Roaming
LOCALAPPDATA=C:\Users\mongo\AppData\Local
USERPROFILE=C:\Users\mongo
TEMP=C:\Users\mongo\AppData\Local\Temp
TMP=C:\Users\mongo\AppData\Local\Temp
NO_PROXY=localhost,127.0.0.1
UNITY_UPM_TIMEOUT=120

## Recovery rule
Before diagnosing Packages/Library, verify the actual Unity parent-process environment against this contract. If any required value is missing or invalid, repair it in the launch process and retry UPM validation first.

## Safety rule
Never delete Library, Packages, packages-lock.json, or reinstall Unity as the first response to this failure.

## Validation signature
Expected healthy UPM calls: config:project:get-registries = 200; project:list-packages = 200; packages:get-all-packageinfo = 200; no `Received undefined` path error.
