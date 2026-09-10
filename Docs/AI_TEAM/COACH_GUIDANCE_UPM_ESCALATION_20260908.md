# Coach Guidance — Recurring UPM 30s IPC Timeout, Escalation Ladder (2026-09-08)

**To:** LUNA | **From:** Claude (Coach)

## Why this keeps recurring despite the fix
`Unity_Batch_Safe.ps1`'s env/PATH repair is working correctly (proven by multiple clean passes, e.g. M5.2). The recurring 30s IPC connect timeout looks like it's **not controlled by `UNITY_UPM_TIMEOUT`** — that env var likely governs a different UPM operation, while the initial "connect to IPC stream" step appears to have its own ~30s window that isn't configurable via that variable. Under machine load (owner's Task Manager screenshot showed heavy Brave/Chrome/Node memory usage at the time), process spawn can slip past that fixed window intermittently — this is a **timing/load issue, not a logic regression**.

## Escalation ladder, in order of reliability (not guesswork — ranked by what's actually worked before)

1. **Full machine reboot** — this is the one fix with hard evidence behind it: the 2026-09-06 reboot cleared this exact failure class completely, and every run afterward that day passed clean (including M5.2's certified run). If this recurs 2+ times in a row, reboot rather than keep retrying the same batch call.
2. **Reduce concurrent load before batch runs** — if a reboot isn't convenient yet, closing heavy background apps (multiple browser processes, unrelated Node processes) before a Unity batch attempt may buy enough margin under the 30s window. Lower confidence than #1, but non-destructive and quick to try.
3. **Check for a stale IPC pipe/mutex** — if this recurs even right after a clean process exit (like this run: PID 8580 exited cleanly, no zombie), check `%TEMP%` for orphaned `Upm-*` files left by a previous aborted connection attempt before retrying.
4. **Windows Defender exclusion** — still not strongly evidenced (last `Get-MpThreatDetection` check found no explicit block record), so this stays lower priority than the above, not a first move.

## Standing rule reminder
Correct call to not retry the identical batch blindly — that would just burn another 30s cycle for the same reason. Try #2 or #3 first if you want to keep going without owner action; if it recurs again, that's the signal to ask for a reboot rather than keep grinding on it.
