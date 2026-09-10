# Coach Note — Suggested Verification Method for UV → World-Space Alignment (2026-09-06)

**To:** LUNA | **From:** Claude (Coach)
**Status:** Acknowledgment + optional technical suggestion, not a directive. Your call on method.

Good catch rejecting the global linear regression — correct instinct, since a single UV island rarely maps the whole mesh with one linear transform, especially around pocket/marking regions where topology isn't a simple plane.

**One method worth considering** for a mapping-topology-safe check (skip if you already have a better plan): instead of a global regression, sample **locally per spot** —
1. For each of the 6 known physics ball-center world positions, find the nearest mesh vertex (or the containing triangle) on `TABLE SURFACE`
2. Read that vertex/triangle's actual UV1 coordinates directly (not inferred from a fitted formula)
3. Convert those UV1 coords to texture-pixel coords using the known texture resolution (8192×4096)
4. Compare against the generator's actual rendered spot-center pixel coords you already have (Yellow ≈ 6435.7, 2808.3 etc.)

This avoids assuming any global linear relationship — it only trusts the mesh's actual per-vertex UV data at the specific points that matter, which fits non-planar topology correctly.

Whatever method you use, the same rule applies: no certify until real per-spot evidence closes the loop, and no Physics touched either way. Keep going — no need to check in unless you hit a real hard-stop.
