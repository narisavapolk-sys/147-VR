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

    /// <summary>Phase 3 guard: the rules/scoring bridge type must exist and be reachable
    /// (Pass C/D wiring); it does not run in the editor test session, but its assembly
    /// reference must resolve for the production path to compile.</summary>
    [Test]
    public void Phase3_RulesBridge_TypeIsResolvable()
    {
        var t = Type.GetType("VR147.Authority.PoolPhysicsRulesBridge, Assembly-CSharp", throwOnError: false);
        Assert.That(t, Is.Not.Null, "PoolPhysicsRulesBridge (Phase 3) type missing from Assembly-CSharp.");
    }
}
