# M5.3 / Table 008 Integration Handoff — 2026-09-22

## Verified
- Production source: `SNOOKER   VR pool table/Blender/147VR_Table_WPBSA_Visual_Clean_v008_MARKING_CLEAN.blend`
- Blender: 5.2.1 LTS
- Objects: 168
- Collections include `147VR_POCKET_ART`, `147VR_SPATIAL_ANCHORS`, `BALL CATCHERS`, `CUSHIONS`, `MARKINGS`, `POCKET PADS`
- `TABLE SURFACE` dimensions: 3.569 x 1.778 m
- Pocket anchors: E, W, NE, NW, SE, SW present
- Cue ball and full snooker colour set present
- Existing Main Scene table reference is `Assets/AAA/ImportedSnooker/Prefab_WPBSA_12Foot_Snooker.prefab`
- Main Scene currently references that prefab; its source contains legacy V006-named visual parts.
- `Assets/AAA/PhysicsCalibration/Golden/147VR-PHY-008.asset` exists and records REAL M3 Angle30 measurements from Unity 6000.4.4f1.

## Decision / Safety
- Do NOT replace the Main Scene table prefab with the raw Blender 008 source yet.
- M5 gameplay core remains locked.
- 008 is the intended production integration target.
- Next safe path: create/verify a Unity-side 008 visual asset or conversion, map it to the existing physics/collider contract, then run integration validation and REAL10.
- Preserve the historical V007 Golden certification baseline; 008 becomes the production integration target, not a rewrite of historical evidence.

## Pending
- Unity-side 008 conversion/provenance verification.
- Geometry/collider alignment check against the existing M5 contract.
- REAL10 rerun on the verified 008 production target.
- Then return to remaining M5.3/REAL10 certification work.
