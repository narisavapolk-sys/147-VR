# GitHub Integration — 3-AI Coordination Rule (2026-09-08)

**Team now:** Claude (Coach), LUNA (Executor — Unity/Physics/M5), Opus5/Notion AI (new — GitHub setup, file triage)

## Why this needs its own lock discipline
Git operations don't have a built-in "one instance at a time" protection like Unity's `Library/EditorInstance.json`. Two AIs running `git add`/`commit`/`push` concurrently can create broken commits, lost changes, or merge conflicts — worse than the Unity collision risk, not less.

## Rule
1. **Before any `git add`/`commit`/`push`**, check `Docs/AI_TEAM/GIT_LOCK.md` (new file, same pattern as `LOCK.md`). If it says another AI holds it, wait.
2. Whoever is doing GitHub setup work (currently: Opus5 + LUNA together per owner's direction) claims the lock, does the batch of git operations, releases it.
3. **Never commit/push while Unity batch is mid-run** writing to the same working tree (e.g. `Library/`, scene files) — check `Docs/AI_TEAM/LOCK.md` too before committing scene/asset changes, to avoid committing a half-written file.
4. **What NOT to commit without explicit review:** `.env`/credentials, `Library/`, `Temp/`, large binary caches, anything under `Docs/AI_TEAM/BACKUPS/` unless intentionally archiving. Opus5 should propose a `.gitignore` and get it reviewed before the first real commit, not commit everything by default.
5. Physics Authority / Golden data files — fine to commit (that's exactly what should be version-controlled), but the same "no committing mid-write" caution applies.

## Setup reminder for Opus5
Same Desktop Commander connection steps as any team AI — connect via the custom MCP connector to `wIn-NaRIs`, then read `Docs/AI_TEAM/LOCK.md` and this file before running any git command.
