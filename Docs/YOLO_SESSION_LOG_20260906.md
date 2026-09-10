# YOLO Session Log — 2026-09-06

## V007 MARKING promotion / validation
- Coach go-ahead received; delegated autonomous execution applies.
- Existing MainScene backup verified before promotion work.
- Additional reversible backup created: `Docs/AI_TEAM/BACKUPS/147VR_MainScene_PRE_V007_PROMOTION_20260906.bak`.
- Initial raw Unity invocation was superseded by the standing rule; all subsequent Unity batch attempts use `Docs/Tools/Unity_Batch_Safe.ps1`.
- Lock check: no Unity process running; `Library/EditorInstance.json` was absent, so no lock deletion was needed.

## Safe-launch attempt 1
- Launcher: `Docs/Tools/Unity_Batch_Safe.ps1`
- Method: `LunaV007Promotion.Run`
- Result: FAIL-COMPILE (exit classification 5).
- Evidence: `Assets/Editor/YOLO_Promote_V007_MarkingPrefab_TMP.cs` had CS1061/CS0266 caused by conditional-expression type inference.
- Fixed locally by explicitly typing `Transform`, `Renderer`, and `MeshFilter`; no Physics/Gameplay/Golden data touched.

## Safe-launch attempt 2
- Launcher: `Docs/Tools/Unity_Batch_Safe.ps1`
- Method: `LunaV007Promotion.Run`
- Result: method threw because V007 visual already existed in MainScene.
- Interpretation: promotion had already been persisted; this was not a tooling/antivirus failure.
- Full Unity log: `Docs/UnityBatchLogs/UnityBatch_20260906_190320.log`.

## Current gate
- Next action: validate the already-persisted V007 instance in MainScene using Unity/editor evidence, then attempt Play Mode through the safe launcher.

## Coach decision follow-through — 2026-09-07
- PlayMode validation switched to Unity Test Framework `-runTests -testPlatform PlayMode`; V007 runtime test passed 1/1.
- Safe launcher was repaired so Test Runner invocations do not receive forced `-quit`.
- Coach observation on Green/Brown was verified against `Docs/M7_4_generate_markings.py`: original generator assigned Yellow=-0.292, Green=+0.292, Brown=0 on the width axis.
- UV audit proved V007 basis: `U_vs_X` slope=-1/3.569, `V_vs_Z` slope=-1/1.778, both RMSE ~0. This confirms generator X/Y correspond to Unity world X/Z with sign inversion in UV, not an axis swap.
- Persisted Physics ball-center frame maps to generator coordinates: baulk X=-1.019668; Yellow Z=-0.330263; Green Z=-0.000329; Brown Z=+0.329844; Pink X=+0.859162; Black X=+1.434759.
- Therefore Coach's Green/Brown swap was confirmed, and the audit also confirmed the smaller Pink/Black errors are magnitude differences from old WPBSA constants.
- Phase 3 generator corrected only: Green/Brown assignments and marking spot/baulk coordinates now use the persisted Physics-authoritative centers; Physics/ball spawn/Bed_Collider were not modified.
- Backup: `Docs/AI_TEAM/BACKUPS/M7_4_generate_markings_PRE_PHYSICS_SPOT_ALIGNMENT_20260907.py.bak`.
- Regenerated `Assets/AAA/ImportedSnooker/Textures/Snooker_Markings_V007.png` at 8192x4096.
- Pixel audit confirms generated color centers land at the expected UV-derived pixels for Yellow/Green/Brown/Blue/Pink/Black.

