# 147VR AI Work Protocol

## Purpose
This document is a mandatory handoff and execution contract for every AI working on 147VR.

## Scope
147VR only unless the project owner explicitly changes scope. Do not inspect or modify other games to solve a 147VR task unless explicitly authorized.

## Source of Truth
The actual project files, scenes, prefabs, scripts, settings, test output, and runtime evidence are authoritative. Markdown status is a handoff record, not proof by itself.

## Mandatory Workflow
1. Read the current 147VR state and relevant Markdown before changing code.
2. Inspect the actual implementation and scene/prefab bindings before claiming completion.
3. Distinguish implementation-complete from runtime-proven and production-complete.
4. Work through the current plan in dependency order; do not rebuild systems that are already proven.
5. Validate changes with the strongest available evidence: compile, tests, runtime, regression, and target-device proof.
6. Never invent calibration truth, golden values, PASS results, or completion percentages.
7. If a command fails because of tooling/connection/quoting, diagnose the tool failure separately from project state.

## Mandatory Documentation Rule
After EVERY completed work session or material project change, update the relevant `.md` handoff/state documents in the same project. At minimum update `Docs/147VR_CURRENT_STATE.md` and `Docs/147VR_PROJECT_MEMORY.md` when state changes.

## Handoff Requirement
Every update must leave enough information for another AI to continue without chat history: what changed, exact files, current verified state, evidence, blockers, and the next concrete action.

## 100% Rule
A subsystem is 100% only when its implementation, integration, runtime behavior, and required validation evidence are complete for its defined scope. A code checklist marked 100% alone does not make the game 100%.

## YOLO Rule
When the owner authorizes YOLO and is away from the screen, proceed autonomously within the approved 147VR scope. Do not pause for cosmetic decisions. Stop only for destructive/irreversible risk, missing authorization, a true tooling timeout/failure that prevents safe execution, or a decision that materially changes project direction.

## Reporting Rule
During autonomous execution, report only when: (a) a defined framework reaches 100%, (b) a major milestone is completed, (c) Commander genuinely times out/fails, or (d) owner decision is required.

## Safety
Never claim work was performed unless the actual project was changed and/or verified. Preserve existing working systems and use additive, reversible changes where possible.

## Windows Commander Quoting Rule

For every Windows Commander command, follow this quoting policy strictly:

1. **Preferred:** for long commands or any path containing spaces, write the command to a temporary `.ps1` script first, then run it with `powershell -NoProfile -ExecutionPolicy Bypass -File .\temp_task.ps1`.
2. **Inline fallback:** use `powershell -NoProfile -Command "& { ... }"` and wrap paths containing spaces with single quotes.
3. Avoid `cmd.exe` for commands containing PowerShell single-quote syntax.
4. A quoting/tool execution error is a tooling failure, not evidence of a project failure; diagnose it separately.

## Execution Timeout Rule

All autonomous Commander work must prefer asynchronous/background execution for potentially long operations. Do not synchronously wait on broad searches, builds, process scans, or other heavy commands.

Use `$env:TEMP` for background output when practical because it is an OS-managed writable location. If project-local logs are required, use an absolute project path and create the directory first; never rely on relative paths in background jobs.

Every long-running background task must write deterministic output to a known log file and be inspected incrementally with `read_file`/process-output reads.

Use `-NoProfile -NonInteractive` and appropriate non-blocking/error-handling flags for unattended PowerShell work. Do not use flags blindly when they would hide a failure that must be diagnosed.

Limit searches and code inspection to the narrowest relevant project subdirectories. Avoid recursively scanning the Unity project root when `Assets/Scripts/...`, `Assets/Scenes/...`, or another focused path is sufficient.

For builds/tests/runtime validation, launch asynchronously, record PID/log path, poll status, then inspect the resulting log. A timeout of the Commander wrapper must not be confused with the background task's actual state.

## Quoting Implementation Note

Do not nest here-strings and escaped PowerShell expressions inside an inline `-Command` when a script file can be written directly. Prefer `write_file` to create the `.ps1`, then launch that file with `-File <absolute path>`. This is the canonical path for reliable unattended Commander execution.

## Unity Batch / UPM Recovery Rule

When a Unity Batchmode run fails because Unity Package Manager IPC times out, first verify the machine has been cleaned of zombie Unity/UPM processes and stale socket state.

Before retrying, inspect and, when safe, remove stale `Library/EditorInstance.json` and project `Temp` state. Do not delete active project data while Unity is running.

For compile, editor-method, or tests that do not require Package Manager startup, prefer `-noUpm` to avoid unnecessary UPM IPC dependency.

Always provide an explicit absolute `-logFile` under `$env:TEMP` for unattended Unity runs and inspect the log after the process exits.

Never treat a UPM infrastructure failure as a code failure. Record it as an environment/tooling blocker and retry with the recovery procedure before changing gameplay code.

## PowerShell Script Transport Rule

Never use a PowerShell Here-String inside a Commander wrapper for autonomous project work. Here-String terminators are vulnerable to JSON/MCP newline and column-position transformations.

