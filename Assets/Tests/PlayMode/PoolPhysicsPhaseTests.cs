using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using PoolPhysics.Unity;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// -----------------------------------------------------------------------------
// Phase 1-3 physics Play Mode tests — one shot on every wired bay (Bay1..Bay4).
//
// What each test proves, in order (authority contract):
//   Phase 1  Handoff   — Pass C wiring intact (spec/profile/balls/cue/scale), the bay
//                        takes physics ownership on demand and releases it again.
//   Phase 2  Collision — the fired shot actually moves the cue ball, produces at
//                        least one collision, never produces NaN, and never
//                        explodes past ~5x shot speed (earlier M2.1 regression
//                        had balls accelerate to 49 m/s after contact).
//   Phase 3  Settle    — every table reaches AllAtRest inside the timeout, so the
//                        rules bridge / scoring can act on a finished shot.
//
// Evidence note (AI protocol): a PASS here is a controlled Play Mode result for
// the wired bays as of this commit; it is not a Golden certification.
// -----------------------------------------------------------------------------

public class PoolPhysicsPhaseTests
{
    const string SceneName      = "147VR_MainScene";
    const float  ShotSpeed      = 3.0f;   // m/s break — same order as the Golden batches
    const float  RestTimeoutSec = 45f;
    const float  MotionSpeed    = 0.05f;
    const float  ExplosiveSpeed = 15f;

    static IEnumerator LoadMainScene()
    {
        var scene = SceneManager.GetSceneByName(SceneName);
        if (!scene.isLoaded)
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
        yield return null;
        yield return new WaitForFixedUpdate();
    }

    static IEnumerator RunBay(string bayName)
    {
        yield return LoadMainScene();

        var runner = UnityEngine.Object.FindObjectsByType<PoolPhysicsEventRunner>(FindObjectsSortMode.None)
            .FirstOrDefault(r => r != null && string.Equals(r.BayName, bayName, StringComparison.OrdinalIgnoreCase));
        Assert.That(runner, Is.Not.Null, $"Bay '{bayName}': PoolPhysicsEventRunner missing — Pass C wiring failed.");

        // ---- Phase 1 wiring sanity ----
        Assert.That(runner.Spec, Is.Not.Null, $"{bayName}: TableSpec not wired.");
        Assert.That(runner.PlayfieldSize, Is.Not.EqualTo(Vector2.zero), $"{bayName}: playfield size is zero.");
        Assert.That(runner.BallCount, Is.GreaterThanOrEqualTo(3), $"{bayName}: balls not wired (Pass C).");
        Assert.That(runner.CueBallIndex, Is.EqualTo(0), $"{bayName}: cue ball must sit at index 0 (Build hardcodes it).");
        var tableT = runner.TableRootTransform;
        Assert.That(tableT, Is.Not.Null, $"{bayName}: tableTransform not wired.");
        Assert.That(tableT.lossyScale.x, Is.EqualTo(1f).Within(0.01f), $"{bayName}: table scale must be 1 (sim runs metres).");

        // ---- Phase 1 handoff (ownership) ----
        Assert.That(runner.OwnsPhysics, Is.False, $"{bayName}: already owns physics before the test claim.");
        runner.Enabled = true;
        yield return null;
        Assert.That(runner.OwnsPhysics, Is.True, $"{bayName}: TakeOwnership failed after Enabled=true.");

        // ---- Phase 2 shot + settle ----
        Physics.SyncTransforms();

        // Aim the break at the ball cluster in front of the cue ball, so the scene
        // rack layout decides the direction (no hard-coded axis guessing).
        var cueT = runner.BallTransformAt(runner.CueBallIndex);
        Assert.That(cueT, Is.Not.Null, $"{bayName}: cue ball transform is null.");
        Vector3 acc = Vector3.zero; int targets = 0;
        for (int i = 0; i < runner.BallCount; i++)
        {
            if (i == runner.CueBallIndex) continue;
            var b = runner.BallTransformAt(i);
            if (b == null) continue;
            acc += b.position; targets++;
        }
        Vector3 worldDir = targets > 0
            ? Vector3.Normalize((acc / targets) - cueT.position)
            : (runner.PlayfieldSize.x >= runner.PlayfieldSize.y ? tableT.TransformDirection(Vector3.right)
                                                                : tableT.TransformDirection(Vector3.forward));
        worldDir.y = 0f;
        if (worldDir.sqrMagnitude < 1e-6f) worldDir = Vector3.right;

        float tStart   = Time.time;
        bool  sawMotion    = false;
        bool  sawCollision = false;
        bool  nan          = false;
        float maxSpeed     = 0f;

        runner.Shoot(worldDir, ShotSpeed, Vector2.zero, 0f);

        while (Time.time - tStart < RestTimeoutSec)
        {
            yield return new WaitForFixedUpdate();

            if (runner.TryGetSimBall(runner.CueBallIndex, out var _, out var cueV, out var _, out var _) &&
                cueV.magnitude > MotionSpeed)
                sawMotion = true;

            if (runner.CollisionCount > 0) sawCollision = true;

            float fastest = 0f;
            int   n = runner.BallCount;
            for (int i = 0; i < n; i++)
            {
                if (!runner.TryGetSimBall(i, out var p, out var v, out var _, out var _)) continue;
                if (!float.IsFinite(p.x) || !float.IsFinite(p.y) || !float.IsFinite(p.z) ||
                    !float.IsFinite(v.x) || !float.IsFinite(v.y) || !float.IsFinite(v.z))
                    nan = true;
                fastest = Mathf.Max(fastest, v.magnitude);
            }
            maxSpeed = Mathf.Max(maxSpeed, fastest);

            if (runner.AtRest && sawMotion && fastest < MotionSpeed) break;
        }

        Assert.That(sawMotion,    Is.True,  $"{bayName}: cue ball never moved (Shoot did not take).");
        Assert.That(sawCollision, Is.True,  $"{bayName}: zero collisions — break missed the rack (check racked layout).");
        Assert.That(nan,          Is.False, $"{bayName}: NaN appeared in sim state — physics blew up.");
        Assert.That(maxSpeed, Is.LessThan(ExplosiveSpeed),
            $"{bayName}: ball reached {maxSpeed:F1} m/s on a {ShotSpeed:F1} m/s shot — explosive/fall-through regression.");
        Assert.That(runner.AtRest, Is.True,
            $"{bayName}: shot never settled within {RestTimeoutSec:F0}s (last observed speed {maxSpeed:F2} m/s).");

        // ---- Phase 1 release ----
        runner.Enabled = false;
        yield return null;
        Assert.That(runner.OwnsPhysics, Is.False, $"{bayName}: ReleaseOwnership failed after Enabled=false.");

        Debug.Log($"[PHASE123] {bayName}: shot ok — collisions={runner.CollisionCount} maxSpeed={maxSpeed:F2} m/s");
    }

