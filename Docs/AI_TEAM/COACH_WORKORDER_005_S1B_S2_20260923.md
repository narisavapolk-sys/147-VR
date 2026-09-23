# COACH WORK ORDER #005 — S1b (L2 reproducibility probe) + S2 (rules validation on the integration line)

**Issued by:** Coach seat · **Date:** 2026-09-23
**Executor:** Luna · **Decision owner:** the project owner
**Base of record:** `integration/008-m53` @ `f94a431c59b06c18a06dec521dd9896340d974fc`
**Predecessors:** `COACH_DISPOSITION_R3_PACKAGES_LOCK_20260923.md` (D1) · `COACH_WORKORDER_004_INTEGRATION_LINE_20260923.md` (R1–R4)

---

## 0. VERDICT ON D1 — PASS, with two corrections

**Independently verified from GitHub (Coach side), not taken from the report:**

| Check | Result |
|---|---|
| Branch tip | `f94a431c59b06c18a06dec521dd9896340d974fc` ✅ |
| D1 commit | `4e1b131d11b9b14df4a422b110039386ac09209d` — **one file only**: `Packages/packages-lock.json` ✅ |
| **Actual lock delta** | The **entire** file diff is **two lines**: `"version": "3.4.0"` → `"3.5.0"` (line 424) and `"version": "2.5.3"` → `"2.6.0"` (line 465). File size unchanged at 22,251 bytes ✅ |
| `Packages/manifest.json` | **byte-identical** across the D1 commit ✅ |
| Evidence commit | `f94a431c` = `Artifacts/M5_3/R3_packages_lock_idempotence_20260923.txt` (25 lines) + `Docs/147VR_CURRENT_STATE.md` ✅ |
| **F9 artifact hash** | Canonical blob sha256 = `e4c4810d3d48b4b78a924f84e3e9e938c089d999202463d507f3c49683f47277` = **the recorded value** ✅ — the hash was taken from canonical bytes, so the CRLF trap did **not** bite here |
| Artifact completeness | All required fields present: `WHICH_COMMIT` · `TARGET_TREE_*` · `UNITY_EDITOR` · cause · requester check · XR minima · before/accepted versions · both log paths + SHAs · `upm.log` + SHA + HTTP-200 timestamps · idempotence result · shutdown · return code · exposure surface · limitations ✅ |

**⇒ D1 = PASS. The gate is closed.**

### 0.1 Correction H1 — the artifact records a truncated commit SHA

The artifact contains:

```
WHICH_COMMIT: 4e1b131d11b9b14df4a422b110039386ac09209     <- 39 characters
```

The real commit is `4e1b131d11b9b14df4a422b110039386ac09209d` (**40 characters** — the trailing `d` is missing). A future verifier resolving the recorded string will fail. This is small but it is exactly the class of defect the F9 discipline exists to prevent: **an identifier that cannot be resolved is not provenance.**

Fix: one-line correction, plus record both the abbreviated and the full SHA (`git rev-parse HEAD` output verbatim, never retyped).

### 0.2 Strengthening H2 — the rationale can be made *stronger* than recorded

The artifact records: `REQUESTER_CHECK: no listed dependent requests >=2.6.0 for core-utils or >=3.5.0 for performance`.

Because the **entire** lock diff is only those two version strings, we can state something strictly stronger than "we did not find a requester":

> **No dependent's declared minimum changed anywhere in the graph** — no entry in the lock gained or lost a dependency, and no requested version on any edge changed. Therefore the elevation did **not** originate from any package in this project's graph; it came from the editor's own resolution / bundled metadata for 12f1.

Record it that way: it converts an inference-from-absence into a proof about the graph.

### 0.3 Also confirmed by the independent check

- `XR_MINIMA_OBSERVED: hands 2.2.0; management 2.2.1; openxr 2.3.0` matches Coach's own read of the committed lock ⇒ the exposure surface recorded (XR init / hands / management / OpenXR) is correct.
- The non-fatal observations (duplicate `Unsafe.dll` warning, XR `StopSubsystems` shutdown warning) are correctly recorded and **not chased**. Leave them that way.

---

## 1. S1b — make L2 reproducible (closes F9b)

**Objective:** any future session can re-prove the three Package Manager endpoints on 12f1 **from the repository alone**, without remembering an internal API name.

**Placement (mandatory): outside `Assets/`.** `Tools/D3A_L2_Probe/` — because a `.cs` under `Assets/` is compiled by every Unity open of that project, and this probe must never be able to execute in the real project.

**Required contents of `Tools/D3A_L2_Probe/`:**

| File | Purpose |
|---|---|
| `D3A_L2_Probe.cs` | the probe: `Client.List()` → `project:list-packages`; `SearchAll()` → `packages:get-all-packageinfo`; the internal `UnityEditor.PackageManager.UI.Internal.UpmRegistryClient` reached through an initialized service instance → `config:project:get-registries`. Each call emits a **stable marker line** so the log can be grepped deterministically |
| `README.md` | exact steps for a fresh empty 12f1 project: where to copy the probe, the exact launcher invocation, the exact expected log lines, where `upm.log` lives, and the cleanup step |
| `Run-D3A-L2-Probe.ps1` | optional driver that performs copy-in → run → collect `upm.log` → copy-out the evidence → delete the probe from the throwaway project |

**Acceptance criteria (all required):**

