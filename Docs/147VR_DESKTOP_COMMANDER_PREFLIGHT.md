# 147VR DESKTOP COMMANDER PREFLIGHT

> **MANDATORY EXECUTION GATE â€” READ BEFORE ANY 147VR EXECUTION**
>
> This document defines the minimum safe startup workflow for AI/automation operating on the 147VR project through Desktop Commander.
>
> **Scope:** `C:\Users\mongo\UnityProjects\147 VR` only.
>
> **Core rule:** No gameplay/code modification is allowed until every prior gate is explicitly passed or a blocker is recorded.

## LOCKED WORKFLOW

```text
1. Commander connection
        â†“
2. Device health
        â†“
3. Read MANDATORY PREFLIGHT
        â†“
4. Unity process health
        â†“
5. Compile Gate
        â†“
6. Inspect Console / Editor.log
        â†“
7. Calibration
```

## 1. COMMANDER CONNECTION

- Confirm the intended Desktop Commander device is `online`.
- Confirm authentication/connection responds to `ping`.
- Confirm the active device is the user's Windows development machine.
- A connection failure is a tooling blocker, not a Unity/code failure.

**PASS condition:** device online + successful ping.

## 2. DEVICE HEALTH

- Confirm the project path exists: `C:\Users\mongo\UnityProjects\147 VR`.
- Confirm the project is readable/writable through Commander.
- Confirm no destructive system operation is required.
- Prefer focused project paths over broad filesystem scans.

**PASS condition:** project root accessible and Commander filesystem operations succeed.

## 3. READ MANDATORY PREFLIGHT

Before touching project source, AI must read:

- `Docs/147VR_DESKTOP_COMMANDER_PREFLIGHT.md`
- `Docs/147VR_AI_WORK_PROTOCOL.md`
- `Docs/147VR_REALITY_MAP.md`
- `Docs/147VR_REALITY_MAP_AUTHORITY_ADDENDUM.md`

Then identify the current state/blockers from the latest handoff documents.

**PASS condition:** required documents were actually read from the project.

## 4. UNITY PROCESS HEALTH

- Determine whether Unity Editor is currently running.
- Determine whether Unity Hub is running separately from the Editor.
- Check for stuck/zombie Unity or UPM-related processes before any batch validation.
- Never kill an active Unity process merely to make a check pass; diagnose first.

**PASS condition:** Unity state is understood and there is no unexplained stuck process blocking safe validation.

## 5. COMPILE GATE

- Do not modify gameplay/source merely because Unity is slow or appears busy.
- Determine whether Unity compilation/import is active or idle.
- Treat compile errors as a hard gate.
- For package-aware validation, follow `147VR_AI_WORK_PROTOCOL.md` UPM rules; do not blindly add `-noUpm`.
- Prefer explicit absolute `-logFile` paths for unattended validation.

**PASS condition:** compilation/import is complete enough to inspect authoritative errors and the project has no unexplained compile blocker.

## 6. CONSOLE / EDITOR.LOG

- Inspect the Unity Console/log evidence after the compile gate.
- Separate code errors from UPM/tooling/infrastructure failures.
- Record exact error text, file, line, and evidence before proposing a fix.
- Never infer a clean Console from absence of visible UI output.

**PASS condition:** current error state is known from actual log evidence.

## 7. CALIBRATION

- Calibration is downstream of compilation and runtime readiness.
- Use the existing M1 cue/physics authority chain.
- Do not create a second cue-strike or physics authority.
- Never fabricate Golden Truth, calibration measurements, PASS results, or percentages.
- Runtime-proof calibration requires actual measured samples and recorded evidence.

**PASS condition:** calibration runs through the intended authority path and produces real measured evidence.

## CHANGE SAFETY

- Scope is 147VR only unless the project owner explicitly changes scope.
- Preserve existing working systems.
- Prefer additive, reversible changes.
- After every material change, update relevant handoff/state documentation.
- If any gate fails, stop downstream execution and document the blocker.

## AUTHORITATIVE REFERENCES

- `Docs/147VR_AI_WORK_PROTOCOL.md`
- `Docs/147VR_REALITY_MAP.md`
- `Docs/147VR_REALITY_MAP_AUTHORITY_ADDENDUM.md`
- `Docs/147VR_CURRENT_STATE.md`
- `Docs/147VR_PROJECT_MEMORY.md`

## STARTUP RECORD

The first execution after Commander recovery must record the observed connection, device, Unity, compile, log, and calibration state in the session handoff.
Do not mark a gate PASS unless it was actually verified on the project machine.


## 2026-08-29 POST-RECOVERY STARTUP RECORD

- Commander device: `wIn-NaRIs` online and filesystem operations verified.
- Canonical project: `C:\Users\mongo\UnityProjects\147 VR`.
- Unity/UPM process check before resumed execution: no active Unity Editor/UPM blocker.
- Historical GUI recovery log verified package-aware UPM connection, package registration, and successful script compilation.
- First resumed package-aware batch reproduced the UPM IPC timeout. Evidence showed the Commander inherited an npx npm-cache entry in `PATH`.
- Clean launch mitigation: remove only the inherited `npm-cache\_npx` PATH entry; preserve normal package-aware UPM mode and `UNITY_UPM_TIMEOUT=120`.
- Clean package-aware Unity launch connected to UPM in 0.3s and completed the Straight runtime batch.
- Compile/runtime evidence: Unity 6000.4.4f1 persisted five real Straight measurements and exited 0.
- Golden regression evidence: Unity headless regression processed five revision-3 samples and exited 0 with 5 PASS / 0 FAIL.
- Calibration gate is therefore no longer blocked for the Straight proof path. M2.1 Stun remains a separate downstream calibration gate.
- Known non-blocking startup warnings remain: Package Manager `path argument ... Received undefined` callbacks and Meta XR Project Setup recommended/required fix notifications. They were not accepted as source-code failures.
- Future unattended Unity execution must use the clean child environment pattern documented in `147VR_AI_WORK_PROTOCOL.md`.


## 2026-08-29 M2.1 EXECUTION CHECKPOINT

- Use canonical project root only: `C:\Users\mongo\UnityProjects\147 VR`.
- Single Unity instance rule remains mandatory. Kill stale batch Unity processes before launching the next one; never launch a second instance against the same project.
- Preserve clean child-process environment: remove inherited `npm-cache\_npx` entries from PATH; keep package-aware UPM enabled and `UNITY_UPM_TIMEOUT=120`.
- M2.1 controlled lane is X=+0.30m, cue/object Z separation 0.65m. This avoids the middle-pocket trajectory while retaining a straight +Z calibration shot.
- Runtime evidence was captured with actual `Bed_Collider` support and runtime `Surface` contact. Do not replace the surface with an invisible/fake floor.
- Real Stun JSON and PHY-002 Golden are now certified for the current controlled contract. Future reruns must preserve the same scene, lane, shotSpeed=4.0m/s, and measurement semantics unless a deliberate revision is documented.
- Straight PHY-001 revision 3 remains locked; do not regenerate or overwrite it during M2.1 work.
