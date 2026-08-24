using UnityEngine;

/// <summary>
/// Meta Quest 2 / 3 Controller Mapping Reference for the VR Snooker game.
///
/// This class is documentation-only — it is not executed at runtime.
/// Attach it to any GameObject if you want to see the mapping in the Inspector.
///
/// ═══════════════════════════════════════════════════════════════════════
///  LEFT HAND CONTROLLER
/// ═══════════════════════════════════════════════════════════════════════
///
///  ┌──────────────────────────────────────────────────────────────────┐
///  │  Thumbstick        → Move (strafe left/right, forward/back)    │
///  │  Y button          → Toggle Tablet Options Menu                │
///  │  X button          → Reset Frame / New Game                     │
///  │  Left Trigger      → Grab (not used in snooker mode)           │
///  │  Left Grip         → Sprint / Fast movement (hold)             │
///  │  Menu button       → Open Tablet Options Menu (same as Y)     │
///  └──────────────────────────────────────────────────────────────────┘
///
/// ═══════════════════════════════════════════════════════════════════════
///  RIGHT HAND CONTROLLER
/// ═══════════════════════════════════════════════════════════════════════
///
///  ┌──────────────────────────────────────────────────────────────────┐
///  │  Thumbstick        → Aim cue direction (projected on table)    │
///  │  A button          → Cycle environment (Prom/Bokeh/Night/MR)  │
///  │  B button          → Cycle environment (same as A)            │
///  │  Right Trigger     → HOLD to charge shot power, RELEASE = shoot│
///  │  Right Grip        → Cancel shot / Reset charge                │
///  └──────────────────────────────────────────────────────────────────┘
///
/// ═══════════════════════════════════════════════════════════════════════
///  BUTTON COMBOS
/// ═══════════════════════════════════════════════════════════════════════
///
///  Left Trigger + Right Trigger (simultaneously)
///     → Not used yet. Reserved for future "spectator camera" mode.
///
///  Left Grip + Right Trigger
///     → Not used yet. Reserved for future "telekinesis ball placement".
///
///  Both Thumbsticks pressed (L3 + R3)
///     → Not used yet. Reserved for future "quick reset table" shortcut.
///
///  Y + B (simultaneously)
///     → Not used yet. Reserved for future "developer debug menu".
///
/// ═══════════════════════════════════════════════════════════════════════
///  DESKTOP FALLBACK (no VR headset)
/// ═══════════════════════════════════════════════════════════════════════
///
///  Mouse           → Aim cue (raycast onto table plane)
///  Left Click (hold) → Charge shot, release = shoot
///  Right Click      → Cancel shot
///  W/A/S/D          → Move camera (desktop preview)
///  Space            → Next turn
///  Tab              → Cycle skin / dance
///  1..6             → Jump to specific dance (CuteDancer)
///  M                → Toggle tablet options menu
///
/// ═══════════════════════════════════════════════════════════════════════
///  SNOKER GAME FLOW
/// ═══════════════════════════════════════════════════════════════════════
///
///  1. Player 1 breaks from the D (baulk) side.
///  2. After each shot, the rig auto-moves to the shooting player's end.
///  3. Right Trigger: hold → charge power → release = strike.
///  4. Turn auto-advances when all balls come to rest (AfterShot mode)
///     or after a timer (Timer mode), or manually via Left Grip.
///
/// ═══════════════════════════════════════════════════════════════════════
///  TABLET OPTIONS MENU (left controller Y or Menu button)
/// ═══════════════════════════════════════════════════════════════════════
///
///  Movement Speed    → Slider 1–10 (default 3)
///  Player Height     → Slider 160–185 cm (default 170)
///  Volume            → Slider 0–100% (default 80)
///  Mute button       → Toggle audio on/off
///  Scene selector    → MR MODE / NightSky / Dreamy_OLED / ConcertRoom / promDance
///
/// ═══════════════════════════════════════════════════════════════════════
/// </summary>
public sealed class ControllerMapInfo : MonoBehaviour
{
    // This class is documentation-only. No runtime code needed.
}
