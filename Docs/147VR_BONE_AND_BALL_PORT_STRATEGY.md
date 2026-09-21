# 147 VR — BONE & BALL / POOL-SNOOKER PORT STRATEGY

> Decision memo: 2026-08-25
> Scope: 147 VR only. Other Unity projects are read-only reference/component sources.
> Sources reviewed: BoneAndBall, CueStrike, existing 147 VR architecture/docs.

## 1. Executive Decision

147 VR should **not become a copy of BONE AND BALL**.

The strongest path is to keep the existing 147 VR gameplay spine and replace/upgrade only the weak subsystems with proven billiards technology:

`VR Cue/Input → Deterministic Snooker Physics → Shot Lifecycle → Rule/Score → Turn → Presentation`

BONE & BALL contributes the cleaned pool/snooker physics and gameplay modules.
CueStrike contributes the deeper VR interaction, calibration, trajectory and AI patterns.
147 VR remains the owner of the final architecture, UX, scenes and game identity.

## 2. Highest-Value Components To Borrow

### Tier A — MUST PORT / ADAPT

1. **SnookerPhysics** — primary ball simulation candidate.
   - WPBSA-sized ball/mass values.
   - Slip-to-roll friction model.
   - Cushion restitution + spin response.
   - Side-spin / english support.
   - Impulse + spin transfer.
   - `Strike(aimDirection, strikePoint, triggerPull)` is naturally VR-friendly.
   - Do NOT combine with a second competing ball-velocity controller.

2. **PhysicalShotController** from CueStrike/B&B lineage.
   - Idle → Aiming → Charged → Shooting → Resolving.
   - Converts physical hand/cue movement into a controlled shot.
   - Add haptics at contact/impact stages.

3. **SnookerRuleset / WBPS rules logic**.
   - Red → color sequence.
   - Color phase.
   - Foul validation.
   - Min 4 / max 7 penalty.
   - Respot-color logic.
   - Keep rules independent from UI and physics.

4. **Rack / ball identity model**.
   - Cue ball + 15 reds + 6 colors.
   - Stable BallId mapping.
   - Data-driven rack positions rather than scene hardcoding.

5. **Shot lifecycle/event contracts**.
   - Shot started.
   - First contact.
   - Ball potted.
   - All balls stopped.
   - Foul.
   - Shot resolved.
   - Frame won.

## 3. Tier B — HIGH VALUE, AFTER PHYSICS IS GREEN

6. **Trajectory prediction / Kalman predictor** from CueStrike RCA.
   - Use for aiming assistance, ghost-ball preview and accessibility.
   - Never let prediction drive authoritative physics.
   - Render prediction separately from simulation.

7. **RCA calibration architecture**.
   - Five-step calibration concept.
   - Persist calibration data.
   - Separate controller-less hand tracking from real-cue adapter input.
   - 147 VR should expose one `ICueInputSource`-style abstraction.

8. **VR stance / aim orbit / visual compensation**.
   - Useful for comfortable aiming.
   - Keep comfort aids optional and non-authoritative.
   - Visual latency compensation is especially valuable on Quest.

9. **PlayerStats + Save/Load pattern**.
   - Reuse JSON/data-driven persistence ideas.
   - Prefer a 147 VR save service over direct PlayerPrefs for core progression.

10. **AI difficulty architecture**.
    - Easy / Medium / Hard / Expert skill profiles.
    - Reuse the concept of a bridge between turn state, target selection and physical shot execution.
    - Do not copy the old heuristic AI as final competitive AI.

## 4. Tier C — BORROW THE IDEA, NOT THE CODE

- BONE & BALL leaderboard/monthly TOP 10 concept.
- BONE & BALL game-mode/rules hub concept, but 147 VR should stay snooker-first.
- CueStrike referee/mascot event bridge concept, only if 147 VR needs a commentator/referee layer.
- CueStrike audio event routing: ball hit, cushion, pocket, cue, ambient, UI.
- CueStrike Quest frame-rate policy: device-aware 72/90Hz baseline with 120Hz opt-in where appropriate.
- CueStrike URP shader/material audit workflow.
- Editor setup/self-test/idempotent tooling.

## 5. Explicitly DO NOT PORT

