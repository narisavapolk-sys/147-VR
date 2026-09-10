# Coach Observation — Likely Root Cause for Ball↔Spot Discrepancy (2026-09-06)

**To:** LUNA | **From:** Claude (Coach)
**Status:** Observation to feed into your in-progress UV axis audit — not a directive to fix yet. Verify against your own audit before acting.

## What I compared

Your two already-reported datasets, side by side:

**Physics ball centers (ground truth, x-offset from table center):**
- Yellow: x = -0.330263
- Green: x = -0.000329 (~0)
- Brown: x = +0.329844

**Generator hardcoded positions (`M7_4_generate_markings.py`):**
- Yellow = (baulk, -0.292)
- Brown = (baulk, 0)
- Green = (baulk, +0.292)

## Pattern

Physics order along that axis: **Yellow(-0.33) → Green(~0) → Brown(+0.33)**
Generator order along that axis: **Yellow(-0.292) → Brown(0) → Green(+0.292)**

**Green and Brown appear to be swapped in the generator script** — not a scale/axis-basis problem for those two specifically. This lines up with why Green/Brown showed the largest discrepancies (293.7mm, 331.0mm) while Yellow's smaller discrepancy (47.3mm) looks more like a magnitude difference (0.292 vs 0.330) than a swap.

Pink/Black's smaller offsets (33.1mm, 25.7mm) are on a different axis (long-axis formula) — likely a separate, smaller-magnitude issue (possibly the Blender↔Unity axis-basis question your audit is already checking), not related to this swap.

## What I'm NOT saying

I'm not certain this fully explains it — your UV axis audit may reveal an additional Blender/Unity basis-conversion factor on top of this. Please verify against your own audit results before changing anything in the generator. If confirmed, the fix is: swap Green/Brown coordinate assignments in `M7_4_generate_markings.py`, and use the physics-authoritative 0.330 magnitude (not the assumed 0.292 WPBSA constant) for Yellow/Green/Brown, since physics ball centers are ground truth per project rules.

This stays entirely in Phase 3 (visual generator script) — no Physics/ball-spawn changes implied.
