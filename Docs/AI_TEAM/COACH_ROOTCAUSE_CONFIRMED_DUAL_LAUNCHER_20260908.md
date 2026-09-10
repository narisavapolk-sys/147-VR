# Coach Verified — Root Cause Confirmed: Two Competing Launcher Scripts (2026-09-08)

**Verified independently by Coach.** Read `Tools\Launch_147VR_Safe.ps1` directly.

## Confirmed
This script (dated 2026-08-31, predates our 2026-09-06 fix work) sets the correct env vars but has **zero PATH sanitization** and **no single-instance guard**:
```powershell
& $unity -batchmode -projectPath $project -quit -logFile -
```
No stripping of `npm-cache\_npx\...\node_modules\.bin` from PATH at all. This exactly matches the failure signature we've been chasing. File was accessed today (19:03), consistent with it being what actually got invoked during recent M5.1 attempts instead of our hardened `Docs\Tools\Unity_Batch_Safe.ps1`.

## Decision
1. **LUNA: only ever invoke `Docs\Tools\Unity_Batch_Safe.ps1` going forward.** Treat `Tools\Launch_147VR_Safe.ps1` as deprecated — do not call it again.
2. To prevent this mistake recurring (for any AI/session, including future ones): rename or move the old script so it can't be accidentally invoked again — e.g. `Tools\Launch_147VR_Safe.ps1.DEPRECATED` — rather than deleting it outright (keep for reference/history).
3. Once confirmed this was indeed the actual cause of the M5.1 failures, retry the clean M5.1 sequence through the correct script.

## Boundary unchanged
This is a tooling/launcher consolidation, zero Physics/M5 code touched. Still no M5.1 code/scene changes until a clean run through the correct script actually produces `[M5.1 INSTALL PASS]` with persisted evidence.
