# 147 VR — S2 selective integration and asset audit

Date: 2026-11-05
Canonical checkout: `C:\Users\mongo\UnityProjects\147 VR`
Canonical branch at start: `integration/008-m53` (`bdfd16c3`)
Source checkout: `C:\Users\mongo\UnityProjects\147VR-import-wip`
Source branch at start: `feat/unity-event-physics-import` (`961945c`)

## Imported into canonical

- `Assets/PoolPhysics/Sim`: deterministic event simulation core, 9-ball rules evaluator, save model/codec, undo stack/policy, and supporting math/physics types.
- `Assets/PoolPhysics/Unity`: Unity runtime adapters, including `PoolPhysicsEventRunner`, `TableSpec`, and `CueVRPhysicsProfile`.
- `Assets/PoolPhysics/PoolPhysics.asmdef` with source `.meta` files preserved from WIP.
- `PoolPhysics/Tests`: headless .NET test harness and golden fixtures.
- `Docs/AI_TEAM/BHP_CONTROL_MAP_WIP_REFERENCE.md`: retained as a proposal with an explicit correction for canonical scene status.

The module is imported but deliberately dormant: no `PoolPhysicsEventRunner` was attached to a scene, no WIP Bay1–Bay4 TableSpec assets were copied, and no second physics authority was enabled.

## Deliberately not imported

- `PoolPhysicsRulesBridge.cs` and `PoolPhysicsAuthorityDirector.cs`: WIP bridge assumes APIs absent from canonical (`M5ShotLifecycle.BindRunner`, `SnookerShotTracker.snookerRulesEnabled`) and targets older score/turn/cue controller versions. Copying these files as-is would either fail compile or bypass the canonical M5 transaction model.
- `BhpCueGripController.cs`: event-only scaffold; not wired to canonical cue/input state.
- WIP `PoolPhysics/Editor` auto-wire and fallback `TableSpecGenerator`: AutoWire scans scene-wide rigidbodies rather than a scoped ball set; fallback table geometry does not author real cushion/jaw geometry. Not safe for the production table.
- WIP PlayMode scene/phase tests: they exercise the WIP scene architecture, not the canonical main scene.
- WIP Bay1–Bay4 scenes and TableSpec assets: test fixtures, not the V008 production table.

The canonical `SnookerBallTracker.cs` was not overwritten. In particular, its `activeInHierarchy` filter remains intact; the WIP revision removed that guard and could have admitted inactive legacy visual ball sets into M5 state.

## Validation performed in canonical checkout

The imported headless test project built successfully with 0 warnings and 0 errors. The following suites all returned exit code 0:

- Nine-ball rules: 13 checks plus 150 fuzz iterations.
- Save model/codec: 40 checks plus 300 fuzz iterations.
- Undo stack/policy: U1–U11 checks plus 500 fuzz iterations.
- Simulation validation, event validation, touch/controller math, properties (400 iterations), and jar tests.
- Strict golden checks: `golden_regression_baseline.json` and `golden_bay4.json`, both with zero reported delta.

Prior WIP evidence (`PoolPhysicsPhaseTests` 5/5 and restore E2E 22/22) remains WIP-only evidence. No Unity Editor compile or PlayMode run was performed in canonical; no Unity/Blender GUI was launched.

## Table and asset cleanup

Removed from canonical project:

- `_Archive/Old_Tables` legacy table exports/prefabs with no project references.
- V006 DARC table FBX and its non-build `147VR_MainScene_ART_V006` scene.
- V008 CLEAN test FBX/texture and non-build `147VR_MainScene_V008_CLEAN_TEST` scene.
- Unreferenced V008_OPT LOD1/LOD2 FBXs.
- Unreferenced `AAA/TableSkins` US-table package and its one-off builder/placement tools.
- V006/US/V008-CLEAN one-off editor tools. The generic world-bounds measurement menu item was retained in `147VR_ArtStage_Tools.cs`.

Retained because they are still referenced by actual game-mode or QA scenes:

- `PoolTable_8Ball` and `PoolTable_9Ball` build scenes and their table/ball-rack dependencies.
- `V008_OPT_GAME.fbx` and its materials: the active `147VR_Table_V008_OPT_VISUAL` in MainScene uses its mesh sub-assets.
- `V008_MARKING_OPT.fbx`: retained because it is referenced by a child of the inactive legacy prefab in MainScene and by `147VR_MainScene_V008_TEST` / `147VR_ClubRoom`; it is not the active table root.
- `Prefab_WPBSA_12Foot_Snooker` and its V007 dependencies, because the 9-ball/calibration/ClubRoom scenes still reference them.
- `Assets/Prefabs/PoolTable` and `AAA/PhysicsCalibration`: still referenced by game-mode/calibration scenes. MainScene also references `TableSurfaceProfile_RuntimeBaseline.asset` and the `.m5_certified` baseline, so this calibration data is a real dependency, not an orphan support table.

