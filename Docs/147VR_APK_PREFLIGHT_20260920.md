# 147 VR — APK Preflight 2026-09-20

## Scope
Active project only:
C:\Users\mongo\UnityProjects\147 VR

Unity:
6000.4.4f1

## READY — verified

- AndroidPlayer module exists.
- Bundled Android SDK exists.
- Bundled Android NDK exists.
- Bundled OpenJDK exists.
- Bundled Gradle tooling exists.
- Android SDK build-tools directory exists.
- Bundled ADB reports 36.0.0-13206524.
- Main shipped scene candidate exists:
  Assets/Scenes/147VR_MainScene.unity
- Android Build Profile exists:
  Assets/Settings/Build Profiles/Android™.asset
- Android Build Profile is configured as APK (m_BuildAppBundle: 0).
- XR Android settings have automatic loading/running enabled.
- XR Android loader GUID matches Assets/XR/Loaders/OpenXRLoader.asset.
- Meta XR OpenXR Android feature is enabled in OpenXR Package Settings.
- Oculus Touch Controller Profile Android is enabled.
- MainScene contains:
  SnookerBallTracker
  SnookerScoreManager
  SnookerTurnManager
  SnookerShotTracker
  M5ShotEventContract
  M5ShotLifecycle
- MainScene lifecycle thresholds are:
  settledSpeedThreshold = 0.01
  settledDuration = 1.5
- Android scripting backend is IL2CPP.
- Android minimum SDK = 32.
- Android target SDK = 34.
- Android build automation candidate added:
  Assets/Editor/AAA/Build147VRQuestCandidate.cs
- Candidate build uses MainScene only and does not modify the global scene list.

## BLOCKED — must resolve before final/shippable APK

1. Android application identifier is still the Unity template placeholder:
   com.UnityTechnologies.com.unity.template.urpblank
   A final package identifier has not been supplied/approved, so no release identity was invented.

2. Global Editor Build Settings still contain four scenes:
   Assets/Scenes/147VR_MainScene.unity
   Assets/Scenes/SampleScene.unity
   Assets/Scenes/PoolTable_8Ball.unity
   Assets/Scenes/PoolTable_9Ball.unity

   The new APK candidate builder intentionally ignores the global list and builds MainScene only. The global list has not been destructively changed.

3. Android signing/keystore is not configured in ProjectSettings. This is acceptable for a local Development APK smoke build, but blocks a signed release APK.

4. Quest 2/3 hardware runtime has deliberately NOT been executed yet per current gate direction. No device PASS is claimed.

5. M4.2 current-profile runtime recapture has NOT completed yet because the active Unity Editor currently owns the project. Historical M4.2 Golden remains valid as historical runtime evidence only. The new current-profile runner exists and requires a later clean execution before current-profile M4.2 can be claimed.

## Candidate automation

Menu:
Tools/147/APK/Quest Candidate Preflight
Tools/147/APK/Build Quest Candidate (Dev)

Output:
Builds/Quest/147VR-MainScene-Dev.apk

The legacy Build147VR.QuestDevelopment() entry point is NOT used for the candidate APK because it targets:
PoolTable_8Ball
PoolTable_9Ball
SampleScene

## Current gate semantics

Physics:
- Cloth runtime-equivalent matrix certified.
- Straight regression 5/5 PASS.
- M3 fresh current-profile runtime 35/35 PASS and promoted.
- M4.2 historical 30/30 REAL + symmetry PASS, but NOT current-profile fresh PASS.

Gameplay:
- MainScene contains the M5 transaction/lifecycle chain.
- M5 rules engine includes nomination, multi-pot validation, foul transaction, ordered colours, and final-black recovery logic.
- Existing M6 integration test targets SampleScene and saves that scene; it is NOT treated as shipped-MainScene certification.

Next APK-stage order:
1. Resolve final/debug Android application ID.
2. Run candidate preflight inside the active Unity Editor.
3. Build MainScene-only Development APK.
4. Only then perform Quest 2/3 device install/runtime validation.
5. After hardware pass, prepare signed/release APK settings separately.
