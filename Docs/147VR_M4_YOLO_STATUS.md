# 147VR M4 YOLO Status

## M4.1 Ball-to-Ball Collision Authority — CERTIFIED

Date: 2026-08-30
Unity: 6000.4.4f1
Method: REAL runtime PhysX simulation + response-sign observer

## REAL Dataset
- Full Ball: 5/5
- 3/4 Ball: 5/5
- 1/2 Ball: 5/5
- 1/4 Ball: 5/5
- 1/8 Ball: 5/5
- Thin / Edge: 5/5
- Total: 30/30 REAL samples

## Detector Contract
1. Contact distance observed at the expected ball radius boundary.
2. Pre-collision relative normal velocity must be positive.
3. Post-step velocity response must be non-zero.
4. Sample stores collision normal, relative normal velocity, post velocities, energy ratio and delta-velocity response.

## Thin / Edge
The Thin / Edge diagnostic representative uses 63 degrees. A 65-75 degree grazing setup was rejected because it could become numerically marginal at the exact tangent boundary. This is a test-geometry stability choice, not a physics lookup rule.

## Golden
- PHY-014 Full Ball
- PHY-015 3/4 Ball
- PHY-016 1/2 Ball
- PHY-017 1/4 Ball
- PHY-018 1/8 Ball
- PHY-019 Thin / Edge
- M4 Golden Catalog: 6 cases

## Regression
- PHY-014 through PHY-019: 5/5 PASS each
- 30/30 REAL samples accepted
- M4.1 status: CERTIFIED

## Baseline Protection
PHY-001 through PHY-013 remain untouched by this M4.1 certification cycle.
