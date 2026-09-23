# COACH WORK ORDER #004 — Return to the 147 VR integration line (controlled provenance)

**Issued by:** Coach seat · **Date:** 2026-09-23
**Executor:** Luna · **Decision owner:** the project owner (Coach does not authorize merges)
**Predecessors:** `COACH_WORKORDER_003_D3A_CREATION_20260923.md` · `COACH_REVIEW_002_UPM_INSTALL_ARCHITECTURE_20260923.md`
**Trigger:** B1 / D-3a now reported PASS. Toolchain work stops here unless a new failure is proven.

---

## 0. VERDICTS ON THE D-3A RESULT

| Item | Verdict |
|---|---|
| A1 (`launcher + -projectPath + -createProject`) | **INVALID VECTOR — correctly discarded.** Log said `Creating project folder:  failed.` — the single changed variable in A2 was the creation syntax |
| A2 native `-createProject C:\Temp\147VR_D3A_EMPTY_A2` | **PASS** |
| L3 (empty project: UPM connect → package resolution → compile → clean shutdown) | **PASS** — `*** Tundra build success`, `AssetDatabase: script compilation time: 0.688912s`, `Exiting batchmode successfully now!`, return code 0, no residual Unity/UnityPackageManager process |
| L2 endpoint coverage | **PASS 3/3** — `project:list-packages --> 200` · `packages:get-all-packageinfo --> 200 (2668 ms)` · `config:project:get-registries --> 200 (4 ms)` |
| IPC connect latency | **0.4 s** (`Connected to IPC stream "Upm-5652" after 0.4 seconds.`) and **0.0 s** in the L2 run |
| Licensing | **No block.** `Product: Unity Personal / Type: Assigned / Expiration: Unlimited`; the `Code 10 while verifying Licensing Client signature` line is followed by a successful entitlement resolve ⇒ **not** to be read as a failure on its own |
| Project identity | `6000.4.12f1 (3ca267ce8005)`, `Assets` empty, no 147 VR material used |
| Cleanup | probe `.cs`/`.meta` removed; no residue, no orphan processes |
| **B1 / D-3a** | **PASS** |

**Two consequences worth recording:**

1. **S3 (the 2026-09-16 case where IPC took ~218 s) did not recur** — 0.4 s and 0.0 s here. S3 therefore remains *unexplained but episodic*, not structural. Keep it in the register; do not retro-fit it to a cause, and do not use it to justify future "it's just slow" assumptions.
2. **The withdrawn "no IPC after ~302 s" observation is now formally closed** by this PASS, not merely withdrawn.

### 0.1 One open gap in an otherwise clean PASS — L2 reproducibility (**F9b**)

The L2 evidence was produced by an **in-editor probe that was then deleted**, and `GetRegistries()` **does not exist in the public `UnityEditor.PackageManager.Client` API** — the probe had to reach `UnityEditor.PackageManager.UI.Internal.UpmRegistryClient` and call it through an initialized service instance.

That is a legitimate method and the 200s are real runtime evidence. But **as it stands, L2 cannot be re-run by anyone from the repository.** Per the F9 discipline, one of these must exist on a tracked path:

- **(preferred)** a minimal probe script committed under `Docs/Tools/` (or `Assets/Editor/` behind a `#if` guard) with the exact call, plus the `upm.log` excerpt; or
- **(minimum)** the probe source, verbatim, recorded as an artifact together with the log excerpt and the environment/argv header.

Also record the finding itself in the state docs, because it changes future method: **L2 (`get-registries`) is not reachable through the public package-manager API on 12f1.**

**Severity:** does not invalidate L3 or the gate; does block "we can re-verify L2 on demand". Fix it in the same cycle as S1 below, while the empty project is still at hand.

---

## 1. THE FOUR RULES BEFORE THE REAL PROJECT IS TOUCHED

### R1 — Declare the integration line (F7). Owner decision.

Today three trees exist:

| Tree | Where |
|---|---|
| production | `1da8ed55` on `fix/008-prop-floor-height-20260922` |
| validated history | `7cf232e` (`checkpoint/147vr-apk-clean-publish`) |
| baseline | `24cc95a` (`main`) |

Exactly one must be declared **the** integration line, and final shipping validation must happen there. Coach presents the options and their trade-offs — **the owner decides; Luna does not choose silently.**

| Option | Shape | Trade-off |
|---|---|---|
| **A** `integration/008-m53` from `7cf232e`, then merge the 008 line in | carries the APK/M5.3 integration history forward | the merge itself is the risk surface; must be a clean, reviewable merge |
| **B** `integration/008-m53` from `1da8ed55`, then merge the APK line in | production tree is already here; smallest delta from what is running | the M5.3 integration arrives as a merge rather than as ancestry |
| **C** rebase one line onto the other | linear history | rewrites SHAs of work other agents have referenced — **Coach advises against** unless the owner explicitly wants linearity |

Record the decision in `Docs/147VR_CURRENT_STATE.md` by name (branch + tip), so a future session cannot guess.

### R2 — The editor-version change goes first, alone (F8)

On the integration line, **before any functional work**:

1. Commit `ProjectSettings/ProjectVersion.txt` (`6000.4.4f1` → `6000.4.12f1`) as **its own commit**, with no other file.
2. In the same commit message or an adjacent doc, record the **lock dispositions**: V007 baseline, M5.1 REAL Physics, M5.2 Event Contract, M5.3 rules core, D8/W1 geometry — each marked *expected-unchanged* or *requires re-certification*.

