# 147VR UPM Child-Process Environment Inheritance Fix

Status: VERIFIED — 2026-08-28
Project: `C:\Users\mongo\UnityProjects\147 VR`
Unity: `6000.4.4f1`

## Problem

Unity showed `Failed to start the Unity Package Manager local server process`.
The UPM executable itself was healthy and executable, but Package Manager API requests returned HTTP 500.

The decisive Editor error was:

`The "path" argument must be of type string. Received undefined`

## Root Cause — VERIFIED

The Desktop Commander parent process had missing environment variables:

- `PROGRAMDATA` — not defined
- `ALLUSERSPROFILE` — not defined
- `TMP` — not defined

`TEMP`, `APPDATA`, `LOCALAPPDATA`, and `USERPROFILE` were present.

The UPM server depends on the Windows environment for its configuration path. With the inherited environment broken, Unity could start UPM and connect to IPC, but Package Manager requests returned HTTP 500.

## Proof Before Fix

UPM log showed:

- IPC server started: PASS
- `project:list-packages`: HTTP 500
- `config:project:get-registries`: HTTP 500
- `packages:get-all-packageinfo`: HTTP 500

Editor log showed the matching `path argument ... Received undefined` error.
## Verified Recovery Procedure

1. Close Unity Editor and Unity Hub. Confirm no `Unity.exe` or `UnityPackageManager.exe` remains.
2. Launch Unity from a command file or terminal where the required variables are explicitly set in the same parent session.
3. Use the authoritative project path only:
   `C:\Users\mongo\UnityProjects\147 VR`

Required launch environment:

```text
PROGRAMDATA=C:\ProgramData
ALLUSERSPROFILE=C:\ProgramData
APPDATA=C:\Users\mongo\AppData\Roaming
LOCALAPPDATA=C:\Users\mongo\AppData\Local
USERPROFILE=C:\Users\mongo
TEMP=C:\Users\mongo\AppData\Local\Temp
TMP=C:\Users\mongo\AppData\Local\Temp
NO_PROXY=localhost,127.0.0.1
```

## Verification — PASS

Clean launch was performed after all competing Unity Editor instances were closed.

Observed:

- Unity 6000.4.4f1 started: PASS
- Project path resolved to authoritative 147 VR path: PASS
- `UnityPackageManager.exe` started: PASS
- UPM IPC server started: PASS
- Unity connected to UPM IPC: PASS (0.3 s)
- `config:project:get-registries`: HTTP 200
- `project:list-packages`: HTTP 200 repeatedly
- `packages:get-all-packageinfo`: HTTP 200
- No `Received undefined` error in the final launch log: PASS
- Editor remained running during verification: PASS

Representative final UPM results:

`config:project:get-registries --> 200`
`project:list-packages --> 200`
`packages:get-all-packageinfo --> 200`

## Important AI/Automation Rule

Do NOT trust the parent process environment. Always verify the actual environment before launching Unity/UPM.

Do NOT delete `Library`, `Packages`, `packages-lock.json`, or reinstall Unity as the first response to this failure.

Do NOT use the old `C:\Users\mongo\UnityProjects\147 VR` Hub alias. The authoritative project path is:

`C:\Users\mongo\UnityProjects\147 VR`

This document records the verified launch/recovery procedure, not a speculative workaround.

## Regression Re-Verification — Phase 1 Audit

Date: 2026-09-01

A fresh controlled Unity launch was performed with the exact required environment explicitly injected into the parent process before Unity startup.

Observed:
- Unity 6000.4.4f1 launch: PASS
- Unity exit code: 0
- UPM IPC connection: PASS (`Upm-13172`, connected after 0.2 s)
- UPM restored resolved package state from cache: PASS
- UPM registered 86 packages: PASS
- UPM completed package registration in 0.03 s: PASS
- No `Received undefined` error observed in this exact-environment regression

Conclusion: UPM child-process environment inheritance fix remains valid on 2026-09-01.

AI handoff rule: when reproducing this blocker, first inspect the actual environment inherited by the Unity launcher. Explicitly set PROGRAMDATA, ALLUSERSPROFILE, APPDATA, LOCALAPPDATA, USERPROFILE, TEMP, TMP, and NO_PROXY before launching Unity. Do not begin by deleting Library/PackageCache/packages-lock.json or reinstalling Unity.

Physics Authority was not modified during this verification.
