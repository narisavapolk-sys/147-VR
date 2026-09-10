using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Samples each dance clip and captures a screenshot to show the girl actually dancing.</summary>
public static class ShotDanceScene
{
    [MenuItem("Tools/147/Shot Dance Scene")]
    public static void Run()
    {
        string outDir = @"C:/Users/mongo/UnityProjects/147 VR/SNOOKER   VR pool table/Images";
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
        var scene = SceneManager.GetActiveScene();

        GameObject girl = null;
        foreach (var go in scene.GetRootGameObjects())
            if (go.name.Contains("CuteGirl")) girl = go;
        if (girl == null) { Debug.LogError("NO_GIRL"); return; }

        var anim = girl.GetComponentInChildren<Animator>();
        var controller = (UnityEditor.Animations.AnimatorController)anim.runtimeAnimatorController;
        if (controller == null) { Debug.LogError("NO_CONTROLLER"); return; }
        // Camera: reuse Main Camera, move to a wide view showing both girls
        Camera cam = Camera.main;
        if (cam == null) { Debug.LogError("NO_CAMERA"); return; }
        cam.transform.position = new Vector3(0f, 1.3f, 4.2f);
        cam.transform.LookAt(new Vector3(0f, 1.0f, 0.8f));
        cam.fieldOfView = 60;

        int w = 720, h = 960;
        RenderTexture rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt;
        cam.enabled = false;

        // Disable all animators during sampling to avoid conflicts
        foreach (var go in scene.GetRootGameObjects())
            foreach (var a in go.GetComponentsInChildren<Animator>()) a.enabled = false;

        // Bake helper: replaces SkinnedMeshRenderer with a baked static mesh
        var originalSmrs = new System.Collections.Generic.List<SkinnedMeshRenderer>();
        var bakedHolders = new System.Collections.Generic.List<(GameObject go, SkinnedMeshRenderer smr)>();

        foreach (var smr in girl.GetComponentsInChildren<SkinnedMeshRenderer>())
            originalSmrs.Add(smr);

        int idx = 0;
        foreach (var state in controller.layers[0].stateMachine.states)
        {
            var clip = state.state.motion as AnimationClip;
            if (clip == null) continue;
            float t = Mathf.Min(clip.length * 0.5f, clip.length - 0.05f);
            // Direct clip sampling on the root (works without AnimationMode)
            clip.SampleAnimation(girl, t);
            EditorApplication.Step();
            SceneView.RepaintAll();
            // verify pose actually changed: log Hips bone world position
            Transform hipsT = girl.transform.Find("CuteDanceRig/mixamorig9:Hips");
            if (hipsT != null)
                Debug.Log($"POS_{clip.name} hips={hipsT.position}");
            // Bake skin to static meshes and render those
            foreach (var smr in originalSmrs)
            {
                smr.enabled = false;
                Mesh baked = new Mesh();
                smr.BakeMesh(baked);
                // create a temporary holder with MeshFilter + MeshRenderer
                var holder = new GameObject("_baked");
                holder.transform.SetParent(smr.transform.parent, false);
                holder.transform.localPosition = smr.transform.localPosition;
                holder.transform.localRotation = smr.transform.localRotation;
                holder.transform.localScale = smr.transform.localScale;
                var mf = holder.AddComponent<MeshFilter>();
                mf.sharedMesh = baked;
                var mr = holder.AddComponent<MeshRenderer>();
                mr.sharedMaterials = smr.sharedMaterials;
                bakedHolders.Add((holder, smr));
            }
            // render
            cam.Render();
            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            string clipShort = clip.name.Replace("Cute_", "").Replace(" ", "_");
            string file = Path.Combine(outDir, $"_dance_shot_{idx:D2}_{clipShort}.png");
            File.WriteAllBytes(file, tex.EncodeToPNG());
            Debug.Log("SHOT " + clip.name + " -> " + file);
            // cleanup baked holders, restore smr
            foreach (var (go, smr) in bakedHolders)
            {
                Object.DestroyImmediate(go);
                smr.enabled = true;
            }
            bakedHolders.Clear();
            idx++;
        }
        cam.targetTexture = null;
        RenderTexture.active = null;
        Object.DestroyImmediate(rt);
        Debug.Log("SHOTS_DONE " + idx);
    }
}