    [UnityTest] public IEnumerator Phase123_Bay1_Handoff_Shot_Settle() { yield return RunBay("Bay1_PhysicsRoot"); }
    [UnityTest] public IEnumerator Phase123_Bay2_Handoff_Shot_Settle() { yield return RunBay("Bay2_PhysicsRoot"); }
    [UnityTest] public IEnumerator Phase123_Bay3_Handoff_Shot_Settle() { yield return RunBay("Bay3_PhysicsRoot"); }
    [UnityTest] public IEnumerator Phase123_Bay4_Handoff_Shot_Settle() { yield return RunBay("Bay4_PhysicsRoot"); }

    /// <summary>Phase 3 runtime integration: the authority bootstrap must create the rules
    /// bridge AND the Quest controller producer, and the bridge must subscribe to its UNDO event.</summary>
    [UnityTest]
    public IEnumerator Phase3_RulesBridge_AndUndoController_AreBootstrapped()
    {
        yield return LoadMainScene();

        var directorType = Type.GetType("VR147.Authority.PoolPhysicsAuthorityDirector, Assembly-CSharp", throwOnError: false);
        var bridgeType = Type.GetType("VR147.Authority.PoolPhysicsRulesBridge, Assembly-CSharp", throwOnError: false);
        var controllerType = Type.GetType("BhpCueGripController, Assembly-CSharp", throwOnError: false);
        Assert.That(directorType, Is.Not.Null, "PoolPhysicsAuthorityDirector type missing from Assembly-CSharp.");
        Assert.That(bridgeType, Is.Not.Null, "PoolPhysicsRulesBridge type missing from Assembly-CSharp.");
        Assert.That(controllerType, Is.Not.Null, "BhpCueGripController type missing from Assembly-CSharp.");

        var bootstrap = directorType.GetMethod("Bootstrap",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        Assert.That(bootstrap, Is.Not.Null, "PoolPhysicsAuthorityDirector.Bootstrap is missing.");
        bootstrap.Invoke(null, null);

        var director = Resources.FindObjectsOfTypeAll(directorType)
            .OfType<Behaviour>().FirstOrDefault(c => c.gameObject.scene.IsValid());
        Assert.That(director, Is.Not.Null, "Runtime bootstrap did not create/reuse the authority director.");

        GameObject testHost = director.gameObject;
        var bridge = testHost.GetComponent(bridgeType);
        var controller = testHost.GetComponent(controllerType);
        Assert.That(bridge, Is.Not.Null, "Authority bootstrap did not create PoolPhysicsRulesBridge.");
        Assert.That(controller, Is.Not.Null, "Authority bootstrap did not create BhpCueGripController.");

        yield return null; // let RulesBridge.Start subscribe to the controller event

        bool undoInputBound = (bool)bridgeType.GetProperty("UndoInputBound").GetValue(bridge);
        bool shotStartBound = (bool)bridgeType.GetProperty("ShotStartBound").GetValue(bridge);
        bool undoMapValid = (bool)bridgeType.GetProperty("UndoMapValid").GetValue(bridge);
        string boundBay = bridgeType.GetProperty("BoundBay").GetValue(bridge).ToString();
        string gameMode = bridgeType.GetProperty("CurrentGameMode").GetValue(bridge).ToString();
        bool undoAvailable = (bool)bridgeType.GetProperty("UndoAvailable").GetValue(bridge);
        Assert.That(undoInputBound, Is.True, "RulesBridge did not subscribe to the controller's UNDO event.");
        Assert.That(shotStartBound, Is.True, "RulesBridge did not subscribe to the runner's pre-impulse ShotStarted event.");
        Assert.That(boundBay, Is.Not.EqualTo("<none>"), "Authority director did not bind an active physics bay.");
        Assert.That(undoMapValid, Is.True, "Snooker 22-ball UNDO number map failed validation for the active bay.");
        Assert.That(gameMode, Is.EqualTo("Match"),
            "Game mode must default to Match; SoloPractice must be explicitly selected.");
        Assert.That(undoAvailable, Is.False,
            "UNDO must remain unavailable in Match even when the controller is present.");

        var gameModeType = Type.GetType("PoolPhysics.Sim.PoolGameMode, PoolPhysics", throwOnError: false);
        Assert.That(gameModeType, Is.Not.Null, "PoolGameMode enum missing from PoolPhysics assembly.");
        var trySetGameMode = bridgeType.GetMethod("TrySetGameMode");
        object soloPractice = Enum.Parse(gameModeType, "SoloPractice");
        bool enteredPractice = (bool)trySetGameMode.Invoke(bridge, new[] { soloPractice });
        Assert.That(enteredPractice, Is.True, "RulesBridge rejected SoloPractice at a settled boundary.");
        Assert.That(bridgeType.GetProperty("CurrentGameMode").GetValue(bridge).ToString(), Is.EqualTo("SoloPractice"));
        Assert.That((bool)bridgeType.GetProperty("UndoAvailable").GetValue(bridge), Is.False,
            "SoloPractice alone must not enable UNDO without a valid snapshot and physics authority.");

        var activeRunner = directorType.GetProperty("Active").GetValue(director);
        Assert.That(activeRunner, Is.Not.Null, "Authority director has no active runner for the UNDO integration shot.");
        var shoot = activeRunner.GetType().GetMethod("Shoot");
        shoot.Invoke(activeRunner, new object[] { Vector3.forward, 0f, Vector2.zero, 0f });

        yield return new WaitForFixedUpdate();
        yield return null;
        yield return null; // let RulesBridge.LateUpdate close the shot and publish the undo entry

        bool undoReadyAfterShot = (bool)bridgeType.GetProperty("UndoAvailable").GetValue(bridge);
        Assert.That(undoReadyAfterShot, Is.True,
            "A settled SoloPractice shot must produce one valid pre-shot snapshot.");

        bool undoApplied = (bool)bridgeType.GetMethod("RequestUndo").Invoke(bridge, null);
        Assert.That(undoApplied, Is.True, "RulesBridge failed to restore the captured pre-shot state.");
        Assert.That((bool)bridgeType.GetProperty("UndoAvailable").GetValue(bridge), Is.False,
            "Undoing the only snapshot must leave the history empty.");

        object match = Enum.Parse(gameModeType, "Match");
        bool returnedToMatch = (bool)trySetGameMode.Invoke(bridge, new[] { match });
        Assert.That(returnedToMatch, Is.True, "RulesBridge rejected Match at a settled boundary.");

        UnityEngine.Object.Destroy(testHost);
        yield return null;
    }
}
