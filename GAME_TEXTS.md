# Game Texts — English Only

> **Rule:** All user-visible text and debug/log messages in the game must be in English. No Thai or other non-English text is allowed in scripts, UI, headers, tooltips, or documentation files.

---

## ✅ What Already Exists (All English)

### Snooker Score UI (`SnookerScoreUI.cs`)
| Context | English Text |
|---------|-------------|
| Score display | `Player 1: {p1}   vs   Player 2: {p2}   |   Ball On: {ballOn}   Reds: {reds}` |
| Turn tag | `Turn: Player {turn}` |
| Last pot message | `Last Pot: {who} scored {points} points — {ballName} into {pocketName}` |
| Player names | `Player 1`, `Player 2` |

### Snooker Score Manager (`SnookerScoreManager.cs`)
| Context | English Text |
|---------|-------------|
| Ball-on names | `Red`, `Colour` (and colour order: `Yellow`, `Green`, `Brown`, `Blue`, `Pink`, `Black`) |
| Frame over | `Frame over ({Player1Score}:{Player2Score}) — press Reset Frame to play again` |
| Cue ball foul | `Foul! Cue ball potted in {pocket.name} — Player {opponent} awarded {penalty} points (turn passes)` |
| Frame won | `Frame over! Player {striker} wins {Player1Score} : {Player2Score}` |
| Legal pot | `{ball.name} potted in {pocket.name} — Player {striker} awarded {ball.points} points (continues)` |
| Wrong ball foul | `Foul! {ball.name} potted in {pocket.name} but ball on was {BallOnName()} — Player {opponent} awarded {penalty} points (turn passes)` |
| Frame reset | `Frame reset: Score 0 : 0, Reds 15, Ball on: Red` |
| Log scores | `Player 1: {player1Score}  |  Player 2: {player2Score}  |  Ball on: {BallOnName()}  |  Reds left: {redsRemaining}` |
| Re-spot log | `Re-spot {ball.name} → ({target.x}, {target.z})` |

### Snooker Ball Tracker (`SnookerBallTracker.cs`)
| Context | English Text |
|---------|-------------|
| Pot event log | `{ball.name} potted in {pocket.name} (+{ball.points})` |
| Ready log | `Ready: {_balls.Count} balls, {_pockets.Count} pockets.` |
| Potted flags cleared | `Potted flags cleared.` |
| Simulated pot | `Simulated: {ball.name} → {target.name}` |
| No ball found | `No in-play ball starting with '{namePrefix}' found.` |

### Snooker Shot Tracker (`SnookerShotTracker.cs`)
| Context | English Text |
|---------|-------------|
| Cue ball off table | `Foul! Cue ball off the table — Player {opponent} awarded {penalty} points (turn passes)` |
| Missed all balls | `Foul! Missed all balls — Player {opponent} awarded {penalty} points (turn passes)` |
| Wrong ball first | `Foul! Wrong ball first ({firstBall.name} when ball on was {scoreManager.BallOnName()}) — Player {opponent} awarded {penalty} points (turn passes)` |
| Legal shot | `Legal shot — hit {firstBall?.name ?? "?"} first` |
| Pot during shot | `Pot occurred during shot — ScoreManager already handled turn.` |

### Snooker Cue Controller (`SnookerCueController.cs`)
| Context | English Text |
|---------|-------------|
| XR detected | `XR controller detected — cue follows right hand.` |
| Desktop mode | `Desktop mode — move mouse to aim, hold LMB to charge, release to shoot.` |
| No aim | `Aim point is at the cue ball — nothing to shoot at.` |
| Shot log | `Shot! power={power} speed={speed} m/s dir=({dir.x}, {dir.z})` |

### Snooker Turn Manager (`SnookerTurnManager.cs`)
| Context | English Text |
|---------|-------------|
| No balls warning | `No Rigidbody balls found — falling back to Manual mode. Use the configured key to pass the turn.` |
| Striker continues | `Striker keeps the table (legal pot).` |
| Turn broadcast | `Turn → Player {currentPlayer}` |
| Tracking balls | `Tracking {_balls.Count} balls for shot-end detection.` |

### Snooker Physics Setup (`SnookerPhysicsSetup.cs`)
| Context | English Text |
|---------|-------------|
| Surface not found | `Playing surface not found — physics not built.` |
| Ready log | `Ready: surface top y={_surfaceTopY}, bounds=({_tableBounds.size.x} x {_tableBounds.size.z})` |
| Caught ball | `Caught ball {ballCollider.name} at pocket ({pocketCentre.x}, {pocketCentre.z})` |

