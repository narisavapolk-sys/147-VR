# Unity integration plan — event-based physics into 147VR (NON-DESTRUCTIVE)

The event-based core (`Sim/`) is validated headless. This is the staged, reversible
path to run it inside 147VR. **Nothing here breaks the game until you flip the toggle.**

## Files (drop into `Assets/Scripts/PoolPhysics/`)
```
Sim/    pure C# (no UnityEngine) — EventMotion, EventSimulator, Ball, TableSpecData,
        PhysicsProfile, CueStrike   (headless-testable; keep as-is)
Unity/  PoolPhysicsEventRunner.cs   <- the toggle adapter (default OFF)
        TableSpec.cs                <- ScriptableObject per table
        CueVRPhysicsProfile.cs      <- ScriptableObject with real-physics coefficients
```

## Staged rollout (each step reversible)
1. **Import scripts only.** Unity recompiles. Game unchanged (adapter not attached).
2. **Create assets:** `Create > 147VR/Pool/Cue VR Physics Profile` (real defaults) +
   one `Table Spec` per bay (snooker 2.54×1.27, 9-ball, 8-ball — fill playfield + pockets).
3. **Attach `PoolPhysicsEventRunner`** to a director GameObject; assign ball Transforms
   (index 0 = cue) + the two SOs. Leave `enabled_override = false`.
4. **Wire the cue strike** at the seam `CuePhysicsAdapter.Apply(CueShotData, english)`:
   when the toggle is on, call `runner.Shoot(dir, speed, tip)` INSTEAD of setting
   Rigidbody velocity. Keep the old path behind `if (!runner.Enabled)`.
5. **A/B test in-editor:** toggle ON -> the runner drives transforms from EventSimulator;
   OFF -> PhysX as before. Compare feel.
6. **Run the Golden harness** (`PhysicsGoldenRegressionRunner`) against the new
   `golden_real_physics.json` reference (re-baselined). Iterate `[TUNE]` params.
7. **VR playtest** every mode (snooker / 8-ball / 9-ball).

## Retiring the old physics (ONLY after steps 1–7 pass)
- **Do NOT hard-delete** `BallMotionController`/`CuePhysicsAdapter`/PhysicMaterials —
  scene/prefab components reference them; deleting = missing-script + compile break.
- Safe path: back up the project (git commit / copy), move legacy scripts to
  `Assets/_LegacyPhysics/` (Unity keeps GUIDs; references survive), disable their
  components, and remove PhysX ball Rigidbodies once the runner is proven.
- Keep a one-line kill-switch so you can revert to PhysX instantly.

## What's validated already (headless)
- Motion: analytic slide→roll→rest matches closed-form to 0.000%; no substep energy bug.
- Collisions: head-on momentum/energy, oblique 90° separation, cushion |v| preserved,
  4-ball break stable (no NaN, in-bounds).
- Real-physics reference golden generated (`Tests/golden_real_physics.json`).

## Still needs the editor (cannot be done headless)
- In-editor Golden harness pass on the new reference.
- VR feel playtest across all tables/modes.
- Per-bay `TableSpec` dimensions read from the actual prefabs (`SnookerPhysicsSetup`,
  `147VR_Table_WPBSA_*`, `BallRack`) — fill these into the TableSpec SOs.
