# 147VR — YOLO Long Run Standing Rules

**Effective:** 2026-09-06
**Authority:** Coach Standing Brief / delegated owner authority

## Operating mode
1. Work continuously through the queue without routine owner/Coach check-ins.
2. Do not stop and message for ordinary environment/tooling problems, retries, or troubleshooting.
3. Resolve routine blockers autonomously, record evidence in `Docs/YOLO_SESSION_LOG_20260906.md`, and continue.
4. If one task is blocked, move to other non-blocked work instead of idling.

## Unity execution
5. Launch Unity batch operations **only** through `Docs/Tools/Unity_Batch_Safe.ps1`.
6. Never use a raw `Unity.exe` invocation for batch validation.
7. Before retrying, check Unity/UnityPackageManager process state.
8. If `Library/EditorInstance.json` exists while no real Unity process is running, it may be deleted as stale lock recovery.
9. Retry through the safe launcher after lock/environment recovery.

## Antivirus recovery
10. If evidence confirms Windows Defender is blocking/quarantining Unity or UnityPackageManager, autonomously add Defender exclusions for:
    - `C:\Program Files\Unity\Hub\Editor\6000.4.4f1\`
    - `C:\Users\mongo\UnityProjects\147 VR\`
11. Defender exclusions are reversible environment changes and do not modify project data.

## Hard-stop conditions
12. Stop only for genuinely irreversible actions, fabricated certification, touching Physics Authority/Golden data outside authority, or genuinely contradictory evidence after real investigation.
13. For the V007 sequence, never modify colliders, ball spawn data, or `Bed_Collider` merely to make visual validation pass. Stop that specific sub-step and log/report the conflict.
14. After safe-launch retries and antivirus/lock troubleshooting are exhausted, log the exact command, full relevant log tail, and process state; continue other non-blocked work.

## Evidence discipline
15. Never convert a tooling failure into a source-code failure without compiler/runtime evidence.
16. Never certify from fabricated or inferred Golden data; certification requires real persisted evidence.
17. Keep Physics, Gameplay, and Golden authority isolated from visual promotion work.
18. Other AI-team agents must follow these standing rules for long-running autonomous work unless a newer explicit authority document supersedes them.
