## Purpose
Single source for Unity execution ownership between LUNA and Claude.

## Hard Rule
ONE Unity Executor at a time. Multiple AI may reason/review, but only the holder may open Unity, edit project files, run AssetDatabase-sensitive operations, build, or execute runtime tests.

## Current Lock
- Holder: NONE
- Status: RELEASED
- Last Holder: LUNA
- Last Domain: Unity Integration QA (2026-11-05)
- Released: 2026-11-05
- Next executor must record a fresh takeover before Unity-sensitive work.

## Before Taking the Lock
1. Read this file.
2. Read `147VR_AI_WORK_PROTOCOL.md`.
3. Read `147VR_CURRENT_STATE.md`.
4. Confirm no Unity/Unity Hub execution is active that could conflict.
5. Write the intended task and ownership here.

## Shared-Zone Rule
Scene setup, prefabs, rendering settings, build settings, and other shared files require the lock even if the change appears cosmetic.

## Handoff
The current holder must record changed files, verification, blockers, and next action before releasing the lock.

## Emergency Rule
If the holder becomes unreachable, do not assume the lock is stale. Inspect actual processes and state, then record a takeover with evidence before editing.

## Takeover 2026-10-10 (compile-gate verification)
- Holder: QwenWork agent (acting on project owner Mon's explicit authorization to run the UPM/batch proof).
- Prior holder LUNA lock left ACTIVE from 2026-09-08; owner authorized takeover to continue after a machine freeze/reboot.
- Evidence at takeover (2026-10-10 ~06:40 Europe/Rome): `Get-Process` found NO `Unity` and NO `UnityPackageManager` process running; no `Library/EditorInstance.json` present. No conflicting live Unity session.
- Intended task: run `Docs\Tools\Unity_Batch_Safe.ps1` in COMPILE-GATE mode (no `-executeMethod`, no `-runTests`) to obtain real evidence that UPM IPC connects and Unity batch exits 0.
- Scope boundary: tooling/verification only. No Physics Authority, no M5 code, no scene/prefab edits. If the bare open triggers asset import, that is Unity's own refresh only.
- Release condition: QwenWork records the run result (UPM connect / exit code / compile signature) in this file and to the current-state doc, then marks RELEASED.

## Takeover outcome 2026-10-10 — RELEASED
- Compile-gate result: UPM IPC **CONNECTED in 0.7s** on 4f1 (license OK, 86 packages registered, no "Received undefined"). The historical "Could not connect to IPC after 30s" blocker is **resolved** via the correct launcher `Docs\Tools\Unity_Batch_Safe.ps1` (sanitized PATH + full env incl. ALLUSERSPROFILE). Launcher drift already consolidated (`Tools\Launch_147VR_Safe.ps1` is `.DEPRECATED`).
- The run did NOT reach a clean exit-0 verdict: the machine **hard-froze and was power-cycled twice** (owner). Root cause identified = **RAM exhaustion** (16GB, only ~4.5GB free at idle with Blender+Chrome+QwenWork open). NOT Defender (`C:\Users\mongo` already AV-excluded), NOT HDD (C: is a 512GB SSD), pagefile is system-managed.
- Owner decision this session: **canonical editor = Unity 6000.4.4f1** (pinned in ProjectVersion.txt). Stop opening with 12f1 — it wipes/rebuilds `Library/Bee` (full recompile) every switch.
- Owner-approved cleanup: 5 redundant `Library_*` backups + `%TMP%` + `New Unity Project` moved to **Recycle Bin** (not permanently deleted; empty bin to reclaim space).
- **Lock now RELEASED.** No Unity/UnityPackageManager running. Next Unity batch attempt: close Blender/Chrome first, single instance; expect a one-time full rebuild, then fast incremental opens.

## Takeover 2026-10-11 (HDRI atmosphere staging — visual only)
- Holder: QwenWork agent (acting on owner Mon's explicit request to add switchable warm-club HDRI atmospheres for 147-VR).
- Prior state: lock was RELEASED (2026-10-10). Verified `tasklist` shows NO Unity / UnityPackageManager / Blender process at takeover.
- Intended task: stage 3 CC0 Poly Haven HDRI atmospheres (warm_bar, warm_reception_dinner, country_club @2k) into `Assets/AAA/ImportedSnooker/Venue/Skybox/Atmospheres/`, plus a runtime `AtmosphereSkyboxSwitcher.cs` and an editor `147VR_AtmosphereSkybox.cs` (MenuItem-only, mirrors existing `147VR_Skybox.cs`) to build `Skybox/Panoramic` materials and preview each atmosphere.
- Scope boundary: ADDITIVE + VISUAL ONLY. No scene/prefab edits, no physics/collider/transform changes, no Unity opened by the agent, no build. `RenderSettings.skybox` is only touched when the OWNER clicks a menu item in Unity (owner becomes executor at that point).
- Release condition: agent finishes staging files and marks RELEASED; owner performs the Unity menu click + on-device (Quest) visual/perf check.

### Release 2026-10-11 — RELEASED
- Agent staged files only; Unity was NEVER opened by the agent. No scene/prefab/physics/build changes.
- Changed/added files (all additive):
  - `Assets/AAA/ImportedSnooker/Venue/Skybox/Atmospheres/Atmos_WarmBar_2k.hdr`
  - `Assets/AAA/ImportedSnooker/Venue/Skybox/Atmospheres/Atmos_ReceptionDinner_2k.hdr`
  - `Assets/AAA/ImportedSnooker/Venue/Skybox/Atmospheres/Atmos_CountryClub_2k.hdr`  (all 2048x1024 Radiance, CC0 Poly Haven)
  - `Assets/Scripts/AtmosphereSkyboxSwitcher.cs` (runtime, Assembly-CSharp)
  - `Assets/Editor/147VR_AtmosphereSkybox.cs` (MenuItem-only, Assembly-CSharp-Editor)
- Owner next actions (owner becomes executor when clicking): open Unity 6000.4.4f1 (single instance) → menu `147VR/Art/Atmosphere/Build Skybox Materials` → optionally `Add Switcher GameObject (auto-wire materials)` → wire UI buttons to `SetAtmosphere(i)` → verify on Quest (memory/perf) before adopting.
- Lock now RELEASED.

## Takeover 2026-10-11 (scene cleanup — Recycle Bin, non-destructive)
- Holder: QwenWork agent (on owner Mon's explicit instruction: keep only the certified/golden + still-in-use scenes, move the rest to the Recycle Bin).
- Prior state: lock was RELEASED. Unity verified NOT running.
- KEEP (certified/golden + active): `147VR_MainScene.unity`, `147VR_MainScene_V008_TEST.unity`, `147VR_MainScene_V008_CLEAN_TEST.unity`, `147VR_MainScene_ART_V006.unity`, `PoolTable_8Ball.unity`, `PoolTable_9Ball.unity`.
- MOVE TO RECYCLE BIN (7 scenes + their .meta): `147VR_MainScene_PRE_V008.unity`, `SampleScene.unity`, `SampleScene_PRE_M7_4_CLEAN_MAIN_20260903.unity`, `SampleScene_PRE_M7_4_SCENE_CLEANUP_20260902.unity`, `SampleScene_PRE_M7_4_TABLE_SWAP_20260902.unity`, `SampleScene_PRE_M7_4_V004_REPLACEMENT_20260903.unity`, `SampleScene_PRE_V004_REPLACEMENT_20260903_045322.unity`.
- Also: remove the now-deleted `SampleScene.unity` entry from `ProjectSettings/EditorBuildSettings.asset` (was build index 1) to avoid a missing-scene reference.
- Scope boundary: Recycle Bin only (recoverable); `.bak` / `precert_*.bak` files untouched; no edits to any KEPT scene; no physics/authority change; Unity NOT opened by agent.
- Release condition: agent records the moved-file list + verification, marks RELEASED.

### Release 2026-10-11 (scene cleanup) — RELEASED
- Executed via PowerShell `SendToRecycleBin` (recoverable). Moved to Recycle Bin: `147VR_MainScene_PRE_V008.unity`, `SampleScene.unity`, and the 5 `SampleScene_PRE_M7_4_*` / `SampleScene_PRE_V004_*` snapshot scenes — each `.unity` + its `.unity.meta` (14 files total). All verified gone from `Assets/Scenes`.
- `ProjectSettings/EditorBuildSettings.asset` trimmed 4 → 3 scenes (removed deleted `SampleScene`); build order now: `147VR_MainScene` (0), `PoolTable_8Ball` (1), `PoolTable_9Ball` (2).
- Verified remaining scenes in `Assets/Scenes`: `147VR_MainScene.unity`, `147VR_MainScene_ART_V006.unity`, `147VR_MainScene_V008_TEST.unity`, `147VR_MainScene_V008_CLEAN_TEST.unity`, `PoolTable_8Ball.unity`, `PoolTable_9Ball.unity`. `.bak`/`precert_*.bak` untouched.
- No KEPT scene opened/edited; no physics/authority change; Unity NOT opened by agent. Owner: open Unity once so AssetDatabase refreshes (drops deleted .meta) and re-check Build Settings.
- Lock now RELEASED.

## Takeover 2026-10-11 (procedural cue tool — additive editor script)
- Holder: QwenWork agent (owner Mon requested a procedural cue for 147-VR; start = code-generated cue mesh).
- Prior state: lock RELEASED; Unity verified NOT running.
- Intended task: ADD an Editor-only tool `Assets/Editor/147VR_ProceduralCue.cs` (MenuItem `147VR/Art/Cue/Generate Procedural Cue (mesh)`) that builds a surface-of-revolution cue mesh and saves it to `Assets/AAA/ProceduralCue/ProceduralCue.asset`. Original generated geometry (no third-party assets). No scene/physics edits.
- Scope: additive, editor tool only. OWNER clicks the menu in Unity to generate (agent does not open Unity).
- Release condition: agent records the added file and marks RELEASED; owner runs the menu + reviews.

### Release 2026-10-11 (procedural cue tool) — RELEASED
- Added file: `Assets/Editor/147VR_ProceduralCue.cs` (editor-only, MenuItem `147VR/Art/Cue/Generate Procedural Cue (mesh)`). Original generated geometry; no third-party assets; no scene/physics edits.
- Agent did NOT open Unity. Owner next: open Unity 6000.4.4f1 → menu `147VR/Art/Cue/Generate Procedural Cue (mesh)` → creates `Assets/AAA/ProceduralCue/ProceduralCue.asset` + a `ProceduralCue` GameObject (URP Lit). Review the cue, then proceed to the procedural-wood shader step.
- Lock now RELEASED.

## Takeover 2026-10-11 (procedural wood shader + cue material presets — additive)
- Holder: QwenWork agent (owner Mon requested procedural wood shader, several styles, applied to the cue).
- Prior state: lock RELEASED; Unity NOT running.
- Intended task: ADD `Assets/AAA/ProceduralCue/CueProceduralWood.shader` (URP HLSL, fBm wood grain + varnish/clearcoat + fresnel, section-colored by UV.y) + `Assets/Editor/147VR_CueMaterials.cs` (MenuItem to create several preset materials: Ash/Maple/Rosewood/Ebony/Bubinga). All original, additive; no scene/physics edits.
- Scope: additive assets/editor only; OWNER runs the menu in Unity to create/assign materials (agent does not open Unity).
- Release condition: agent records added files and marks RELEASED.

### Release 2026-10-11 (procedural wood shader + presets) — RELEASED
- Added files: `Assets/AAA/ProceduralCue/CueProceduralWood.shader` (URP HLSL procedural wood) + `Assets/Editor/147VR_CueMaterials.cs` (MenuItem `147VR/Art/Cue/Create Cue Material Presets`). All original; no scene/physics edits; Unity NOT opened by agent.
- Owner next: open Unity → (1) `147VR/Art/Cue/Generate Procedural Cue (mesh)`, (2) `147VR/Art/Cue/Create Cue Material Presets` → drag a preset (Ash/Maple/Rosewood/Ebony/Bubinga) onto the cue. If the shader shows a compile error, paste it to the agent to fix.
- Lock now RELEASED.

## Takeover+Release 2026-10-12 (Safe Mode emergency — pre-existing compile error) — RELEASED
- Owner opened Unity → SAFE MODE. Reported errors were in the PRE-EXISTING `Assets/Editor/147VR_LODAudio_Tools.cs` (missing `using UnityEngine.Audio;` → CS0246/CS0103 on `AudioMixer`), NOT the agent's cue/atmosphere scripts (those compiled clean).
- Fix (agent): added `using UnityEngine.Audio;` and removed a call to the non-existent `AudioMixer.CreateAsset(...)` (replaced with a manual-create guidance log; SFX import-settings logic unchanged).
- Owner: let Unity recompile and click "Exit Safe Mode". If any other errors remain, paste them to the agent.
- Lock RELEASED (emergency compile fix only; no scene/physics changes).

## Takeover+Release 2026-10-12 (cue skins + physics seed — additive / non-destructive) — RELEASED
- Added files (all additive; no live scene/physics/golden modified):
  - `Assets/Scripts/CueSkinSelector.cs` (runtime, VISUAL-only player cue-skin switcher).
  - `Assets/Editor/147VR_CuePrefabs.cs` (MenuItem builds one cue prefab per material preset from the generated mesh).
  - `Assets/Editor/147VR_CueMaterials.cs` (added 6th preset Cue_Padauk_Sunset → 6 styles total).
  - `Assets/AAA/Physics/PhysicMaterial_seed.json` (reference data: competitor PhysicMaterial/Rigidbody values + WPBSA + a candidate CushionProfile).
  - `Assets/Editor/147VR_PhysicsSeedProfile.cs` (MenuItem creates a SEPARATE candidate `CushionProfile_SEED.asset` from the seed — does NOT touch the live/certified table).
- Deliberately did NOT build a parallel GOLDEN pipeline — the project already has `PhysicsGoldenRegressionRunner` + `PhysicsGoldenEvaluator` (RegressionTolerancePercent). Seed is a candidate to A/B via the existing golden, per the single-physics-authority rule.
- Owner: after exiting Safe Mode, run the cue menus (mesh → materials → prefabs) and optionally the physics-seed menu, then validate any cushion change with the EXISTING golden runner.
- Lock RELEASED.

## Takeover+Release 2026-10-12 (cue selection UI + skip-CPU2IL decision) — RELEASED
- Added `Assets/Scripts/CueSkinSelectorUI.cs` (runtime: auto-builds one UI button per cue skin, wired to CueSkinSelector). Added `GetSkin(int)` accessor to `CueSkinSelector.cs`. Both additive, visual-only; no physics/scene changes.
- DECISION: NOT running Cpp2IL spin/throw dump — 147VR's existing GOLDEN already certifies spin (M2.2 Follow / M2.3 Draw / M2.4 English / M3 Cushion spinTransfer). Importing a competitor's opaque constants would be noise, not signal. Seed JSON left as passive reference (not applied).
- Lock RELEASED.

## Takeover+Release 2026-10-12 (table art polish — frame wood material + DARC light lift) — RELEASED
- Edited `Assets/AAA/ProceduralCue/CueProceduralWood.shader`: added `_UseSections` toggle (0=plain wood for frame/furniture, 1=cue sections).
- Added `Assets/Editor/147VR_TableArt_Tools.cs`: MenuItems `147VR/Art/Table/Apply Varnished Wood to TABLE FRAME` (creates `Materials/V008/M_TableFrame_Wood.mat`, _UseSections=0, mahogany) and `.../Move DARC Lights Above Table` (repositions DARC_Key/Fill/RimL/RimR above TABLE SURFACE).
- These are VISUAL scene edits the OWNER runs via menu (agent did not open Unity); no physics/golden changes. Verified v008 table import complete (~58 parts, all 6 pockets' throat/net/lining/brass present).
- Lock RELEASED.

## Takeover+Release 2026-10-12 (club dressing — CC0 props + additive layout tool) — RELEASED
- Copied 8 CC0 Poly Haven props (FBX+textures) into `Assets/AAA/ClubProps/` (Chandelier_01, Sofa_01, ArmChair_01, CoffeeTable_01, bar_chair_round_01, Shelf_01, calathea_orbifolia_01, bronze_ray_statue). CC0 = ship-safe.
- Added `Assets/Editor/147VR_ClubDressing.cs` (MenuItem `147VR/Art/Club/Place CC0 Props Around Table (additive)`) — instantiates props under a "ClubDressing" parent around the table in a multiplayer layout. Additive/non-destructive (delete ClubDressing to undo). No physics/scene-structure changes.
- Owner: open Unity (imports the FBX), run the menu, review placement, iterate. Full furnished room + Quest LOD/baked-light is a multi-session effort; concept renders are aspirational, not pixel-guaranteed.
- Lock RELEASED.

## Takeover+Release 2026-10-12 (clean club scene tool — non-destructive) — RELEASED
- Added `Assets/Editor/147VR_ClubSceneSetup.cs` (MenuItem `147VR/Art/Club/Create Clean Club Scene`): duplicates the current scene to `Assets/Scenes/147VR_ClubRoom.unity` and strips venue roots (147VR_VENUE*, Crowd_BB*, Ceil_BB*, ClubDressing), keeping table+physics+XR rig+lights. Original scene file untouched.
- Also fixed a compile collision in `147VR_ClubDressing.cs` (renamed method `Place()`→`PlaceProps()` to avoid CS0102 with the `Place` struct).
- Lock RELEASED.

## Takeover+Release 2026-10-12 (club lighting setup tool) — RELEASED
- Added `Assets/Editor/147VR_ClubLighting.cs` (MenuItem `147VR/Art/Club/Setup Club Lighting`): assigns warm_bar HDRI as `Skybox/Panoramic` skybox + `DynamicGI.UpdateEnvironment()`, adds a `Club_TableLight` spotlight above TABLE SURFACE, creates `Assets/Settings/ClubVolumeProfile.asset` (ACES + Bloom + ColorAdjustments + Vignette, persisted via AddObjectToAsset) bound to a global Volume, and brightens `Table_Mahogany` material assets (`_BaseColor`/`_Smoothness`).
- Additive/editor-only; owner runs the menu; no physics/golden changes. Original scenes untouched.
- Lock RELEASED.

## Takeover+Release 2026-10-13 (club room v1 shell + Vintage props into Assets) — RELEASED
- Copied Miracle Pool "vintage" environment meshes+textures (27 OBJ + 13 PNG, from UnityPy bundle export) into `Assets/AAA/ClubProps/MiraclePool_vintage/`. (Note: OBJ+loose textures — materials not auto-linked; proper glTF export via AssetRipper is the follow-up for full materials.)
- Added `Assets/Editor/147VR_ClubRoomBuild.cs` (MenuItem `147VR/Art/Club/Build Club Room Shell (v1)`): clears leftover venue/ClubDressing roots (KEEPS table visual + physics + XR rig + lights), builds a floor + 4 walls + a low center dais around the table with walnut/parquet colors.
- Additive/editor-only; owner runs the menu; no physics/golden changes. Table+physics left in place (not delete/re-add — avoids breaking certified physics alignment).
- Lock RELEASED.

## Takeover+Release 2026-10-13 (club room consolidated to one-click) — RELEASED
- Rewrote `Assets/Editor/147VR_ClubRoomBuild.cs` into a single menu `147VR/Art/Club/★ Build Full Club Room (one-click)`: idempotently clears prior generated + venue roots (keeps table+physics+rig), turns OFF HDRI background (skybox=null + flat warm ambient), builds floor/walls/CEILING/dais, adds chandelier + warm point light, and places CC0 props. Owner runs ONE menu instead of several. Additive; no physics changes.
- Lock RELEASED.

## Takeover+Release 2026-10-13 (WPBSA table prefab from Unlimited Snooker) — RELEASED
- Created cleaned texture `0108_CushionBorderLogo_CLEAN.png` (solid wood, "snooker UNLIMITED" logo removed).
- Copied Unlimited Snooker meshes+textures into `Assets/AAA/TableSkins/US/{meshes,textures}` (Unity imports OBJ on refresh).
- Added `Assets/Editor/147VR_USPrefabTool.cs` (MenuItem `147VR/Art/Cue/Build WPBSA Table Prefab (from Unlimited Snooker)`): assembles table meshes excluding Slate/TableBase + UI, scales ball to 52.5mm (WPBSA), plain green-cloth/wood/logo-free-border materials, saves `Assets/AAA/TableSkins/US_Table_WPBSA.prefab` (reusable skin). Additive/editor-only; no physics/golden changes.
- Lock RELEASED.

## Takeover+Release 2026-10-13 (vintage room import tool) — RELEASED
- Copied Miracle Pool vintage room meshes+textures (26 OBJ + 13 PNG) into `Assets/AAA/ClubVenue/MiraclePool_vintage/`.
- Added `Assets/Editor/147VR_VintageRoomImport.cs` (MenuItem `147VR/Art/Club/Import Vintage Room (Miracle Pool, scaled)`): assembles the room from OBJ meshes, heuristic texture→material linking by name, scales longest side to ~13 m, drops to floor, saves `VintageRoom_Scaled.prefab`. Heuristic materials + uniform scale are a first pass; positions mostly baked in meshes (layout.json was empty). Additive/editor-only; no physics changes.
- Lock RELEASED.

## Takeover+Release 2026-10-13 (city-night HDRI + bar + bigger room + club placement tool) — RELEASED
- Downloaded CC0 `cobblestone_street_night` (2k) → `Assets/AAA/ImportedSnooker/Venue/Skybox/Atmospheres/Atmos_CityNight_2k.hdr` (replaces pub HDRI seen through windows).
- Copied Bowlliards2 bar meshes+textures → `Assets/AAA/ClubVenue/Bowlliards2_bar/`.
- Enlarged `147VR_VintageRoomImport` target to 18 m.
- Added `Assets/Editor/147VR_ClubPlace.cs` (MenuItem `147VR/Art/Club/Place Tables + Bar + CityNight Skybox`): sets city-night skybox, places VintageRoom + US_Table_WPBSA (rotated 90°) + 2 pool table prefabs + bar group. Additive/editor-only; positions are starting points to nudge.
- Lock RELEASED.


## Takeover 2026-11-05 (owner-approved scene de-duplication) — ACTIVE
- Holder: LUNA (same executor named by the current lock); owner explicitly approved moving obsolete 147VR Unity scenes to the Recycle Bin.
- Preflight: no Unity.exe, UnityPackageManager.exe, Blender process, or Blender MCP listener on TCP 9876 was found. The Unity checkout is on `integration/008-m53` at `f6a746d5`; working tree contains extensive pre-existing modifications/untracked work and MUST NOT be reset, staged, or committed as part of this cleanup.
- Current audit evidence (2026-11-05 `S2_IMPORT_AUDIT_20261105.md`): `147VR_MainScene.unity` remains the canonical Build Settings scene with V008/physics references; preserve it. `147VR_MainScene_V008_TEST.unity` is modified in the working tree; `147VR_ClubRoom.unity` is untracked and references retained table assets. Owner's prior approval is applied only to reversible Recycle Bin moves; no permanent deletion.
- Planned scope: move only `Assets/Scenes/147VR_MainScene_V008_TEST.unity` + `.meta` and `Assets/Scenes/147VR_ClubRoom.unity` + `.meta` to Recycle Bin; keep `147VR_MainScene.unity`, `PoolTable_8Ball.unity`, and `PoolTable_9Ball.unity`. Do not alter Build Settings. Preserve all .bak, Blender asset/source files, and the user's authoritative Blender room file.
- Release condition: record exact moved files and verify canonical main scene/build entries remain intact; update project state handoff and mark RELEASED.

### Release 2026-11-05 â€” owner-approved scene de-duplication
- Moved to Recycle Bin (recoverable): Assets/Scenes/147VR_MainScene_V008_TEST.unity + .meta; Assets/Scenes/147VR_ClubRoom.unity + .meta.
- Preserved canonical Assets/Scenes/147VR_MainScene.unity because the current S2 audit confirms active V008/physics references and Build Settings index 0. Preserved PoolTable_8Ball.unity and PoolTable_9Ball.unity; no Build Settings change.
- No Unity/Blender GUI was opened; no compile/PlayMode test; no Git staging/reset/commit. Existing dirty working tree preserved.
- Blender main room file and .blend1 backup preserved; Blender MCP port 9876 had no listener.
- Lock RELEASED. Next task: resume S2 integration QA from S2_IMPORT_AUDIT_20261105.md under a new recorded lock.

### Supplemental 2026-11-05 — Blender legacy-scene cleanup (file-level, reversible)
- Recycle Bin verified 14 old 147VR/Crucible full-room/blockout .blend files; authoritative room .blend, .blend1 backup, and extracted table asset preserved.
- No Blender GUI/MCP session or scene-content edit. Unity lock remains RELEASED.

### Release 2026-11-05 - Unity Integration QA
- Unity 6000.4.4f1 batch compile completed with UPM IPC connected and zero C# compiler errors. Existing non-blocking warnings were recorded in INTEGRATION_QA_20261105.md.
- Canonical MainScene V008 references were rewired to 147VR_Table_V008_OPT_VISUAL; inactive legacy prefab and unused disabled Bed_Collider marker removed after reference checks. Pre-edit scene backup is in Docs/AI_TEAM/UnityLogs/Backups.
- Targeted PlayMode test V008_MainScene_SerializedWiring_IsValid passed 1/1. Runtime systems remain disabled under Quest Setup and active ball bodies are zero; full shot/rail/pocket regression is blocked pending activation and ball-spawn work.
- Temporary QA editor scripts removed. Durable PlayMode test and QA evidence retained. Unity and UnityPackageManager are no longer running.
- Unity_Safe_Batch_Launch.ps1 restored exactly from HEAD. No Git reset, checkout, stash, stage, or commit. Lock RELEASED.