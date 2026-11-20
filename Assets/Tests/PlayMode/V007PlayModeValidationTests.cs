using System.Collections;
using System.Linq;
using NUnit.Framework;
using PoolPhysics.Unity;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class V007PlayModeValidationTests
{
    private const string SceneName = "147VR_MainScene_WipRoom";

    [UnityTest]
    public IEnumerator ProductionEntry_ContainsFourConfiguredPhysicsBays()
    {
        Scene scene = SceneManager.GetSceneByName(SceneName);
        if (!scene.isLoaded)
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);

        yield return null;
        yield return new WaitForFixedUpdate();

        scene = SceneManager.GetActiveScene();
        Assert.That(scene.IsValid() && scene.isLoaded, Is.True);
        Assert.That(scene.name, Is.EqualTo(SceneName));

        PoolPhysicsEventRunner[] runners = Object
            .FindObjectsByType<PoolPhysicsEventRunner>(FindObjectsSortMode.None)
            .Where(r => r != null && r.gameObject.scene == scene)
            .OrderBy(r => r.BayName)
            .ToArray();

        Assert.That(runners.Select(r => r.BayName),
            Is.EquivalentTo(new[]
            {
                "Bay1_PhysicsRoot",
                "Bay2_PhysicsRoot",
                "Bay3_PhysicsRoot",
                "Bay4_PhysicsRoot"
            }));

        foreach (PoolPhysicsEventRunner runner in runners)
        {
            Assert.That(runner.Spec, Is.Not.Null, runner.BayName + " TableSpec missing.");
            Assert.That(runner.BallCount, Is.GreaterThanOrEqualTo(3), runner.BayName + " ball wiring missing.");
            Assert.That(runner.TableRootTransform, Is.Not.Null, runner.BayName + " table root missing.");
            Assert.That(runner.TableRootTransform.lossyScale.x, Is.EqualTo(1f).Within(0.01f),
                runner.BayName + " table scale is not metric.");
        }

        Debug.Log("[WIPROOM V007] PASS | four configured bays | " +
            string.Join(", ", runners.Select(r => r.BayName + ":" + r.BallCount + " balls")));
    }
}
