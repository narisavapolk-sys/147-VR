# 147 VR — Remaining-Work Map (re-based on current evidence)

**Author:** Coach seat (per `COACH_OPERATING_CONTRACT_147VR.md`)
**Date:** 2026-09-23
**Base of record:** repository state at `1da8ed55` — blob-verified this session (`Docs/147VR_CURRENT_STATE.md` = `962646f59a6d758c6efed47d18c7907e6796054a`, `Docs/147VR_PROJECT_MEMORY.md` = `b685836723b3c8456ef02c37f946bb874d70fbd9`)
**Status of this document:** planning input only. **It authorizes no change.** Per contract §18, the human remains the authorization boundary.

Confidence key used throughout: **[F]** fact (cited to a committed artifact, or to a file-tree observation made this session) · **[I]** inference · **[U]** unknown / not established.

---

## 0. Why this map exists: the planning docs under-report reality

**[F] The three planning documents on `main` are stale relative to the certification record:**

- `Docs/147VR_MASTER_PRODUCTION_ROADMAP.md` — `> Current focus: Phase 3 — Table Visual & Marking Overlay`, and in Phase 1: `- M5 Shot Lifecycle / Gameplay Physics — NOT CERTIFIED (authority-chain evidence pending; see Docs/M5_AUTHORITY_CHAIN_STATUS.md)`
- `GATE_STATUS.md` — `> Status: GATE 3 — COMPLETE (manual scene validation passed)` · `> Last updated: 2026-08-24` · `## Next Gate` → `GATE 4 is not defined yet. The next production step should be a focused gameplay/runtime verification pass rather than more foundation scaffolding.`
- `PRODUCTION_READINESS.md` — `Unity Editor version is pinned by ProjectSettings/ProjectVersion.txt to 6000.4.4f1`

**[F] Meanwhile, later committed artifacts exist for the M5 chain** (e.g. on the 008 line: `M5_REAL_10SHOT_CERT_20260919.json`, `M5_RESET_FRAME_CERT_20260919.json`, `M5_YOLO_BASELINE_STATUS_20260919.txt`, `M5_YOLO_POSTRUN_MANIFEST_20260919.txt`, `M5_EXECUTE_PROBE_20260919.txt`) and `Docs/AI_TEAM/COACH_CERTIFICATION_M5_1_20260908.md` certifies M5.1.

**[I] Consequence:** any plan (or estimate) built from the roadmap/GATE_STATUS without this correction is wrong in *both* directions — it over-counts finished physics work and under-counts the true next blocker (toolchain + device).

**[F] New topology fact reported this session:** the production working tree's last commits are `1da8ed55` → `cc480a10` → `3ce83872`, i.e. **the real project currently sits on the 008 prop-fix tip**, not on an integration line. Confirm the branch name with `git branch --show-current` and record it — this feeds F7 (see §4).

---

## 1. Closed, and load-bearing (do not re-open)

**[F] Phase 0 — Foundation.** `TODO.md` GATE 1 (foundation, git baseline, structure/dependency/architecture audits) and GATE 2 (core systems, state/event architecture, boundaries, integration contracts) are fully `[x]`.

**[F] Phase 1 — Physics Authority (large parts).** `Docs/147VR_PROJECT_MEMORY.md` records the certified floor:

- `- Persisted certified coefficients: rollingFriction=0.45200002, rollingDamping=0.21000001, slidingFriction=0.6807868, spinFriction=0.20040171, measuredTruthCertified=1.`
- `- Acceptance evidence: 5 repetitions; max replay error rolling=0.88%, sliding=1.50%, spin=0.02%; acceptance gate <=2% on all channels.`
- `- Fresh current-profile M3 Cushion runtime batch completed 7 cases x 5 repetitions. Current measurements regressed against the existing M3 Goldens at 35/35 PASS…`
- `- Straight Golden regression after certified cloth bridge: 5/5 PASS at 0.50% tolerance.`
- `- M4.2 Pocket/Jaw/Rattle Golden certification: 6/6 cases, 30/30 REAL samples, Left/Right Jaw symmetry PASS.` — **explicitly historical, not a post-cloth fresh re-capture** (see §5).

