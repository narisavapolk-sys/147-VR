# S2 #2 — BHP (BLACK HOLE POOL) cue-grip control map → 147VR

**Source (read-only):** `C:\Users\mongo\Tools\pulled_apks\com.el.blackholepooldemo\base.apk` (547 MB, Unity/OVR/Fusion Quest title),
strings dump `_work\bhp_meta_strings.txt` (159,116 sorted strings), script list `_work\bhp_script_paths.txt`.
**No assets were extracted or imported.** Owner instruction: borrow the BUTTON-USAGE SCHEME only.

---

## 1. Evidence — BHP's own tutorial strings (verbatim recovered)

| BHP step | Recovered string (bold slots = button the game inserts at runtime) |
|---|---|
| **AIM** | "You can aim by simply moving your hands. Just like in real life." |
| **LINE UP** | "The orbit-around movement method is the easiest and fastest way to line up a shot." + "Press and hold the `<…>` button and move your **left hand** left/right to line up the shot." + "When your aim looks good, press and hold the `<…>` **to lock your aim**!" |
| **SHOOT** | "Move your `<…>` **back and forth** to shoot the ball!" (physical stroke, no charge meter) |
| **PICK UP BALL** | "Press the `<…>` button to pick up the cueball!" … "`<…>` button again to drop the cueball!" + "Red color indicates invalid position, while white color means valid position." |
| **Movement** | "You can move forward/backward and strafe left/right with the `<…>`." + "You can turn left/right with the `<…>`." + "You can also teleport by bending forward the `<…>`." |
| **Recenter** | "To get the most out of this game, it's important to recenter yourself when the game starts or when you feel something is off! Recenter yourself by `<…>`". |
| **Laser Pointer** | "Press and hold the `<…>` to activate the laser, aim at the button, then use the `<…>` to click on the button!" |
| **FINAL WORDS** | "…find more rooms and game modes using the **ELEVATOR**." (menu) |
| Foul copy (rule validated) | **"No ball hit the cushion after first contact!"** — independently confirms our S2 #1 `NoRailAfterContact` rule. |

Grabbing architecture (Discovery): **Oculus Interaction SDK HandGrab + CylinderGrabSurface** (physics-honest hands-on-cue),
game scripts `LocalCue.cs / PoolBall.cs / PoolTable(s).cs / TablePocketController.cs / TriggerEventDispatcher.cs`.
The SHOOT step being a pure hand **motion** (no trigger hold) matches this: the stroke velocity itself powers the shot.

## 2. Finding: exact button labels are NOT stored in the APK

Enumerated **all 45 literal `<b>…</b>` tokens**: headers (`AIM`, `LINE UP`, `SHOOT`, `PICK UP BALL`, `PLACE BALL`,
`FINAL WORDS`, `Laser Pointer`, `Movement`, `Recenter`, `ELEVATOR`) + body refs (`left hand`, `OK`) + UI/debug junk.
**The per-action button names exist nowhere as literals** (searched line-starts GRIP/TRIGGER/PRIMARY/SECONDARY/MENU/STICK/
JOYSTICK/RECENTER and completions `grip</…>`, `GRIP</…>`, `A/X/B/Y</…>` — zero hits). BHP assembles "press the <NAME>"
sentences at runtime from the active device config (touch controllers vs hand tracking ⇒ dynamic labels).
⇒ 147VR must not guess BHP's literal label; we mirror the **gesture structure** with Quest Touch controls.

## 3. 147VR mapping (Quest Touch — state-gated on "player is staged on the ball")

| BHP action | BHP gesture | 147VR control | Conflict notes vs `ControllerMapInfo.cs` |
|---|---|---|---|
| AIM | free hands | free cue/head aim (no button); Right-thumbstick coarse aim stays as assist | unchanged |
| LINE UP / LOCK aim | hold button + left-hand left/right orbit | **HOLD Left Grip = fine-aim lock**; left-hand left/right rotates the lock; release = locked line | Left Grip = Sprint → re-gated: Sprint active only while **not** staged (walking), fine-aim lock only while staged |
| SHOOT | physical stroke (back-and-forth) | **HOLD Right Grip = grip the cue**; pull back → push forward; forward-pass peak velocity along the cue axis = strike power | replaces Trigger-hold-charge as PRIMARY path; **Right Trigger charge stays as accessibility fallback** |
| Cancel/reset | (BHP: whiff the stroke) | **Right Trigger while staged** = cancel/reset charge (frees Right Grip for the cue) | Right Grip cancel moves to the new stroke grip; resolved by the staged/walking gate |
| PICK UP / PLACE cue ball | press button to lift; press again to drop; red/white validity | **Left X (primary)** toggles lift/drop when ball-in-hand rules allow; ghost cue **red = invalid / white = valid** | X currently unused; rules gate = `PoolPhysicsRulesBridge` verdict state (S2 #1) |
| Movement | L-stick + bend-forward teleport | unchanged (Left stick move); bend-forward teleport **DEFERRED** (comfort review, needs owner approval) | — |
| RECENTER | combo | **Left Grip + Right Grip (both pressed)** = recenter | uses the reserved-combo slot pattern |
| LASER UI | hold button → laser; trigger click | **HOLD Y (left secondary)** = laser; Right Trigger click (world-UI/tablet) while **not** staged | state-gate disambiguates from staged-cancel on the same trigger |
| Menu / tablet | menu | unchanged (Y or Menu button = TabletOptionsMenu) | — |

**One-line summary ของสารบบ BHP ที่เรายืม:** เล็งด้วยมือปกติ → กดค้างเพื่อล็อค → ชัก-ภาคด้วยมือจริงเป็นแรงยิง → ปุ่มเดียวยก/วางลูก
พร้อมสีแดง/ขาวบอกพื้นที่ดรอป → ปุ่มคอมโบ recenter → laser สำหรับ UI.

## 4. Unity-side scaffold

`Assets/Scripts/Quest/BhpCueGripController.cs` — thin MonoBehaviour, reads the same XR API as `SnookerCueController`
(`InputDevices.GetDeviceAtXRNode` + `CommonUsages`), publishes semantic events for the tracker to consume.

**Not yet wired (deliberate):** connection to `SnookerShotTracker`/`SnookerCueController` happens in the NEXT Unity session
(lock + compile + PlayMode rerun 5/5), so this scaffold net-compiles today and integration lands as one reviewed change
later. No scene edits shipped unattended (project rule: MenuItem/click flow stays with เจ้าของ).
