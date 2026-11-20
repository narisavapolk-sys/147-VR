using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using PoolPhysics.Unity;
using PoolPhysics.Sim;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class V008MainScenePlayModeTests
{
    private const string SceneName = "147VR_MainScene_WipRoom";
    private static readonly string[] ExpectedBays =
    {
        "Bay1_PhysicsRoot",
        "Bay2_PhysicsRoot",
        "Bay3_PhysicsRoot",
        "Bay4_PhysicsRoot"
    };

    [UnityTest]
    public IEnumerator WipRoom_IsTheSingleProductionEntry_AndAllBaysAreWired()
    {
        Scene scene = SceneManager.GetSceneByName(SceneName);
        if (!scene.isLoaded)
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);

        yield return null;
        yield return new WaitForFixedUpdate();

        scene = SceneManager.GetActiveScene();
        Assert.That(scene.IsValid() && scene.isLoaded, Is.True, "WipRoom did not load.");
        Assert.That(scene.name, Is.EqualTo(SceneName));

        string[] productionScenes = Enumerable.Range(0, SceneManager.sceneCountInBuildSettings)
            .Select(SceneUtility.GetScenePathByBuildIndex)
            .Where(path => path.StartsWith("Assets/Scenes/", StringComparison.Ordinal))
            .ToArray();
        Assert.That(productionScenes, Has.Length.EqualTo(1),
            "Production Build Settings must contain exactly one enabled scene (excluding Test Runner scenes).");
        Assert.That(productionScenes[0], Is.EqualTo("Assets/Scenes/147VR_MainScene_WipRoom.unity"),
            "WipRoom must be the only production scene under Assets/Scenes.");

        PoolPhysicsEventRunner[] runners = UnityEngine.Object
            .FindObjectsByType<PoolPhysicsEventRunner>(FindObjectsSortMode.None)
            .Where(r => r != null && r.gameObject.scene == scene)
            .ToArray();

        Assert.That(runners.Length, Is.EqualTo(4), "WipRoom must contain exactly four physics bays.");
        foreach (string bayName in ExpectedBays)
        {
            PoolPhysicsEventRunner runner = runners.SingleOrDefault(
                r => string.Equals(r.BayName, bayName, StringComparison.Ordinal));
            Assert.That(runner, Is.Not.Null, bayName + " is missing.");
            Assert.That(runner.Spec, Is.Not.Null, bayName + " has no TableSpec.");
            Assert.That(runner.TableRootTransform, Is.Not.Null, bayName + " has no table transform.");
            Assert.That(runner.PlayfieldSize, Is.Not.EqualTo(Vector2.zero), bayName + " has an empty playfield.");
            Assert.That(runner.BallCount, Is.GreaterThanOrEqualTo(3), bayName + " has no usable ball set.");
            int cueIndex = runner.CueBallIndex;
Assert.That(cueIndex, Is.InRange(0, runner.BallCount - 1), bayName + " cue index must remain valid after canonical permutation.");
Transform cue = runner.BallTransformAt(cueIndex);
Assert.That(IsCueBallName(cue != null ? cue.name : null), Is.True, bayName + " CueBallIndex must resolve to the cue ball after canonical sorting.");
            Assert.That(runner.TableRootTransform.lossyScale.x, Is.EqualTo(1f).Within(0.01f),
                bayName + " physics table scale must be 1 metre-to-world.");
        }

        Assert.That(runners.Single(r => r.BayName == "Bay1_PhysicsRoot").Variant, Is.EqualTo(GameVariant.Snooker));
        Assert.That(runners.Single(r => r.BayName == "Bay2_PhysicsRoot").Variant, Is.EqualTo(GameVariant.Snooker));
        Assert.That(runners.Single(r => r.BayName == "Bay3_PhysicsRoot").Variant, Is.EqualTo(GameVariant.EightBall));
        Assert.That(runners.Single(r => r.BayName == "Bay4_PhysicsRoot").Variant, Is.EqualTo(GameVariant.NineBall));

        PoolPhysicsEventRunner bay2 = runners.Single(r => r.BayName == "Bay2_PhysicsRoot");
        Transform[] bay2Balls = Enumerable.Range(0, bay2.BallCount).Select(bay2.BallTransformAt).ToArray();
        Assert.That(bay2.BallCount, Is.EqualTo(22), "Bay2 must use a standard 22-ball Snooker rack.");
        Transform bay2Cue = bay2.BallTransformAt(bay2.CueBallIndex);
Assert.That(IsCueBallName(bay2Cue != null ? bay2Cue.name : null), Is.True, "Bay2 CueBallIndex must resolve to the cue ball after canonical sorting.");
        Assert.That(bay2Balls.Count(t => PoolBallIdentity.SnookerPoints(t.name) == 1), Is.EqualTo(15), "Bay2 must have 15 reds.");
        Assert.That(bay2Balls.Count(t => PoolBallIdentity.SnookerPoints(t.name) >= 2 && PoolBallIdentity.SnookerPoints(t.name) <= 7), Is.EqualTo(6), "Bay2 must have six colours.");
        Assert.That(bay2Balls.All(t => t != null && t.GetComponent<MeshFilter>() != null && t.GetComponent<MeshRenderer>() != null && t.GetComponent<SphereCollider>() != null && t.GetComponent<Rigidbody>() != null), Is.True, "All Bay2 balls must have Mesh, Renderer, Collider and Rigidbody.");

        var eventSystem = UnityEngine.Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
        Assert.That(eventSystem, Is.Not.Null, "Runtime tablet did not create an EventSystem.");
        Assert.That(eventSystem.GetComponent("InputSystemUIInputModule"), Is.Not.Null, "Runtime tablet must use InputSystemUIInputModule.");
        Assert.That(eventSystem.GetComponent("StandaloneInputModule"), Is.Null, "Legacy StandaloneInputModule must not be installed.");

        Assert.That(runners.Select(r => r.TableRootTransform).Distinct().Count(), Is.EqualTo(4),
            "Each bay must own a distinct table transform.");

        string[] requiredSystems =
        {
            "PlayerViewManager",
            "SnookerPhysicsSetup",
            "SnookerBallTracker",
            "SnookerShotTracker",
            "SnookerTurnManager",
            "SnookerScoreManager"
        };

        foreach (string componentName in requiredSystems)
        {
            int count = Resources.FindObjectsOfTypeAll<MonoBehaviour>()
                .Count(component => component != null
                    && component.gameObject.scene == scene
                    && component.gameObject.activeInHierarchy
                    && component.enabled
                    && component.GetType().Name == componentName);
            Assert.That(count, Is.EqualTo(1),
                componentName + " must exist exactly once in the production scene.");
        }

        Debug.Log("[WIPROOM V008] PASS | single build entry | bays=" +
            string.Join(",", runners.Select(r => r.BayName)) +
            " | balls=" + string.Join(",", runners.Select(r => r.BallCount)));
    }

    [UnityTest]
    public IEnumerator RuntimeAudioAndHaptics_AreInstalledForEveryPhysicsBay()
    {
        Scene scene = SceneManager.GetSceneByName(SceneName);
        if (!scene.isLoaded)
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);

        yield return null;
        yield return null;

        PoolPhysicsEventRunner[] runners = UnityEngine.Object
            .FindObjectsByType<PoolPhysicsEventRunner>(FindObjectsSortMode.None)
            .Where(r => r != null && r.gameObject.scene == SceneManager.GetActiveScene())
            .ToArray();

        Assert.That(runners.Length, Is.EqualTo(4));
        foreach (PoolPhysicsEventRunner runner in runners)
        {
            PoolPhysicsAudioHapticsBridge bridge = runner.GetComponent<PoolPhysicsAudioHapticsBridge>();
            Assert.That(bridge, Is.Not.Null, runner.BayName + " has no runtime audio/haptics bridge.");
            Assert.That(bridge.AudioSourcePoolCount, Is.EqualTo(6), runner.BayName + " audio source pool is incomplete.");
            Assert.That(bridge.HasCoreAudioClips, Is.True, runner.BayName + " failed to load one or more core SFX clips.");
        }

        Debug.Log("[WIPROOM AUDIO] PASS | runtime audio/haptics bridges=4 | pooled sources=24");
    }
    [UnityTest]
    public IEnumerator RuntimeAuthorityAndRulesBridge_AreCreatedForWipRoom()
    {
        Scene scene = SceneManager.GetSceneByName(SceneName);
        if (!scene.isLoaded)
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);

        yield return null;
        yield return null;

        Type directorType = Type.GetType(
            "VR147.Authority.PoolPhysicsAuthorityDirector, Assembly-CSharp", false);
        Type bridgeType = Type.GetType(
            "VR147.Authority.PoolPhysicsRulesBridge, Assembly-CSharp", false);

        Assert.That(directorType, Is.Not.Null, "PoolPhysicsAuthorityDirector type is missing.");
        Assert.That(bridgeType, Is.Not.Null, "PoolPhysicsRulesBridge type is missing.");

        Behaviour director = Resources.FindObjectsOfTypeAll(directorType)
            .OfType<Behaviour>()
            .FirstOrDefault(component => component != null && component.gameObject.scene == scene);
        Assert.That(director, Is.Not.Null, "Runtime bootstrap did not create the authority director.");

        Component bridge = director.GetComponent(bridgeType);
        Assert.That(bridge, Is.Not.Null, "Authority director did not create the rules bridge.");

        object active = directorType.GetProperty("Active").GetValue(director);
        Assert.That(active, Is.Not.Null, "Authority director did not claim an active bay.");

        int ownedCount = UnityEngine.Object
            .FindObjectsByType<PoolPhysicsEventRunner>(FindObjectsSortMode.None)
            .Count(r => r != null && r.gameObject.scene == scene && r.OwnsPhysics);
        Assert.That(ownedCount, Is.EqualTo(1),
            "Exactly one bay may own the physics simulator at runtime.");

        string boundBay = bridgeType.GetProperty("BoundBay").GetValue(bridge).ToString();
        Assert.That(boundBay, Is.Not.EqualTo("<none>"),
            "Rules bridge did not bind to the active physics bay.");

        Component tracker = FindSceneComponent("SnookerBallTracker", scene);
        Assert.That(tracker, Is.Not.Null, "SnookerBallTracker must be active in WipRoom.");
        int trackerBallCount = (int)tracker.GetType().GetMethod("BallsCount").Invoke(tracker, null);
        Assert.That(trackerBallCount, Is.EqualTo(22), "SnookerBallTracker must bind to the active 22-ball Snooker bay.");
        Transform trackerTableRoot = tracker.GetType().GetField("tableRoot").GetValue(tracker) as Transform;
        Assert.That(trackerTableRoot, Is.Not.Null, "SnookerBallTracker must follow the active bay table root.");

        Component shotTracker = FindSceneComponent("SnookerShotTracker", scene);
        Assert.That(shotTracker, Is.Not.Null, "SnookerShotTracker must be active in WipRoom.");
        bool snookerRulesEnabled = (bool)shotTracker.GetType().GetField("snookerRulesEnabled").GetValue(shotTracker);
        Assert.That(snookerRulesEnabled, Is.True, "Active Snooker bay must enable Snooker rules.");
        Assert.That(FindSceneComponent("SnookerTurnManager", scene), Is.Not.Null, "SnookerTurnManager must be active in WipRoom.");
        Component lifecycle = FindSceneComponent("M5ShotLifecycle", scene);
        Assert.That(lifecycle, Is.Not.Null, "M5ShotLifecycle must be active in WipRoom.");
        Assert.That(lifecycle.GetType().GetField("eventRunner").GetValue(lifecycle), Is.Not.Null, "M5ShotLifecycle must bind to the active EventRunner.");

        Debug.Log("[WIPROOM AUTHORITY] PASS | activeBay=" + boundBay + " | ownedBays=" + ownedCount);
    }
    private static Component FindSceneComponent(string typeName, Scene scene)
    {
        Type type = Type.GetType(typeName + ", Assembly-CSharp", false);
        if (type == null) return null;
        return Resources.FindObjectsOfTypeAll(type)
            .OfType<Component>()
            .FirstOrDefault(component => component != null && component.gameObject.scene == scene && component.gameObject.activeInHierarchy);
    }

    private static bool IsCueBallName(string name)
    {
        return !string.IsNullOrEmpty(name) &&
               (name.IndexOf("Cue", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("White", StringComparison.OrdinalIgnoreCase) >= 0);
    }

}
