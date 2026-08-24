using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Verifies the Chubby Girl fix: per-submesh (material slot) bounding boxes must
/// sit on the body (no floating head parts), then captures a verification photo.
/// </summary>
public static class VerifyChubbyFix
{
    const string FbxPath = "Assets/Models/Girls/Chubby Girl Dancing.fbx";
    const string ScenePath = "Assets/Scenes/SampleScene.unity";
    const string ShotDir = @"C:/Users/mongo/UnityProjects/147 VR/SNOOKER   VR pool table/Images";

    [MenuItem("Tools/147/Verify Chubby Fix")]
    public static void Run()
    {
        var go = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);
        if (go == null) { Debug.LogError("FBX_MISSING"); return; }
        var smr = go.GetComponentInChildren<SkinnedMeshRenderer>();
        if (smr == null) { Debug.LogError("NO_SKINNED"); return; }

        // Bake the rest-pose mesh so vertex positions reflect the skinned result
        var baked = new Mesh();
        smr.BakeMesh(baked);
        int nSub = baked.subMeshCount;
        Debug.Log($"SUBMESHES={nSub} verts={baked.vertexCount}");
        Vector3[] verts = baked.vertices;

        // Body material slot (index 3 in the FBX mat order: boot,eyes,bra,BODY,Corset,...)
        int bodySlot = -1;
        for (int i = 0; i < smr.sharedMaterials.Length; i++)
        {
            if (smr.sharedMaterials[i] != null && smr.sharedMaterials[i].name.Contains("BODY"))
            { bodySlot = i; break; }
        }
        Debug.Log("MATERIALS: " + string.Join(", ", System.Array.ConvertAll(smr.sharedMaterials, m => m == null ? "null" : m.name)));
        if (bodySlot < 0) { Debug.LogError("BODY_SLOT_NOT_FOUND"); return; }

        // Compute bbox per submesh
        System.Func<int, (Vector3 lo, Vector3 hi, Vector3 cen)> bbox = (idx) =>
        {
            int[] tris = baked.GetTriangles(idx);
            var lo = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            var hi = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            foreach (int vi in tris)
            {
                lo = Vector3.Min(lo, verts[vi]);
                hi = Vector3.Max(hi, verts[vi]);
            }
            return (lo, hi, (lo + hi) / 2f);
        };

        var bodyB = bbox(bodySlot);
        Debug.Log($"BODY slot={bodySlot} z={bodyB.lo.z:F3}..{bodyB.hi.z:F3} h={bodyB.hi.z - bodyB.lo.z:F3}");
        int scattered = 0;
        for (int i = 0; i < nSub; i++)
        {
            var b = bbox(i);
            string nm = smr.sharedMaterials[i] == null ? "null" : smr.sharedMaterials[i].name;
            string flag = "";
            if (b.cen.z > bodyB.hi.z + 0.12f || b.cen.z < bodyB.lo.z - 0.12f) { flag = "  <-- SCATTERED"; scattered++; }
            Debug.Log($"  sub[{i}] {nm}: z={b.lo.z:F3}..{b.hi.z:F3} cen=({b.cen.x:F3},{b.cen.y:F3},{b.cen.z:F3}){flag}");
        }
        Debug.Log($"UNITY_SCATTERED={scattered} (must be 0)");

        // --- Capture a verification photo from the scene ---
        Directory.CreateDirectory(ShotDir);
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Scene scene = SceneManager.GetActiveScene();
        GameObject girl = null;
        foreach (GameObject o in scene.GetRootGameObjects())
            if (o.name.Contains("ChubbyGirl")) { girl = o; break; }
        if (girl == null) { Debug.LogError("NO_CHUBBY_IN_SCENE"); return; }

        var cam = Camera.main ?? Object.FindObjectOfType<Camera>();
        if (cam == null) { Debug.LogError("NO_CAMERA"); return; }

        // Frame the girl: place camera in front
        var target = girl.transform.position + Vector3.up * 0.8f;
        cam.transform.position = target + new Vector3(0f, 0.2f, -2.6f);
        cam.transform.LookAt(target);
        cam.fieldOfView = 50f;

        cam.enabled = true;
        var rt = new RenderTexture(1280, 1600, 24);
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(1280, 1600, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, 1280, 1600), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        cam.targetTexture = null;
        cam.enabled = false;
        File.WriteAllBytes(Path.Combine(ShotDir, "_chubby_fix_unity.png"), tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        Debug.Log("UNITY_SHOT_SAVED _chubby_fix_unity.png");

        EditorSceneManager.SaveScene(scene, ScenePath);
        Debug.Log("VERIFY_CHUBBY_DONE");
    }
}