The main scene file was not edited automatically. It still contains an inactive legacy prefab instance referenced by the three `tableRoot` fields. An undoable Editor menu item was added: `147VR > Scene Cleanup > Validate Main Table Wiring` and `147VR > Scene Cleanup > Rewire V008 + Remove Inactive Legacy Table`. The latter rewires the known fields and removes the inactive instance/disabled orphan Bed_Collider only when no serialized references remain; it marks the scene dirty but never auto-saves. The main scene audit also found no active ball-sized Rigidbody/SphereCollider set, so ball-rack/spawn readiness remains a separate blocker.

## Next steps

1. Open `Assets/Scenes/147VR_MainScene.unity`, run the Validate menu item, then run the undoable Rewire menu item, inspect the Hierarchy/Console, and save only if correct.
2. Restore or identify the actual snooker ball-rack/spawn path in the canonical main scene; the current scene has no active ball-sized Rigidbody set.
3. Author a production `TableSpec` from the active V008 dimensions, pocket centers, and real cushion/jaw geometry. Do not use the WIP approximate rectangle fallback.
4. Adapt the event runner/rules bridge to canonical M5 lifecycle and transaction APIs, or retain the existing PhysX authority; never run two physics authorities on the same balls.
5. Wire `NineBallRules`, save/load, and pool undo to the actual 8/9-ball game-mode scenes; validate each separately.
6. Integrate BHP grip/aim/undo gestures only after confirming the actual XR controller layout and current cue-controller state machine.
7. Run Unity Editor compile and canonical PlayMode regression after scene wiring.

## Skills applied

- Qwen Work `senior-lazy-coder`: reuse existing work, minimal scoped changes, no broad rewrite, no blind scene automation.
- `unity-developer`, `game-developer`, `snooker-vr-pro`, `3d-spatial` for Unity/gameplay/physics architecture review.
- `headless-csharp-sim-qa` for the imported deterministic core and regression harness.
- `asset-audit` for GUID/reference-based table cleanup.
- `adversarial-reviewer` for compatibility and dual-authority checks.
- `blender-aaa` / `blender-pipeline` were kept available but not invoked because this pass did not edit Blender source assets.

## Phase 2 completion — merge-by-content of the full WIP branch (2026-11-05, OpenCode session)

The unrelated-histories constraint (empty merge-base) was confirmed, so integration was done by
content, mirroring this audit's Phase 1 approach (no `git merge`). Staged from
`feat/unity-event-physics-import` @ `41a0dcb3` on `integration/008-m53`:

- `Assets/Scripts` WIP side: `Authority/{PoolPhysicsRulesBridge,PoolPhysicsAuthorityDirector}`,
  `Quest/BhpCueGripController`, AAA/Quality `QuestRenderBootstrap`, and refreshed
  Quest/AAA-Physics lifecycle sources (M5ShotLifecycle, SnookerCueController/PhysicsSetup/
  ScoreManager/ShotTracker/TurnManager, TableSurface*, BallMotionEvaluator, M5RealStraightBatchRunner).
- `SnookerBallTracker.cs` was manually combined: WIP body (BindToSimulation / ReorderToSimulation /
  ReportPot / BallAt / IndexOf / PointsForSimName) + canonical `activeInHierarchy` guard restored
  in `Refresh()` (WIP had dropped the guard; the audit rule "keep canonical guard" was applied).
- `Assets/Editor` WIP tools: `ToolRunner` (Logs/editor_cmd.txt mechanism), `FbxAutoImportRules`,
  `QuestQualitySetup`, `Lighting/147VRBakeGI`, `Lighting/147VRLightRigAB` (+ pre-existing
  GUID metas kept where canonical already tracked them), refreshed `Apply147VRLightingPass`,
  `147VR_M5YoloHarness`, `PhysicsGoldenCertificationAutomation.broken.txt`.
- `Assets/Prefabs/Lighting` LightRig A/B prefabs, `AGENTS.md`/`CLAUDE.md` parity refresh,
  `PoolPhysics/README.md` + `INTEGRATION_UNITY.md`, `PoolPhysics/Tests/PoolSim.Tests.csproj`
  (canonical `Tests/.gitignore` retained for bin/obj), and WIP-only evidence
  (`WireReports/*`, `Docs/AI_TEAM/UnityLogs` Round11/12 + S2_2c_UNDO runs).

Still excluded, per this audit's original exclusions (unchanged): `Assets/PoolPhysics/Editor`
(WIP one-shot tools + AutoWire + TableSpecGenerator), `Assets/PoolPhysics/Data` (bay TableSpec
fixtures), `Assets/Tests/PlayMode/PoolPhysicsPhaseTests.cs` (WIP scene-architecture tests),
all bay materials/content (`Assets/AAA/Imports/naris_vintage/Rebound/*Bay*`), the WIP
`Assets/Scenes/SampleScene.unity`, WIP `Packages/` + `ProjectSettings/` + `Assets/Settings/`
deltas, and WIP `Assets/Resources/DevAgentSettings.asset` modifications (canonical is authoritative
for scenes/docs/settings). Scene wiring of the imported runtime is deferred to owner-click Editor
menu items per project rules.
