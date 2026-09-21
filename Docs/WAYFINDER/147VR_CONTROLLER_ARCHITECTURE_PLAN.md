# 147 VR Controller Architecture Plan

## Goal
Build a controller layer that preserves the best CueWarp interaction patterns without importing CueWarp architecture.

## Proposed Layers

XR Device / Input System
        ↓
147VR Input Reader
        ↓
Semantic Actions
        ↓
Interaction Controllers
        ↓
Gameplay Authority / UI / Player Systems

## Semantic Actions

Move(Vector2)
Aim(Vector2)
GrabCue(bool)
CuePose(position, rotation)
StrikeIntent
CancelShot
OpenTablet
ResetFrame
Recenter
CalibrateHeight
CycleRest
CycleEnvironment
Sprint

## Cue Interaction Boundary

Cue interaction may calculate pose, grip state, alignment and stroke intent.
It must NOT directly apply Rigidbody impulses.
The existing CuePhysicsAdapter remains the only shot physics authority.

## Handedness

Introduce a single dominant-hand policy so gameplay bindings do not duplicate left/right logic. Support hand owns locomotion/rest where appropriate; dominant hand owns cue interaction. Product-specific conflicts are resolved here, not inside gameplay managers.

## CueWarp Patterns Worth Adapting

- dominant-hand-aware input routing
- cue alignment modes (AutoChin / Offset / FreeHand concept)
- eye-dominance calibration
- player-height calibration
- mechanical REST cycle and dynamic shaft positioning
- dynamic haptic feedback
- comfort acceleration and vignette
- world-space tablet interaction
- player view repositioning around the table

## Patterns Not To Adapt Directly

- CueWarp RCA physics
- CueWarp SpecialAbilitySystem
- CueWarp inventory coupling
- direct FindFirstObjectByType-heavy global orchestration
- project-specific scene cycling
- Undo Shot placeholder without a real state snapshot system
- arbitrary button mappings that conflict with 147 VR

## Binding Decision

Current 147 VR mapping has X = Reset Frame and Y/Menu = Tablet. CueWarp uses X for Bridge Height and Y for Laser Aim in its richer controller. Therefore we preserve the 147 VR product mapping and add capabilities through semantic actions rather than replacing bindings wholesale.

## Implementation Order

1. Environment/CI cleanup remains separate and protected.
2. Create semantic input abstraction.
3. Add handedness routing.
4. Connect cue pose to SnookerCueController without touching physics authority.
5. Add calibration services.
6. Add REST interaction.
7. Add haptics.
8. Add comfort locomotion.
9. Add aim visualization.
10. Integrate Tablet and player view through semantic events.
11. Run M5 + M6 regression after every integration boundary.

## Acceptance Criteria

- CueWarp project has zero modified files from this workstream.
- 147 VR compiles cleanly.
- Existing M5/M6 certification remains green.
- Controller input cannot bypass CuePhysicsAdapter.
- Reset Frame cannot be accidentally rebound by REST controls.
- Left/right handedness changes do not alter gameplay authority.
- Desktop fallback remains usable.

## SESSION UPDATE — 2026-08-31

This plan is the active implementation frontier. Do not restart previously certified gameplay/physics milestones.

### Working rule
After each meaningful implementation step, update the Wayfinder checkpoint before proceeding.

### Current implementation state
- Semantic vocabulary: implemented.
- Input router boundary: implemented.
- Input source/context layer: implemented.
- CueWarp: reference-only, untouched.
- Physics authority: unchanged.

### Next
Validate compilation and runtime binding, then implement dominant-hand routing and cue interaction adapter.