### Player View Manager (`PlayerViewManager.cs`)
| Context | English Text |
|---------|-------------|
| No turn manager | `No SnookerTurnManager found — falling back to activePlayer only.` |
| Rig position | `Rig at ({eye.x}, {eye.y}, {eye.z}) yaw {yaw} — player {player}` |
| Camera position | `Camera at ({eye.x}, {eye.y}, {eye.z}) facing table centre — player {player}` |
| No rig/camera | `No rig and no main camera found.` |
| No table renderer | `No table renderer found under '{root.name}'.` |

### Quest Spawn Setup (`QuestSpawnSetup.cs`)
| Context | English Text |
|---------|-------------|
| Header | `Near Center Pocket Spawn Points` |
| Missing spawn | `Spawn point missing for {objectName}.` |
| Object not found | `Could not find {objectName}.` |

### Skin Cycler (`SkinCycler.cs`)
| Context | English Text |
|---------|-------------|
| Skin names | `Navy & Gold`, `Walnut`, `Dark Wood` |
| Button label | `Skin: {name}` |
| Toast message | `Skin: {name}` |
| No manager error | `No TableSkinManager (or empty prefab list) — disabling.` |

### Skin Select Menu (`SkinSelectMenu.cs`)
| Context | English Text |
|---------|-------------|
| Title | `Select Table Skin` |
| Start button | `START GAME` |
| Status text | `Selected: {skinNames[index]}` |

### Rotating Prom Skybox (`RotatingPromSkybox.cs`)
| Context | English Text |
|---------|-------------|
| Header | `Start Environment`, `HDRI per Mode`, `HDRI Rotation` |
| Tooltip | `Press B on right hand or Y on left hand to cycle environments` |

### Drifting Concert Smoke (`DriftingConcertSmoke.cs`)
| Context | English Text |
|---------|-------------|
| Headers | `Required: assign SmokeParticle_Texture.png here`, `Smoke spread area (meters)`, `Smoke density (particles per second)`, `Drift speed upward (meters/second)`, `Smoke particle size (meters)`, `Smoke particle lifetime (seconds) — longer = drifts further`, `Smoke opacity (0=invisible, 1=opaque)`, `Smoke color (cool concert tone or warm white)` |

### Aurora Flow (`AuroraFlow.cs`)
| Context | English Text |
|---------|-------------|
| Headers | `Scroll Speed`, `Aurora Colors`, `Dome Size`, `Aurora band height/position (0=horizon, 1=overhead)` |

### Fireflies (`Fireflies.cs`)
| Context | English Text |
|---------|-------------|
| Headers | `Firefly drift area (meters)`, `Number of fireflies`, `Drift speed`, `Light point size (meters)`, `Firefly color`, `Flicker speed` |

### Shooting Stars (`ShootingStars.cs`)
| Context | English Text |
|---------|-------------|
| Headers | `Interval (seconds) — average about 10 seconds`, `Shooting star speed (units/second)`, `Trail length`, `Distance from player (should be beyond normal view range to simulate sky)`, `Shooting star color` |

### Twinkling Stars (`TwinklingStars.cs`)
| Context | English Text |
|---------|-------------|
| Headers | `Number of twinkling stars`, `Star sprite (soft glowing white circle, transparent background)`, `Distance from player`, `Star size (world units)`, `Twinkle speed (cycles/second)` |

### Pool Table Scripts
| Script | English Text |
|--------|-------------|
| BallRack | `No tableBed found — assign it in the Inspector.`, `Racked {9/8}-ball — {count} balls + cue at head spot, shuffle={shuffle}.` |
| SkinCycler | `No TableSkinManager (or empty prefab list) — disabling.` |
| TableSkinManager | `No table prefabs assigned.`, `Applied skin {skin}` |

### CuteDancer (`CuteDancer.cs`)
| Context | English Text |
|---------|-------------|
| Dance names (auto-generated) | `Arms Hip Hop Dance`, `Booty Hip Hop Dance`, `Dancing Twerk`, `Hip Hop Dancing`, `Hip Hop Dancing 1`, `Rumba Dancing` |

