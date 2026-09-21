# 147 VR Baseline Freeze - 2026-09-19

This is a non-destructive baseline record created before the Local Snooker Vertical Slice pass.

## Repository state

- Project: `C:\Users\mongo\UnityProjects\147 VR`
- Unity: `6000.4.4f1`
- Branch: `checkpoint/m5-certified-baseline-20260915`
- HEAD at freeze: `72cd5fb5b4a6d05352bcabb09c7e6796e2cc33ec`
- Tracked modified paths at freeze: 42
- Untracked paths at freeze: 135330

The existing working tree contains prior project work, generated artifacts, backups, and runtime changes. They are preserved. No reset, checkout, clean, or deletion is part of this freeze.

## Scope lock

The vertical slice may change only the cue/local-test path and its direct documentation until a clean Unity validation result is available. Certified physics authority remains `CuePhysicsAdapter`; downstream gameplay remains behind `M5ShotLifecycle` and `M5ShotEventContract`.

## Testing contract

- Desktop mouse aim and left-button charge/release remain available in the Editor.
- `ControlMode.Desktop` is the deterministic editor path.
- `R` resets the frame during Editor testing.
- `U` restores the last settled local test shot when a snapshot exists.
- VR remains available through `ControlMode.VR` or `ControlMode.Auto` outside the Editor preference.

## Next gate

Run a package-aware Unity compile and then a focused PlayMode/runtime check for desktop aim, shot lifecycle, settle, score/turn transaction, reset, and undo. Do not report the vertical slice as complete until those checks produce evidence.
