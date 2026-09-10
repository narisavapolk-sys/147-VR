# 147 VR Input Implementation Notes

## Boundary
The semantic input layer is an adapter boundary, not a new gameplay system.

## Current API
- `VR147Action` defines product-level actions.
- `VR147InputState` carries continuous axes and button state.
- `IVR147InputSource` isolates the physical Input System implementation.
- `VR147InputRouter` publishes button-edge actions and exposes continuous state.

## Safety
- No Rigidbody access.
- No CuePhysicsAdapter access.
- No CueWarp dependency.
- No modification of M5/M6 authority.
- Existing controller bindings remain untouched until an adapter is proven.

## Next implementation
Build one concrete Input System source that maps the existing 147 VR action asset into `IVR147InputSource`.
Then create focused tests for action edges and continuous axes before wiring gameplay.

## Why this shape
The router lets Quest/OpenXR, desktop fallback, or future controllers provide the same semantic contract. The rest of 147 VR can remain unaware of hardware-specific bindings.
