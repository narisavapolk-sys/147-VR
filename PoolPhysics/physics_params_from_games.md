# Physics params from the ripped games (cross-game priors)

Extracted with **UnityPy** from the PhysX `PhysicMaterial` + `PhysicsManager`
assets inside the pulled APKs. These seed the `[TUNE]` fields of
`Sim/PhysicsProfile.cs`. **Caveat:** PhysX bounciness/friction are a different
model/units than the analytic solver's e/μ — use as *bounds & ordering*, then fit
to GOLDEN.

## Games scanned
| Game | Package | Engine |
|---|---|---|
| UnlimitedSnooker | com.SPSoftwareProductions.UnlimitedSnooker | Unity |
| Saitara (cueist2) | com.Saitarasoft.new2021poolgame | Unity |
| Backspin | com.portuslabs.backspinvr | Unity |

## Ball ↔ ball / ball ↔ surface (bounciness = restitution proxy)
| Game | Material | Bounciness | Dyn friction | Static friction |
|---|---|---|---|---|
| UnlimitedSnooker | Ball | **0.925** | 0.01 | 0.01 |
| Saitara | ballphysmat1–8 | **0.90** | 0.02–0.45 | same |
| Backspin | Ball | **0.90** | 0.0 | 0.0 |
→ **BallBallRestitution ≈ 0.90–0.93** (set 0.93). Consistent across all three.

## Cushion / rail
| Game | Material | Bounciness | Friction |
|---|---|---|---|
| UnlimitedSnooker | TableCushion | **0.78** | 0.35 |
| UnlimitedSnooker | NearJawCushion | 0.60 | 0.125 |
| UnlimitedSnooker | CushionBorder | 0.35 | 0.01 |
| Saitara | cushphysmat | 1.0 (combine→min) | 0.0003 |
| Backspin | Cushion / Jaw | 0.0 / 0.45 (combine→max w/ ball) | 0.005 |
→ **CushionRestitution ≈ 0.78**, **CushionFriction ≈ 0.2–0.35**. (Backspin/Saitara
rely on PhysX combine modes; the explicit snooker "TableCushion" 0.78 is the
cleanest prior.)

## Cloth (rolling surface)
| Game | Material | Bounciness | Dyn friction | Static friction |
|---|---|---|---|---|
| Backspin | Cloth Pool | 0.0 | **0.20** | 0.20 |
| Backspin | Cloth Snooker | 0.0 | **0.50** | 0.08 |
| Saitara | slatephysmat1–8 | 0.009 | 0.09–0.5 | 0.18–0.75 |
| UnlimitedSnooker | TableSurface | 0.35 | 25 (avg-combine) | 25 |
→ **Ordering: snooker cloth slower than pool cloth.** SlidingFriction pool ~0.2,
snooker ~0.5. Real rolling resistance is small (RollingFriction ~0.01 kept).

## PhysicsManager (solver settings — informational)
- UnlimitedSnooker: SolverIterations 255, FrictionType 2 (one-contact), BounceThreshold 0.
- Saitara: SolverIterations 8, FrictionType 0, ImprovedPatchFriction true, SleepThreshold 2e-4.
- Backspin: SolverIterations 24, FastMotionThreshold 0.5, SleepThreshold 8e-3.
→ These are PhysX knobs (irrelevant to our analytic core), but **SleepThreshold
~2e-4–8e-3** matches our `RestSpeedEpsilon` order of magnitude.

## What the games did NOT give us
- **Ball mass** — not in PhysicMaterial (PhysX uses Rigidbody mass, in prefabs;
  not scanned here). Kept WPA 0.16 kg.
- **Spin/nap model, throw, cushion `x/√y`** — none of these games expose it via
  materials (they lean on PhysX). Only Cue VR's analytic model has it → still
  `[TUNE]` / to be fitted to GOLDEN.

## Verdict
Cross-game data **corroborates** the Cue VR seeds and tightens two:
`BallBallRestitution 0.95→0.93`, `CushionRestitution 0.55→0.78`,
`CushionFriction 0.2→0.3`. It cannot replace GOLDEN fitting for the spin/throw/nap
terms, which no PhysX-based game exposes.
