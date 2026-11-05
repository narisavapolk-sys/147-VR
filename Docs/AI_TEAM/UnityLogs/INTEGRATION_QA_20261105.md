# 147VR Unity Integration QA â€” 2026-11-05

## Build / compile
- Unity 6000.4.4f1; canonical safe batch launcher; UPM IPC connected.
- Editor/runtime scripts compiled with zero C# compiler errors (error CS count = 0).
- Non-blocking warnings remain: duplicate System.Runtime.CompilerServices.Unsafe.dll resolution; Meta XR partial-class filename warnings; obsolete FindObjectsByType(..., FindObjectsSortMode) calls in Apply147VRLightingPass.cs.
- No Editor Utility source changes were needed because no compile errors reproduced.

## Canonical MainScene / V008 wiring
- Main scene: Assets/Scenes/147VR_MainScene.unity.
- Backup before scene edit: Docs/AI_TEAM/UnityLogs/Backups/147VR_MainScene.pre_V008_rewire_20261105.unity.bak (SHA-256 matched source before modification).
- Rewired PlayerViewManager.tableRoot, SnookerBallTracker.tableRoot, and SnookerPhysicsSetup.tableRoot to 147VR_Table_V008_OPT_VISUAL.
- Removed the inactive legacy Prefab_WPBSA_12Foot_Snooker instance after external-reference scan reached zero.
- Removed the separate orphan Bed_Collider marker because its only collider was disabled, it had no renderer, and no external serialized references.
- Kept the visual table and all other scene roots. Did not add a second physics runner or alter Build Settings.

## PlayMode regression
- Test: V008MainScenePlayModeTests.V008_MainScene_SerializedWiring_IsValid.
- Result: Passed 1, Failed 0, Duration approximately 0.88 seconds.
- Verified active V008 table root, TABLE SURFACE renderer/material FELT, 5 pocket pads, all three serialized tableRoot references, no legacy prefab, no orphan Bed_Collider, and no PoolPhysicsRunner / PoolPhysicsEventRunner components.
- Important runtime blocker: all three gameplay components are hosted on inactive Quest Setup (coreSystemsActive=False); there are zero active ball-like Rigidbody objects. The PlayMode smoke test is therefore not a shot/rail/pocket physics regression. Full gameplay physics remains blocked until the intended runtime activation flow and ball-rack/spawn set are restored and validated.
- Official Unity Test Framework result XML copied to V008_PlayMode_Regression_20261105.xml.

## Git / safety
- Branch integration/008-m53, HEAD f6a746d5, one local commit ahead of origin. The ahead commit is pre-existing PoolPhysics import work.
- Working tree had extensive pre-existing V008 art, package, scene, Blender-source, and editor-tool changes. Only the MainScene wiring cleanup, the owner-approved V008_TEST scene recycle move, QA test file, and QA documentation/log artifacts belong to this QA pass.
- No reset, checkout, stash, stage, or commit. Unity_Safe_Batch_Launch.ps1 restored exactly from HEAD after QA.
- EditorBuildSettings.asset was not edited during this pass. Existing dirty status was preserved.