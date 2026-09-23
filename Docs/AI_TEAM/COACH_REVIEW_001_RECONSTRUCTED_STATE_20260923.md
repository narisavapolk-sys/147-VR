# COACH REVIEW #001 — RECONSTRUCTED CURRENT STATE + UPM FRONTIER

**Reviewer seat:** Coach (per `COACH_OPERATING_CONTRACT_147VR.md`)
**Date:** 2026-09-23
**Input:** `COACH_BRIEF_MEMORY_RESET_RECOVERY_20260923.md` (owner brief, Part A + Part B/Step-2 closure)
**Method:** read-only. Git objects via `git ls-remote` + depth-2 fetch; committed docs/artifacts from the repo mirror; issue bodies #6–#12.
**Not available to this seat:** the Windows host (`wIn-NaRIs`), Unity, Blender, the live processes. No claim below depends on those unless explicitly marked **OWNER-PROVIDED**.
**Files modified during this review:** none. No issue created.

---

# PART A — "RECONSTRUCTED CURRENT STATE"

## A1. CONFIRMED — re-derived from committed evidence in this review

| # | Statement | Evidence I actually read |
|---|---|---|
| A1.1 | Issue #7 **is pushed**. `refs/heads/fix/008-prop-floor-height-20260922` = `1da8ed55e269d07bd2d5b7b059eb4c0eb6d35856` | `git ls-remote --heads` on origin |
| A1.2 | `1da8ed55` parent = `cc480a1012333d7ef41e5fee24efa92c41c2553e` (the 008 line tip) | `git cat-file -p 1da8ed55` |
| A1.3 | `1da8ed55` changes **exactly one file**: `Assets/Editor/Stage008Props.cs` (+20 / −8), two hunks only | `git show --stat` + full diff |
| A1.4 | The patch **matches its pre-registered acceptance**: `const float FloorY = 0f;` removed → `const float BedAboveFloor = 0.827f;`; `float floorY = surfaceTopY - BedAboveFloor;` derived in `Stage()`; a `floorY DERIVED = …` log line added; REST / CUERACK / TRIANGLE / SCOREBOARD now use `floorY`; **CHALK line untouched** (appears as context, still `railTopY`); `x`/`z` expressions unchanged | the actual diff |
| A1.5 | Luna's six prior fixes are not touched (no third hunk, no second file) | diff scope check |
| A1.6 | 008 read-only measurement artifact: **10 assertions, 9 PASS, 1 FAIL** — the failure is `ASSERT_NO_NEGATIVE_SCALE` | `Artifacts/M5_3/008_Measurement/m53_008_measure.txt` lines 39–49 + `.json` lines 337–346 |
| A1.7 | M5.1 is certified **with a committed certificate**, quoting `[M5.1 INSTALL PASS] lifecycle=-3526 contract=-3528 ballTracker=True scene=147VR_MainScene` and verifying scene-YAML serialization of `M5ShotEventContract` / `M5ShotLifecycle` | `Docs/AI_TEAM/COACH_CERTIFICATION_M5_1_20260908.md` |
| A1.8 | The env-guard contract exists and is **ACTIVE**, with an explicit *healthy signature*: `config:project:get-registries = 200`, `project:list-packages = 200`, `packages:get-all-packageinfo = 200`, no `Received undefined` | `Docs/147VR_UPM_ENV_GUARD.md` |
| A1.9 | 2026-08-28 root cause was **verified** as missing `PROGRAMDATA` / `ALLUSERSPROFILE` / `TMP`; **and its signature was IPC-started-with-HTTP-500**, not a startup crash | `Docs/147VR_UPM_CHILD_PROCESS_INHERITANCE_FIX.md` |
| A1.10 | 2026-09-16 signature: args `server -s 10328 --ipc-path Unity-Upm-10328 -l 2`; **IPC started at +218 s**; UPM then quit because the parent had exited; classification `INFRASTRUCTURE BLOCKED / INCONCLUSIVE`, Rules NOT REACHED | `Docs/147VR_UPM_IPC_INCIDENT_20260916.md` |
| A1.11 | The repo still declares **Unity 6000.4.4f1**; the 12f1 authority appears nowhere in committed config | `README.md` + `GATES_STATUS`/`PRODUCTION_READINESS` references to `ProjectVersion.txt` |

