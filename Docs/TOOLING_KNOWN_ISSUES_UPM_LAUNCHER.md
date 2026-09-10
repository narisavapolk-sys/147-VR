# 147 VR — Known Tooling Issue: UPM Launcher PATH Contamination

**Date:** 2026-09-08
**Status:** CONFIRMED ROOT CAUSE / RECOVERY PATTERN LOCKED
**Scope:** Tooling only. No Physics Authority or M5 code.

## Symptom
Unity batch launches can fail during UPM bootstrap with:

```text
[Package Manager] Could not connect to IPC stream after 30.0 seconds.
[Package Manager] Failed to start the Unity Package Manager local server process.
```

The failure can occur before `-executeMethod` runs, so the target test/install script is never reached.

## Confirmed Root Cause
A legacy launcher inherited a contaminated Windows `PATH` containing an npm npx path similar to:

```text
npm-cache\_npx\...\node_modules\.bin
```

This can interfere with Unity Package Manager child-process startup and produce the UPM IPC timeout signature.

## Launcher Rule — LOCKED

Do **not** invoke the deprecated launcher:

```text
Tools\Launch_147VR_Safe.ps1
```

It was renamed to:

```text
Tools\Launch_147VR_Safe.ps1.DEPRECATED
```

The only approved batch launcher is:

```text
Docs\Tools\Unity_Batch_Safe.ps1
```

## Required Recovery Pattern
1. Verify no Unity process is running.
2. Build a clean child environment before spawning Unity.
3. Keep package-aware Unity startup; **never use `-noUpm`**.
4. Set canonical Windows environment values for `PROGRAMDATA`, `APPDATA`, `LOCALAPPDATA`, `USERPROFILE`, `TEMP`, `TMP`, and `NO_PROXY`.
5. Set `UNITY_UPM_TIMEOUT=120`.
6. Sanitize `PATH` and remove inherited `npm-cache\_npx\...\node_modules\.bin` entries.
7. Launch Unity only after the sanitized environment is ready.
8. For certification, verify the target method actually ran and verify persisted evidence separately.

## M5.1 Boundary
A runtime `[M5.1 INSTALL PASS]` is **not valid certification evidence** if `SaveScene()` failed.
The scene YAML must contain the serialized M5.1 components after a successful save/reload verification.

## Historical Lesson
Two competing launchers caused operational drift: one hardened launcher and one older launcher with correct env vars but no PATH sanitization. Consolidating to one approved launcher prevents accidental regression.

**Do not delete the deprecated launcher. Keep it only as historical reference.**
