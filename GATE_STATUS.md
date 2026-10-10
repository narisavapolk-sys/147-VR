# 147 VR — Production Gate Status

> Project: 147 VR
> Unity: 6000.4.4f1 (canonical, locked)
> Platform focus: VR (Meta Quest 2/3)
> Status: GATE 3 COMPLETE · GATE 4 (toolchain) PROVEN · GATE 5 (device) PENDING
> Last updated: 2026-10-10

## Gates

- GATE 1 Foundation: COMPLETE
- GATE 2 Core Systems: COMPLETE
- GATE 3 Production Readiness: COMPLETE
- GATE 4 Toolchain & integration-line proof: **PROVEN (2026-10-10)** — see below
- GATE 5 Device (Quest) gate: **PENDING** — no on-device evidence yet

## GATE 4 — Toolchain (resolved 2026-10-10)

- The long-standing "Package Manager could not connect to IPC after 30s" blocker is **resolved**: Unity 6000.4.4f1 batch now connects UPM in **0.7s** (license OK, 86 packages registered) via the canonical launcher `Docs\Tools\Unity_Batch_Safe.ps1` (sanitized PATH + full env incl. ALLUSERSPROFILE, single-instance guard, no `-noUpm`).
- Launcher drift consolidated: `Tools\Launch_147VR_Safe.ps1` → `.DEPRECATED`; only `Unity_Batch_Safe.ps1` is approved.
- Editor standardized to **6000.4.4f1** (pinned in `ProjectSettings/ProjectVersion.txt`). 6000.4.12f1 and 6000.4.5f1 removed from the machine to stop Bee full-rebuild churn.
- The batch-mode ShaderGraph "GUID" compile errors were **batch-only artifacts**; opening the project in the Unity GUI editor loads and compiles cleanly (MainScene renders).
- Machine stability: hard freezes during this work were traced to **RAM exhaustion (16 GB)** when Unity batch overlapped Blender/Chrome — NOT Defender (project path already AV-excluded) and NOT disk (SSD). Workflow rule: run Unity and Blender **one at a time**, other heavy apps closed.
- Residual: a clean package-aware batch exit-code-0 capture has not been recorded post-fix (batch aborts on editor-only package errors); the GUI editor is the current compile authority.

## ART — Phase 3 Table Visual (in progress, 2026-10-10)

- New table + venue art received as Blender sources (`.blend`): `147VR_Table_WPBSA_Visual_Clean_v006_DARC_LIGHTING_ART_V04` (table) and `147VR_008_CRUCIBLE_..._NO_TABLE` (venue).
- Measured vs certified: play surface **3.57 × 1.78 m** matches the Golden-certified footprint → **not a geometry regression**. No colliders/rigidbodies in the art (physics authority untouched).
- Exported a clean **game FBX** (137 parts; 22 balls + a 43×82 m stray floor excluded; pocket clamps included), staged at `Assets\AAA\ImportedSnooker\V006_DARC_V04\`.
- SFX imported to `Assets\Audio\SFX` (13 clips: cue strike/chalk, ball-ball/cushion/pocket, crowd ambience/gasp/cheer/applause, fireworks).

### ART — open items / risks

- **Materials are procedural/DARC-lit and do NOT survive FBX export** → must **bake** basecolor/normal/roughness/AO (Cycles; Windows headless cannot use EEVEE) and rebuild URP materials, then relight in-engine.
- **~409k tris** on the table alone → needs **decimation/LOD** before Quest certification.
- **Visual felt top Z ≈ 0.83 vs certified physics plane 0.756** → lower/align the visual ~7 cm on placement; long axis is Blender-X (may need 90° rotation).
- Swap must be **non-destructive**: duplicate `147VR_MainScene` first; keep `Bed_Collider` + `SnookerPhysicsSetup` + spot markings authoritative; re-run Golden regression after.

## Rule (unchanged)

Source/contract completeness is never reported as runtime/production completeness. No PASS claim without committed evidence artifacts.

## ART verification result (2026-10-10)

Headless landmark comparison **v006 (new art) vs v007 (current certified)** — evidence in `Docs/ART_STAGE/compare_v006_v007.json`:
- **Pockets (6) and cushions (4): identical coordinates** → potting/cushion gameplay unaffected by a swap.
- **Black / Pink / centre(Blue) / reds triangle: identical.**
- **Baulk line / D / bottom colours differ — and v006 is the WPBSA-correct one:** v006 baulk 737mm, D radius 292mm, Brown at baulk centre (all per WPBSA); v007 has D radius ~330mm and Green/Brown swapped (off-spec). v006 is therefore an upgrade, not a regression.
- Code check: `CuePhysicsAdapter`/`BallMotionController` use `ballRadius = 0.02625` (52.5mm) — matches v006 art; `SnookerBallTracker` captures ball `homePosition` from scene transforms at runtime (no hardcoded spot conflict). Balls are runtime-spawned, so edit-mode shows none (expected).

**Swap status:** executed non-destructively to `Assets/Scenes/147VR_MainScene_ART_V006.unity` (original `147VR_MainScene.unity` untouched, LastWrite 2026-09-25). Old table disabled there for rollback.

**Next steps (require Unity CLOSED before any Blender work — RAM/freeze):**
1. Bake v006 materials (Cycles: basecolor/normal/roughness/AO) → rebuild URP materials (procedural/DARC did NOT survive FBX; table currently looks flat).
2. Align new table felt top to physics plane 0.756 (art felt ~0.83 → lower ~0.07).
3. Import venue (`147VR_008_CRUCIBLE_..._NO_TABLE.blend`) as separate dressing layer.
4. LOD/decimate the ~409k-tri table before Quest.
5. AudioMixer + set SFX import to Vorbis/Streaming + wire SFX to gameplay events.
6. Re-run Golden regression to confirm physics unchanged after the swap.

## Current handoff state (2026-10-11) — see Docs/ART_STAGE/STATUS_LOG.md for the authoritative snapshot

- Chosen base = **v008_OPT** (WPBSA-verified, 243k tris + LODs, URP materials + FELT + AO staged).
- **Commit `377eb543`** on `integration/008-m53` exists but is **NOT pushed** (read-only PAT; owner pushes).
- **Still pending verification**: arena skybox display (owner saw gray), table height/placement vs physics plane 0.756, SFX import settings.
- **Not done**: AudioMixer + SFX event wiring, LODGroup assembly, material bake, **Golden regression re-run**, Quest perf/device gate.
- Do NOT trust "source complete" as "runtime complete"; re-run Golden before claiming the swap is safe.
