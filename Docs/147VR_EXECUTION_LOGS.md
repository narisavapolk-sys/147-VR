# 147 VR — Execution Logs

## 2026-09-04 — YOLO Long Run Start
- Master Production Roadmap approved and persisted as `Docs/147VR_MASTER_PRODUCTION_ROADMAP.md`.
- Current phase confirmed: Phase 3 — Table Visual & Marking Overlay.
- Physics Authority remains protected; no physics code/data modified in this run.
- Located V007 Blender source: `SNOOKER   VR pool table/Blender/147VR_Table_WPBSA_Visual_Clean_v007.blend`.
- V007 source inspection completed with Blender 5.2 background `bpy` audit.
- `TABLE SURFACE` exists as a mesh with active `UVMap` and material `FELT`.
- V007 TABLE SURFACE world bounds measured approximately X=3.569m, Y=1.778m, Z=0.0127m.
- V007 TABLE SURFACE world center is approximately (0.000566, -0.000574, 0.820861).
- UV bounds measured: U 0.335959–0.610543, V 0.007877–0.880145.
- Existing prefab audit confirms WPBSA 12-foot play-area dimensions 3.569m × 1.778m and ball diameter 52.5mm.
- Existing visual prefab contains named colour balls and current positions; these are being treated as evidence only, not as a replacement for Physics Authority.
- Important architecture finding: Blender/V007 mesh plane and Unity runtime physics plane use different axis conventions; the final mapping must be derived explicitly before baking markings.

## Next autonomous execution
1. Derive exact V007 UV↔surface affine mapping from mesh UV data.
2. Extract authoritative runtime ball/table coordinates from the active physics scene/path.
3. Generate deterministic high-resolution anti-aliased marking texture.
4. Bind marking overlay to TABLE SURFACE/FELT without adding floating marking geometry.
5. Disable only confirmed giant-plane visual junk and duplicate character visuals after scene-level evidence.
6. Verify ball-center ↔ spot alignment.
7. Run compilation/runtime verification when package-aware Unity startup permits it.
8. Append PASS/FAIL evidence here after each meaningful gate.

## 2026-09-04 — M7.4 Marking Geometry / Asset Preparation
- V007 top-face UV audit completed: top-face UV mapping is planar and deterministic, with U driven by surface Y and V driven by surface X; RMSE is below 1e-8 for the fitted top-face mappings.
- Existing UVMap is unsuitable as a dedicated marking UV because its top-face UV island occupies only a very small region; this would not provide robust marking pixel density.
- Added a dedicated normalized `MarkingUV` channel to a parallel V007 source, preserving the original source UV channel.
- Created non-destructive candidate source: `147VR_Table_WPBSA_Visual_Clean_v007_MARKING.blend`.
- Exported parallel Unity candidate FBX: `Assets/AAA/ImportedSnooker/Source/147VR_Table_WPBSA_Visual_Clean_v007_MARKING.fbx`.
- Extracted packed V007 FELT image to `Assets/AAA/ImportedSnooker/Textures/Green_Felt_Texture_V007.png`.
- Generated high-resolution 8192×4096 transparent `Snooker_Markings_V007.png` with deterministic Baulk Line, D arc and six colour spots.
- Marking geometry uses WPBSA dimensions: play area 3569×1778mm, Baulk line 737mm from Baulk cushion, D radius 292mm, Black 324mm from Top cushion. Official WPBSA rules corroborate these dimensions.
- Created candidate custom URP material shader source `Assets/Shaders/147VR_TableSurfaceMarking.shader` using UV0 for FELT and UV1 for markings.
- Unity batch asset invocation exited 0, but the already-running Unity editor prevented observable asset-build output; no active scene/prefab was overwritten.
- Current safety state: original V007 blend preserved; active scene/Physics Authority untouched.