**Not re-derived by me** (documented by Coach's own earlier review with reproducible commands, issue #8 §1/Appendix A): the `merge-base` result and the 24-vs-2 commit counts that establish the divergence. I did not re-run those two commands in this review; treat them as **cited**, not re-confirmed.

## A2. OWNER-PROVIDED — measured on the machine; this seat cannot verify

- 12f1 UPM identity: path `C:\Temp\UnityEditors\6000.4.12f1\Editor\Data\Resources\PackageManager\Server\UnityPackageManager.exe`, SHA-256 `95FDDF582FCC653C2E90D4E21E370BF3E6DCEBC891E731BCB3A878EC4412BEE6`, size `95,112,112`
- 12f1 `Unity.exe` SHA-256 `62439E457048ED6EA887A8536DC0B4329C64BBEE507C906ECBBFCE4C544FF6C6`
- 4f1 UPM control SHA-256 `8F9C6D223CE5FC5154BECD8B58312BBD76B36C0618F4FC721119D0CC5BF9D14E`
- Step-2 run results: empty-project 12f1 = UPM connect timeout at 30 s; direct execution = starts normally; **unguarded** = exits ~1.3 s with `TypeError [ERR_INVALID_ARG_TYPE] … getLocalConfigFolder → readConfig → initialize`; **guarded** = alive ~302 s, no `IPC server started`, no stderr, no leftover process

**Corroboration available:** the owner's 4f1 control hash `8F9C6D22…9D14E` **matches the committed 2026-09-16 record byte-for-byte**, so at least that value is consistent with a committed artifact. The 12f1 values have no committed counterpart yet.

## A3. UNKNOWN

- Whether the guarded standalone run produced a `upm.log` at all, and what it contains — **the single decisive artifact for this failure class is missing**
- The exact argument vector, working directory, and effective environment of the guarded run (nothing in the brief fixes these)
- Whether the IPC named pipe `Unity-Upm-*` ever appeared during the ~302 s
- Whether UPM was blocked internally, waiting on a dependency, or merely slower than the wrapper's bound
- `MissingScripts=0` has no committed decisive log line (F3)
- Committed NUnit XML for 43/43 and 20/20 — still absent (F3)
- Whether the 4f1 Hub install still exists (F11b)
- M4.2 current-profile fresh re-capture — still deferred, runner exists

## A4. CONTRADICTIONS / CORRECTIONS THIS SEAT MUST RAISE

| ID | Raise | Severity |
|---|---|---|
| C1 | **Register hygiene:** issue #8 §6 item 5 ("`1da8ed55` still not on origin") is now **CLOSED** (A1.1). If it stays open it will keep being re-chased. | LOW |
| C2 | Brief §8 says the current unguarded failure *"is the same class as the historical 2026-08-28 UPM environment/config failure."* Same error string, same function (`getLocalConfigFolder`), **but a different stage and different severity**: 08-28 = UPM alive, IPC up, API returning HTTP 500 *inside a request*; now = UPM dies at `initialize()` *before any IPC*. This is a **HYPOTHESIS**, not a classification. | HIGH |
| C3 | Brief §"verdict" says *"Variant B **confirmed** as an environment-inheritance trigger"*, while the same brief contains no `upm.log` and no API-call signature. Per the project's own pre-registered discriminator (issue #8 §4), classification requires `upm.log`. Recommend rewording to *"consistent with Variant B; not yet classified"*. | HIGH |
| C4 | `Docs/M5_AUTHORITY_CHAIN_STATUS.md` (main) states *"M5.1, M5.3–M5.6 are not declared CERTIFIED by this file"*, while M5.1 **is** certified (A1.7) and M5.2 is certified by the 09-08 batch evidence. Not a real conflict — different dates and scopes — but under F8 every lock needs an explicit disposition line, or a future reader will misread the lock state. | MED |
| C5 | Documentation defect: `147VR_UPM_CHILD_PROCESS_INHERITANCE_FIX.md` says *"Do NOT use the old `C:\Users\mongo\UnityProjects\147 VR` Hub alias. The authoritative project path is `C:\Users\mongo\UnityProjects\147 VR`"* — the "old alias" and the authoritative path are the same string (the retired alias was `C:\147VR`). Path authority matters here; fix the sentence. | LOW |
| C6 | Two assertion counts are in circulation for the 008 read-only gate and must never be conflated: **measure artifact = 9/10** (A1.6) vs **props manifest = 12/12** (referenced in the `Stage008Props.cs` header). Always label by artifact. | LOW |

---

# PART B — UPM FRONTIER ANALYSIS

## FINDING

**F-B1 (HIGH) — "survived ~302 s" is a wrapper-bounded observation, not a property of UPM.**
The brief states the guarded wrapper "completed after ~302 s" and that no `UnityPackageManager` remained. That is consistent with the wrapper reaching its own bound and terminating the child. Therefore the experiment **cannot distinguish** *"UPM will never start IPC"* from *"UPM starts IPC later than the wrapper's bound."* The 09-16 precedent is precisely the second case (IPC at +218 s). As recorded, the run has **no discriminating power** on the main question.

**F-B2 (HIGH) — "no stderr / no error" is not evidence of a clean run.**
Every prior classification of this failure class was made from **`upm.log`**, never from stdout/stderr: 08-28 (`IPC server started` PASS + three HTTP 500 lines) and 09-16 (`IPC server started` at a timestamp). UPM's diagnostics go to `upm.log`. If the standalone guarded run captured only stdout/stderr, **nothing about UPM's internal state was observed.** The project's own rule (issue #8 §4 preamble) is: *read `upm.log` before doing anything else.*

**F-B3 (HIGH) — the classification is open, and the current failure is a *different stage* from 08-28.**
See C2/C3. The unguarded crash is **fatal at startup** (`initialize()`), whereas the 08-28 defect was **non-fatal and in-request**. A fatal startup crash means the env guard is being asked to fix a stage it did not previously need to fix — so "guard applied" passing or failing tells us about *that* stage only.

**F-B4 (MED) — first-order variables were not held constant.**
The 09-16 known invocation is `server -s 10328 --ipc-path Unity-Upm-10328 -l 2`. The brief never states the guarded run's arg vector or CWD, nor whether `--ipc-path`/`-l` were supplied. Comparing the two runs without that is not a controlled comparison.

**F-B5 (HIGH, constructive) — a proven launch path already exists in the repo and does not require winning the 30-second race.**
`Docs/147VR_CURRENT_STATE.md` (2026-09-20 entry) records: *"UPM workaround used for verified unattended runs: explicit Windows environment values plus a pre-started UnityPackageManager IPC server and `-upmIpcPath`. This is now a proven launch path for this installation."* Note also that the same `-upmIpcPath` injection was **rejected with exit code 1** in the 08-28 era and **worked** by 09-20 — so it is environment-sensitive and must be validated, not assumed. If it transfers to 12f1, root-causing the bare standalone launch becomes a **deferred** task rather than the gate.

**F-B6 (MED, new variable class) — the 12f1 binaries are new, never-executed, 95 MB, and live under `C:\Temp`.**
Nothing in the incident register covers *first-execution* effects on a freshly placed binary: MOTW/Zone.Identifier, on-access AV scan, or cold filesystem cache. These are well-known causes of **multi-minute** first-launch stalls and are consistent with both the 09-16 218 s anomaly and the current >300 s silence. This is a HYPOTHESIS with a cheap discriminator (below), not a conclusion.

## INTERPRETATION

The current evidence supports exactly one statement: **the 12f1 UPM binary is executable; a guarded launch changes the failure mode from a fatal `initialize()` crash to silence.** It does **not** yet support any statement about *why* IPC never appeared, and it does not yet reproduce either documented variant's signature. The brief's own verdict sentence ("Variant B confirmed") over-claims relative to its evidence.

A second, procedural reading matters more for schedule: the team may be spending machine time re-deriving a launch recipe that the repository says was already proven on 4f1 (F-B5). That should be tested **before** further root-cause work.

## UNKNOWN

U1 arg vector / CWD / effective env of the guarded run
U2 existence + content of `upm.log` for that run
U3 whether the IPC endpoint appeared during the ~302 s
U4 internal block vs. dependency wait vs. simply slow
U5 behaviour beyond the wrapper's bound
U6 whether `-upmIpcPath` is still accepted by 12f1
U7 whether the 4f1 Hub install still exists (rollback anchor)

## NEXT DISCRIMINATOR — smallest set, in order, one run each

**D-1 (one launch, read-only, decisive for U1–U5).** Repeat the *identical* guarded launch with five instrumented additions:
1. no wrapper timeout (or a bound ≥ 900 s);
2. `upm.log` **located and captured** (search the project `Library/`, the UPM local-config folder, `%TEMP%`, and the CWD — record whichever exists, or record that none exists);
3. named-pipe probe sampled every ~5 s: `Get-ChildItem \\.\pipe\ | Where-Object Name -like '*Upm*'` → timestamps when the endpoint appears/disappears (this answers "did IPC start?" independently of any log);
4. two CPU-time samples of the live PID ~60 s apart → idle vs spinning vs blocked;
5. exact command line + executable path + CWD + full env dump taken **in the same process** that spawns UPM.
→ Output is one `.txt` artifact under a tracked path (F9), with the standard header. This single run reclassifies or closes U1–U5.

**D-2 (one launch, unchanged).** Re-run D-1 as-is and compare time-to-IPC. Fast second run ⇒ cold-start/MOTW/AV (F-B6). Identically slow ⇒ structural.

**D-3 (bypass test).** On an **empty/controlled project only**, validate the repo-proven recipe against 12f1: guarded env + pre-started UPM + Unity `-upmIpcPath`. If Unity 12f1 accepts it → the gate is bypassed without needing root cause.

**D-4 (cheap, parallel).** First-execution forensics on the 12f1 binaries: `Get-Item -Stream *` for Zone.Identifier, Defender scan history/events, and a file-hash-time comparison of first vs second execution.

## RISK

- **R1** Repeating bare guarded launches without capturing `upm.log` looks like progress but produces no classification — the same trap as reboot-roulette, which §9 of the brief forbids by name.
- **R2** Testing `-upmIpcPath` against the **real** project before validating it on a controlled project risks Unity writing `ProjectSettings`/`Library` on a locked-adjacent surface. Keep D-3 on an empty project.
- **R3** Reading "guarded = 302 s, no IPC" as "the guard failed" invites the two forbidden responses: 4f1 rollback and system-wide env edits.
- **R4** Chasing the bare-launch root cause while an already-proven bypass exists (F-B5) risks burning the session on a non-gating problem.
- **R5** All of D-1…D-4 are **Luna** actions on the host. This seat cannot run them and must not simulate their results.

## ACTION

1. **STOP** before classifying: per §14 (evidence provenance missing) the current UPM state is **UNCLASSIFIED**, not "Variant B confirmed". Re-word any document that says otherwise.
2. Run **D-1** once, capture `upm.log` + pipe timeline + PID CPU delta + arg vector, commit as a tracked `.txt` artifact with header + sha256 (F9).
3. Run **D-2** immediately after D-1, unchanged, and compare time-to-IPC.
4. Prepare **D-3** on an empty project; do not touch the real project or the launchers (F11) until it is validated.
5. Close issue #8 §6 item 5 (C1); add disposition lines for M5.1/M5.2/M5.3 locks (C4); correct the path sentence (C5).
6. Do not modify: Main Scene · M5 Rules/Scoring/Turn · V007/Golden · D8/W1 · safe launchers.

---

## STOP-CHECK against contract §14

| Condition | Triggered? | Consequence |
|---|---|---|
| evidence provenance missing (UPM classification) | **YES** | classify as UNCLASSIFIED; request `upm.log` — done in ACTION 1 |
| branch topology unclear | NO | topology confirmed at the tips (A1.1–A1.2); divergence counts cited, not re-derived |
| certified asset touched | NO | Issue #7 touches one editor tool file only (A1.3–A1.5) |
| second physics authority | NO evidence | no physics file in Issue #7's diff |
| test environment ≠ authority | **YES, latent** | the 12f1 authority is not reflected in committed config (A1.11, F8) — record the editor revision in every artifact header as already specified |

## Register update from this review

| ID | Status change |
|---|---|
| F1 (evidence chain) | **OPEN** — new instance pending: the guarded UPM run's `upm.log` is not yet a tracked artifact |
| F3 | **OPEN** — unchanged |
| F4 (Quest hardware) | **OPEN** — unchanged |
| F5 (~3 GB aborts) | **OPEN** — unchanged, plausible relation to first-run/AV/IO pressure is a HYPOTHESIS only |
| F7 | **OPEN** — divergence confirmed as cited; item 5 of issue #8 §6 **CLOSED** (C1) |
| F8 | **OPEN** — required disposition lines still missing for M5.1/M5.2/M5.3 (C4) |
| F9 | **OPEN, actionable now** — `upm.log`/run artifacts must land in tracked `Artifacts/` |
| F10 | **OPEN** — refine into named signatures: S1 08-28 (IPC up + HTTP 500), S2 09-08 (spawn stall), S3 09-16 (IPC at +218 s), **S4 current (unguarded = fatal `initialize()` crash; guarded = no IPC, unclassified)** |
| F11 / F11b | **OPEN** — untouched, deferred by brief §10 |
| F12 | **OPEN** — EOL ambiguity unchanged |
