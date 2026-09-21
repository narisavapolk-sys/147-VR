# 147VR AAA Physics Integration Checklist

## Verified scene anchors
- PoolTable_8Ball.unity contains a Table object.
- The scene already references a table asset and BallRack.
- Do not replace the existing table hierarchy during AAA integration.

## Integration order
1. Bind TableSurfaceController to the existing playing-surface collider.
2. Bind PocketResponder only to dedicated pocket trigger colliders.
3. Keep gameplay scoring/rules outside physics responders.
4. Run calibration with one controlled ball before multi-ball tests.
5. Validate fixed timestep and Rigidbody settings before tuning friction.
6. Compare measured travel against ShotCalibrationCase targets.

## Guardrails
- AAA components remain isolated under the AAA namespace.
- Existing pool/snooker gameplay remains the authority until calibration passes.
- No scene-wide replacement of colliders or rigidbodies.
- Never tune by visual guess alone.

## Current milestone
Physics foundation + calibration harness: READY FOR SCENE BINDING.
Next milestone: controlled real-table calibration pass.
