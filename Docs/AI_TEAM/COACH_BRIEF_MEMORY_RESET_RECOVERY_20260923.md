# MEMORY RESET RECOVERY / 147 VR — BRIEF FOR COACH

**Prepared for:** COACH (independent review agent)
**Prepared by:** Accio (owner's assistant), on behalf of the project owner
**Date:** 2026-09-23
**Delivery rule:** this brief IS the handoff. Do **not** create a new issue, and do **not** modify any file while reconstructing.

---

## 0. INSTRUCTIONS — READ FIRST

Do **not** start a fresh analysis from zero. Do **not** modify any file right now.

**Goal:** reconstruct the current work from GitHub + evidence, then continue from Luna's latest checkpoint.

**Do it in this order:**

1. Read this brief first (all of it).
2. Then inspect GitHub **only** the documents/issues named in §13 (Evidence Index). Do not do a broad sweep of branches — that wastes time and risks mis-reading branch state.
3. Reply with **"reconstructed current state"**: an explicit list of (a) what you can confirm from evidence, and (b) what is still missing/unknown. Do not invent missing state.
4. Only after that step, help analyze the UPM question in §11.

Do **not** try to recover the old chat memory wholesale. The current evidence is **more detailed and more recent** than the prior memory, and it supersedes it where they disagree.

**State-of-play headline:** the current blocker is **UPM BLOCKED**, not M5 BLOCKED.

---

## 1. PROJECT / AUTHORITY

| Item | Value |
|---|---|
| Project | `147 VR` (VR snooker/pool, Meta Quest 2/3, Unity) |
| Source | `C:\Users\mongo\UnityProjects\147 VR` |
| Current declared Unity authority | `C:\Temp\UnityEditors\6000.4.12f1\Editor\Unity.exe` |
| Validation worktree | `C:\Temp\147VR_M53_VALIDATE` |
| Validation checkpoint | `7cf232e` |
| Device / host | `wIn-NaRIs` (Windows, via Desktop Commander) |

**Hard rules:**

- `12f1` = current authority.
- **DO NOT** fall back to `4f1` as authority. `4f1` is **rollback/control evidence only**.
- **DO NOT** open the real project yet.
- **DO NOT** reset/clean the dirty validation worktree.
- **DO NOT** touch D8 / W1 / V007 / M5 certified core.

---

## 2. CERTIFIED BASELINE — DO NOT BREAK

- **V007** = last proven visual marking baseline. **No verified V008.**
- **M5.1 REAL Physics = CERTIFIED**
- **M5.2 Event Contract = CERTIFIED**
- **M5.3 rules core** = largely frozen / protected
- Previous **REAL 10-SHOT = PASS**
- Priority order: **Correctness → Determinism → Evidence → Stability → Performance → UX → Polish**

---

## 3. IMPORTANT GIT TOPOLOGY

```
origin/main = 24cc95a7

M5.3 / APK validation line : 7cf232e8   (checkpoint/147vr-apk-clean-publish)
008 props line             : cc480a10   (tools/008-staging-tool-urp-20260922)
```

They are **divergent** (forked at `main`, never merged).

- **Do NOT apply Issue #7 directly onto `7cf232e`** — the file it patches does not exist there.
- Issue #7 floor fix: `1da8ed55e269d07bd2d5b7b059eb4c0eb6d35856`
  branch `fix/008-prop-floor-height-20260922`
- **Issue #7 has already been pushed.** *(Verified on origin 2026-09-23 — see §13. Note: Coach's own earlier report, issue #8 appendix §6 item 5, listed this as an outstanding provenance gap. That gap is now CLOSED.)*
- Issue #7 is independently validated in **provenance/tree** terms, but final **shipping** validation must happen on an **explicit integration line**.

---

## 4. ISSUE #7 — FLOOR FIX

| Item | Value |
|---|---|
| Issue | `COACH_008_PROP_FLOOR_FIX` |
| Patch SHA256 | `5235c3284f8d93e3a47d65f32786c00a5362d6aa614031b8c603c3820f718d50` |
| Commit | `1da8ed55e269d07bd2d5b7b059eb4c0eb6d35856` |
| Parent | `cc480a10` |
| Files changed | `Assets/Editor/Stage008Props.cs` **only** (+20 / −8) |

**Bug:** floor props used `FloorY = 0f`, placing them at **bed height** instead of floor height.

**Expected result:**

- `PROP_REST` / `PROP_CUERACK` / `PROP_TRIANGLE` / `PROP_SCOREBOARD` ≈ `-0.827`
- `PROP_CHALK` **unchanged**

**Do not mix cleanup or scene changes into this acceptance.**

*Audit trap to keep active:* the scene contains **two** objects named `Bed_Collider`. The authoritative one (inside the prefab) has collider top at world Y = 0; the decoy at scene root has top Y = 0.334. The tool searches only under `tableRoot`. If a log ever reports `surfaceTopY 0.3340`, **stop** — every floor prop would land 0.334 m too high.

---

## 5. F7 / F8 / F9 / F11 / F12

**F7 — Git lines diverged.** (See §3.) A validation tree only answers "does this change pass?" for the tree it stands on. If that tree is not the shipping tree, a PASS does not transfer. Owner decision still required before step [3].

**F8 — 4f1 → 12f1 is an editor *migration event*, not a normal editor switch.** It is a project upgrade (Unity rewrites `ProjectVersion.txt`; UPM may auto-update incompatible packages; `Library/` is untracked so almost nothing appears in git). Locks previously certified on 4f1 (V007 / M5.1 / M5.2 / M5.3 / D8) are therefore **unstated**, not automatically invalid and not automatically preserved. Both directions need recording: do not revert to 4f1, and do not let 12f1 silently overwrite the certified baseline.

**F9 — raw `.log` files are ignored** (`.gitignore` line `*.log`), so certification evidence must be copied into a **tracked `Artifacts/`** path (`.txt` / `.json` / `.xml`), with sha256 recorded.

**F11 — tracked safe launchers still point to 4f1:**

- `Docs/Tools/Unity_Batch_Safe.ps1`
- `Docs/AI_TEAM/Tools/Unity_Safe_Batch_Launch.ps1`
- the deprecated launcher also references 4f1

**Do NOT modify these yet.** Launcher correction must be a **separate tracked change** (F11b also stands: there is no tracked statement that the 4f1 Hub install still exists — if it is gone, 4f1 is not re-runnable and the migration is one-way rather than controlled).

**F12 — `Packages/manifest.json` has CRLF/EOL ambiguity.** `git status` showing `M` alone does **NOT** prove migration. Issue #11 carries the `.gitattributes` EOL pinning patch: SHA256 `602161bd9b166514f960be09a179a71ea15643ec351f9c9e9ec420e135ec43a2`. **Never infer migration from `git status`.**

---

## 6. PREFLIGHT / ENVIRONMENT

**Issue #12 v2 — `Tools/Preflight-Capture-147VR.ps1`**
Patch SHA256: `d81cbfbe2ff2b929984b199316b17a1b0d9c188cce44b57f5a8ba7f0c8190c25`

- **Correct default target:** `C:\Temp\147VR_M53_VALIDATE` — **NOT** the Source repo.
- Important target-tree fields it must emit:
  `TARGET_TREE` · `TARGET_TREE_HAS_GIT` · `TARGET_TREE_HEAD` · `TARGET_TREE_BRANCH` · `TARGET_TREE_IS_VALIDATION_WORKTREE`
- A non-git target is a **BLOCKER**, not a silent empty capture.

**Do not trust migration evidence unless HEAD is confirmed as `7cf232e`.**

---

## 7. UPM INVESTIGATION — THIS IS THE CURRENT FRONTIER

### 7.1 Binary identity (12f1)

| Artifact | Path / value |
|---|---|
| 12f1 UPM | `C:\Temp\UnityEditors\6000.4.12f1\Editor\Data\Resources\PackageManager\Server\UnityPackageManager.exe` |
| 12f1 UPM SHA256 | `95FDDF582FCC653C2E90D4E21E370BF3E6DCEBC891E731BCB3A878EC4412BEE6` |
| 12f1 UPM size | `95,112,112` |
| 12f1 `Unity.exe` SHA256 | `62439E457048ED6EA887A8536DC0B4329C64BBEE507C906ECBBFCE4C544FF6C6` |
| 4f1 UPM (control) SHA256 | `8F9C6D223CE5FC5154BECD8B58312BBD76B36C0618F4FC721119D0CC5BF9D14E` |

### 7.2 Results already proven (Step 2 CLOSED — do not re-run from zero)

| Experiment | Result |
|---|---|
| 12f1 UPM binary hash/size | **PASS** — matches the real artifact |
| Direct execution of the UPM binary | **PASS** — CLI starts normally ⇒ the binary itself is executable |
| Empty-project 12f1 launch | Unity failed to connect to UPM after **30 s** |
| **Unguarded** manual UPM | Exits after **~1.3 s** with `TypeError [ERR_INVALID_ARG_TYPE]: The "path" argument must be of type string. Received undefined` · stack includes `getLocalConfigFolder` → `readConfig` → `initialize` |
| **Guarded** (process-local ENV) | **Does not die immediately** · stays alive **~302 s** · **NO `IPC server started`** · **no stderr error** · wrapper completes after ~302 s · no leftover `UnityPackageManager` process · **IPC never established** |

**Guarded environment values used:**

```
PROGRAMDATA=C:\ProgramData
ALLUSERSPROFILE=C:\ProgramData
APPDATA=C:\Users\mongo\AppData\Roaming
LOCALAPPDATA=C:\Users\mongo\AppData\Local
USERPROFILE=C:\Users\mongo
TEMP=C:\Users\mongo\AppData\Local\Temp
TMP=C:\Users\mongo\AppData\Local\Temp
NO_PROXY=localhost,127.0.0.1
UNITY_UPM_TIMEOUT=120
```

### 7.3 Therefore

> **ENV guard fixes the immediate `Received undefined` crash, but is insufficient to bring UPM up to IPC.**
> Step 2 therefore did **NOT** pass, and Step 3 (empty-project Unity) is deliberately **NOT** started — running it now would mix two problems and contaminate the evidence.

The unguarded failure is the same **class** as the historical 2026-08-28 UPM environment/config failure.

---

## 8. HISTORICAL INCIDENT — LOCAL EVIDENCE TO COMPARE AGAINST

Document: `Docs/147VR_UPM_IPC_INCIDENT_20260916.md`

- UPM command: `UnityPackageManager.exe server -s 10328 --ipc-path Unity-Upm-10328 -l 2`
- IPC eventually started after **~218 seconds**; the log explicitly says `IPC server started`
- The log gives Unity the hint: `-upmIpcPath Upm-10328`

This proves **delayed IPC startup has happened before on this machine**.

**But the current guarded run did NOT reproduce IPC even by 302 s.**
**Do not assume the current cause is identical to 09-16.**

---

## 9. WHAT WE MUST NOT DO

- firewall changes
- reboot roulette
- system-wide environment edits
- 4f1 rollback
- open the real project
- run an InitializeOnLoad-heavy validation project
- modify source code to "make migration pass"
- create a new issue
- create a new diagnostic script
- change the safe launchers yet
- clean/reset the dirty validation worktree
- (F9 rule) never let certification evidence exist only as a `.log`

---

## 10. NEXT INVESTIGATION

**The question:** why does 12f1 UPM

- execute correctly,
- fail immediately without an environment guard,
- survive with the guard,
- **but never reach IPC after 300 s?**

**Investigate only this layer first.** Potential next discriminators:

1. Compare the exact guarded environment/config inputs against the **2026-09-16 known-good delayed-start** environment.
2. Inspect UPM **process state / command line / working directory / config location**.
3. Verify whether `server --help` / the server invocation exposes **another required runtime/config input**.
4. Determine whether the guarded process is **blocked internally** versus **waiting on a dependency**.
5. Only after UPM can establish IPC: run **12f1 empty-project end-to-end**.
6. Only after empty-project passes: inspect the **dirty validation worktree** and the migration discriminator.
7. Only after that: **real clean validation / integration evidence**.

---

## 11. CURRENT VERDICT (state to hold)

```
12f1 binary        = executable
unguarded          = immediate config/env initialization failure
guarded            = initialization crash avoided
guarded IPC        = NOT achieved after 302 s
```

**Current status: `BLOCKED — UPM initialization/IPC investigation`**

**Not:** Unity project failure · M5 rules failure · migration failure · firewall failure · certified-core failure.

**Step 2 closure statement (verbatim, project owner's wording):**

> 12f1 UPM Variant B confirmed as an environment-inheritance trigger, but process-local guard alone is insufficient to reach IPC. Unguarded fails immediately with `Received undefined`; guarded survives >300 s but produces no IPC and no stderr. Next investigation must determine why guarded UPM does not initialize IPC, without changing system environment or falling back to 4f1.

No HARD RESET is needed and nothing needs to start over. This round already closes the question *"is it only missing ENV?"* — **the answer is no; there is at least one further layer.**

---

## 12. EVIDENCE INDEX — READ ONLY THESE (no broad GitHub sweep)

Branches currently on origin (17 total), with tips:

```
main                                             24cc95a7
checkpoint/147vr-apk-clean-publish               7cf232e8   <- validation checkpoint
checkpoint/m5-certified-baseline-20260915        3f7cf73a
tools/008-staging-tool-urp-20260922              cc480a10   <- 008 line tip
tools/008-props-generator-20260922               7ef6dd45
tools/008-staging-tool-20260922                  2713fb8f
tools/008-measurement-script-20260922            859a51ac
fix/008-prop-floor-height-20260922               1da8ed55   <- Issue #7 (pushed)
fix/m5-determinism-fixedupdate                   ec89add2
fix/m5-sequence-guard                            f7469882
docs/008-geometry-collider-contract-20260922     c381756d
docs/coach-008-measurement-review-20260922       01a8734c
docs/coach-evidence-audit-20260922               1eeb242f
docs/architecture-aaa-quest                      837efeb3
docs/readme                                      6da43c54
chore/tools-blender                              c361e607
chore/cleanup-backup-files                       00a046a9
```

| Claim / topic | Inspect exactly this | Verify with |
|---|---|---|
| Divergent lines, validation HEAD not on 008 line | — | `git merge-base cc480a10 7cf232e` → `24cc95a` · `git rev-list --count 24cc95a..cc480a10` → 24 · `...24cc95a..7cf232e` → 2 |
| Issue #7 pushed + scope + parent | `fix/008-prop-floor-height-20260922` | `git ls-remote --heads <repo>` (ref = `1da8ed55…`) · `git cat-file -p 1da8ed55` (parent `cc480a10…`) · `git show --stat 1da8ed55` (1 file: `Assets/Editor/Stage008Props.cs`) |
| Floor-fix expectation & decoy rule | issue **#8** §5 (COACH_008_ENVIRONMENT_AND_LINE_AUTHORITY_20260922) | patch body in issue #8 (pre-registered acceptance, expected Y values, the two required log lines) |
| Editor migration is a real upgrade event | `ProjectSettings/ProjectVersion.txt` @ `7cf232e` → `6000.4.4f1 (360f97ecca93)` vs declared 12f1 authority | F8 in issue #8 §2 |
| Evidence must be tracked, not `.log` | `.gitignore` @ `7cf232e` (`*.log`) | F9 in issue #8 §3 |
| Launchers still 4f1 | `Docs/Tools/Unity_Batch_Safe.ps1` (:6) · `Docs/AI_TEAM/Tools/Unity_Safe_Batch_Launch.ps1` (:29) | F11 in issue #9 |
| EOL/CRLF ambiguity | `Packages/manifest.json` blob | issue **#11** |
| Preflight capture contract (target tree fields) | issue **#12** | patch body of issue #12 |
| UPM env guard contract | `Docs/147VR_UPM_ENV_GUARD.md` | — |
| Delayed-IPC precedent (218 s) | `Docs/147VR_UPM_IPC_INCIDENT_20260916.md` | — |
| 008 measurement artifact (read-only proof) | `Artifacts/M5_3/008_Measurement/m53_008_measure.txt` (+ `.json`) | measurement asserts 9/10 PASS; `ASSERT_NO_NEGATIVE_SCALE` FAIL; source sha256 `c4e0fb16…`, size `77,619,695` |
| M5 evidence audit (F1–F6) | `Docs/AI_TEAM/COACH_EVIDENCE_AUDIT_20260922.md` | Coach's own audit, 22 Sep |
| 008 integration gate + locks | `Docs/M5_3_008_INTEGRATION_HANDOFF_20260922.md` · `Docs/AI_TEAM/M5_3_008_GEOMETRY_COLLIDER_CALIBRATION_CONTRACT_REV1_20260922.md` | — |

### Verification status of this brief (transparency)

- **Independently verified by Accio on 2026-09-23** (read-only, via `git ls-remote` + a depth-2 fetch of `1da8ed55`):
  1. branch `fix/008-prop-floor-height-20260922` exists on origin at `1da8ed55…`;
  2. its parent is `cc480a10…`;
  3. it changes `Assets/Editor/Stage008Props.cs` only (+20 / −8);
  4. the 17-branch topology and all tips listed above;
  5. no branch tip is newer than 2026-09-22 (nothing new pushed on 09-23).
- **Owner-provided, not independently verifiable from here** (no access to the Windows host): the 12f1/4f1 binary hashes and sizes, and the Step-2 guarded/unguarded run results in §7.2. Treat them as the project owner's measured record.

---

## 13. REQUIRED RESPONSE FORMAT

Reply in two parts, nothing else:

**Part A — "reconstructed current state"**

1. What you **can confirm** from the committed evidence (list, with the exact artifact/commit you read for each item).
2. What is **still missing / unknown** (list).
3. Any item in this brief that **contradicts** what you find on GitHub — flag it explicitly rather than silently preferring one side.

**Part B — UPM analysis**

Only after Part A: your analysis of the §10 question, limited to the current frontier layer (`why does guarded 12f1 UPM never reach IPC?`). Propose discriminators that are read-only and do not violate §9.

**Constraints for both parts:** do not invent state, do not modify files, do not create issues or new diagnostic scripts, do not change system environment, do not fall back to 4f1.