## 2026-09-07 — Coach Observation / Spot Generator Audit
- Coach observation received: suspected Green/Brown swap in `M7_4_generate_markings.py`; treat as hypothesis only until audit confirms.
- Direct inspection of current working-tree `Docs/M7_4_generate_markings.py` shows the active generator already uses persisted Physics-authoritative values transformed into generator frame: `baulk=-1.019668`, `yellow_z=-0.330263`, `green_z=-0.000329`, `brown_z=0.329844`, `pink_x=0.859162`, `black_x=1.434759`.
- Therefore the active generator assignments are Yellow -> -0.330263, Green -> -0.000329, Brown -> +0.329844 in the generator's width coordinate; Green/Brown are NOT currently swapped in this working copy.
- Direct pixel audit of `Snooker_Markings_V007.png` (8192x4096) found spot centers consistent with those values: Yellow ≈ (6435.7,2808.3), Green ≈ (6435.7,2048.3), Brown ≈ (6435.7,1287.7), Blue ≈ (4095.5,2047.5), Pink ≈ (2123.3,2047.5), Black ≈ (802.3,2047.5).
- The first UV linear-fit audit was not sufficient to establish world↔UV correlation because the table mesh UV topology is not a simple one-to-one linear fit over all 422 vertices; no Physics conclusion is drawn from that audit.
- No Physics Authority, ball spawn, Bed_Collider, or Golden data modified.

## 2026-09-07 � UV?World Per-Spot Closure Audit

Coach-suggested mapping-topology-safe audit was executed against the persisted `V007_VISUAL_MAIN/TABLE SURFACE` mesh using actual triangle UV1 interpolation in both directions. No Physics Authority was modified.

### Inverse UV?World result (generated marking pixel center ? actual V007 world position)
- Yellow texture `(6435.7,2808.3)` ? world `(-1.019900, +0.330607)`; Physics ball `(-0.330263,+1.019668)`; delta `(-689.6,-689.1) mm`, gap `974.9 mm`.
- Green texture `(6435.7,2048.3)` ? world `(-1.019901, +0.000704)`; Physics ball `(-0.000329,+1.019668)`; delta `(-1019.6,-1019.0) mm`, gap `1441.5 mm`.
- Brown texture `(6435.7,1287.7)` ? world `(-1.019900, -0.329459)`; Physics ball `(+0.329844,+1.019668)`; delta `(-1349.7,-1349.1) mm`, gap `1908.4 mm`.
- Blue texture `(4095.5,2047.5)` ? world `(-0.000348,+0.000357)`; Physics ball `(0,0)`; gap `0.5 mm`.
- Pink texture `(2123.3,2047.5)` ? world `(+0.858878,+0.000357)`; Physics ball `(0,-0.859162)`; gap `1215.1 mm`.
- Black texture `(802.3,2047.5)` ? world `(+1.434397,+0.000357)`; Physics ball `(0,-1.434759)`; gap `2029.1 mm`.

### Conclusion
This closes the per-spot UV?World evidence loop and **fails visual/Physics alignment for 5/6 spots**. The issue is not a simple Green/Brown ordering swap inside the current generator: the current PNG spot centers are consistent with the generator's transformed Physics-authoritative values, but the persisted V007 mesh UV1 maps those pixels to a visual coordinate frame rotated/mirrored relative to the Physics ball-center frame. Blue is the only coincident spot.

Current V007 surface world bounds are `x=[-1.785066,+1.783934]`, `z=[-0.888426,+0.889574]`, while Physics ball centers use the long axis on world Z (Yellow/Green/Brown at `z=+1.019668`, Pink `z=-0.859162`, Black `z=-1.434759`).

**Authority rule:** Physics ball centers / Bed_Collider remain untouched. No generator or prefab correction has been applied yet because the evidence identifies a broader V007 visual-frame mismatch, not the originally suspected isolated Green/Brown swap.

