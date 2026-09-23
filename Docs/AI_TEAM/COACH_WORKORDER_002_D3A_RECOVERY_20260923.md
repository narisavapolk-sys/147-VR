# COACH WORK ORDER — D-3a recovery, F11 elevation check, and the `Received undefined` criterion correction

**Issued by:** Coach seat (per `COACH_OPERATING_CONTRACT_147VR.md`)
**Date:** 2026-09-23
**Executor:** Luna (this order contains no action Coach can perform)
**Predecessors:** `COACH_BRIEF_D3_EMPTY_PROJECT_20260923.md` · `COACH_REVIEW_002_UPM_INSTALL_ARCHITECTURE_20260923.md`
**Scope:** unblock D-3a. No migration, no real project, no certified core.

---

## 0. VERDICTS ON WHAT YOU REPORTED

| Your item | Coach verdict |
|---|---|
| **D-3.0 `-s` semantics = PASS** | **Accepted.** Two-case discriminator, clean result, and it proves more than stated (see §0.1). |
| Round without env guard → `Received undefined`, excluded from D-3.0 evidence | **Correct call.** That run is excluded. It also confirms the unguarded signature is **reproducible** (2nd occurrence) — it is now a stable signature, not noise. |
| **D-3a = INVALID / creation blocker**, not reported as PASS | **Correct call**, and it is the behaviour the contract demands. |
| Not calling D-3b until plain vector is proven to fail | **Correct.** D-3b remains **NOT AUTHORIZED**. |

### 0.1 D-3.0 proves a second thing — and it matters for D-3b

In the Alive case the watched PID was `explorer.exe` (7888) — **not the server's true parent**. The server stayed alive regardless.

> **INFERENCE: `-s <pid>` is a *polled liveness token*, not a parent-child relationship.** The server may be watched against any deliberately-created long-lived process.

**Consequence for D-3b:** do **not** use `explorer.exe` as the watched PID. If the shell restarts, logs off, or crashes, the server dies mid-session and the run becomes unattributable. Use a **purpose-created host process you control**, and record its PID + start time + a proof it was alive at the start and end of the Unity session.

---

## 1. YOUR "NO LOG ARTIFACT" BLOCKER IS ALREADY SOLVED — APPLY THE COMMITTED PRECEDENT

This exact symptom class is already in the repo, from 2026-09-07:

> *"Unity is failing before it even opens the `-logFile` target, which usually means the failure is at Editor startup itself, before your batch log path is reached."*
> — `Docs/AI_TEAM/COACH_CHECKPOINT_ONLY_REPORTING_20260907.md`

**The ordered remedy that document already prescribes (apply verbatim, in order):**

1. **Unlike your assumption, Unity does not necessarily leave you nothing:** even when a custom `-logFile` target is never created, Unity writes to its **own default log**:
   - `%LOCALAPPDATA%\Unity\Editor\Editor.log`
   - `%LOCALAPPDATA%\Unity\Editor\Editor-prev.log`
   **Look there before concluding "no artifact".** This is a marked difference from the UPM investigation: the evidence is very likely already on disk.
2. **Check for a licensing failure specifically.** The 09-07 record notes `Unity Licensing: Access token is unavailable` had previously appeared as background noise; if it is now the actual cause of the early exit, `Editor.log` usually says so directly (expired / activation required / no valid licence found). **12f1 has never had a successful unattended run on this machine — a licensing/activation gate for the new editor is a first-class hypothesis, not an afterthought.**
3. **If it is a licence/activation issue needing interactive login → that is the one case worth surfacing immediately.** It is not autonomously resolvable.
4. **If it is the environment/PATH class again:** verify that the env repair + PATH sanitization actually ran for *this* invocation — not bypassed (see §2).
5. Only after 1–4: report as a checkpoint ("blocked on X after trying Y, Z").

**Additionally — one pitfall that manufactures "no log" by design:** the deprecated launcher used `-logFile -`, i.e. **log to stdout**. If any invocation in your chain still does that, *no log file is expected*. Confirm the exact `-logFile` value used, every time.

---

## 2. THE 2026-09-08 DUAL-LAUNCHER PRECEDENT APPLIES DIRECTLY

