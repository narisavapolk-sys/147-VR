# 147 VR Physics Certification Runbook

## Purpose
Controlled runtime measurement is the only source of Golden Truth.
Physics Core tuning is locked during certification.

## Pipeline
1. Open `Assets/AAA/PhysicsCalibration/147VR_PhysicsCalibration.unity`.
2. Confirm cue and cue-ball are discoverable at runtime.
3. Reset the shot to a deterministic starting state.
4. Execute a controlled shot.
5. Wait for velocity to remain below settle threshold.
6. Capture distance and peak speed.
7. Reject non-finite measurements.
8. Repeat the same case enough times to establish variance.
9. Promote measured values into Golden Case assets.
10. Run regression against the committed catalog.

## Case Order
Straight -> Stun -> Follow -> Draw -> Left English -> Right English -> Cushion -> Pocket.

## Certification Rules
- Never manufacture expected values to make a test pass.
- Never tune Physics Core during a certification run.
- Invalid or incomplete samples cannot certify a case.
- A matching count with invalid results is not full coverage.
- Golden assets are created only after measured Truth exists.
