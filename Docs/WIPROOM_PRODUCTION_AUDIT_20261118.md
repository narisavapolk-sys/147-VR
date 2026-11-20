# WipRoom Production Scene — Integration Takeover Audit (Historical Snapshot: 2026-11-18)

> This document preserves the 2026-11-18 audit as a historical snapshot. Its statements about the then-current working tree, commits, and deleted scene files are superseded by the 2026-11-20 addendum at the end.

Date: 2026-11-18
Project: `C:\Users\mongo\UnityProjects\147 VR`
Branch: `integration/008-m53`

## Ownership / safety
- Integration takeover of the existing AI/agent working tree; no reset, clean, checkout, stash, commit, or push was performed.
- Existing tracked and untracked work was preserved. No unrelated files were deleted.
- No Unity scene was saved or modified during this takeover.
- No Unity Editor process was running at the start. Two read-only batch audit attempts were started by this session and then stopped when FBX/AssetDatabase initialization stalled; only those exact batch processes and their children were terminated.
- The WipRoom scene and existing art preview scenes were not deleted or overwritten.
- No Git commit was created. The working tree remains intentionally dirty.

## Current architecture facts (verified from current files)
- Canonical production candidate: `Assets/Scenes/147VR_MainScene_WipRoom.unity`.
- `ProjectSettings/EditorBuildSettings.asset` currently enables WipRoom only.
- `Assets/Editor/Build147VR.cs` and `Assets/Editor/AAA/Build147VRQuestCandidate.cs` currently target WipRoom.
- PlayMode tests `PoolPhysicsPhaseTests`, `SoloPracticeUndoAndCueGripTests`, `V007PlayModeValidationTests`, and `V008MainScenePlayModeTests` currently load `147VR_MainScene_WipRoom`.
- V008 asserts the one-scene Build Settings contract, four physics runners, per-bay variants, one active authority owner, and a bound rules bridge.
- The old `Assets/Scenes/147VR_MainScene.unity` is deleted in the working tree. The legacy 8-Ball and 9-Ball scene assets still exist and remain until their dependencies are fully migrated.

## WipRoom serialized Physics Runner configuration
| Bay | Variant | Serialized ball transforms | TableSpec |
|---|---|---:|---|
| Bay1 | Snooker | 22 | TableSpec_Bay1 |
| Bay2 | Snooker | 17 | TableSpec_Bay2 |
| Bay3 | EightBall | 16 | TableSpec_Bay3 |
| Bay4 | NineBall | 10 | TableSpec_Bay4 |

- The counts are serialized transform references, not proof of correct ball identity.
- Bay2's 17-ball Snooker configuration may be a 10-red short-frame rack (10 reds + 6 colours + cue), but its actual imported FBX child names and cue-first ordering have not yet been verified. Do not change its variant or rack until this is checked.
- All four runners use the shared physics profile and their own TableSpec assets.
- Runtime authority/bridge objects are created by the `PoolPhysicsAuthorityDirector` bootstrap rather than serialized into the scene.
- The scene YAML had no literal null `m_Script: {fileID: 0}` references in the earlier read-only scan.

## Integrated changes retained from prior agents
- Explicit `GameVariant` values: Snooker / EightBall / NineBall.
- `PoolPhysicsEventRunner` serializes the variant per bay.
- `PoolSaveModel` validates EightBall as cue + object balls 1..15 (maximum 16 total), NineBall as cue + 1..9, and Snooker up to 22.
- `PoolUndoPolicy` keeps Match vs SoloPractice separate from GameVariant.
- Build Settings and the main Quest candidate build utility target WipRoom only.
- V007/V008 PlayMode tests target WipRoom and verify the single-scene / four-bay architecture.
- Existing art follow-up tools and `WipRoom_VenueArtFix_v001..v003` preview scenes are preserved as untracked work; they are not enabled in Build Settings.

