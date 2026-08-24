using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class VerifyDanceScene
{
    [MenuItem("Tools/147/Verify Dance Scene")]
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
        var scene = SceneManager.GetActiveScene();
        foreach (var go in scene.GetRootGameObjects())
        {
            if (!go.name.Contains("CuteGirl") && !go.name.Contains("Danc")) continue;
            var anim = go.GetComponentInChildren<Animator>();
            var smr = go.GetComponentInChildren<SkinnedMeshRenderer>();
            var dancer = go.GetComponent<CuteDancer>();
            Debug.Log($"GIRL {go.name} at {go.transform.position} rot={go.transform.rotation.eulerAngles}");
            Debug.Log($"  animator={(anim != null)} controller={(anim != null && anim.runtimeAnimatorController != null ? anim.runtimeAnimatorController.name : "NONE")}");
            Debug.Log($"  skinned={(smr != null)} bones={(smr != null ? smr.bones.Length : 0)}");
            Debug.Log($"  dancer={(dancer != null)} clips={(dancer != null ? dancer.danceClips.Length : 0)}");
            if (dancer != null)
                foreach (var c in dancer.danceClips) Debug.Log("  DANCECLIP " + c);
        }
        Debug.Log("VERIFY_DONE");
    }
}
