# COACH DISPOSITION — R3 `packages-lock.json` delta (decision package)

**Issued by:** Coach seat · **Date:** 2026-09-23
**Executor:** Luna · **Decision owner:** the project owner (Coach recommends; it does not authorize)
**Context:** `integration/008-m53` @ `ae2d9c82` · R2 = `db9e2738` · R3 captured at `4a18404`, Unity `6000.4.12f1` (`3ca267ce8005`)

---

## 0. VERDICTS ON R1 / R2 / R3

| Item | Verdict |
|---|---|
| R1 declaration (Option B: `integration/008-m53` from the production/008 tree) | **ACCEPTED.** Bases the line on the tree closest to shipping, and the APK checkpoint enters by controlled merge rather than rebase — no history rewrite, so no SHA churn for work other agents have referenced. Independence re-verified from GitHub: `origin/integration/008-m53 = ae2d9c828fab843578a7afd17db37649a72293d5` |
| R2 (`db9e2738`) | **PASS, and correctly scoped.** Verified independently: `ProjectSettings/ProjectVersion.txt` only, `1 file changed, 2 insertions(+), 2 deletions(-)`, `6000.4.4f1 (360f97ecca93)` → `6000.4.12f1 (3ca267ce8005)`. The editor-version change is isolated, exactly as ordered |
| R3 capture | **PASS as a capture.** One tracked file changed; nothing in `Assets` / Scenes / `ProjectSettings` / certified core; clean shutdown; log ends with batchmode return code 0 |
| **R3 verdict** | **CONDITIONAL / STOP — correct.** A real dependency-graph mutation is not a "harmless first open". Stopping instead of absorbing it is the behaviour this gate exists for |
| Provenance self-correction in the state doc (naming `d6fe5b9a` as the production tree) | **ACCEPTED and good practice** — the ancestry is what git says, not what the prose label says |

**Recorded, per F8:** the pre-migration revision `360f97ecca93` (4f1) and post-migration `3ca267ce8005` (12f1) are now both on the record.

---

## 1. WHAT COACH ADDED INDEPENDENTLY (facts neither party had stated)

Verified this session against the pushed blobs at `ae2d9c8`:

**F1 — Neither package is declared by the project.**
`Packages/manifest.json` declares `"com.unity.test-framework": "1.6.0"` and **no entry** for `com.unity.xr.core-utils` or `com.unity.test-framework.performance`. In the committed lock:
- `com.unity.xr.core-utils`: `source=registry`, **`depth=1`**
- `com.unity.test-framework.performance`: `source=registry`, **`depth=2`**

⇒ Both changes are **transitive resolver state**, not project-declared dependencies. This is the single most important fact for the disposition: the project never asked for these versions, so "we chose the new version" is not what a commit here would mean. A commit means *"we accept the 12f1 resolver's graph as canonical for this line"*.

**F2 — The exposure of `com.unity.xr.core-utils` is the XR runtime stack.**
Dependents found in the committed lock:

```
com.unity.xr.hands       -> com.unity.xr.core-utils
com.unity.xr.management  -> com.unity.xr.core-utils   (requests 2.2.1)
com.unity.xr.openxr      -> com.unity.xr.core-utils   (requests 2.3.0)
```
The old lock resolved `2.5.3` while the loudest requester asks for `2.3.0`; the new lock resolves `2.6.0`.
⇒ The affected surface is **XR initialisation and hands/interaction**, not physics or rules. That is what makes the risk **asymmetric** between the two packages.

**F3 — `com.unity.test-framework.performance` is test-only tooling.** It sits under the test framework, is not part of a player build, and its bump (3.4.0 → 3.5.0) cannot reach the shipping artifact. Low consequence either way.

**F4 — The requester that demands the new minimum is not yet identified.** The old lock does not record a `2.6.0` requirement from any listed dependent. **UNKNOWN** — and it is a one-minute check, not an investigation (see §3, step 1).

---

## 2. RECOMMENDATION

**Recommend `D1 — ACCEPT WITH DECLARATION`: commit the 12f1-resolved `packages-lock.json` as its own commit, with the cause, scope and from→to recorded, and no manifest change.**

Why, in order of weight:

1. **A bare revert is not durable.** `packages-lock.json` is a *derived* artifact. Reverting it does not restore 4f1 behaviour; it leaves a file that the next 12f1 open will re-derive into the same state. The tree would be permanently dirty, every subsequent evidence run would carry the same noise, and each session would re-litigate it. **A revert is only meaningful together with something bigger — a manifest pin (D2) or an editor rollback (forbidden).**
2. **The delta is small, bounded and explainable.** Two entries, two version strings, one file, `2 insertions / 2 deletions`. No `Assets`, scene, `ProjectSettings` or certified-core change — so nothing in the certified physics chain is re-serialised by this.
3. **The risk is confined and nameable.** One package is test-only (F3). The other touches the XR/hands stack (F2) — which means it must be cleared by **runtime evidence**, not by "it compiled". That is an obligation we can meet at S3/S5 and at the device gate; it is not a reason to freeze the line.
4. **It keeps the record honest.** If we pin instead (D2) we add direct dependencies the project does not need; if we revert (D3) we create permanent churn. D1 states exactly what is true: *this line's dependency graph is the 12f1-resolved graph.*

**Alternatives, with trade-offs — owner's call:**

