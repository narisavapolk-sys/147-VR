# Quest 2/3 Controller Mapping — VR Snooker 147

> **File:** `Assets/147 main/design/CONTROLLER_MAPPING.md`
> **Version:** 2024-01-20
> **Status:** ACTIVE

---

## Left Hand Controller

```
┌──────────────────────────────────────────────────────────────────┐
│                                                                  │
│   Thumbstick (move)                                              │
│     ↑ Forward      ← Left / Right →     ↓ Backward              │
│     Move the player around the room.                             │
│     Speed controlled by Tablet Options slider (1–10).           │
│                                                                  │
│   Y Button                                                       │
│     Toggle Tablet Options Menu (same as Menu button).           │
│     Press again to close.                                        │
│                                                                  │
│   X Button                                                       │
│     Reset Frame — restarts the current snooker frame.           │
│     Score goes to 0:0, balls re-rack, turn resets to Player 1. │
│                                                                  │
│   Left Trigger (index finger)                                    │
│     Reserved — not used in snooker mode.                        │
│     (Future: spectator camera toggle.)                          │
│                                                                  │
│   Left Grip (palm)                                               │
│     Sprint — hold to move faster (2x speed multiplier).         │
│     Can also be used to manually advance turn in Manual mode.   │
│                                                                  │
│   Menu Button (small button)                                     │
│     Toggle Tablet Options Menu (same as Y).                     │
│                                                                  │
└──────────────────────────────────────────────────────────────────┘
```

---

## Right Hand Controller

```
┌──────────────────────────────────────────────────────────────────┐
│                                                                  │
│   Thumbstick (aim)                                               │
│     Aim the cue direction on the table surface.                 │
│     The cue stick follows your aim point.                        │
│                                                                  │
│   A Button                                                       │
│     Cycle Environment:                                          │
│       Prom → Bokeh → NightSky → MR → Prom (loop)               │
│                                                                  │
│   B Button                                                       │
│     Cycle Environment (same as A — redundant for comfort).     │
│                                                                  │
│   Right Trigger (index finger) — SHOOT                           │
│     HOLD to charge power (visual cue turns red).                │
│     RELEASE to strike the cue ball.                              │
│     Power = 0% at release → gentle tap.                         │
│     Power = 100% at release → maximum speed (8 m/s).            │
│     Right-click equivalent on desktop.                           │
│                                                                  │
│   Right Grip (palm)                                              │
│     Cancel shot — resets charge to zero without shooting.       │
│     Same as right-click on desktop.                              │
│                                                                  │
└──────────────────────────────────────────────────────────────────┘
```

---

## Button Combos

| Combo | Action | Status |
|-------|--------|--------|
| Left Trigger + Right Trigger | Spectator camera mode | ⏳ Reserved |
| Left Grip + Right Grip | Telekinesis ball placement | ⏳ Reserved |
| L3 + R3 (both thumbsticks pressed) | Quick reset table | ⏳ Reserved |
| Y + B simultaneously | Developer debug menu | ⏳ Reserved |

---

## Desktop Fallback (no VR headset)

| Input | Action |
|-------|--------|
| Mouse | Aim cue (raycast onto table) |
| Left Click (hold) | Charge shot |
| Left Click (release) | Shoot |
| Right Click | Cancel shot |
| W/A/S/D | Move camera |
| Space | Next turn |
| Tab | Cycle skin / dance |
| 1–6 | Jump to dance (CuteDancer) |
| M | Toggle tablet options menu |

---

## Snooker Game Flow

1. **Player 1 breaks** from the D (baulk) side.
2. **After each shot**, the rig auto-moves to the shooting player's end.
3. **Right Trigger**: hold → charge power → release = strike.
4. **Turn auto-advances** when all balls come to rest (AfterShot mode) or after a timer (Timer mode).
5. **Manual turn advance**: Left Grip in Manual mode.
6. **Fouls** are auto-detected by `SnookerShotTracker`:
   - Cue ball potted → opponent +4, cue re-spotted
   - Missed all balls → opponent +4
   - Wrong ball hit first → opponent +max(4, ball-on value, ball hit value)

---

## Tablet Options Menu

Press **Y** or **Menu button** (left controller) to open.

| Setting | Range | Default | Persistence |
|---------|-------|---------|-------------|
| Movement Speed | 1–10 | 3 | PlayerPrefs |
| Player Height | 160–185 cm | 170 | PlayerPrefs |
| Volume | 0–100% | 80% | PlayerPrefs |
| Mute | On/Off | Off | PlayerPrefs |
| Scene | 5 options | MR MODE | PlayerPrefs |

### Scene Options
| # | Scene Name | Description |
|---|-----------|-------------|
| 0 | MR MODE | Passthrough mixed reality (real room) |
| 1 | NightSky | Stargazing with aurora, fireflies, shooting stars |
| 2 | Dreamy_OLED | Dark room with OLED-style skybox |
| 3 | ConcertRoom | Concert hall with drifting smoke effects |
| 4 | promDance | Prom/dance party with rotating HDRI |

---

## Character Placement

Both display characters (CuteGirl_Dancing, ChubbyGirl_Dancing) are now placed **1.5 metres (≈ 3 steps)** further back from the table than their original spawn points. This prevents them from blocking the shooting player's view.

Configurable via `QuestSpawnSetup.backOffset` in the Inspector.

---

## Files Reference

| File | Purpose |
|------|---------|
| `Assets/Scripts/Quest/TabletOptionsMenu.cs` | VR tablet menu with all settings |
| `Assets/Scripts/Quest/ControllerMapInfo.cs` | Documentation-only script |
| `Assets/Scripts/Quest/QuestSpawnSetup.cs` | Character placement (updated with backOffset) |
| `Assets/InputSystem_Actions.inputactions` | Unity Input System bindings |