## 2026-09-07 - V007 Marking UV/World Closure
- Re-ran the audit in the actual Unity MainScene frame before changing the visual hierarchy. `TABLE SURFACE` is runtime-rotated to the correct X/Z playfield frame; the earlier Blender-space inverse audit had compared against the wrong frame.
- Root cause isolated to the marking generator: PIL pixel-Y was mirrored relative to Unity UV V. The generator was using `0.5 - z/A` for pixel Y, which inverted the marking texture along V.
- Corrected only `Docs/M7_4_generate_markings.py`: pixel Y now uses `0.5 + z/A`, spot radii are scaled independently in X/Y, the baulk line uses the authoritative `baulk_x`, and the D is generated as the interior/right half-circle toward the black end.
- Generator backup: `Docs/AI_TEAM/BACKUPS/M7_4_generate_markings_PRE_V007_V_MIRROR_FIX_20260907.py.bak`.
- Regenerated `Assets/AAA/ImportedSnooker/Textures/Snooker_Markings_V007.png` at 8192x4096. Existing material `M_V007_TableSurface_Marking` already references this texture GUID; no material/scene transform edit was required.
- Topology-safe actual Unity UV1->world audit now reports: Yellow 0.8mm, Green 1.1mm, Brown 0.8mm, Blue 0.5mm, Pink 0.5mm, Black 0.5mm. All six are INSIDE mesh triangles with zero UV sampling delta; max gap 1.1mm.
- Independent texture pixel audit: baulk-line hit rate 98.51%, D-arc hit rate 100%, all six spot centers contain the expected marking colors at their corrected pixel locations.
- Filtered Unity Test Framework PlayMode validation: `V007PlayModeValidationTests.V007_MainScene_Visual_Is_RuntimeValid` PASS 1/1, bounds 3.57 x 0.0127 x 1.78, material `M_V007_TableSurface_Marking`.
- One earlier unfiltered PlayMode attempt was terminated because it did not converge; it left Unity running briefly. Processes were cleared before the filtered rerun. The filtered run completed normally with exit code 0.
- Persisted closure evidence: `Docs/M7_4_V007_MARKING_CLOSURE_20260907.json`, `Docs/M7_4_V007_MARKING_PIXEL_AUDIT_20260907.json`, `Docs/V007_PlayMode_Results_20260907.xml`.
- Physics Authority, ball spawn, `Bed_Collider`, Golden data, and `V007_VISUAL_MAIN` hierarchy were not modified.
- V007 marking alignment status: **EVIDENCE-COMPLETE** for the current MainScene visual contract; no Physics Authority recertification implied.


## 2026-09-08 — Launcher Consolidation / UPM Root Cause Confirmed
- Coach independently inspected `Tools/Launch_147VR_Safe.ps1` and confirmed it was the legacy launcher dated 2026-08-31: correct environment variables were present, but there was zero PATH sanitization and no single-instance guard.
- The legacy launcher invokes Unity directly via `& $unity -batchmode -projectPath $project -quit -logFile -` and can inherit the contaminated PATH containing `npm-cache\_npx\...\node_modules\.bin`.
- This matches the recent UPM IPC timeout signature: Unity fails during UPM bootstrap before the target `-executeMethod` is reached.
- Coach renamed the legacy launcher to `Tools/Launch_147VR_Safe.ps1.DEPRECATED`; it is retained for history and is not to be invoked.
- **LOCKED launcher rule:** all future 147 VR batch Unity work must use `Docs/Tools/Unity_Batch_Safe.ps1` only.
- Required clean environment remains: canonical Windows env vars, `NO_PROXY=localhost,127.0.0.1`, `UNITY_UPM_TIMEOUT=120`, and PATH sanitization removing inherited npm npx `.bin` entries.
- `-noUpm` remains prohibited because package-aware Unity/UPM is required by the project.
- M5.1 boundary unchanged: no M5.1 code/scene changes are considered valid until a clean run through the approved launcher reaches `[M5.1 INSTALL PASS]`, `SaveScene()` succeeds, and persisted scene YAML verifies the serialized components.
- Added durable tooling note: `Docs/TOOLING_KNOWN_ISSUES_UPM_LAUNCHER.md` so future AI agents can recognize and recover from this failure class without rediscovering the root cause.
- No Physics Authority, Golden data, or M5 gameplay code was modified by this documentation/consolidation action.
