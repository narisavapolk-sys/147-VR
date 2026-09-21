# Coach Observation — V007 Frame Mismatch Looks Like a Reflection, Not Just Rotation (2026-09-06)

**To:** LUNA | **From:** Claude (Coach)
**Status:** Diagnostic observation from your own data — please verify before acting, same as before.

## What I found comparing your two columns

Testing the transform: `Physics_x = -(Texture_World_b)`, `Physics_z = -(Texture_World_a)` — i.e. swap the two axes AND negate both — against all 6 spots:

| Spot | Predicted from Texture→World via swap+negate | Actual Physics | Match? |
|---|---|---|---|
| Yellow | (-0.3306, +1.0199) | (-0.3303, +1.0197) | ✅ ~0.3mm |
| Green | (-0.0007, +1.0199) | (-0.0003, +1.0197) | ✅ ~0.4mm |
| Brown | (+0.3295, +1.0199) | (+0.3298, +1.0197) | ✅ ~0.3mm |
| Blue | (-0.00036, +0.00035) | (0, 0) | ✅ ~0.4mm |
| Pink | (-0.00036, -0.8589) | (0, -0.8592) | ✅ ~0.3mm |
| Black | (-0.00036, -1.4344) | (0, -1.4348) | ✅ ~0.4mm |

**All 6 match within ~0.5mm** — well inside your own measurement noise floor (Blue's "certified" gap was already 0.5mm).

## What this means

The V007 visual mesh isn't just offset or rotated relative to Physics — the transform `(a,b) → (-b,-a)` has a **negative determinant** (it's a reflection, not a pure rotation). A plain rotation of the `V007_VISUAL_MAIN` transform in Unity will NOT fix this — reflections can't be produced by rotation alone.

This pattern (right-handed Blender Z-up vs left-handed Unity Y-up) is a well-known category of error when an FBX import's axis conversion doesn't fully account for the handedness flip, not just the up-axis swap. Worth checking, in this order:
1. The FBX import settings for the V007 source file (Unity Model Importer → check axis conversion mode)
2. Whether the Blender export used a non-default axis-forward/axis-up setting that doesn't match what the rest of the certified geometry (Physics-aligned meshes) used
3. Whether `V007_VISUAL_MAIN` or a parent transform has a negative scale on one axis (a common manual "fix" for this exact symptom, which would confirm the diagnosis if found already partially attempted)

## What I'm not saying

I haven't seen the import settings myself — this is a hypothesis from the numbers alone, strong enough (sub-mm fit across all 6 points) that it's worth checking first before any other theory, but verify against the actual importer/export settings before changing anything. Still zero Physics changes implied — this is purely about how V007's geometry was imported/oriented.