| Option | Shape | Trade-off |
|---|---|---|
| **D1** accept + commit (recommended) | lock committed as its own commit; graph = "as resolved by 12f1" | graph is editor-determined; future editor versions may move it again — mitigated by the idempotence check in §3 |
| **D2** accept + **pin** in `manifest.json` | declare `com.unity.xr.core-utils: 2.6.0` and `com.unity.test-framework.performance: 3.5.0` as direct deps | makes the graph project-determined (consistent with this project's determinism priority) but adds two direct dependencies the project does not otherwise want, and turn a resolver fact into a maintenance commitment |
| **D3** revert + investigate | `git checkout` the lock, then find the requester | **Coach advises against as a standalone**: not durable (see #1); and the investigation is worth doing *anyway* (step 1 below) — but it should inform D1/D2, not replace them |

Whatever is chosen, **the decision belongs to the owner** and must be recorded in `Docs/147VR_CURRENT_STATE.md` by name.

---

## 3. ACCEPTANCE CRITERIA FOR THE DISPOSITION

**Step 1 — identify the requester (one minute, before committing).**
In the *new* lock, search the `dependencies` maps for the entries that now demand the higher minimum:
```
search: com.unity.xr.core-utils  -> any entry demanding >= 2.6.0
search: com.unity.test-framework.performance -> any entry demanding >= 3.5.0
```
Record the answer. If a listed dependent's requested version changed, the story is "a bundled ecosystem package raised its floor"; if **no** listed dependent changed, the story is "the editor's resolver elevated the floor itself" — and that distinction belongs in the commit message. Do not guess it.

**Step 2 — the disposition commit.** One commit, one file, message must contain: cause (first open under 12f1), scope (two transitive packages), exact from→to, the requester finding from step 1, and the explicit statement *no manifest change was made*. Nothing else may be bundled into it.

**Step 3 — idempotence check (the decisive acceptance test).**
After committing, open the integration line **once more** with 12f1 and close it cleanly:
- if `packages-lock.json` shows **no further change** → the graph is **settled** for this editor version. This is what converts D1 from "we gave up and committed it" into "we verified the graph is stable".
- if it changes again → STOP; the graph is not settled and D1's rationale is void.

**Step 4 — F9 fix on the R3/R3-b evidence.**
`UnityBatch_20260923_114111.log` (12,083 lines, SHA256 `4DC81FC1DD6F38E8DB62538FB27C2355870E779C7CE238025EF9AC1364CC9E52`) is a `.log` under the batch-log area, i.e. **not a tracked artifact** — the F1/F9 pattern. Copy the decisive lines into a tracked `.txt` with a header containing: `WHICH COMMIT` (`4a18404`) · `UNITY_EDITOR=6000.4.12f1 (3ca267ce8005)` · `TARGET_TREE` + `TARGET_TREE_HEAD` + `TARGET_TREE_BRANCH` · `UNITY_PACKAGE_MANAGER_SHA256` · the two lock lines before/after · `Exiting batchmode successfully now!` + the return code · **the original log path and its SHA256**. The log's hash is good provenance; it does not substitute for the tracked artifact.

**Step 5 — record the exposure surface.** Add to the state doc, next to the disposition, the three `xr.core-utils` dependents from F2, so the re-verification set is not decided from memory later.

**Step 6 — idempotence + status.** After steps 1–5: working tree clean, `git status --short` empty, and no Unity/`UnityPackageManager` residue.

**INVALID, not PASS** — if the second open's argv/CWD/`-logFile` (absolute) were not captured, or the run was bounded shorter than the observation window, the idempotence check is invalid and must be re-run.

---

## 4. WHAT THE DISPOSITION DOES *NOT* CLEAR

Accepting this lock delta clears **only** "the dependency graph moved and we acknowledge it". It does **not** clear, and must not be cited as clearing:

- **XR/Quest runtime behaviour under the new `core-utils`** → S3 (Golden regression) + S5 (REAL10) on this line, and XR init + hands tracking specifically at the device gate (GATE 5).
- **Physics certification** → untouched by this delta, but any claim that the line is *validated* still requires the S-series evidence.
- **F9b (L2 reproducibility debt)** → still open: the L2 probe was deleted and `config:project:get-registries` is reachable only through the internal UI client, so L2 cannot currently be re-verified from the repository. Fix it while the empty project is still to hand.
- The non-fatal log observations from R3 (Packages directory-monitor buffer overflow messages, duplicate `System.Runtime.CompilerServices.Unsafe.dll` warning, XR `StopSubsystems` shutdown warning) → **record as observations, do not chase**. They are not failures and no disposition depends on them; only revisit if they later correlate with a real failure.

---

## 5. NEXT STEPS, IN ORDER

1. Step 1 (requester) → Step 2 (disposition commit) → Step 3 (idempotence) → Step 4 (F9 artifact) → Step 5 (exposure record) → Step 6 (clean tree).
2. **S1b** — commit the F9b probe + `upm.log` excerpt so L2 is reproducible.
3. **S2** — Phase 2 rules validation on `integration/008-m53` (legal/illegal, foul, ball-on, frame end, match) — the rules exist; they are not yet validated on the shipping-shaped tree.
4. **S3** — fresh current-profile M4.2 re-capture (currently explicitly historical).
5. **S5** — REAL10 / M5 re-run on this line.
6. **S4** — Phase 3 / 008 non-destructive integration using the corrected containment criterion (strip `147VR_PROPS_ROOT` from both versions, compare as parsed `fileID→block` maps).
7. **S6** — close GATE 4, then the device gate.

**Prohibitions unchanged:** certified core · `147VR_M53_VALIDATE` · the real project beyond what this order authorizes · system environment · firewall · tracked launcher (F11 resolved by B′) · no further toolchain work without a proven new failure.