**[F] Phase 2 — Rules / Gameplay is substantially built.** First-party scripts present include `SnookerScoreManager`, `SnookerTurnManager`, `SnookerRulesTests`, `M5ShotLifecycle`, and the Main Scene carries the M5 runtime chain (per `Docs/147VR_CURRENT_STATE.md`: `SnookerBallTracker, SnookerScoreManager, SnookerTurnManager, SnookerShotTracker, M5ShotEventContract, M5ShotLifecycle`).

**[F] Toolchain sub-findings that are now settled (owner-reported machine runs; Coach-audited):** 12f1 UPM binary executable; **standalone IPC PASS (~59 ms)** with `--ipc-path` + env guard + CWD = `…\PackageManager\Server`; **D-3.0 PASS** (`-s <pid>` is a polled liveness token, not a parent relationship); H-b PASS; H-c no licence block; **F11 needs no tracked change** (`Docs\Tools\Unity_Batch_Safe.ps1` is already parameterized on `-UnityExe` / `-ProjectPath`).

---

## 2. The two gates that currently gate everything else

### B1 — Toolchain gate (machine-side) **[F, blocking]**

The 12f1 editor cannot yet complete an unattended run, so **no phase that needs the editor on the 12f1 authority can close**. Level status:

| Level | State |
|---|---|
| L0 attributable launch | PASS |
| L1 12f1 standalone IPC | PASS (~59 ms) |
| L2 Unity ↔ UPM client round-trip (3 endpoints = 200) | **UNKNOWN** |
| L3 Unity 12f1 empty-project E2E | **BLOCKED** — empty project not yet created |
| D-3b (`-upmIpcPath` workaround) | NOT AUTHORIZED |

