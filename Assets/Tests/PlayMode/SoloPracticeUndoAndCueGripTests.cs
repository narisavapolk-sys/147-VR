using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using PoolPhysics.Sim;
using PoolPhysics.Unity;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// -----------------------------------------------------------------------------
// Solo-practice UNDO + cue grip length — Play Mode integration tests.
//
// Why these exist: both features are written and their PURE logic passes headless QA
// (PoolPhysics/Tests mode "undo": U1-U11 incl. the SoloPractice gate matrix). What
// headless QA cannot see is the runtime wiring, which is what this file asserts:
//   * the rules bridge and the grip controller are created by the authority
//     director's RuntimeInitializeOnLoad bootstrap (they exist in no scene file),
//   * LEFT X while walking is actually bound to the bridge's undo entry point,
//   * a practice shot really produces a snapshot that restores the cue ball,
//   * Match mode can never undo, even after a snapshot exists,
//   * grip length publishes to subscribers, persists under the shared key, and clamps.
//
// Assembly note: the bridge and the controller live in Assembly-CSharp, which a test
// assembly cannot reference directly, so they are reached by reflection exactly like
// PoolPhysicsPhaseTests does. PoolPhysics types are reachable normally.
//
// Evidence rule (AI protocol): a PASS here is a controlled Play Mode result for the
// wired scene at this commit. It is not a Quest device proof and not a Golden
// certification, and it does not prove the cue VISUAL looks right - that stays a
// rendered-image check by the owner.
// -----------------------------------------------------------------------------

public class SoloPracticeUndoAndCueGripTests
{
    const string SceneName = "147VR_MainScene";
    const float RestTimeoutSec = 45f;
    const float ShotSpeed = 3.0f;

    static readonly Type BridgeType =
        Type.GetType("VR147.Authority.PoolPhysicsRulesBridge, Assembly-CSharp", throwOnError: false);
    static readonly Type DirectorType =
        Type.GetType("VR147.Authority.PoolPhysicsAuthorityDirector, Assembly-CSharp", throwOnError: false);
    static readonly Type GripType =
        Type.GetType("BhpCueGripController, Assembly-CSharp", throwOnError: false);
    static readonly Type GameModeType =
        Type.GetType("PoolPhysics.Sim.PoolGameMode, PoolPhysics", throwOnError: false);

    static IEnumerator LoadScene()
    {
        Scene scene = SceneManager.GetSceneByName(SceneName);
        if (!scene.isLoaded)
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
        yield return null;
        yield return new WaitForFixedUpdate();
    }

    static IEnumerator WaitForRest(PoolPhysicsEventRunner runner)
    {
        float deadline = Time.realtimeSinceStartup + RestTimeoutSec;
        while (runner != null && !runner.AtRest)
        {
            if (Time.realtimeSinceStartup > deadline)
                Assert.Fail("table never reached rest within " + RestTimeoutSec + " s.");
            yield return null;
            yield return new WaitForFixedUpdate();
        }
    }