## 2026-09-04 — M7.4 Runtime Reality Audit
- UPM startup was verified healthy when launched with the persisted Desktop Commander environment (`PROGRAMDATA`, `APPDATA`, `LOCALAPPDATA`, `USERPROFILE`, `TEMP`, `TMP`, `NO_PROXY`; no `npx` contamination).
- Runtime audit opened `Assets/Scenes/147VR_MainScene.unity` successfully and enumerated the active table hierarchy.
- The active runtime TABLE SURFACE is under `ConcertRoom/Prefab_WPBSA_12Foot_Snooker/Visual_Meshes_Drop_Here/147VR_Table_WPBSA_12ft_VISUAL_MAIN/TABLE SURFACE`.
- Critical finding: the active MainScene table is scaled approximately 100× too large in world space: TABLE SURFACE bounds are about 356.90m × 177.80m, while the source asset is 3.569m × 1.778m.
- The active `Bed_Collider` is affected by the same scene-scale multiplication: world bounds are about 177.80m × 356.90m, while the authority prefab dimensions are 1.778m × 3.569m.
- This is a MainScene transform/parent-scale integration defect, not a source-asset dimension defect. The source V007 TABLE SURFACE dimensions remain correct.
- The active visual colour-ball transforms are likewise multiplied by ~100 in world space (Blue Y≈85.35m, Black X≈143.48m, Pink X≈85.92m), confirming a common parent-scale issue rather than independent ball-placement drift.
- The standalone older visual prefab in MainScene is inactive; the visible runtime table is the visual nested inside the approved physics prefab.
- The runtime hierarchy also confirms two active character rigs (`CuteGirl_Dancing` and `ChubbyGirl_Dancing`); these are separate scene roots and should be de-duplicated only after table scale is corrected.
- The V007 candidate source reference in `Prefab_WPBSA_12Foot_Snooker.prefab` is now versioned as `V007` and points to GUID `1ccd8aa316db96d48a7113b34b093648`; the previous V006 GUID is absent from that nested visual block.
- Geometry integrity audit found eight genuinely self-intersecting `TABLE FRAME` ngons in the V007 marking copy; a parallel V008 candidate was created by triangulating only those eight faces.
- Unity still reports one self-intersecting TABLE FRAME polygon on V008 import, so V008 is diagnostic only and is not promoted into the active physics prefab.
- The self-intersection locations include table-end cap geometry and a side-pocket/rail region; this remains a visual geometry issue requiring targeted inspection, not a reason to alter Physics Authority.
- A MainScene scale override of 0.01 is the correct intended normalization because it neutralizes the observed 100× parent multiplication while leaving the physics prefab asset and Golden calibration data unchanged.
- The MainScene file is currently held open by another process, so the scale override has not been written yet; no partial scene write occurred. A pre-fix backup exists as `147VR_MainScene.unity.PRE_M74_SCALEFIX_20260904.bak`.
- M7.4 remains OPEN pending: MainScene scale normalization, authoritative visual↔physics spot alignment verification, and pocket/seam geometry sanity gate.

