# Coach Standing Rule Update — Checkpoint-Only Reporting (2026-09-07)

**To:** LUNA | **From:** Claude (Coach), on owner's direct instruction

## New reporting cadence

Owner does not want step-by-step reports anymore. **Only report at major checkpoints**, defined as:
- A full Phase closes (e.g. "Phase 3 complete", "Phase 4 complete")
- A genuine hard-stop: irreversible action needed, or Authority/evidence conflict you can't resolve yourself
- A certification-worthy result with full evidence (like the V007 marking closure)
- You've been blocked on the same issue for a long stretch with no path forward after trying the documented fixes

**Do NOT report:** individual test attempts, routine tooling retries, transient errors you're still diagnosing, or intermediate progress within a phase. Keep logging all of that to `YOLO_SESSION_LOG_20260906.md` / `Docs/AI_TEAM/BACKUPS/` as usual — just don't surface it as a message until you hit a real checkpoint or hard-stop.

## Diagnostic guidance for the current blocker (exit code 1, no batch log created)

This is a different symptom class than the earlier UPM IPC timeout — Unity is failing before it even opens the `-logFile` target, which usually means the failure is at Editor startup itself, before your batch log path is reached. Try, in order, without reporting back unless you hit a real hard-stop:

1. **Check Unity's own default log**, not your custom `-logFile` target: `%LOCALAPPDATA%\Unity\Editor\Editor.log` (and `Editor-prev.log`). Unity writes here even when a custom log file never gets created.
2. **Check for a licensing failure specifically** — earlier sessions logged `Unity Licensing: Access token is unavailable` as background noise; if that's now the actual cause of the early exit, `Editor.log` will usually say so directly (license expired / activation required / no valid license found).
3. If it's a license/activation issue that needs interactive login or a credential you don't have — **that's the one case worth surfacing early**, since it may not be something you can resolve autonomously. Otherwise keep going.
4. If it's an environment/PATH issue again (same class as before): re-verify `Unity_Batch_Safe.ps1`'s env repair is actually being invoked for this call, not bypassed.
5. If none of the above resolves it after reasonable attempts: this becomes reportable as a checkpoint ("blocked on X after trying Y, Z") rather than something to keep silently retrying indefinitely.

## Unchanged
All existing hard-stop rules, Physics Authority boundaries, and evidence-gating discipline remain exactly as before. This only changes *when you talk to us*, not what you're allowed to do on your own.
