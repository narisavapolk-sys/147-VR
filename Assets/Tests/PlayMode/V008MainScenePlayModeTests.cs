using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class V008MainScenePlayModeTests
{
    private const string SceneName = "147VR_MainScene";
    private const string TableName = "147VR_Table_V008_OPT_VISUAL";

    [UnityTest]
    public IEnumerator V008_MainScene_SerializedWiring_IsValid()
    {
        var scene = SceneManager.GetSceneByName(SceneName);
        if (!scene.isLoaded)
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);

        yield return null;
        yield return new WaitForFixedUpdate();

        scene = SceneManager.GetActiveScene();
        Assert.That(scene.IsValid() && scene.isLoaded, Is.True, "Canonical MainScene did not load.");
        Assert.That(scene.name, Is.EqualTo(SceneName));

        var table = GameObject.Find(TableName);
        Assert.That(table, Is.Not.Null, "Active V008 visual root is missing.");
        Assert.That(table.activeInHierarchy, Is.True);

        var surface = table.GetComponentsInChildren<Renderer>(true)
            .FirstOrDefault(r => r.name == "TABLE SURFACE");
        Assert.That(surface, Is.Not.Null, "V008 TABLE SURFACE renderer is missing.");
        Assert.That(surface.enabled && surface.gameObject.activeInHierarchy, Is.True);
        Assert.That(surface.sharedMaterial, Is.Not.Null, "V008 table surface has no material.");

        int pocketPads = table.GetComponentsInChildren<Transform>(true)
            .Count(t => t.gameObject.activeInHierarchy && t.name.StartsWith("Pocket Pad", StringComparison.Ordinal));
        Assert.That(pocketPads, Is.GreaterThanOrEqualTo(4), "V008 pocket-pad hierarchy is incomplete.");

        bool coreSystemsActive = true;
        string coreSystemHost = "<missing>";
        foreach (string componentName in new[] { "PlayerViewManager", "SnookerBallTracker", "SnookerPhysicsSetup" })
        {
            var matches = Resources.FindObjectsOfTypeAll<MonoBehaviour>()
                .Where(c => c != null && c.gameObject.scene == scene && c.GetType().Name == componentName)
                .ToArray();
            Assert.That(matches.Length, Is.EqualTo(1), componentName + " must exist exactly once.");

            var field = matches[0].GetType().GetField("tableRoot",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, componentName + ".tableRoot field is missing.");
            Assert.That(field.GetValue(matches[0]), Is.EqualTo(table.transform),
                componentName + ".tableRoot does not target the active V008 table.");

            coreSystemsActive &= matches[0].gameObject.activeInHierarchy;
            coreSystemHost = matches[0].gameObject.name;
        }

        int legacyCount = Resources.FindObjectsOfTypeAll<GameObject>()
            .Count(go => go != null && go.scene == scene && go.name == "Prefab_WPBSA_12Foot_Snooker");
        int bedMarkerCount = Resources.FindObjectsOfTypeAll<GameObject>()
            .Count(go => go != null && go.scene == scene && go.name == "Bed_Collider");
        Assert.That(legacyCount, Is.EqualTo(0), "Inactive legacy table prefab still exists.");
        Assert.That(bedMarkerCount, Is.EqualTo(0), "Orphan Bed_Collider marker still exists.");

        int simRunnerCount = Resources.FindObjectsOfTypeAll<MonoBehaviour>()
            .Count(c => c != null && c.gameObject.scene == scene &&
                (c.GetType().Name == "PoolPhysicsRunner" || c.GetType().Name == "PoolPhysicsEventRunner"));
        Assert.That(simRunnerCount, Is.EqualTo(0),
            "PoolPhysics simulator must remain dormant until a deliberate authority handoff.");

        int activeBallBodies = Resources.FindObjectsOfTypeAll<Rigidbody>().Count(rb =>
        {
            if (rb == null || rb.gameObject.scene != scene || !rb.gameObject.activeInHierarchy) return false;
            var sphere = rb.GetComponent<SphereCollider>();
            return sphere != null && sphere.enabled && sphere.radius >= 0.02f && sphere.radius <= 0.04f;
        });

        Debug.Log("[V008 PLAYMODE] PASS | table=" + table.name +
            " | pocketPads=" + pocketPads +
            " | surfaceMaterial=" + surface.sharedMaterial.name +
            " | legacy=" + legacyCount +
            " | simRunners=" + simRunnerCount +
            " | coreSystemsActive=" + coreSystemsActive +
            " | coreSystemHost=" + coreSystemHost +
            " | activeBallBodies=" + activeBallBodies +
            " | gameplayPhysics=" + (activeBallBodies > 0 && coreSystemsActive
                ? "READY_FOR_SHOT_TEST"
                : "BLOCKED_SYSTEMS_OR_BALL_SET_NOT_ACTIVE"));
    }
}
