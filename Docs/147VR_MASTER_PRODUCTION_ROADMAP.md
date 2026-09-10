# 147 VR — Master Production Roadmap

> Single Source of Truth — approved YOLO Long Run roadmap
> Project: 147 VR
> Unity: 6000.4.4f1
> Current focus: Phase 3 — Table Visual & Marking Overlay

## Production Principle
- Physics Authority is authoritative for dimensions, ball spawn positions, collisions and gameplay truth.
- Visual assets must align to Physics; visual polish must not alter Physics Authority.
- No fabricated measurements or Golden values.
- Other projects are reference/component sources only and must remain untouched.
- Every meaningful change is verified and recorded in project documentation.

## Phase 0 — Foundation / Project Stability
- Unity/project structure stable
- Package/dependency workflow
- Desktop Commander workflow
- Backup/recovery workflow
- Documentation / Reality Map

## Phase 1 — Physics Authority
- M1 Straight — certified
- M1.5 Golden Infrastructure — certified
- M2.1 Stun — certified
- M2.2 Follow — certified
- M2.3 Draw — certified
- M2.4 English — certified
- M3 Cushion — certified / locked
- M4 Ball-to-Ball — certified
- M4.1 Collision Authority — certified
- M5 Shot Lifecycle / Gameplay Physics — NOT CERTIFIED (authority-chain evidence pending; see `Docs/M5_AUTHORITY_CHAIN_STATUS.md`)

## Phase 2 — Game Rules / Gameplay
- Shot lifecycle and turn flow
- Red / colour sequence
- Legal / illegal shot validation
- Foul scoring
- Ball-on state
- Frame end / winner
- Match structure
- Gameplay UI state integration

## Phase 3 — Table Visual & Marking
- Audit V007 source geometry, UVs, bounds and orientation
- Preserve clean V007 visual source
- Generate deterministic Snooker Marking Texture Overlay
- Baulk Line
- D Arc
- Six colour spots
- Bind markings to TABLE SURFACE material
- Remove/disable visual junk without touching Physics
- Verify TABLE FRAME / rail visual materials
- Verify Physics ball centers against visual spots
- Lock Physics↔Visual alignment

## Phase 4 — VR Perception
- Pocket jaw / mouth / drop geometry
- Cushion nose / rubber / cloth wrap
- Cloth micro detail
- Wood and rubber materials
- Contact shadows and realistic material response
- VR close-range perception pass

## Phase 5 — Lighting / Rendering
- Key / fill / environment lighting
- Reflection and shadow quality
- Exposure / tone mapping
- Post-processing restraint
- VR rendering optimization

## Phase 6 — Cue / Interaction
- Cue visual hierarchy
- Grab / aim / bridge / stroke
- Power and English interaction
- Cue obstruction / collision handling
- Rest interaction

## Phase 7 — VR UX
- Room-scale / seated / standing support
- Stable table reference
- Comfort-safe interaction
- Hand/controller UX
- Haptics

## Phase 8 — Game UI
- HUD: score, ball-on, reds, turn, foul, break
- Main menu
- Practice / frame / match flows
- Settings
- Frame result / restart / quit

## Phase 9 — Audio
- Ball / cushion / pocket impacts
- Cue strike
- Environment ambience
- UI feedback
- Voice / commentary where applicable

## Phase 10 — Multiplayer
- Mirror networking layer
- Authoritative shot/state flow
- Deterministic state replication
- Match / turn synchronization
- Dissonance voice integration where applicable

## Phase 11 — QA / Certification
- Physics Golden regression
- Gameplay legal/foul/frame tests
- Visual alignment tests
- Pocket / cushion verification
- Full regression matrix

## Phase 12 — Performance
- CPU / GPU frame time
- Draw calls / SetPass
- Physics cost
- Shadow / reflection cost
- GC / memory
- XR latency
- Real-headset profiling

## Phase 13 — AAA Polish / Release
- Material breakup and micro-detail
- Realistic wear / imperfection
- Lighting polish
- UI / audio / haptics polish
- Final regression
- Release candidate validation

## Current Gate
**ACTIVE: Phase 3 — Table Visual & Marking Overlay**

Current execution order:
1. Inspect V007 Blender source directly with bpy.
2. Inspect Unity Physics/table authority data.
3. Derive World→Surface UV mapping from measured geometry, not screenshots.
4. Generate high-resolution anti-aliased marking texture.
5. Bind overlay to TABLE SURFACE.
6. Sanity-clean known visual junk and preserve Physics Authority.
7. Verify ball-center ↔ spot alignment.
8. Record evidence and promote only after verification.