- BONE & BALL's Savanna/wildlife/theme dressing.
- BONE & BALL's 8-Ball / 9-Ball rules unless 147 VR later becomes multi-mode.
- Old CueStrike multiplayer implementation wholesale.
- Legacy singleton-heavy systems when an explicit reference/event contract is cleaner.
- Old audio/FX dependencies tied to CueStrike.
- Any source-project scene/prefab/GUID structure.
- Any component that requires modifying the source project.

## 6. Critical Physics Rule

The strongest finding from the source audit is that multiple ball-physics controllers must **not** fight each other.

Preferred 147 VR ball prefab:

`Rigidbody + Collider + BallIdentity + SnookerPhysics + BallVisual`

Optional surface/cue systems may report data, but only one authoritative component should integrate ball velocity/spin.

Do not stack `SnookerPhysics + FeltFriction + generic BallPhysics` on the same ball.

## 7. 147 VR Architecture Target

Existing architecture remains the contract:

`Cue Controller → Physics → Ball Tracker → Shot Tracker → Score Manager → Turn Manager → UI/View`

Upgrade it into:

`CueInputAdapter`
→ `CueInteractionState`
→ `SnookerPhysics`
→ `BallTracker`
→ `ShotLifecycle`
→ `SnookerRules`
→ `ScoreManager`
→ `TurnManager`
→ `Presentation`

Physics reports facts. Rules interpret facts. UI only presents state.

## 8. Product Vision — 147 VR Should Feel Like A Real Snooker Instrument

The goal is not simply "snooker in VR". The differentiator should be **physical credibility + coaching + mastery**.

### Core loop
1. Calibrate stance/cue.
2. Read table.
3. Build aim line.
4. Choose contact point / english.
5. Execute physical cue motion.
6. Feel impact through haptics/audio.
7. Watch real physics resolve.
8. Receive rule/scoring feedback.
9. Review shot quality.
10. Repeat and improve.

### Signature systems worth building

- **Shot Lab:** slow-motion review, cue path, contact point, predicted vs actual trajectory.
- **147 Challenge:** structured break-building challenge focused on a perfect 147.
- **Practice Table:** unlimited setup, respot, cue-ball placement and repeat-shot workflow.
- **Coach Layer:** optional visual guidance that can be disabled for pure simulation.
- **Skill Metrics:** pot success, cue-ball control, positional quality, break score, foul rate, long-pot consistency.
- **Comfort Layer:** seated/standing calibration, reduced-motion camera behavior, stable table-relative UI.

## 9. Implementation Order

### Phase A — Physics Foundation
- Port/adapt SnookerPhysics.
- Validate mass, radius, friction, cushion and spin.
- Build deterministic test shots.
- Verify no competing velocity controllers.

### Phase B — Shot System
- Integrate PhysicalShotController.
- Connect existing `SnookerCueController` through an input adapter.
- Add shot state machine + haptics.

### Phase C — Rules / Scoring
- Adapt SnookerRuleset into existing `SnookerScoreManager` boundary.
- Validate foul/score/respot behavior.
- Keep UI completely downstream.

### Phase D — Mastery Systems
- Trajectory predictor.
- Shot Lab.
- Coach layer.
- Skill metrics.
- 147 challenge.

### Phase E — Polish / Production
- Audio event layer.
- Quest performance tuning.
- URP/material audit.
- Build/runtime verification.
- Multiplayer only after the single-player physical loop is excellent.

## 10. Acceptance Gates

A port is accepted only when:

- [ ] 100 controlled physics shots reproduce expected outcomes within tolerance.
- [ ] Cushion + spin behavior is stable.
- [ ] VR cue movement produces repeatable shot power.
- [ ] Shot lifecycle resolves exactly once.
- [ ] Ball events reach rules without UI coupling.
- [ ] Full red/color sequence scores correctly.
- [ ] Fouls and respots are deterministic.
- [ ] Quest frame-time remains inside the project's VR budget.
- [ ] No source project was modified.

## 11. Final Decision

**Use BONE & BALL as a component/reference laboratory, not as the new base project.**

The best technical inheritance is:

`B&B Snooker Physics + CueStrike VR Interaction/Calibration + 147 VR existing gameplay architecture`

That combination gives 147 VR the highest upside without destroying the clean Gate 1–3 foundation already established.

This document is the persistent memory for this decision. Future 147 VR sessions should read it before importing or adapting pool/snooker components.