### Tablet Options Menu (`TabletOptionsMenu.cs`)
| Context | English Text |
|---------|-------------|
| Title | `⚙  OPTIONS` |
| Slider labels | `MOVEMENT SPEED`, `PLAYER HEIGHT (CM)`, `VOLUME` |
| Mute button | `🔇  UNMUTE` / `🔊  MUTE` |
| Scene label | `SCENE` |
| Scene buttons | `MR MODE`, `NightSky`, `Dreamy_OLED`, `ConcertRoom`, `promDance` |
| Value labels | `{speed}x`, `{height} cm`, `{volume}%` |
| Debug log | `Scene selected: {sceneName} — load this scene to apply.` |

### Quest Spawn Setup (`QuestSpawnSetup.cs`)
| Context | English Text |
|---------|-------------|
| Placement log | `Placed {objectName} at ({x}, {y}, {z}) — offset {backOffset} m back from table.` |

### Controller Map Info (`ControllerMapInfo.cs`)
| Context | English Text |
|---------|-------------|
| Documentation-only | Full controller mapping reference (see file for details) |

---

## ⚠️ What's Still Missing / Should Be Added

### 1. User-Facing UI Text
| Missing Text | Suggested Location | Notes |
|--------------|-------------------|-------|
| Foul notification popup | `SnookerScoreUI.cs` | Show "FOUL!" on screen briefly when a foul occurs |
| Frame over announcement | `SnookerScoreUI.cs` | Large centered "FRAME OVER — Player X Wins!" overlay |
| Turn indicator | `SnookerScoreUI.cs` | Visual arrow/highlight showing whose turn it is |
| Shot power indicator | `SnookerCueController.cs` | A power bar or percentage display while charging |
| Game over screen | New script or `SnookerScoreManager.cs` | End-of-match winner screen with play again option |
| Pause menu | New script | "PAUSE" / "RESUME" / "QUIT" menu |
| Settings menu | New script | "SETTINGS" with volume, graphics, controls options |

### 2. Snooker Rules Explanations
| Missing Text | Suggested Location | Notes |
|--------------|-------------------|-------|
| "Ball on: Red/Colour/Yellow..." | In-game tooltip | Explain what "ball on" means to new players |
| Foul explanation | `SnookerScoreUI.cs` | Brief reason for each foul (e.g., "Wrong ball hit first") |
| Break-off hint | `SnookerCueController.cs` | "Break off by hitting the reds" on first shot |
| Snooker rules summary | Help screen or README | A reference card for snooker rules |

### 3. Pool/8-Ball/9-Ball Mode Text
| Missing Text | Suggested Location | Notes |
|--------------|-------------------|-------|
| "8-Ball" / "9-Ball" mode label | `SkinSelectMenu.cs` or new menu | Game mode selector |
| "Break" instruction | `BallRack.cs` or UI | "Player 1 breaks" at start |
| Ball-in-hand notification | `SnookerScoreUI.cs` or equivalent | "Ball in hand — place the cue ball" |
| Win/loss messages | Pool mode scripts | "Player X wins!" for pool modes |

### 4. XR/Quest-Specific Text
| Missing Text | Suggested Location | Notes |
|--------------|-------------------|-------|
| Controls tutorial | New script or overlay | "Hold trigger to charge, release to shoot" |
| MR passthrough notice | `QuestPassthroughBridge.cs` | "Passthrough enabled/disabled" (already in log, could be user-visible) |
| Comfort settings | Settings UI | "Snap turning", "Vignette" options |

### 5. Accessibility Text
| Missing Text | Suggested Location | Notes |
|--------------|-------------------|-------|
| Color-blind mode labels | Settings | Ball color names displayed near balls |
| Subtitles/captions | All audio sources | For any future sound effects or music |
| High-contrast UI | All UI elements | Text should have sufficient contrast ratios |

### 6. Documentation & Metadata
| Missing Text | Suggested Location | Notes |
|--------------|-------------------|-------|
| README.md | Project root | English project description and setup instructions |
| Changelog | Project root | Track English text additions/changes |
| In-game credits | End screen | "Made with Unity" / team credits |

---

## 🔧 Enforcement Notes

1. **All `[Header()]` and `[Tooltip()]` attributes** must use English only.
2. **All `Debug.Log()`, `Debug.LogWarning()`, `Debug.LogError()`** messages must use English only.
3. **All XML doc comments (`///`)** must use English only.
4. **All inline comments** should use English (or be removed).
5. **All UI text** (Text components, button labels, toast messages) must use English only.
6. **Future scripts** must follow the English-only rule from the start.
