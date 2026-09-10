using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class V007PlayModeValidationTests
{
    const string SceneName = "147VR_MainScene";
    const string VisualName = "V007_VISUAL_MAIN";

    [UnityTest]
    public IEnumerator V007_MainScene_Visual_Is_RuntimeValid()
    {
        var scene = SceneManager.GetSceneByName(SceneName);
        if (!scene.isLoaded)
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
        yield return null;
        yield return new WaitForFixedUpdate();

        var visual = GameObject.Find(VisualName);
        Assert.That(visual, Is.Not.Null, "V007 visual root is missing in Play Mode.");
        Transform surface = null;
        foreach (var t in visual.GetComponentsInChildren<Transform>(true))
            if (t.name == "TABLE SURFACE") { surface = t; break; }
        Assert.That(surface, Is.Not.Null, "V007 TABLE SURFACE is missing in Play Mode.");

        var renderer = surface.GetComponent<Renderer>();
        Assert.That(renderer, Is.Not.Null);
        Assert.That(renderer.sharedMaterial, Is.Not.Null);
        Assert.That(renderer.sharedMaterial.name, Is.EqualTo("M_V007_TableSurface_Marking"));
        var bounds = renderer.bounds.size;
        Assert.That(bounds.x, Is.EqualTo(3.569f).Within(0.002f));
        Assert.That(bounds.z, Is.EqualTo(1.778f).Within(0.002f));

        Debug.Log($"[V007 PLAYMODE TEST] PASS visual={visual.name} surface={surface.name} bounds={bounds} material={renderer.sharedMaterial.name}");
    }
}