## Integration fixes made in this takeover
1. `PoolSaveTests`: added an EightBall round-trip for cue + 15 object balls and rejection of object number 16; changed the invalid ruleset case to `UnknownVariant`.
2. `GoldenRunner.EventVal`: corrected the momentum test oracle to use the actual mass of each simulated ball and the initial momentum. No physics constants or simulation behavior were changed.
3. `PoolPhysicsRulesBridge`: corrected the player mapping between `SnookerTurnManager` IDs (1/2) and `PoolSaveState.CurrentPlayer` indices (0/1).
4. `PoolPhysicsRulesBridge.ApplyUndoState`: now restores the saved player turn as well as ball positions and pocketed state.
5. `SoloPracticeUndoAndCueGripTests`: added a player-2 save-index assertion and a turn-restoration assertion after Undo; corrected the JSON field assertion to the canonical `Balls` / `CurrentPlayer` field names.
6. Added `Assets/Editor/AAA/WipRoomProductionAudit.cs`, a read-only menu audit that requires WipRoom to already be open. It does not open, dirty, or save scenes. Menu: `Tools/147/Production/Audit Loaded WipRoom (Read Only)`.

## Headless QA evidence
The pure C# test project builds with 0 warnings / 0 errors. The following groups passed after the integration fixes:
- Save / Load: 300 fuzz cases, including EightBall 16-ball round-trip.
- Undo policy: 200 fuzz cases.
- NineBall rules: 150 fuzz cases.
- Motion validation.
- Event physics validation (including momentum conservation).
- Touching / contact tests.
- Physics property tests: 400 fuzz cases.
- Golden regression: strict score `0.000E+000`.
- Total: 8/8 headless groups passed.

`git diff --check` passes for the source and test files changed in this takeover. A whole-tree diff check still reports trailing whitespace in generated WipRoom scene YAML empty fields; those Unity-serialized lines were intentionally not mass-edited.

## Production blockers still open
1. **Pool rules are not production-complete.** `PoolPhysicsRulesBridge.ApplyPoolTurnRule` still uses generic pot-count continuation for Pool. The pure `NineBallRules` engine is not yet wired into runtime event evaluation, and there is no complete EightBall group / legal-8 / foul / ball-in-hand runtime rules flow.
2. **Undo/save state is still partial.** `RacksWon` is placeholder data and `BallInHand` is still hard-coded false. The player turn mapping and restore bug were fixed, but the full match-state transaction is not implemented.
3. **Cue-first identity and rack identity are unverified.** WipRoom ball transforms are stripped children of a large imported FBX. The exact cue transform at index 0 and Bay2's red/colour composition must be checked from the loaded scene.
4. **PlayMode tests have not been executed in Unity** after the latest code edits. The latest Unity compile log from the prior agent is not evidence for these new edits.
5. **XR / multiplayer / voice acceptance is not proven.** The scene and scripts contain XR-related setup, but no Quest device test, Mirror authority/reconnect test, or Dissonance voice test was run in this takeover.
6. **Quest build acceptance is not proven.** The build utility is pointed at WipRoom, but no final Android/Quest build or device run was performed.
7. Several legacy Editor utilities still contain old MainScene path assumptions. They are not in the primary Build path, but should be reviewed and retired or migrated after their use is understood.

## Safe next sequence
1. Open WipRoom in the Unity Editor and run `Tools/147/Production/Audit Loaded WipRoom (Read Only)`; verify cue-first ordering, every pool ball number, Bay2 rack composition, missing scripts, XR, Mirror, and Dissonance components.
2. Compile in the Unity Editor and run the WipRoom PlayMode suites, especially `V008MainScenePlayModeTests` and `SoloPracticeUndoAndCueGripTests`.
3. Implement a variant-specific runtime rules layer: integrate `NineBallRules`; add EightBall group assignment, legal first contact, 8-ball win/loss, fouls, ball-in-hand placement, and match state.
4. Replace placeholder Save/Undo fields with a complete match-state snapshot and restore contract.
5. Run XR controller / comfort / physics tests, multiplayer authority and reconnect tests, and Dissonance voice tests.
6. Run Quest build preflight, build Android/ARM64, and verify on device.
7. Only after all runtime references and acceptance tests pass, archive/remove legacy scene assets and obsolete editor utilities.

