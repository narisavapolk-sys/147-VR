# M5.3 / 008 Geometry, Collider & Calibration Contract — REVISION 1

**Date:** 2026-09-22  
**Status:** CONDITIONAL PASS — GOVERNANCE REVISION / NO INTEGRATION  
**Base contract:** `Docs/AI_TEAM/M5_3_008_GEOMETRY_COLLIDER_CALIBRATION_CONTRACT_20260922.md`  
**Independent review:** Coach CONDITIONAL PASS; Coach audit remains immutable.  
**Protected baseline:** V007 / Golden remains locked.

## 0. Decision / gate state

This revision records the human-approved governance decisions following Coach's independent review.

**Current state**
- 008 Contract: CONDITIONAL PASS
- Main Scene: LOCKED
- M5 Rules / Scoring / Turn core: LOCKED
- V007 / Golden: LOCKED
- REAL10: LOCKED / NOT CERTIFIED
- Production integration: NOT AUTHORIZED

Required sequence remains:

`Contract revision → Coach review → 008 read-only measurement artifact → Coach review → non-destructive staging → integration validation → tracked REAL10 → independent Coach audit → Human approval → production integration`

No step may be skipped because a preceding document says PASS.

## 1. Red-line physics ownership: indivisible calibration unit

`SnookerPhysicsSetup.MakeMaterial()` and `TableSurfaceController` are treated as one shared physics/calibration contract for certification purposes.

The pair is **not** independently certifiable.

Rules:
1. `TableSurfaceProfile` remains the sole authority for table-surface cloth/motion coefficients.
2. `TableSurfaceController` is the consumer/bridge of that profile.
3. `SnookerPhysicsSetup` owns runtime geometry installation and contact-material installation only where explicitly classified by the contract.
4. The serialized `ballBounciness=0.8` / `ballFriction=0.05` values are not assumed to control the measured table-motion truth.
5. Any change to either member of this physics pair invalidates the calibration chain and requires re-certification plus REAL10 evidence.
6. No second table-surface physics writer may be introduced silently.

**Certification rule:** change either side of the pair → re-certify the pair → repeat REAL10 before production acceptance.

## 2. Explicit 0.8 / 0.05 classification

The `0.8 / 0.05` material path must not be treated as a second source of table-surface truth merely because the values appear in Main Scene / `SnookerPhysicsSetup.MakeMaterial()`.

Before integration, the implementation must have an auditable classification:

- **Allowed:** a distinct ball/rail contact material with a documented responsibility boundary; or
- **Allowed:** reconciled into the single table-surface authority with evidence; or
- **Rejected:** an undocumented duplicate authority for cloth/motion calibration.

No source-code cleanup is authorized merely to make the architecture look cleaner.

## 3. 008 provenance and measurement artifact

The 008 geometry is **not accepted on document assertion alone**.

Required before staging:
- A read-only, reproducible measurement artifact generated from the actual 008 source.
- Artifact must identify the exact source file and provenance.
- TABLE SURFACE dimensions must be measured and recorded.
- Six `ANCHOR_Pocket_*` objects must be measured/mapped one-to-one.
- CUSHIONS, BALL CATCHERS, POCKET PADS and `147VR_SPATIAL_ANCHORS` must have explicit roles.
- `White_CueBall` identity must be preserved semantically; no generic-name inference.
- Any geometry-to-Unity mapping must be evidence-backed; no invented offsets.

Coach cannot open the `.blend` directly. Therefore the measurement artifact is the bridge between source geometry and independent review.

## 4. Main Scene table authority / two-table governance

The Main Scene must never contain two ambiguous gameplay authorities.

**Production candidate:**
`Prefab_WPBSA_12Foot_Snooker`

This is the candidate that 008 is intended to replace **only after all gates pass**.

**V007 visual option / future asset:**
`147VR_Table_WPBSA_Visual_Clean_v007_MARKING`

V007 remains historical/Golden and is not an active second physics/gameplay authority.

Governance rule:
- 008 candidate has one explicit production ownership path.
- V007 remains protected reference/option.
- No duplicate collider, trigger, physics, scoring, or gameplay authority may be introduced by leaving both tables active without explicit role assignment.
- Choosing 008 as production candidate does not authorize Main Scene mutation yet.

## 5. Collider authority

Until evidence-based replacement is approved:
1. Existing `Bed_Collider` remains the current authoritative playfield collider when present and usable.
2. 008 `TABLE SURFACE` is reference/geometry evidence unless explicitly assigned collision authority.
3. Cushion collision geometry must be measured and mapped to 008.
4. Decorative FBX colliders must not become accidental gameplay authority.
5. Pocket catchers/triggers must map to the six 008 pocket anchors while preserving the existing gameplay event contract.
6. Existing Bed_Collider dimensions must be compared against measured 008 geometry before replacement.
7. `SnookerPhysicsSetup` runtime bounds derivation must not be changed during contract review.

## 6. Calibration / REAL10 evidence contract

A production 008 REAL10 result is not certified from an untracked Unity `.log` alone.

The decisive artifact must be tracked and contain at minimum:

`SCENE`  
`TABLE_ASSET_PROVENANCE`  
`TABLE_SURFACE_DIMENSIONS`  
`PHYSICS_PROFILE_OWNER=TableSurfaceProfile`  
`PHYSICS_PAIR_ID`  
`COLLIDER_CONTRACT`  
`SHOT_COUNT=10`  
`FIRED=10`  
`RESOLVED=10`  
`SETTLED=10`  
`ERRORS=0`  
`SEQUENCE_CHECK=PASS`  
`SHOT_IN_PROGRESS_CHECK=PASS`  
`UNITY_VERSION`  
`COMMIT`  
`TIMESTAMP`  
`RUNTIME_TARGET`

Quest 2/3 hardware certification may only be claimed after an actual hardware run.

## 7. Protected systems / forbidden changes

Before Coach accepts the revised contract and the measurement artifact:

- No Main Scene prefab replacement.
- No M5 Rules / Scoring / Turn core changes.
- No V007 / Golden / `147VR-PHY-008.asset` mutation to force a pass.
- No rewrite, deletion, or modification of Coach's independent audit.
- No REAL10 certification.
- No source-code edits whose only purpose is to make the contract appear cleaner.
- No second physics authority may be created.

## 8. Approval ownership

LUNA may prepare artifacts, perform read-only inspection, and stage non-destructive candidates.

Coach provides independent review.

**Human approval remains the production authorization boundary.**

A conditional pass is therefore recorded as a contract/review milestone, not as permission to integrate 008 into Main Scene.

## 9. Next gate

**NEXT GATE = 008 READ-ONLY MEASUREMENT ARTIFACT**

Required output:
1. exact 008 source provenance;
2. measured TABLE SURFACE dimensions;
3. six pocket-anchor measurements/mapping;
4. cushion/catcher/pad role mapping;
5. current Bed_Collider comparison;
6. explicit physics-pair ownership map;
7. artifact committed for Coach review.

Until that artifact is independently reviewed, Main Scene remains LOCKED and REAL10 remains LOCKED.