1. `Tools/D3A_L2_Probe/` committed on the integration line; the probe is **not** under `Assets/` in this repo.
2. The access path is recorded in the README: `config:project:get-registries` is reachable **only** through the internal UI client — this is the non-obvious part and the reason F9b exists.
3. A fresh empty 12f1 project + probe produces in `upm.log`:
   `project:list-packages --> 200` · `packages:get-all-packageinfo --> 200` · `config:project:get-registries --> 200`
4. F9 artifact `.txt` committed with: `WHICH_COMMIT` (40 chars) · `UNITY_EDITOR` · `UNITY_PACKAGE_MANAGER_SHA256` · probe source blob hash · `upm.log` path + SHA · the three 200 lines verbatim · `RESULT`.
5. **Negative acceptance — the important one:** one open of `integration/008-m53` after this lands produces **no `Assets/**` diff, no new files, and no probe execution line in the log.** Record that check as evidence. If the probe can even in principle run inside the real project, the placement is wrong — fix the placement, do not add a guard flag.
6. A reader who has never seen this session can reproduce it using only `README.md`.

---

## 2. S2 — Phase 2 rules validation on `integration/008-m53`

**Objective:** the rules/gameplay spine is validated **on the tree that will ship**. Today the rules engine exists and was integrated on the APK line, but it has not been validated on this line.

### 2.1 Pre-registration (mandatory, and it is the whole method)

**Before running anything**, enumerate the case list **from the actual rules code** and commit it. The list must be committed *before* results exist, so it cannot be trimmed to what happened to pass. Required fields per case: `case_id`, `behaviour class`, `input`, `expected`, `basis` (file/symbol in the rules code that defines the expectation).

Behaviour classes to cover — map each to the real implementation, do not invent expectations:

- shot legality (legal / illegal) and foul detection
- ball-on determination
- scoring correctness per event (pot, foul penalty, colours sequence, frame-winning ball)
- turn transition (who plays next, consecutive fouls, re-rack / respot if implemented)
- frame end condition and match progression
- the **M5 lifecycle boundary**: shot start → settled → event → score → turn → next shot, including the `PhysicsSettled` event contract

If a class is **not implemented**, that is a finding — record it as `NOT IMPLEMENTED`, not as a failure and not as a pass.

### 2.2 Execution fork — declare which one, do not mix

| Path | Shape | Trade-off |
|---|---|---|
| **S2a** log-driven deterministic probe | a scripted run that drives cases and asserts against the log | produces evidence now; each case's evidence is a log excerpt; not a durable regression suite |
| **S2b** fill the NUnit shells | `Assets/Tests/**` shells (F6 reports them empty) get real assertions | durable regression value; but it is test-code work before any rule evidence exists, and test code touching the rules area needs its own review |
| **Recommended order** | **S2a first** (evidence now), then **S2b** to make it durable | — |

Do not run both in the same session: two evidence streams on the same tree, with the same log paths, is how provenance gets muddled.

### 2.3 Determinism control

If the project has a deterministic launch/reset path (the M5 deterministic launch / reset-frame harness), use it and record that it was used. If not, run each case from a recorded reset state and **state explicitly in the artifact that determinism was not controlled** — do not present a repeatable-looking result as a deterministic one.

### 2.4 Acceptance criteria

1. Case list committed **before** execution, with `basis` pointing at the rules code for each expectation.
2. Results as **both**: a machine-readable `.json` (one record per case) **and** a summary `.txt` — each with `WHICH_COMMIT` (40 chars), `UNITY_EDITOR`, `TARGET_TREE_HEAD/BRANCH`, per-case `PASS|FAIL|NOT_IMPLEMENTED`, and the log path + SHA.
3. Physics/Golden regression and REAL10 are **not** in scope here (they are S3/S5). Do not mix them into S2 evidence.
4. **INVALID, not FAIL** — a run is invalid if argv/CWD were not captured, `-logFile` was not absolute, the run used a wrapper bound shorter than the observation window, or the tree/HEAD was not recorded.
5. **STOP conditions** — if any case FAILs inside the frozen M5.3 rules core: stop, report, and **do not modify code to make it pass** (contract §5). A FAIL there is a finding about the integration, not permission to edit a protected area.
6. Judge by the co-occurrence rule, not by launcher exit codes: the launcher exits 4 on the documented stock-`-quit` `Received undefined` quirk, which can occur on a healthy run.

---

## 3. ORDER OF WORK

1. **H1 + H2** — the SHA correction and the strengthened rationale (small, do first so the artifact is not left wrong).
2. **S1b** — the L2 probe + its evidence + the negative acceptance check.
3. **S2a** — pre-registered case list, then the log-driven validation run.
4. **S2b** — only after S2a, if the owner wants durable regression.
5. Then **S3** (fresh current-profile M4.2 re-capture) → **S5** (REAL10 / M5 re-run on this line) → **S4** (008 non-destructive integration) → **S6** (close GATE 4) → device gate.

---

## 4. PROHIBITIONS

Unchanged: certified core (V007 / M5.1 / M5.2 / M5.3 / D8 / W1) · `147VR_M53_VALIDATE` · system environment · firewall · Windows settings · the tracked launcher · a second physics authority · any change made to turn a test green.

**STOP conditions** (contract §14): unexpected file mutation · unexpected scene diff · a certified asset touched · an unknown script that auto-executes · dirty state of unknown origin · unclear tree topology · missing evidence provenance · a test environment that is not the declared authority.

---

## 5. WHAT COACH DOES NOT DO

Coach does not authorize the merge of the APK checkpoint into `integration/008-m53`, does not declare any lock re-opened or re-certified, and does not sign off the shipping tree. Those remain owner decisions, and the final shipping tree still requires its own independent verification at a later gate.