Preferred transport is file-based: use Commander `write_file` to create `$env:TEMP\run_unity_task.ps1`, then invoke it with `powershell.exe -NoProfile -NonInteractive -ExecutionPolicy Bypass -File "$env:TEMP\run_unity_task.ps1"`.

Base64 `-EncodedCommand` is an approved fallback for short cases where file transport is impractical. Array-join strings are acceptable only for short scripts.

This rule supersedes ad-hoc Here-String construction in wrapper commands.

## UPM Batch Validation Rule

The `-noUpm` flag must NOT be used for Unity Batchmode validation when project compilation depends on UPM-managed packages such as UGUI/Input System.

For normal package-aware validation, omit `-noUpm` and allow `Packages/manifest.json` dependencies to initialize normally.

Use `-noUpm` only when the specific task has been verified not to require Package Manager packages. If a missing-assembly result appears only under `-noUpm`, treat that as a likely environment/launch-mode artifact and retry package-aware before modifying source.

## UPM IPC Extended-Wait Rule

For Unity environments where UPM startup may exceed the default IPC wait, an extended UPM timeout may be supplied through the environment before launching Unity:

`$env:UNITY_UPM_TIMEOUT = 120`

This is an environment-level mitigation and must be verified from the resulting Unity log; do not assume that setting the variable guarantees a 120-second Unity internal timeout unless the editor/version honors it.

Use the normal package-aware Batchmode invocation (do NOT combine this mitigation with `-noUpm` when packages are required).

Recommended unattended pattern:
`powershell.exe -NoProfile -NonInteractive -ExecutionPolicy Bypass -File "$env:TEMP\run_unity_task.ps1"`

The task script should set `UNITY_UPM_TIMEOUT=120`, launch Unity with `-batchmode -nographics -quit -projectPath <absolute-project-path> -logFile <absolute-TEMP-log>`, and record the final exit code. Monitor the log asynchronously.

## Multi-AI Collaboration Protocol

The project may use multiple AI specialists, but they share one execution lock and one project source of truth.

- `Docs/AI_TEAM/LOCK.md` controls Unity execution ownership.
- `Docs/AI_TEAM/CLAUDE_STATUS.md` is Claude's persistent handoff/status channel.
- `Docs/AI_TEAM/TECH_CONSTRAINTS.md` defines cross-domain technical boundaries.
- `Docs/AI_TEAM/ART_BACKLOG.md` contains visual/art work queued for execution.

### Roles
- LUNA: Lead Developer / primary Unity Executor / Physics authority.
- Claude: Visual / Art / Presentation specialist; turn-based and file-mediated collaboration.
- DeepSeek-V3: External technical reviewer/coach; review input may be recorded as guidance, but project files remain authoritative.
- Owner: final authority for project direction and irreversible decisions.

### Handoff
AI agents do not need owner-mediated copy/paste for routine status exchange. They read/write the shared AI_TEAM documents when invoked. Claude is not a persistent background agent and must be explicitly invoked to process new handoffs unless a separate automation is intentionally installed.

### Shared-Zone Rule
Scene setup, prefabs, rendering settings, build configuration, and any change touching physics-sensitive bindings require the Unity lock and explicit cross-domain handoff.

### No Parallel Unity
No two AI agents may open Unity or perform Unity-sensitive writes simultaneously, regardless of domain ownership.

## Canonical Unity Launch Tool (2026-09-06) — supersedes ad-hoc PATH/env steps

Root cause confirmed 2026-09-06: repeated "Package Manager could not connect to
IPC stream after 30 seconds" failures (28 Aug, 29 Aug, 5 Sep) trace to the same
condition — the calling shell's inherited PATH contains an `npx` cache entry
(`...\AppData\Local\npm-cache\_npx\<hash>\node_modules\.bin`), which breaks the
Unity <-> UnityPackageManager.exe child-process handshake. This is an
environment/tooling defect, not a project/gameplay defect, and it has cost
multiple sessions of repeated diagnosis.

**Fix:** `Docs/AI_TEAM/Tools/Unity_Safe_Batch_Launch.ps1` is now the canonical,
mandatory entry point for every future Unity Batchmode launch by any AI
(Claude or LUNA) or the owner via Desktop Commander. Do not hand-assemble
`Start-Process`/PATH/env steps again; use this script.

The script: verifies no other Unity.exe is already running (refuses to launch
otherwise, honoring "One Unity Executor at a time"), strips any
`npm-cache`/`_npx` PATH entries before launch, sets `UNITY_UPM_TIMEOUT=120`,
writes an absolute log under `Docs/AI_TEAM/UnityLogs/`, polls for completion
instead of blocking, and writes a short SUMMARY.txt (IPC connected/failed,
compile errors found, exit code) so the caller does not have to grep the raw
log.

This does not remove the requirement to read `Docs/AI_TEAM/LOCK.md` first —
the script only guards against a literally-already-running Unity.exe; it does
not know or enforce who currently holds the execution lock.

Verification note: syntax-validated (0 parser errors). NOT yet execution-
verified end-to-end because Unity.exe was already running interactively on
the host when this tool was built (2026-09-06) — running it then would have
violated the same "One Unity Executor" rule it exists to protect. First real
user must confirm one successful run and update this section with the result.