> *"A deprecated script with **zero PATH sanitization and no single-instance guard** … was what actually got invoked … This exactly matches the failure signature we've been chasing."*
> — `Docs/AI_TEAM/COACH_ROOTCAUSE_CONFIRMED_DUAL_LAUNCHER_20260908.md`

Two rules follow, and they are not optional:

- **Exactly one launcher, and know which one ran.** If a hasty invocation of a second launcher (or a directly-typed command) is mixed into this sequence, the D-3a result is unusable — that is precisely the historical failure mode.
- **No second Unity instance.** Confirm the process inventory is empty before launch (§7) and record it. A live instance can absorb or interfere with a new invocation, which would present as "exit 0, nothing created".

---

## 3. F11 IS NOW ON THE CRITICAL PATH — DECISION REQUIRED (owner)

**Problem.** The standing decision from 2026-09-08 is: *"only ever invoke `Docs\Tools\Unity_Batch_Safe.ps1`"*. But **F11 records that this launcher still points at 4f1**. Therefore:

- Using the hardened launcher for D-3a would launch the **wrong editor** (4f1) — i.e. it would commit the forbidden fallback.
- Hand-writing a fresh 12f1 invocation **recreates the dual-launcher risk** that already cost this project a debugging cycle.

This cannot be resolved by silently picking one. Options, with trade-offs:

| Option | What it means | Trade-off | Reversibility |
|---|---|---|---|
| **A — Elevate F11 now** | Make the tracked launcher **parameterized on the editor path/version**, as a **separate, tracked, reviewable change** (its own commit + artifact). D-3a then runs through the one documented launcher. | Adds a change to the critical path before the UPM question is even closed. Must not silently alter the 4f1 default for existing workflows. | High — one revertible commit |
| **B — Derive, don't fork** | Keep F11 deferred. For D-3a, invoke 12f1 explicitly, but **copy the hardened launcher's env-repair + PATH-sanitization + single-instance logic verbatim**, and record the derivation (source file + blob hash) in the artifact. One invocation, one launcher-equivalent, declared. | The 12f1 invocation is not itself the hardened artifact; it must be reviewed as a *derived* vector. | High |
| **C — Do nothing and hand-roll** | **Reject.** | Repeats the exact cause of the 09-08 incident. | — |

**Coach recommendation: B for D-3a (one shot, declared derivation), with A queued immediately after D-3a closes.** Rationale: D-3a is a *diagnostic* on an empty project; a derived, declared vector is auditable, whereas a launcher change before D-3a widens the commit surface while the question is still open. **This is a recommendation, not an authorization — the owner decides.**

---

## 4. D-3a EXECUTION SPEC (instrumented; hypothesis-driven)

### 4.1 Non-negotiable instrumentation (every Unity invocation in D-3a)
1. **Absolute `-logFile`** (committed convention: `147VR_DESKTOP_COMMANDER_PREFLIGHT.md` — *"Prefer explicit absolute `-logFile` paths for unattended validation"*; `147VR_AI_WORK_PROTOCOL.md` — explicit absolute log under `$env:TEMP`). Never `-logFile -`.
2. **Exact argv as received** (not as intended) + **the launcher/derivation identity** (§3B).
3. **Exit code**, plus process lifetime.
4. **`%LOCALAPPDATA%\Unity\Editor\Editor.log` + `Editor-prev.log`** copied **in addition to** the custom log (§1).
5. **Whether a project directory was created**, with a directory listing before/after.
6. **Process inventory**: before launch and after the run.

### 4.2 Hypotheses and their one-line discriminators (cheapest first)

| ID | Hypothesis for "exit 0, no project, no custom log" | Discriminator (one line) |
|---|---|---|
| **H-a** | A **live Unity instance** absorbed/interfered with the invocation | inventory before launch; if a Unity process exists → stop and re-run in a clean state |
| **H-b** | The log **exists** at Unity's default location (§1.1) | read `Editor.log` / `Editor-prev.log` — if present, this is not "no artifact" at all |
| **H-c** | **Licensing/activation gate** for the never-before-run 12f1 | `Editor.log` states licence state directly |
| **H-d** | **Wrong/derived vector**: wrong editor, `-logFile -`, arguments not reaching the editor | record argv as received; confirm the editor path; confirm the log value |
| **H-e** | **PATH/env** class again, or the env repair bypassed | compare against the hardened launcher's sanitization; confirm it ran |
| **H-f** | Argument semantics differ on **6000.4.12f1** | capture the **version-exact** argument reference of this build (§5.1) |

