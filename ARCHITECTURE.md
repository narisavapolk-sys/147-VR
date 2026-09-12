# 147 VR — Architecture Contract

## Script tree ownership — `AAA/` vs `Quest/`

`Assets/Scripts/` contains four trees. They are not interchangeable and one does
not supersede the other.

| Tree | Role | Ships in the build? |
| --- | --- | --- |
| `Assets/Scripts/Quest/` | **Shipping game.** Player-facing snooker gameplay on Meta Quest 2/3: cue control, ball tracking, shot resolution, scoring, turns, UI, XR adapters. | Yes |
| `Assets/Scripts/AAA/` | **Physics authority + measurement lab.** Single source of truth for how a cue strike becomes ball motion, plus the calibration/batch harnesses that prove it. | Partly — the runtime authority ships; the batch runners are measurement tools |
| `Assets/Scripts/PoolTable/` | Separate 8-ball / 9-ball pool modes with their own tables and skin system. | Yes |
| `Assets/Scripts/147/` | Scene dressing and experimental content (dancers, concert room). | Scene-dependent |

### Why the split exists

`AAA/` was created so that physics correctness could be measured and certified
independently of gameplay. Gameplay code asks `AAA/` for the result of a strike;
it never re-implements the maths.

The rule is stated in `CuePhysicsAdapter.cs` (`VR147.AAA.Cue`):

> Single runtime physics authority for cue strikes. This is the only method used
> by gameplay/calibration paths.

### Authority rules

1. **`AAA/` owns physics. `Quest/` owns the game.** A gameplay script must never
   compute strike impulse, spin or cushion response itself.
2. **One direction only.** `Quest/` may depend on `AAA/`. `AAA/` must never
   depend on `Quest/`, on UI, or on a scene.
3. **`AAA/Physics/Golden/` is reference data.** It records measured, certified
   behaviour. Changing it invalidates every existing certification, so treat it
   as evidence rather than as configuration.
4. **Batch runners are instruments, not gameplay.** `M21StunBatchRunner`,
   `M22FollowBatchRunner`, `M23DrawBatchRunner`, `M24EnglishBatchRunner`,
   `M3CushionRuntimeRunner`, `M5RealStraightBatchRunner` and the
   `ShotCalibration*` set exist to produce measurements. They must not be driven
   from gameplay code.
5. **`AAA/M5/` is the event contract boundary.** `M5ShotEventContract` is how the
   physics side announces `ShotStarted` and `PhysicsSettled`. `Quest/` consumes
   those events; it does not poll physics internals.
6. **Colliders are the physics authority, not the art.** `TBL_COLLIDER_*` and the
   table bed collider define play. Visual meshes, including any new table skin,
   must not move them.
7. **Art is never authority.** Blender output under `Tools/Blender/` and any FBX
   it produces are visual candidates only. Importing one never changes physics,
   ball spots or markings.

### Certified table

The certified in-game snooker table is
`Assets/AAA/ImportedSnooker/147VR_Table_WPBSA_Visual_Clean_v007.fbx`. M5 physics
is bound to it. A replacement table requires a full re-certification pass, so the
preferred route for new table art is an additional skin through
`TableSkinManager`, which changes no physics.

## GATE 2 — Core Systems

### Runtime flow

`Cue Controller -> Physics -> Ball Tracker -> Shot Tracker -> Score Manager -> Turn Manager -> UI/View`

The existing Quest scripts form the gameplay spine. Keep this direction one-way: lower-level simulation must not depend on presentation.

### Responsibilities

- `SnookerCueController`: input/aim/shot execution.
- `SnookerPhysicsSetup`: deterministic physics configuration for table/balls.
- `SnookerBallTracker`: ball identity, home state and pocket events.
- `SnookerShotTracker`: shot lifecycle and first-contact/result detection.
- `SnookerScoreManager`: rules, scoring and score events.
- `SnookerTurnManager`: player-turn state and turn transitions.
- `SnookerScoreUI`: presentation only; subscribes to score/turn state.
- `PlayerViewManager`: player-facing camera/table positioning.
- `QuestSpawnSetup`: player spawn configuration.
- `QuestPassthroughBridge`: XR passthrough boundary.
- `TabletOptionsMenu`: user-facing settings/persistence.

### Dependency rules

1. Rules do not directly manipulate UI.
2. UI consumes events/state; it does not own gameplay state.
3. Physics reports facts; scoring decides rules.
4. Turn management consumes shot outcomes instead of inspecting physics internals.
5. XR/presentation adapters remain replaceable.
6. Game-specific scripts stay under `Assets/Scripts`; editor automation stays under `Assets/Editor`.
7. Avoid global static mutable state except explicit persisted settings keys.

## Reusable boundary

The reusable core is the snooker gameplay spine and event contracts. Scene-specific dressing, dancers, concert effects, tablet visuals and XR adapters should remain outside the rules core.

## Current risk register

- The project contains legacy/tutorial/editor scripts mixed with runtime scripts; do not delete them during Gate work without usage verification.
- Several runtime UI scripts use legacy `UnityEngine.UI.Text`; migration is optional and should be performance/UX driven, not mixed into foundation work.
- Existing scripts were created incrementally; future changes should prefer explicit references/events over scene-name discovery.
- Two orphan `.meta` files have no owning asset: `Assets/Scripts/AAA/Gameplay.meta` and `Assets/147 main/ConcertRoom/Backups.meta`. Remove them in a dedicated cleanup change, not inside a code fix.

### Corrected since the last revision

- The build list is no longer `SampleScene` only. `EditorBuildSettings` now contains four enabled scenes: `147VR_MainScene.unity`, `SampleScene.unity`, `PoolTable_8Ball.unity` and `PoolTable_9Ball.unity`. Whether the two pool scenes ship is a product decision that is still open.
