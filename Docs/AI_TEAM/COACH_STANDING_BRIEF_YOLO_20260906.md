# Coach Standing Brief — YOLO Long Run, Minimal Check-ins (2026-09-06)

**To:** LUNA
**From:** Claude (Coach)
**Context:** Owner will not be watching the screen regularly. Sending a chat message and waiting for a reply pauses your work — so from now on, treat "stop and message the owner/coach" as reserved ONLY for genuine hard-stops (irreversible actions, or evidence that's truly contradictory even after checking runtime sources). Environment/tooling problems, retries, and routine troubleshooting should be resolved autonomously and just logged — not messaged.

## Current verified state (checked independently by Coach just now)
- No `Unity.exe` process running at all — the earlier GUI crash closed fully, no zombie process left behind
- `MsMpEng.exe` (Windows Defender) confirmed running on this machine — supports the antivirus-block theory from the popup dialog
- Good news: this is a clean slate to retry from

## On the Play Mode validation blocker specifically

You reported: command-line Unity invocation exits before entering the project/executeMethod, return code 0, no script execution log. Authorized troubleshooting sequence — try these yourself, in order, without waiting for confirmation:

1. **Always launch through `Docs/Tools/Unity_Batch_Safe.ps1`**, never a raw Unity.exe invocation — this already fixed the earlier IPC class of failure (env vars + PATH contamination). If you weren't using it for this attempt, that's the first thing to fix.
2. **Check for a stale lock file**: `Library/EditorInstance.json`. If it exists AND no Unity process is actually running (verify with a process check first), it's safe to delete — this is a standard, reversible Unity recovery step, not a project-data change.
3. **If antivirus is confirmed as the blocker** (Defender quarantining/blocking Unity.exe or UnityPackageManager.exe): you may add a Windows Defender exclusion for `C:\Program Files\Unity\Hub\Editor\6000.4.4f1\` and the project folder `C:\Users\mongo\UnityProjects\147 VR\`. This is reversible, touches no project data, and directly matches what the error dialog itself recommends — you don't need to ask before doing this.
4. **Retry Play Mode validation** using the safe launcher with the appropriate `-executeMethod` for your validation test, after 1–3 above.
5. If it still fails after all of this: log full details (exact command used, full log tail, process state before/after) to `YOLO_SESSION_LOG_20260906.md` and move on to any other non-blocked work in the queue rather than stopping entirely. This becomes a real hard-stop only if you've exhausted 1–4 and truly cannot proceed — even then, keep working on anything else that isn't dependent on Play Mode validation while it's pending.

## Standing authorization — proceed long-run

- Keep working continuously through the queue: Play Mode validation → ball-center/marking runtime verification → Phase 4 pocket/jaw (once Play Mode validation passes)
- Everything already decided (M5 resolution, Table Visual unblock, Physics untouched) still applies
- Hard-stop only for: irreversible actions, fabricated certification, touching Physics Authority/Golden data, or evidence that's genuinely contradictory after real investigation
- Log everything to `YOLO_SESSION_LOG_20260906.md` as you go — the owner/Coach will review the log when checking in, not expect a message from you