## Acceptance rule
WipRoom is the sole enabled production scene, but it is **not yet certified Production Ready**. Do not delete legacy scene assets or mark the project complete until variant rules, cue/rack identity, PlayMode, XR, multiplayer, voice, persistence, and Quest runtime have evidence.


## Latest Unity compile attempt
- Unity 6.4.4f1 started in batch mode after the integration edits.
- Bee/ILPP processed `Assembly-CSharp.dll` and `Assembly-CSharp-Editor.dll`; no `error CS####` or `Scripts have compiler errors` line was found in the log.
- The batch process did not return a clean final exit code and was stopped by this session after the assembly post-processing stage. Treat this as **compile evidence without a clean process-exit confirmation**, not as a fully certified Unity compile.
- The audit menu script was not executed, and no scene was opened, saved, or modified.


## Rollback checkpoint and asset budget note
- Pre-variant rollback checkpoint remains present at `C:\Users\mongo\UnityProjects\_147VR_PreVariantArchitecture_20261118_064757`.
- It contains `147VR_MainScene_from_HEAD.unity` (the original MainScene snapshot) and `147VR_PreVariantArchitecture_20261118_064757_WorkingTree.patch`.
- Main venue FBX `Assets/AAA/Imports/naris_vintage/naris_vintage.fbx` is 96.67 MB. Bay2 clean table-frame FBX is 1.00 MB.
- No art bible or asset-size budget document was found in the checked Docs paths; asset-budget compliance is therefore **NOT ASSESSED**, not passed.

## Adversarial review of takeover changes (scope: files changed in this takeover)
- **Saboteur — WARNING:** Undo now restores the player turn, but the overall save snapshot still has placeholder `RacksWon` and hard-coded `BallInHand=false`. It is not yet a complete match-state transaction.
- **Saboteur — WARNING:** Pool runtime turn handling remains generic pot-count logic. It is not safe to certify 8-Ball or 9-Ball match rules until variant-specific foul, first-contact, legal-8, and ball-in-hand behavior is wired.
- **New Hire — NOTE:** Player ID conversion is currently centralized in two small helpers, but still uses numeric 1/2 and 0/1. A typed PlayerSlot contract should replace the magic values when the full match-state service is introduced.
- **Security Auditor — NOTE:** The Editor audit report uses a project-relative Logs path and assumes Unity was launched with the project as its working directory. No network-facing surface or secret handling was introduced by these changes.
- Verdict: **CONCERNS** — no critical issue found in the takeover patch, but the two gameplay-state warnings prevent a Production Ready claim.


## ToolRunner safety hardening
- Found a pending `bay2-frame` command in `Logs/editor_cmd.txt`. It replaces the Bay2 frame and saves the production WipRoom scene.
- The prior `ToolRunner` used `[InitializeOnLoad]` and `delayCall` to auto-execute queued commands after a domain reload. This was unsafe for a production scene.
- Removed the auto-run path. Queued commands now require the explicit menu `147VR/Tools/Run Queued Editor Commands (Confirm)`, which displays the exact queue and warns that commands may modify/save scenes and assets. Switching to WipRoom from another dirty scene also requires an explicit save confirmation.
- The existing `bay2-frame` queue was preserved and **not executed**. No scene or material was changed by this safety patch.
- Reviewed the new Bay2/venue tools: `InspectBay2Live` and Venue Diagnose are read-only; Bay2 placement and ceiling parity can save production; Venue Art Apply writes a preview scene and a shared material asset; LightRig/ReflectionProbe menus mark the active scene dirty but do not save. None of these actions was invoked during this takeover.


## Addendum — 2026-11-20 integration handoff

