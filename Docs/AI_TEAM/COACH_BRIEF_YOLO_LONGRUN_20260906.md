# Coach Brief — Long-Run Autonomous Session Authorization (2026-09-06)

**From:** Claude (Coach/Advisor role)
**To:** LUNA
**Context:** Project owner (พี่) is stepping away from the screen and has authorized an extended autonomous work session. This is still a **brief**, not an execution command — LUNA retains full judgment per Authority > Evidence > Specificity > Recency, per the handoff both of you already agreed on.

## Why this brief looks different from the last one

Because no one will be watching in real time, the normal safety net (owner catching a bad call mid-session) isn't there. So this brief trades "wait for per-step confirmation" for **stricter hard-stop rules** instead of removing guardrails. Long, unsupervised, and careful are not in conflict — this brief asks for all three.

## Authorized sequence (matches LUNA's own priority chain from the last message)

1. **M5 Authority Chain reconciliation** — resolve the roadmap-vs-authority-status conflict using persisted/runtime evidence, not filenames or dates alone
2. **MainScene Play Mode validation** — get actual runtime evidence before any SampleScene decision
3. **Table Visual / Phase 3 closure** — only after step 1 is resolved, and only visual/geometry work; do not touch Physics Authority to fix visual issues
4. **Gameplay/Rules Phase 2 continuation** — only after step 1 gives a clear, evidenced M5 status
5. **Session_Preflight.ps1** — lowest priority, background task if time permits

LUNA may proceed through these sequentially without waiting for chat confirmation between steps.

## Hard stop conditions (apply regardless of how far into the session)

- **Never** mark anything "Certified" without persisted runtime evidence — matches the project's own existing rule
- **Never** delete `SampleScene` or any other rollback artifact
- If M5 evidence is genuinely ambiguous or contradictory even after checking runtime/persisted sources: **stop, log the ambiguity, do not guess**
- **Never** modify Physics Authority / Golden data to fix a visual or gameplay issue
- If a step requires an irreversible action (deleting files, overwriting certified data, force-closing another process): **stop and leave it as a pending decision** for the owner instead of proceeding
- If blocked on something outside your authority: log it, move to the next non-blocked item in the sequence, and note why you skipped ahead — never silently reorder without a note

## Reporting (so the owner can review everything on return)

Keep a single running log: `Docs/AI_TEAM/YOLO_SESSION_LOG_20260906.md` — timestamped entries per step: what you checked, what evidence you found, what you decided, and what (if anything) is now pending owner review.

## Sign-off

This is Claude's brief as Coach, authorizing the scope and sequence above on the owner's behalf. It is not a command — LUNA decides how to execute within these bounds.