Rationale, and this is the whole point: the **first** 12f1 open re-serializes locked state and produces a diff that git reports as a change with no semantic meaning. Declaring intent **before** the open is what makes that diff auditable **after** it.

### R3 — Capture the first-open diff as evidence

Before opening: record `git hash-object` for at least `Assets/Scenes/147VR_MainScene.unity`, `ProjectSettings/ProjectVersion.txt`, `Packages/manifest.json`.
After the first 12f1 open+quit: record the same values and `git status --short` / `git diff --stat`.
**Then:** either the hashes are unchanged (record "no re-serialization"), or the change set is captured as a named artifact and reviewed **before** proceeding. Do not proceed on an unreviewed first-open diff.

Reminder from the launcher: `-logFile` must be an **absolute** path, and the launcher appends `-quit` for non-test runs — so **exit 4 can occur on a healthy run** (documented stock-`-quit` quirk). Judge by the co-occurrence rule, never by exit code alone.

### R4 — No further toolchain work without a proven new failure

No launcher edit (F11 stays resolved by B′), no system environment change, no Hub route, no new repo-resident scripts except the F9b probe. If a *new* toolchain failure appears, it opens as its own investigation with its own evidence — it does not silently expand this work order.

---

## 2. EXECUTION ORDER (each step = its own evidence set)

| # | Step | Acceptance |
|---|---|---|
| **S1** | Open the **integration line** with 12f1 through the tracked launcher (B′ `-UnityExe` override), absolute `-logFile`, no code change | Editor opens, UPM connects, packages resolve, no unexpected file mutation vs the R3 baseline |
| **S1b** | Commit the **F9b probe** + `upm.log` excerpt so L2 is reproducible | probe on a tracked path; re-run reproduces the three 200s |
| **S2** | **Phase 2 rules validation on the integration line** — legal/illegal, foul, ball-on, frame end, match | rules engine behaves on the tree that will ship; artifacts committed |
| **S3** | **Fresh current-profile M4.2 re-capture** | M4.2 stops being "historical"; Goldens repointed with timestamps |
| **S4** | **Phase 3 / 008 integration, non-destructively** | containment proven by the corrected criterion — strip `147VR_PROPS_ROOT` from both versions and compare as parsed `fileID→block` maps, not by file size or line count — then the V007↔008 decision is recorded |
| **S5** | **REAL10 / M5 re-run** on the integration line | runtime PASS on the shipping tree |
| **S6** | Close **GATE 4** (see the remaining-work map §6) | all GATE 4 acceptance items evidenced |
| **S7** | Then, and only then, the **device gate** (GATE 5) | Quest 2/3 dev APK install → run → full loop → perf baseline → 30-min stability |

---

## 3. EVIDENCE REQUIREMENTS (F9) — applies to every step

Every artifact carries: `WHO` · `WHAT` · `WHEN` · **`WHICH COMMIT`** · `UNITY_EDITOR` (exact version+revision) · `UNITY_PACKAGE_MANAGER_SHA256` · `RUNTIME_TARGET` · `TARGET_TREE` + `TARGET_TREE_HEAD` + `TARGET_TREE_BRANCH` · `RESULT`. Format `.txt`/`.json` on a tracked path — **never `.log`**.

**INVALID, not FAIL** — a run is invalid if any of: argv not captured · CWD/PATH not captured · `-logFile` not absolute or `-` · wrapper bound shorter than the pre-registered observation window · the tree/HEAD was not recorded. Re-run it; do not record it as a failure.

---

## 4. PROHIBITIONS

Do not touch: certified core (V007 / M5.1 / M5.2 / M5.3 / D8 / W1) · `147VR_M53_VALIDATE` · the empty project as a surrogate for the real project · system environment · firewall · Windows settings · the tracked launcher · `Packages/` or `ProjectSettings/` beyond the R2 version commit · scenes during the toolchain/editor step.

**STOP conditions** (previous contract §14): unexpected file mutation · unexpected scene diff on first open · a second physics authority · a certified asset touched · unknown auto-executing script · dirty state of unknown origin · unclear tree topology · missing evidence provenance. On any of these: stop and report — do not "continue and check later".

---

## 5. WHAT COACH DOES NOT DO

Coach presents trade-offs, evidence, risks, reversibility and required validation for R1 — and **does not choose**. Merges, lock re-declarations and any re-certification are owner decisions; independent verification of the final shipping tree remains a separate, later gate.

---

## 6. COACH'S OWN DEFECT, DISCLOSED

`Docs/AI_TEAM/COACH_ARTIFACT_INDEX_20260923.md` as pushed contains a **self-referential row**:

```
| `COACH_ARTIFACT_INDEX_20260923.md` | `5a19335f4a20f5fea6ed81d5ed4d7a545daed0701ad6f9e7190c49249149ee29` |
```
while the file's actual canonical sha256 is `ebd67c11ce54646460226f526db8970519b1e717891d90e477f724c603b95a91`. (The row was written while the index was still being generated.)

The other **8 rows are correct** — independently re-verified against the pushed blobs at `ef6ecf65` (8/8 matched). The bogus row is removed and the index re-issued in the same commit that adds this work order. This is recorded rather than quietly fixed, because an index row that cannot be verified is exactly what the F9 discipline exists to prevent.
