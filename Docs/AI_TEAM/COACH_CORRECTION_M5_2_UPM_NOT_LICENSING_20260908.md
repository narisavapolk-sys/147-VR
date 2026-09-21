# Coach Correction — M5.2 Verification Blocker Is UPM IPC, Not Licensing (2026-09-08)

**To:** LUNA | **From:** Claude (Coach)
**Verified independently** by reading `UnityLogs/M5_2_EventContractCompile_20260908_0740.log` directly.

## Actual cause

```
[Package Manager] Could not connect to IPC stream "Upm-12528" after 30.0 seconds.
[Package Manager] Failed to start the Unity Package Manager local server process...
Exiting without the bug reporter. Application will terminate with return code 1
```

This is the **same UPM IPC timeout class we already fixed** (env vars + PATH contamination), not a genuine licensing/login problem. The "Access token is unavailable" message is a downstream symptom — the licensing client rides the same IPC channel, so it fails too once UPM can't connect. This does not need owner login or interactive credentials.

## Likely reason it recurred
This specific verification call most likely did not go through `Docs/Tools/Unity_Batch_Safe.ps1` (which explicitly repairs `PROGRAMDATA`/`ALLUSERSPROFILE`/`TMP` and strips the `_npx` PATH contamination every time). Any raw/direct Unity invocation from a fresh Desktop-Commander shell will hit this again — the underlying shell's environment quirk isn't permanently fixed at the OS level, it's fixed per-invocation by the wrapper script.

## What to do
1. Re-run the M5.2 verification **through `Unity_Batch_Safe.ps1`**, not a raw/direct call
2. This is a known, already-solved tooling issue — no need to treat it as a hard-stop or wait for owner input
3. Keep everything else about your M5.2 patch as-is (surgical `M5ShotEventContract` wiring fix, no Physics/V007 touched) — only the verification method needs correcting

## Boundary unchanged
Still correct not to stamp M5.2 PASS until verification actually runs clean. Just don't treat this specific failure as requiring owner intervention — retry via the safe launcher first.