**Next action (from WO#003 §5 A1):** one instrumented `-createProject` run — launcher hardening copied verbatim, absolute `-logFile`, argv recorded as received, before/after directory listing and process inventory. Read the log before changing any variable.

### B2 — Device gate **[F, blocking]**

`Docs/147VR_PROJECT_MEMORY.md`: `- Quest hardware gate: no authoritative Quest 2/3 device evidence was obtained from the PC during this run. ADB startup was unreliable/hanging and all stray ADB/shell processes were cleaned up. Do not claim Quest 2/3 PASS yet.`
⇒ Phases 4, 7 and 12 (perception, VR UX, performance) **cannot be closed without hardware**, and Phase 12 cannot even be estimated.

---

## 3. Open work, phase by phase, with the evidence behind each verdict

| Phase (roadmap) | Status | Evidence |
|---|---|---|
| **3 Table Visual & Marking** | **OPEN — the stated active focus** | V007 = last proven marking baseline; **008 is the integration target with a CONDITIONAL PASS contract and is NOT integrated**; the 008 line tip (`cc480a10`) and the production tree (`1da8ed55`) must be reconciled (F7) |
| **4 VR Perception** | **NOT STARTED** | Only the 008 geometry/collider/calibration contract (REV1) exists; no perception pass recorded |
| **5 Lighting / Rendering** | PARTIAL **[I]** | Large asset corpus exists; no lighting/exposure acceptance recorded in the state docs |
| **6 Cue / Interaction** | PARTIAL | VR147 semantic input stack + two-hand cue pose source exist; `TODO.md` still lists `- [ ] Advance VR Interaction after physics/gameplay contracts stabilize` |
| **7 VR UX** | PARTIAL, **device-blocked** | `TabletOptionsMenu` (player height / move speed / audio / scene select) + MR passthrough mode exist in `GAME_TEXTS.md`; no device pass |
| **8 Game UI** | PARTIAL | `HUD.prefab`, `AlertViewHUD`, `SkinSelectMenu`, `TabletOptionsMenu` exist; main menu / practice-frame-match flows / frame-result flows **not verified** |
| **9 Audio** | **NOT STARTED [F]** | **Zero first-party `.wav`/`.mp3` assets; no `AudioMixer`** in the repository |
| **10 Multiplayer** | **NOT STARTED [F]** | **Zero first-party `Mirror` artifacts.** Every networking-shaped file is third-party, inside `Packages/com.meta.xr.sdk.core/…` (e.g. `Editor/BuildingBlocks/BlockData/MultiplayerBlocks/NGO/…`). Note the roadmap says **Mirror**; nothing indicates a platform decision has been made |
| **11 QA / Certification** | PARTIAL | Golden infrastructure exists; `Assets/Tests` is thin (`Assets/Tests/*` — and F6 records empty test shells); M4.2 needs a fresh current-profile re-capture |
| **12 Performance** | **NOT STARTED, device-blocked** | No real-headset profiling; Quest hardware = NOT RUN |
| **13 AAA Polish / Release** | **NOT STARTED** | Only a dev-APK checkpoint line exists; `PRODUCTION_READINESS`-era blockers remain (Android application id is still the template placeholder per `147VR_CURRENT_STATE.md`; no custom keystore configured) |

**First-party scale for context [F]:** 223 `.cs` files outside `Packages/`; first-party scenes = `Assets/Scenes/{147VR_MainScene, PoolTable_8Ball, PoolTable_9Ball, SampleScene}.unity` plus 7 calibration scenes under `Assets/AAA/PhysicsCalibration/`.

---

## 4. Declaration / hygiene debt — cheap to close, expensive to leave open

| ID | Item | Why it matters now |
|---|---|---|
| **F7** | Declare the integration line; decide how the 008 line and the M5.3/APK line reconcile | Final shipping validation must occur on the tree that ships. The production tree currently sits on `1da8ed55` (an 008 fix tip) — so **today, "production" and "validated" are different trees** |
| **F8** | Commit the editor-version change as its own change + record lock dispositions (V007 / M5.1 / M5.2 / M5.3 / D8) | The first 12f1 open will re-serialize locked state with no diff — declare intent before, not after |
| **F9** | Evidence rule: `.txt`/`.json` on a tracked path, sha256, never `.log` | Already partly applied by the Coach pack; still needed for batch logs |
| **F10** | UPM signature register | The "no IPC after ~302 s" observation is **withdrawn** (INVALID run); S4a (unguarded config crash) is reproducible; S3 (09-16 delayed IPC, 218 s) remains a race, not a defect |
| **F11** | **Resolved without a file change** — use `Unity_Batch_Safe.ps1` with `-UnityExe` override | Record this so nobody "fixes" the launcher by editing it |
| **F11b** | Verify the 4f1 Hub install still exists (`C:\Program Files\Unity\Hub\Editor\6000.4.4f1\…`) | Rollback anchor. 4f1 is a Hub install; 12f1 is not |
| **F1 / F3 / F6** | Uncommitted decisive logs; empty test shells | Weakens independent re-verification of otherwise-valid claims |
| **F12** | EOL ambiguity (`Packages/manifest.json`) + the `.gitattributes` patch (issue #11) | Do not infer migration from `git status` |
| **NEW** | Re-base `MASTER_PRODUCTION_ROADMAP` / `GATE_STATUS` / `TODO` against the evidence in §1 | Until this is done, every fresh AI session starts from a wrong map |

---

## 5. Dependency-ordered execution plan

**Tier 0 — unblock (nothing downstream can be trusted before this)**
1. D-3a → L2 + L3 PASS (empty project created by 12f1, instrumented run).
2. Declare the F7 integration line; state which tree is authoritative.
3. F8: commit `ProjectVersion.txt` separately + lock dispositions.
4. Re-base the planning docs (this map is the first input).

**Tier 1 — close what is already built (highest value per unit of risk)**
5. Validate Phase 2 rules on the declared integration line (legal/illegal, foul, ball-on, frame end, match) — the rules exist; they are not yet *validated on the shipping tree*.
6. Finish Phase 3: reconcile V007 ↔ 008, then integrate the 008 table **non-destructively**, using the corrected containment criterion (strip `147VR_PROPS_ROOT` from both versions and compare as parsed `fileID→block` maps) rather than file size/line count.
7. Phase 11 pass 1: re-run the physics Golden regression on the integration line; perform the **fresh current-profile M4.2 re-capture** (it is explicitly historical today).

**Tier 2 — device gate (unlocks three phases at once)**
8. Resolve Android application identity + keystore decision (signed release is blocked; dev APK is not).
9. First Quest 2/3 pass: install dev APK, run `147VR_MainScene`, verify cue → physics → rules → score → turn → UI, capture a perf baseline, and a 30-minute stability run.
10. Then Phase 4 / 5 / 7 refinement and Phase 12 profiling with real numbers.

**Tier 3 — content and systems (parallelizable only if the single-Unity discipline is respected)**
11. Phase 9 Audio (cheapest large win: no assets exist at all).
12. Phase 8 UI flows (main menu, practice/frame/match, settings, frame result, restart/quit).
13. Phase 6 cue/interaction polish + rest interaction.
14. **Phase 10 Multiplayer — treat as its own milestone**, with an explicit platform decision first (roadmap says Mirror; nothing is in the project).
15. Phase 13 polish → RC validation → store requirements.

---

## 6. Proposed re-based gates (GATE 4 onwards)

`GATE_STATUS.md` currently ends with `GATE 4 is not defined yet.` Suggested definitions, each with the evidence rule that no PASS may be claimed without artifacts:

| Gate | Name | Acceptance (all required) |
|---|---|---|
| **GATE 4** | Toolchain & integration-line proof | D-3a L3 PASS on an empty project · L2 = the three endpoints returning 200 · F7 integration line declared and named · Phase 2 rules validated on that line · `ProjectVersion.txt` committed separately · lock dispositions recorded (F8) |
| **GATE 5** | Device gate | Dev APK installs on Quest 2/3 · `147VR_MainScene` runs · full loop verified on device · perf baseline captured · 30-min stability · artifacts committed per F9 |
| **GATE 6** | Content-complete | 008 table integrated and verified against the collider contract · audio present for ball/cushion/pocket/cue/UI · all UI flows reachable · marking alignment ≤ declared tolerance |
| **GATE 7** | Release candidate | Full regression matrix green on the shipping tree · application identity + signing resolved · store metadata/assets · no open F-class blockers |

---

## 7. UNKNOWN — what cannot be estimated responsibly yet

**[U] 1.** Throughput (no closed phase exists after the toolchain gate opened — there is no rate to extrapolate from).
**[U] 2.** Multiplayer platform choice and scope (none / LAN / online; Mirror vs NGO vs none).
**[U] 3.** Whether `PoolTable_8Ball` / `PoolTable_9Ball` / `SampleScene` ship, or Main Scene only (the candidate APK builder is Main-Scene-only by design).
**[U] 4.** Quest 2 vs Quest 3 as primary target, and the performance budget that follows from it.
**[U] 5.** Store/monetization requirements, if any.
**[U] 6.** Ownership/creation of the release keystore.

**An estimate ("how many weeks") becomes defensible when:** (a) GATE 4 closes, (b) the first device pass yields real frame-time numbers, (c) at least one full phase closes *after* the toolchain gate opens.

---

## 8. RISKS

- **R1** Planning from the stale roadmap ⇒ wrong ordering, duplicated work, false confidence.
- **R2** Starting Tier 3 (audio/UI polish) before the device pass ⇒ rework under headset constraints (comfort, legibility, perf).
- **R3** Running two heavy lines on one machine ⇒ violates the single-Unity discipline and re-creates the 09-08 dual-launcher class of failure.
- **R4** Leaving F7 open ⇒ ship validation on a tree that is not the shipped tree — the one error that invalidates an otherwise complete evidence chain.
- **R5** Treating Phase 10 as "just another phase" ⇒ it is a project-sized subsystem and will dominate the schedule if not scoped deliberately.
- **R6** Judging certified physics by test-suite counts: `Assets/Tests` is thin and F6 records empty shells — do **not** read "tests present" as "regression coverage exists".

---

## 9. One-paragraph answer to "how much is left?"

Physically: **14 phases, 3 closed or nearly closed (0, 1, part of 2), 11 open**, with two structural blockers (toolchain B1, device B2) that stop everything except documentation and offline work. Qualitatively the position is favourable: the hardest and most expensive asset — a deterministic, Golden-verified physics and rules core — is already built and certified, and what remains is the *predictable* remainder of a VR game (audio, UI flows, integration of the new table, device validation, performance, polish) plus one genuinely heavy subsystem (multiplayer). **No responsible date can be given until GATE 4 and the first device pass are closed** — anything else would be filling a gap with confidence rather than evidence.
