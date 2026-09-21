# 147VR M4.2 YOLO Status

## Pocket Authority — CERTIFIED
Date: 2026-08-30
Unity: 6000.4.4f1
Method: REAL runtime PhysX simulation with capture-trigger and wall-response observation.

## Contract
- Capture: actual ball/capture trigger overlap on isolated runtime rig.
- Reject: ball passes pocket approach without capture and without jaw contact.
- Jaw: actual wall response observed with pre/post velocity change.
- Rattle: repeated wall responses inside the pocket mouth before terminal state.
- Symmetry: Left/Right Jaw mirror invariants validated from independent REAL runs.

## REAL Dataset
- Clean Capture: 5/5
- Low Speed Capture: 5/5
- Clean Reject: 5/5
- Left Jaw: 5/5
- Right Jaw: 5/5
- Rattle: 5/5
- Total: 30/30 REAL samples

## Validation
- Capture samples: captured=true, rejected=false, jaw=false.
- Reject samples: rejected=true, captured=false, jaw=false.
- Jaw samples: rejected=true, captured=false, jaw=true, wallContacts>=1.
- Rattle samples: rattle=true, jaw=true, wallContacts>=2.
- No synthetic JSON used.
## Golden
- PHY-020 Clean Capture
- PHY-021 Low Speed Capture
- PHY-022 Clean Reject
- PHY-023 Left Jaw
- PHY-024 Right Jaw
- PHY-025 Rattle
- M4.2 Golden Catalog: 6 cases.

## Regression
- PHY-020 through PHY-025: 5/5 PASS each.
- 30/30 REAL samples accepted.
- Left/Right Jaw symmetry: PASS.
- M4.2 status: CERTIFIED.

## Baseline Protection
PHY-001 through PHY-019 remain untouched by this M4.2 certification cycle.
M4.1 Ball-to-Ball authority remains separately certified.

## Environment Notes
UPM connected successfully in the certification run.
Known Unity Account API timeout warnings did not block physics execution.
Batchmode certification exited with code 0 after the evidence run.