## 2026-09-07 — Phase 4 Pocket/Jaw Visual Sanity Gate
- Re-audited the V007 Blender source non-destructively; no source geometry was edited.
- V007 contains six explicit pocket assemblies with `Pocket_Throat_*`, `Pocket_Brass_Rim_*`, `Pocket_Net_*`, leather/stitch details and six `ANCHOR_Pocket_*` spatial anchors.
- Visual pocket anchors were compared against the existing `SnookerPhysicsSetup.BuildPocketCatchers` model using the established 3.569m × 1.778m playfield dimensions.
- Anchor-to-catcher center gaps: SW 9.91mm, NW 7.19mm, SE 9.14mm, NE 7.37mm, W 11.31mm, E 13.11mm; all remain inside the existing 150mm runtime catch radius.
- Pocket throat meshes are present for all six pockets, each 96 vertices / 50 polygons / 112mm × 112mm × 18mm bounds.
- No explicit `Jaw`-named mesh exists in V007; jaw/cushion-facing geometry is represented by the cushion/pocket assembly rather than a separate jaw object.
- Result: **PASS — visual pocket alignment sanity gate**; no visual repair was justified from this evidence alone.
- Physics Authority, Bed_Collider and Golden data were not modified.
- Persisted evidence: `Docs/M7_4_V007_POCKET_ANCHOR_AUDIT_20260907.json`.
## 2026-09-07 — Phase 3/4 Residual Gate
- Re-read the active M7.4 execution plan before continuing; guardrails remain visual-only and Physics Authority protected.
- MainScene YAML confirms the V007 visual prefab is instantiated under the canonical table root with neutral local transform overrides; no V006/V005 visual block was found in MainScene by direct scene-content search.
- MainScene contains only one explicitly inactive root (`Quest Setup`); it was not deleted because no dependency evidence justified removal.
- Existing scene still contains the legacy `Prefab_WPBSA_12Foot_Snooker` prefab instance separately from the V007 visual prefab. Its serialized component overrides are heavily disabled, but it is not safe to classify the whole instance as removable without dependency/runtime evidence; no deletion performed.
- Phase 4 Blender audit reconfirmed six pocket throat meshes, six pocket anchors, four cushion meshes and pocket-art details. No explicit jaw-named mesh exists; jaw-facing geometry is integrated into the pocket/cushion assemblies.
- Pocket anchor sanity remains PASS: maximum anchor-to-runtime-catcher center gap 13.11mm, within existing 150mm catch radius. No visual geometry repair is justified by current evidence.
- A Unity static audit attempt was blocked by a transient UPM IPC startup failure (`Upm-9408`, 30s); no scene or asset changes resulted. This is an environment/tooling blocker only and does not invalidate prior successful filtered PlayMode evidence.
- Decision: do not perform speculative cleanup or pocket/jaw edits. Preserve current V007 visual state and continue with evidence-driven Phase 3/4 checks once package-aware Unity execution is available.

## 2026-09-07 � Phase 3 22-Ball Truth Closure
- Read-only Unity audit executed against MainScene after Bee cache regeneration.
- V007_VISUAL_MAIN contains exactly 22 active named snooker balls.
- All 22 active balls have Rigidbody bodies generated by SnookerPhysicsSetup on the same transforms; no separate visual-only offset was introduced.
- Baulk colors, Pink, Black and full 15-red rack match the persisted V007 marking/world coordinate frame.
- Red rack spacing observed: 0.052578 m, consistent with the authored 52.5 mm WPBSA ball standard within mesh/source precision.
- Evidence: Docs/M7_4_V007_22BALL_ALIGNMENT_AUDIT_20260907.json; Unity log Phase3Truth_20260907_R6.
- No scene save; Physics Authority, Bed_Collider and Golden data untouched.
- Verdict: PASS � no Phase 3 ball alignment repair justified.


## 2026-09-07 � WPBSA Standardization Gate / Phase 3
- Official WPBSA 2024-25 rule geometry was used: playing area 3569 x 1778 mm; Baulk-line 737 mm from Baulk cushion face; D radius 292 mm; Pink midway; Black 324 mm from Top cushion face.
- Pre-fix V007 MainScene colored layout was materially off: Yellow/Green/Brown were not on D corners/center and Pink/Black were displaced; existing red rack spacing was 52.578 mm rather than the 52.5 mm nominal ball diameter.
- Corrected only active V007 MainScene gameplay ball transforms to the standard coordinate frame; legacy inactive 22-ball set untouched.
- Regenerated Snooker_Markings_V007.png with the same verified UV basis, now using the WPBSA coordinates.
- Post-fix audit: exactly 22 active V007 balls; surface bounds 3.569 x 1.778 m; official coordinates persisted in Docs/M7_4_V007_22BALL_ALIGNMENT_AUDIT_20260907.json.
- Pixel spot probe hit the six expected marking colors at the new coordinates.
- Certified physics calibration scenes/data were not edited. MainScene gameplay spawn transforms were intentionally updated as a standardization correction.
- PlayMode test runner was attempted but batch test infrastructure stalled during assembly/test initialization; no failing gameplay assertion was produced. Separate runtime-gate attempt entered Play Mode but did not reach the callback before batch hang; no further scene changes were made.
- Verdict: Phase 3 WPBSA layout correction PASS; Phase 6 runtime PlayMode certification remains OPEN pending a clean test-runner execution.

