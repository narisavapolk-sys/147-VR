# M5.3 / Production 008 Geometry, Collider & Calibration Contract

**Date:** 2026-09-22
**Status:** DRAFT FOR COACH REVIEW - NO MAIN SCENE MUTATION
**Production source:** Blender 147VR_Table_WPBSA_Visual_Clean_v008_MARKING_CLEAN.blend
**Protected baseline:** V007 remains historical/Golden; this document does not modify it.

## 1. Gate
This is a review artifact only. Before Coach accepts it: do not replace the Main Scene table prefab, change M5 gameplay core, or mutate Golden calibration assets.
Gate: Git LOCK -> Coach review -> 008 provenance -> geometry mapping -> collider contract -> physics ownership -> staging -> integration validation -> tracked REAL10 -> Coach audit -> Human approval -> production integration.

## 2. Verified 008 source facts
- Blender 5.2.1 LTS.
- Verified source inventory: 168 objects/collections.
- TABLE SURFACE: 3.569 x 1.778 x 0.0127 m.
- Pocket anchors: ANCHOR_Pocket_E, ANCHOR_Pocket_W, ANCHOR_Pocket_NE, ANCHOR_Pocket_NW, ANCHOR_Pocket_SE, ANCHOR_Pocket_SW.
- Verified source includes 147VR_SPATIAL_ANCHORS, CUSHIONS, BALL CATCHERS, POCKET PADS, MARKINGS, BALLS, White_CueBall.
- Legacy V006_* names in the existing Unity prefab are not provenance evidence for 008.

## 3. Geometry / Unity mapping contract
| 008 source | Unity role | Contract |
|---|---|---|
| TABLE SURFACE | playfield reference | dimensions above; collider authority must be explicit |
| six ANCHOR_Pocket_* | six pocket anchors | one-to-one world-space mapping; no invented offsets |
| CUSHIONS | cushion visual/collision envelope | collider geometry must be measured/verified |
| BALL CATCHERS | catcher support geometry | must not silently become gameplay authority |
| POCKET PADS | pocket visual/support | collider role requires explicit assignment |
| 147VR_SPATIAL_ANCHORS | spatial references | reference only unless assigned a runtime role |
| White_CueBall | cue-ball identity/reference | preserve semantic identity; no generic-name inference |

Current Unity prefab: Assets/AAA/ImportedSnooker/Prefab_WPBSA_12Foot_Snooker.prefab. Observed Bed_Collider BoxCollider size {x: 1.778, y: 0.05, z: 3.569} and center {x: 0, y: -0.025, z: 0}, plus pocket anchors and TABLE SURFACE. This is the comparison baseline, not permission to replace the prefab.

## 4. Physics ownership contract
Observed: SnookerPhysicsSetup exposes 	ableSurfaceProfile; Main Scene currently references a TableSurfaceProfile asset. Main Scene also serializes allBounciness=0.8, allFriction=0.05, allRadius=0.026. SnookerPhysicsSetup.MakeMaterial() independently creates a shared PhysicsMaterial with bounciness 0.8, dynamic/static friction 0.05, minimum friction combine, maximum bounce combine.
Observed runtime profile TableSurfaceProfile_RuntimeBaseline.asset: measuredTruthCertified=1; rollingFriction=0.45200002; rollingDamping=0.21000001; slidingFriction=0.6807868; spinFriction=0.20040171; settleSpeed=0.012; settleSpin=0.035; rollSlipTolerance=0.03.
Observed TableSurfaceController consumes the profile and applies profile-driven collision behavior only when measuredTruthCertified=true.
Contract decision: TableSurfaceProfile is sole authority for table-surface cloth/motion coefficients; TableSurfaceController is consumer/bridge; SnookerPhysicsSetup installs runtime geometry. Its 0.8/0.05 material must be explicitly classified as a separate ball/rail contact material OR reconciled as duplicate table-surface authority. No silent two-writer physics model.
147VR-PHY-008.asset is Golden M3 Angle30 evidence/reference; do not mutate it to make 008 pass.

## 5. Collider contract
1. Bed_Collider is current authoritative playfield collider when present and usable.
2. TABLE SURFACE is visual/reference unless explicitly assigned collision authority.
3. Cushion colliders must be mapped to 008; decorative FBX colliders cannot become accidental gameplay authority.
4. Pocket catchers/triggers must map to the six 008 anchors and preserve the existing gameplay event contract.
5. Existing Bed_Collider dimensions must be compared with imported 008 before replacement.
SnookerPhysicsSetup currently searches for Bed_Collider first and derives runtime bounds from it when usable. Preserve or deliberately change only after evidence review.

## 6. REAL10 evidence contract
A production 008 REAL10 PASS cannot rely on an untracked .log alone. Decisive evidence must be tracked .txt, .json, .xml, or equivalent. Minimum fields: SCENE, TABLE_ASSET_PROVENANCE, TABLE_SURFACE_DIMENSIONS, PHYSICS_PROFILE_OWNER=TableSurfaceProfile, COLLIDER_CONTRACT, SHOT_COUNT=10, FIRED=10, RESOLVED=10, SETTLED=10, ERRORS=0, SEQUENCE_CHECK=PASS, SHOT_IN_PROGRESS_CHECK=PASS.
Also record Unity version, commit, timestamp, and whether Quest 2/3 hardware or editor/desktop runtime was used. Do not claim Quest hardware certification without an actual hardware run.

## 7. Forbidden before Coach review
- No Main Scene prefab replacement.
- No M5 rules/scoring/turn core changes.
- No V007/Golden/PHY-008 mutation to force a pass.
- No rewrite/delete of Coach independent audit.
- No REAL10 certification from .log as sole evidence.

## 8. Coach review request
Please independently review the repository and identify: (1) missing/incorrect 008 geometry mappings; (2) collider authority conflicts; (3) physics ownership conflicts, especially SnookerPhysicsSetup constants vs TableSurfaceProfile; (4) missing calibration evidence; (5) gates that must remain closed.
**No Main Scene production integration is authorized by this document.**