    static Behaviour EnsureDirector()
    {
        Assert.That(DirectorType, Is.Not.Null, "PoolPhysicsAuthorityDirector type missing from Assembly-CSharp.");
        MethodInfo bootstrap = DirectorType.GetMethod("Bootstrap",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(bootstrap, Is.Not.Null, "PoolPhysicsAuthorityDirector.Bootstrap is missing.");
        bootstrap.Invoke(null, null);

        Behaviour director = Resources.FindObjectsOfTypeAll(DirectorType)
            .OfType<Behaviour>().FirstOrDefault(c => c != null && c.gameObject.scene.IsValid());
        Assert.That(director, Is.Not.Null, "runtime bootstrap did not create or reuse the authority director.");
        return director;
    }

    static Vector3 CueBallPosition(PoolPhysicsEventRunner runner)
    {
        Transform cue = runner.BallTransformAt(runner.CueBallIndex);
        Assert.That(cue, Is.Not.Null, "runner has no cue ball transform at index " + runner.CueBallIndex + ".");
        return cue.position;
    }

    // ---------------------------------------------------------------------
    // 1) UNDO: a practice shot can be undone; Match mode never can.
    // ---------------------------------------------------------------------
    [UnityTest]
    public IEnumerator SoloPracticeUndo_RestoresPreShotCue_AndMatchModeBlocksIt()
    {
        yield return LoadScene();
        Behaviour director = EnsureDirector();
        yield return null; // one frame so the bridge can subscribe to ShotStarted + the controller

        Behaviour bridge = director.GetComponent(BridgeType) as Behaviour;
        Assert.That(bridge, Is.Not.Null, "the authority bootstrap did not create PoolPhysicsRulesBridge.");
        Type bt = bridge.GetType();

        Assert.That(bt.GetProperty("CurrentGameMode").GetValue(bridge).ToString(), Is.EqualTo("Match"),
            "game mode must default to Match; practice has to be chosen on purpose.");
        Assert.That((bool)bt.GetProperty("UndoInputBound").GetValue(bridge), Is.True,
            "LEFT X is not bound to the bridge, so the UNDO button would do nothing.");
        Assert.That((bool)bt.GetProperty("UndoMapValid").GetValue(bridge), Is.True,
            "the sim-index to save-number map failed validation, so snapshots cannot be trusted.");
        Assert.That((bool)bt.GetProperty("UndoAvailable").GetValue(bridge), Is.False,
            "UNDO must be unavailable in Match before anything is shot.");

        object runnerObj = DirectorType.GetProperty("Active").GetValue(director);
        Assert.That(runnerObj, Is.Not.Null, "the director has no active physics bay.");
        var runner = (PoolPhysicsEventRunner)runnerObj;
        Assert.That(runner.AtRest, Is.True, "the table must be at rest before the practice shot.");

        // --- enter practice through the same entry point the tablet toggle uses ---
        MethodInfo trySetGameMode = bt.GetMethod("TrySetGameMode");
        Assert.That(trySetGameMode, Is.Not.Null, "TrySetGameMode is missing.");
        object soloPractice = Enum.Parse(GameModeType, "SoloPractice");
        Assert.That((bool)trySetGameMode.Invoke(bridge, new[] { soloPractice }), Is.True,
            "TrySetGameMode(SoloPractice) was rejected at a settled boundary.");
        Assert.That((bool)bt.GetProperty("SoloPractice").GetValue(bridge), Is.True,
            "the bridge did not enter SoloPractice.");

        Vector3 cueBefore = CueBallPosition(runner);

        runner.Shoot(Vector3.forward, ShotSpeed, Vector2.zero, 0f);
        yield return WaitForRest(runner);
        yield return null;

        Vector3 cueAfter = CueBallPosition(runner);
        Assert.That(Vector3.Distance(cueBefore, cueAfter), Is.GreaterThan(0.05f),
            "the shot did not move the cue ball, so this run cannot prove anything about UNDO.");
        Assert.That((bool)bt.GetProperty("UndoAvailable").GetValue(bridge), Is.True,
            "after a settled practice shot the bridge must offer UNDO.");

        string topJson = PeekUndoTop(bridge);
        Assert.That(string.IsNullOrEmpty(topJson), Is.False,
            "the undo stack is empty even though UndoAvailable said true.");
        Assert.That(topJson, Does.Contain("balls"), "the pre-shot snapshot is not a save-model payload.");

        MethodInfo requestUndo = bt.GetMethod("RequestUndo");
        Assert.That(requestUndo, Is.Not.Null, "RequestUndo is missing.");
        Assert.That((bool)requestUndo.Invoke(bridge, null), Is.True,
            "RequestUndo returned false while in practice mode with a snapshot available.");
        yield return null;
        yield return new WaitForFixedUpdate();

        Vector3 cueRestored = CueBallPosition(runner);
        Assert.That(Vector3.Distance(cueRestored, cueBefore), Is.LessThan(0.01f),
            "UNDO did not put the cue ball back where the shot started.");

        // --- leaving practice must kill the capability, snapshot or not ---
        object match = Enum.Parse(GameModeType, "Match");
        Assert.That((bool)trySetGameMode.Invoke(bridge, new[] { match }), Is.True,
            "could not return to Match at a settled boundary.");
        Assert.That((bool)requestUndo.Invoke(bridge, null), Is.False,
            "UNDO must never succeed in Match - this is the owner rule, not a nicety.");
    }

    static string PeekUndoTop(Behaviour bridge)
    {
        FieldInfo stackField = bridge.GetType().GetField("_undo", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(stackField, Is.Not.Null, "PoolPhysicsRulesBridge no longer holds a PoolUndoStack in _undo.");
        var stack = stackField.GetValue(bridge) as PoolUndoStack;
        Assert.That(stack, Is.Not.Null, "the undo stack was never constructed.");
        return stack.PeekTop();
    }

    // ---------------------------------------------------------------------
    // 2) Cue grip length: the controller owns, publishes, persists and clamps it.
    // ---------------------------------------------------------------------
    [UnityTest]
    public IEnumerator CueGripLength_PublishesPersistsAndClamps()
    {
        yield return LoadScene();
        EnsureDirector();
        yield return null;

        Behaviour grip = Resources.FindObjectsOfTypeAll(GripType).OfType<Behaviour>().FirstOrDefault();
        Assert.That(grip, Is.Not.Null, "the authority bootstrap did not create BhpCueGripController.");
        Type gt = grip.GetType();

        string prefsKey = (string)gt.GetField("PrefsKeyGripLength01",
            BindingFlags.Public | BindingFlags.Static).GetRawConstantValue();
        Assert.That(prefsKey, Is.EqualTo("Tablet.CueGripLength01"),
            "the persistence key changed - the tablet slider and the controller would stop sharing it.");

        EventInfo changedEvent = gt.GetEvent("OnGripLengthChanged");
        Assert.That(changedEvent, Is.Not.Null, "OnGripLengthChanged is missing; the cue visual cannot follow.");
        Action<float> handler = value => { Published = value; };
        Delegate bound = Delegate.CreateDelegate(changedEvent.EventHandlerType, handler.Target, handler.Method);
        changedEvent.AddEventHandler(grip, bound);

        MethodInfo setter = gt.GetMethod("SetGripLength01");
        Assert.That(setter, Is.Not.Null, "SetGripLength01 is missing.");

        Published = -1f;
        setter.Invoke(grip, new object[] { 0.8f });
        Assert.That(Published, Is.EqualTo(0.8f).Within(1e-5f),
            "changing the grip length did not publish to subscribers.");
        Assert.That(PlayerPrefs.GetFloat(prefsKey, -1f), Is.EqualTo(0.8f).Within(1e-5f),
            "the value was not persisted - the player's preference would reset every session.");

        setter.Invoke(grip, new object[] { 1.5f });
        Assert.That((float)gt.GetField("GripLength01").GetValue(grip), Is.EqualTo(1f),
            "values above 1 must clamp to the butt end.");
        setter.Invoke(grip, new object[] { -1f });
        Assert.That((float)gt.GetField("GripLength01").GetValue(grip), Is.EqualTo(0f),
            "values below 0 must clamp to the tip end.");

        // setting the same value again must not spam subscribers
        Published = 0.5f;
        setter.Invoke(grip, new object[] { 0f });
        Assert.That(Published, Is.EqualTo(0.5f),
            "an unchanged value should not fire OnGripLengthChanged again.");

        changedEvent.RemoveEventHandler(grip, bound);

        Behaviour cueVisual = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
            .OfType<MonoBehaviour>()
            .FirstOrDefault(c => c != null && c.GetType().Name == "SnookerCueController");
        Assert.That(cueVisual, Is.Not.Null,
            "SnookerCueController is absent, so grip length has nothing to redraw - the feature would be invisible.");

        // the step event is what drives haptics; assert it exists with the expected shape
        EventInfo stepEvent = gt.GetEvent("OnGripLengthStep");
        Assert.That(stepEvent, Is.Not.Null, "OnGripLengthStep is missing; per-step haptics cannot fire.");

        PlayerPrefs.DeleteKey(prefsKey);
        yield return null;
    }

    static float Published = -1f;
}
