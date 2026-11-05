# PoolPhysics — Cue VR–derived billiard core for 147VR (Unity)

A **deterministic, table-agnostic** billiard physics core written as pure C#,
modelled on the physics **reverse-engineered from Cue VR's `libcuevr.so`**
(see `../../cuevr_report_physics.md`, `cuevr_report_geometry.md`,
`cuevr_report_shaders.md`). It is designed to be validated against your existing
**GOLDEN** regression baseline.

## Why not PhysX / not a hybrid
Cue VR is a custom analytic solver, not a black box, and we recovered the *model
+ constants*, not source. Unity PhysX can't reproduce spin/nap/throw or the
determinism. Mixing multiple games' engines leaks energy and breaks determinism.
So: **one coherent custom core**, tuned with Cue VR's constants, gated by GOLDEN.

## Layout
```
Sim/     pure C# (System.Numerics) — NO UnityEngine. Runs headless.
  Ball.cs             ball state (pos, vel, spin, radius, mass)
  PhysicsProfile.cs   recovered constants ([TUNE] marks unknowns)
  TableSpecData.cs    table geometry (rails/pockets/cloth) — per-table
  PoolSimulator.cs    the engine: fixed substep, slide→roll, collisions
  CueStrike.cs        shot intent → cue-ball initial velocity + spin
Unity/   thin bridge (UnityEngine)
  TableSpec.cs            ScriptableObject → TableSpecData (one per bay)
  CueVRPhysicsProfile.cs  ScriptableObject prefilled w/ recovered constants
  PoolPhysicsRunner.cs    MonoBehaviour: drives sim, syncs Transforms, Shoot()
```

## The recovered model (what's implemented)
- **Fixed 0.5 ms substep** accumulator, decoupled from framerate (deterministic).
- **Analytic spheres**; slide→roll via cloth friction; solid-sphere inertia 2/5;
  the 5/7 shared-velocity factor **emerges** from the friction-torque integration.
- **gravity 9.806**, pool R=0.028575 / snooker R=0.02625, mass ~0.155.
- **spin decay 1.4726**, english (vertical spin) decays independently.
- **ball-ball** equal-mass impulse w/ restitution + tangential throw hook.
- **cushion** reflect w/ restitution + spin loss (throw hook).
- **pocket** capture by centre distance.

## Table-agnostic = "ใช้ได้กับทุกโต๊ะ"
The solver reads a `TableSpecData`. Author a `TableSpec` ScriptableObject per
table (12-ft snooker / 9-ball / 8-ball) — same core, different geometry + cloth.
The 4 bays in 147VR share one `PoolSimulator` implementation.

## GOLDEN integration (acceptance harness)
`Sim/` has zero Unity dependency, so GOLDEN can run it **headless**:
1. Feed GOLDEN's reference shot set (initial ball layout + cue shot) into
   `PoolSimulator` with a `PhysicsProfile`.
2. Step to rest; diff final positions / pocket order / resting layout.
3. Fit the `[TUNE]` constants (cushion e/μ, ball-ball pair, throw) until GOLDEN
   passes within tolerance. Keep GOLDEN's outcomes as ground truth.
4. From then on GOLDEN is the regression gate for any physics change.

## Known gaps (honest)
- Cushion struct params (`mu_cush/e_cush_min/cush_spin/cush_efall`) and the exact
  ball-ball restitution/friction pair were **not pinned** by RE (no full
  disassembler). They're `[TUNE]` fields — fit them to GOLDEN.
- No CCD/TOI; the 0.5 ms substep is the anti-tunneling mechanism (as in Cue VR).
- `CushionThrowCoeff` and `BallBallFriction` throw are stubs (0/0.1) — model the
  `x/√y` throw once validated.
- Nap is a per-table heading field; directional-cloth force not yet applied.

## Next steps (roadmap order)
1. ✅ Skeleton (this).
2. ✅ Compile-check `Sim/` headless + smoke test (deterministic) + GOLDEN runner
   (`Tests/`): `gen` / `check` / `fit`.
3. ✅ Optimizer upgraded to **CMA-ES** (`Tests/CmaEs.cs`, bounded, deterministic).
4. Wire 147VR cue input → `Runner.Shoot()`.
5. Fit `[TUNE]` params against the **real** GOLDEN shot set (more shots → better
   identifiability; the 4 synthetic shots leave a rugged local basin ~0.27).
6. Enable per-bay `TableSpec`; add nap + cushion throw.