**Order of work:** H-b (3 minutes, may already answer everything) → H-a/H-d (inventory + argv) → H-c (licence lines) → H-f → H-e. **Do not** re-run creation attempts until H-b has been read.

### 4.3 Decision tree
- If `Editor.log` explains the exit ⇒ treat that as the finding; fix **one variable**; re-run **once**; record.
- If it shows a licence gate ⇒ **surface immediately** (floor case).
- If it shows the editor never reached project creation because it handed off / another instance existed ⇒ clean the state (§7), then re-run once.
- If nothing explains it after the ordered steps ⇒ **checkpoint report**, not indefinite retries.

---

## 5. EMPTY-PROJECT ACQUISITION

### 5.1 Version-authority caveat (provenance)
The reference you cited is the **6000.6** manual. The authority for this work is **6000.4.12f1**. A newer-version manual **is not evidence about this build**. Required: capture the **version-exact** argument reference for the actual editor (the build's own argument reference output, if it supports one) and cite it in the artifact. If only the 6000.6 doc is available, mark it explicitly as *indicative, not authoritative*.

### 5.2 Routes, ranked
1. **Unity Hub creating the project with 12f1 explicitly selected** — vendor scaffold, no hand-made state, `ProjectVersion.txt` is 12f1 by construction. **Preferred.**
2. `-createProject` / `-cloneFromTemplate` **only after** §5.1 gives a version-exact reference and §4.3 has explained the first failure.
3. A **neutral, non-147VR Unity template**, opened by 12f1.
4. **Rejected:** hand-assembling a project from `147VR` `ProjectSettings`/`Packages` files, and any path that carries unknown state into the experiment.

### 5.3 Declaration rule
If route 3 is used **and** the template's project version differs from 12f1, the first open is an **upgrade event**. Record the run as a **declared variant** ("empty project, created at version X, first opened by 12f1"). **Do not merge its results with a pure 12f1-native empty-project run.**

### 5.4 Validity criteria for the empty project (unchanged from the D-3 brief)
Created by 12f1 (or declared variant) · Unity opens it and resolves packages (`Library/` produced) · **no** `Assets/Editor` auto-run or `[InitializeOnLoad]` hooks · **no** `*_TMP.cs` · outside the repo and outside `147VR_M53_VALIDATE` · simple path · **`C:\Temp` must not be cleaned** (it holds the 12f1 editor install).

---

## 6. AMENDED ACCEPTANCE CRITERIA — CORRECTING MY OWN REVIEW #002

I am amending my own criteria. Two committed facts make the Review #002 §4 wording **too broad**:

> *"Package Manager `path argument ... undefined` on stock `-quit` is now a documented, understood quirk — not something papered over or 'fixed' speculatively."* — `Docs/AI_TEAM/COACH_FINAL_SIGNOFF_WPBSA_20260908.md`

> *"Unity's stock `-quit` shutdown path … does not return a clean process code in the Safe Batch wrapper. Compilation itself succeeds, and the controlled post-compilation exit path returns code 0."* — `Docs/AI_TEAM/WPBSA_FINAL_GATE_20260908.md`

**Therefore:**

1. **`Received undefined` alone must not fail a run.** Correct classification is by **co-occurrence**:
   - `Received undefined` **plus** the three endpoints = **200** **plus** compile success ⇒ **documented `-quit`-path quirk** → record it, do not fail. (And state whether that quirk is present on 12f1 or not — that is itself useful information.)
   - `Received undefined` **without** the 200s, or with a UPM connect failure ⇒ **real UPM/config failure**.
2. **Exit code alone must not gate PASS.** Record it, with the documented caveat attached; gate on: **three endpoints 200** + compile success + `Editor.log` showing the connect.
3. Everything else in Review #002 §4 (Levels 0–4, INVALID conditions, the F9 evidence header, "a higher level may never be claimed from a lower level's evidence") stands unchanged.

---

## 7. EXECUTION-STATE RECLAIM (you are right to stop and reclaim)

Your call not to force further commands is correct. Protocol, in order:

1. **Inventory first** (record as an artifact): every `Unity.exe`, `UnityPackageManager.exe`, Unity licensing process, and relevant node process — with **PID, start time, command line, CWD, parent PID**.
2. **STOP rule:** if any process's command line or CWD points at `C:\Temp\147VR_M53_VALIDATE` or `C:\Users\mongo\UnityProjects\147 VR` ⇒ **do not terminate it.** Killing an editor attached to a protected tree can mutate locked-adjacent state. Escalate to the owner instead.
3. If the stuck process is an orphan of the D-3.0 / empty-project choreography and touches **no** protected tree ⇒ terminating it is in-bounds. Re-inventory afterwards to prove a clean state.
4. If the control session itself is wedged, **start a fresh session/terminal** rather than forcing the old one, and **declare which session produced each artifact** so evidence is not cross-contaminated.
5. **Never** clean or delete the `C:\Temp` tree.

---

## 8. EVIDENCE PACKAGE

Unchanged from the D-3 brief: `.txt`/`.json` under a **tracked** path (never `.log`), sha256 recorded, and the same header template including `UNITY_EDITOR`, `UNITY_PACKAGE_MANAGER_SHA256`, `UPM_ARGV`, `UPM_CWD`, `ENV_APPLIED`, `PATH_FILTERED`, `TIMESTAMP`, `RESULT ∈ {PASS, FAIL, BLOCKED, INVALID}` — plus one new mandatory field for D-3a:

```
LAUNCHER : <path + blob hash>  |  DERIVED-FROM: Docs\Tools\Unity_Batch_Safe.ps1 @ <blob sha>
```

---

## 9. PROHIBITIONS AND STOP CONDITIONS (unchanged)

**Do not touch:** `147VR_M53_VALIDATE` · `C:\Users\mongo\UnityProjects\147 VR` · Main Scene · M5 Rules/Scoring/Turn · V007 / Golden / `147VR-PHY-008.asset` · D8/W1 · safe launcher without the §3 decision · system-wide env · firewall · Unity reinstall · `C:\Temp` cleanup.

**STOP on:** unexpected file/scene mutation · unknown auto-run script · second physics authority · dirty state of unknown origin · a second launcher or second Unity instance · any run whose argv/`-logFile` value was not captured · anything that would need a certified system changed to pass.

---

## 10. WHAT COACH WILL DO NEXT

- Audit D-3a artifacts against §4/§5/§6/§8 and issue: `L2/L3 PASS`, `DEGRADED-PASS`, `BLOCKED`, or `INVALID` — naming the missing capture if INVALID.
- On a clean L3 PASS: propose the **L4 migration discriminator** on `147VR_M53_VALIDATE` (preflight `TARGET_TREE_HEAD = 7cf232e` + `PRELAUNCH_SAFE`, dirty state captured as content) plus the **F8 disposition lines** for V007 / M5.1 / M5.2 / M5.3 / D8.
- Coach does **not** authorize production integration (contract §18; F7 governs the integration line).

## 11. REGISTRY DELTA

| Item | Delta |
|---|---|
| D-3.0 | **PASS** — `-s` semantics established (liveness token; not a parent relationship) |
| S4a (unguarded config crash) | now **reproducible** (2 occurrences) — kept as a stable signature |
| **F11** | **elevated to critical path** — the hardened launcher pins 4f1, so D-3a cannot lawfully use it. Owner decision required (§3) |
| F1 / F7 / F8 / F9 / F10 / F12 | unchanged |
| Certified core | unchanged, untouched |

## 12. ONE-LINE SUMMARY FOR LUNA

> Before touching project creation again: **read `%LOCALAPPDATA%\Unity\Editor\Editor.log` and `Editor-prev.log`** — the 2026-09-07 precedent says Unity writes there even when your `-logFile` target is never created, and check the licence lines first. Then reclaim execution state (§7), resolve the F11 launcher question (§3B), and re-run **once** with full instrumentation.