### Git / scene preservation
- Branch: `integration/008-m53`; remote: `origin`.
- All integration commits below were pushed successfully. The branch was verified at 0 ahead / 0 behind after the last push.
- The original `Assets/Scenes/147VR_MainScene.unity` and its .meta were restored from HEAD and preserved. The preview scene `147VR_MainScene_WipRoom_ArtPassPreview_v001` was also restored and preserved.
- WipRoom is the only enabled Build Settings scene. The legacy MainScene and 8-Ball / 9-Ball scene assets remain in the project; they were not deleted.
- The earlier 106 obsolete one-shot Editor utilities were removed after checking references in Assets/Tools. Three still-referenced Phase4A utilities were retained.
- A full WorkingTree snapshot and WipRoom backup remain under `%LOCALAPPDATA%\147VR_GitBackups`.

### Integration commits pushed
- `977a6579` — promote WipRoom scene and add Bay2 clean mahogany frame asset.
- `cda9bf6f`, `57611c71` — Physics variant/save/undo policy and Unity metadata.
- `c45ed07e` — remove obsolete one-shot Editor tools.
- `c0f8c2fc` — Quest gameplay flow and Physics Authority integration.
- `810e4e20` — WipRoom-only production Build Settings and ProBuilder safety setting.
- `6e5cbc9c` — WipRoom build/preflight, async lighting bake + UV2 guard, manual-only ToolRunner, guarded Bay2 apply entry point.
- `3975d762` — FBX secondary lightmap UV importer settings.
- `2669be71` — WipRoom production audit, Bay2 migration/audit, scene proof, art and lighting helper tools.
- `1dcd932b` — velvet, mahogany and neon material updates.
- `f3eb11b3` — cornice material and baked LightingSettings asset.

### Verification evidence
- Pure C# test runner passed: policy 12/12; NineBall fuzz 150; Save fuzz 300; Undo fuzz 200; Physics properties fuzz 400.
- Unity Editor log contained no `error CS####`, `Compilation failed`, or `Scripts have compiler errors` entries after the latest reloads. This is compile-log evidence only.
- Unity PlayMode tests have NOT been executed in the Editor. No Quest APK build or Quest device acceptance was performed.
- WipRoom prefab override audit: 5 connected prefab roots, 3,258 property overrides, 121 added components, 6 added GameObjects, 2 removed GameObjects, Missing Asset 0, Disconnected 0. The audit was read-only; no Apply/Revert was performed.
- A Scene View render captured on 2026-11-20 is at `Logs/renders/Bay2_SceneView_Render_20261119.png`. It shows the Bay2 mahogany frame and green cloth. The before/after Cube.018 test images have identical SHA-256 and therefore do not prove a visual difference.
- WipRoom serializes a non-null skybox material reference. The exact live RenderSettings state was not independently queried through the running Editor.

### Remaining production blockers
1. Bay2 still has the original 17-ball configuration in the scene. The new 22-ball migration tool has been added but has NOT been run or visually accepted.
2. Variant-specific Pool runtime rules are incomplete: full 8-Ball groups/legal-8/fouls/ball-in-hand and NineBall runtime integration are not certified.
3. Save/Undo still has placeholder match-state fields; a complete match-state transaction is not implemented.
4. Unity PlayMode acceptance remains outstanding, especially `V008MainScenePlayModeTests` and `SoloPracticeUndoAndCueGripTests`.
5. XR/Quest device, Mirror authority/reconnect, Dissonance voice, and Android/ARM64 build acceptance remain untested.
6. The orange TableSurface material edits were reverted; the original green material values were preserved because the orange change lacked visual approval.
7. The legacy `BatchSceneOps` HDRI-removal tool was not redirected to WipRoom and was not executed. The current WipRoom skybox reference was preserved.

### Acceptance status
WipRoom is the sole enabled production Build scene and the integration work is pushed, but the project is NOT certified Production Ready. Do not treat the Git handoff as proof of full gameplay, XR, multiplayer, voice, lighting-bake, or Quest acceptance.
