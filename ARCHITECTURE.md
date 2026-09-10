# 147 VR — Architecture Contract

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

- `EditorBuildSettings` currently contains only `Assets/Scenes/SampleScene.unity`; the two pool-table scenes exist but are not yet build-listed.
- The project contains legacy/tutorial/editor scripts mixed with runtime scripts; do not delete them during Gate work without usage verification.
- Several runtime UI scripts use legacy `UnityEngine.UI.Text`; migration is optional and should be performance/UX driven, not mixed into foundation work.
- Existing scripts were created incrementally; future changes should prefer explicit references/events over scene-name discovery.
