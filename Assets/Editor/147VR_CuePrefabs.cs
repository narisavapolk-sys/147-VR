#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// Builds one cue prefab per material preset, using the generated ProceduralCue mesh.
// Run 'Generate Procedural Cue (mesh)' and 'Create Cue Material Presets' first.
public static class _147VR_CuePrefabs
{
    const string MESH = "Assets/AAA/ProceduralCue/ProceduralCue.asset";
    const string MATS = "Assets/AAA/ProceduralCue/Mats";
    const string PARENT = "Assets/AAA/ProceduralCue";
    const string PREFABS = "Assets/AAA/ProceduralCue/Prefabs";

    [MenuItem("147VR/Art/Cue/Create Cue Prefabs (all styles)")]
    public static void Create()
    {
        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(MESH);
        if (mesh == null)
        {
            Debug.LogError("147VR/CuePrefabs: run '147VR/Art/Cue/Generate Procedural Cue (mesh)' first.");
            return;
        }
        if (!AssetDatabase.IsValidFolder(PREFABS))
        {
            if (!AssetDatabase.IsValidFolder(PARENT))
                AssetDatabase.CreateFolder("Assets/AAA", "ProceduralCue");
            AssetDatabase.CreateFolder(PARENT, "Prefabs");
        }

        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { MATS });
        int n = 0;
        foreach (var g in guids)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g));
            if (mat == null) continue;

            GameObject go = new GameObject(mat.name);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;

            string path = PREFABS + "/" + mat.name + ".prefab";
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            n++;
        }
        AssetDatabase.SaveAssets();
        Debug.Log("147VR/CuePrefabs: created " + n + " cue prefabs in " + PREFABS + ".");
    }
}
#endif
