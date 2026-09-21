# CueWarp → 147 VR Controller Capability Gap Matrix

Audit date: 2026-08-31
Reference: CueWarpVrRebornEdition
Policy: read-only reference; no direct asset/code migration.

## Classification
KEEP = 147 VR already owns a suitable implementation.
ADAPT = borrow behavior/pattern and reimplement in 147 VR.
BUILD = capability is useful but absent or incomplete.
REJECT = reference behavior conflicts with 147 VR authority/design.

| Capability | CueWarp evidence | 147 VR state | Decision |
|---|---|---|---|
| Left stick movement | XR player controller | Mapping exists | ADAPT semantic action |
| Right stick aim | Cue alignment + player controller | Mapping exists | ADAPT |
| Dominant-hand awareness | CueWarpXRPlayerController | Not fully represented | BUILD |
| Cue grab | Right Grip pattern | Current cue controller is virtual/trigger-driven | BUILD interaction layer |
| Physical stroke | CueStickController | M5 CuePhysicsAdapter authority exists | ADAPT input only; KEEP physics |
| Cue alignment | CueAlignmentManager | CueAimProfile/CueStrokeModel exist | ADAPT |
| Auto-chin alignment | CueAlignmentManager | No equivalent confirmed | BUILD after interaction baseline |
| Free-hand mode | CueAlignmentManager | No equivalent confirmed | BUILD/optional |
| Side-offset aim | CueAlignmentManager | No semantic action confirmed | ADAPT |
| Eye dominance | EyeDominanceDetector | No confirmed runtime equivalent | BUILD |
| Height calibration | PlayerHeightCalibrator | Tablet has height preference; runtime calibration differs | ADAPT, unify authority |
| Comfort locomotion | ComfortLocomotionController | No confirmed equivalent | BUILD |
| Smooth acceleration | ComfortLocomotionController | Movement mapping exists | ADAPT |
| Comfort vignette | ComfortLocomotionController | No confirmed equivalent | BUILD for XR comfort |
| Mechanical REST | MechanicalRestSystem | Mapping has reserved left trigger | BUILD |
| REST cycling | 4-mode cycle | Not implemented | BUILD |
| Bridge height | X-button cycle | X is Reset Frame | DESIGN CONFLICT — do not copy mapping |
| Haptics | CueStickController dynamic haptics | No confirmed unified semantic layer | ADAPT |
| Friction haptics | CueStickController | Not present | OPTIONAL ADAPT |
| Aim ghost line | CueAlignmentSystem | No confirmed runtime equivalent | BUILD/ADAPT |
| Contact spot | CueAlignmentSystem | No confirmed runtime equivalent | BUILD |
| Overhead camera | CueWarpXRPlayerController | Not in current mapping | OPTIONAL BUILD |
| Undo shot | CueWarpXRPlayerController placeholder | No confirmed undo authority | REJECT until snapshot architecture exists |
| Laser aim | CueWarpXRPlayerController | No confirmed semantic action | OPTIONAL BUILD |
| Tablet menu | CueWarp tablet pattern | TabletOptionsMenu already exists | KEEP + refactor input boundary |
| Menu persistence | CueWarp tablet | 147 VR already uses PlayerPrefs | KEEP |
| Player reposition by turn | PlayerViewManager | 147 VR M6 turn authority exists | ADAPT only after VR loop |
| Desktop fallback | Virtual cue controller | 147 VR has desktop cue path | KEEP |
| Scene cycling | CueWarp A/B environment cycling | 147 VR tablet scene selection exists | REJECT duplicate input behavior |
| Special abilities | CueWarp | Not part of 147 VR product core | REJECT |
| RCA physics | CueWarp | M5 physics authority | REJECT |
| Cue inventory | CueWarp | Not required for core loop | DEFER |
| Multiplayer input | CueWarp | Not current critical path | DEFER |

## Highest-value adaptations
1. Dominant-hand abstraction.
2. Semantic XR input layer.
3. Cue alignment modes.
4. Mechanical REST interaction.
5. Comfort locomotion.
6. Haptic feedback.
7. Aim/contact visualization.
8. Height/eye calibration unified with existing 147 VR calibration.

## Explicitly protected from CueWarp
- Physics impulse logic.
- RCA manager.
- Special ability modifiers.
- Cue inventory dependencies.
- CueWarp player manager.
- CueWarp scene systems.
- CueWarp prefabs and assets.

## Key conflict discovered
CueWarp uses X for bridge-height cycling while 147 VR currently defines X as Reset Frame. Therefore the CueWarp mapping cannot be copied wholesale. The correct solution is a 147 VR semantic action map with product-specific bindings.