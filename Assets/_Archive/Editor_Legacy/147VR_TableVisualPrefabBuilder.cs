using UnityEditor;
using UnityEngine;
using System.IO;

[InitializeOnLoad]
public static class _147VR_TableVisualPrefabBuilder
{
    const string FBX = "Assets/BlenderTest/147VR_Table_WPBSA_12ft_VISUAL_CLEAN_v006.fbx";
    const string PREFAB = "Assets/BlenderTest/147VR_Table_WPBSA_12ft_VISUAL_CLEAN_v004.prefab";
    const string MARKER = "Assets/BlenderTest/147VR_TABLE_VISUAL_V004_BUILD_OK.txt";

    static _147VR_TableVisualPrefabBuilder()
    {
        EditorApplication.delayCall += Run;
    }

    static void Run()
    {
        EditorApplication.delayCall -= Run;
        if (AssetDatabase.LoadAssetAtPath<TextAsset>(MARKER) != null) return;
        if (!File.Exists(Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Assets/BlenderTest/147VR_Table_WPBSA_12ft_VISUAL_CLEAN_v006.fbx")))
        {
            Debug.LogError("147VR TABLE BUILD BLOCKED: FBX missing: " + FBX);
            return;
        }

        AssetDatabase.ImportAsset(FBX, ImportAssetOptions.ForceUpdate);
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(FBX);
        if (model == null)
        {
            Debug.LogError("147VR TABLE BUILD BLOCKED: FBX model could not be loaded.");
            return;
        }

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
        if (instance == null)
        {
            Debug.LogError("147VR TABLE BUILD BLOCKED: could not instantiate FBX.");
            return;
        }

        instance.name = "147VR_Table_WPBSA_12ft_VISUAL_CLEAN_v004";
        instance.transform.position = Vector3.zero;
        instance.transform.rotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;

        var colliders = instance.GetComponentsInChildren<Collider>(true);
        var rigidbodies = instance.GetComponentsInChildren<Rigidbody>(true);
        var joints = instance.GetComponentsInChildren<Joint>(true);
        var scripts = instance.GetComponentsInChildren<MonoBehaviour>(true);
        if (colliders.Length > 0 || rigidbodies.Length > 0 || joints.Length > 0 || scripts.Length > 0)
        {
            Debug.LogError($"147VR TABLE BUILD BLOCKED: visual asset contains physics/script components. Colliders={colliders.Length}, Rigidbody={rigidbodies.Length}, Joints={joints.Length}, Scripts={scripts.Length}");
            Object.DestroyImmediate(instance);
            return;
        }

        var prefab = PrefabUtility.SaveAsPrefabAsset(instance, PREFAB, out bool success);
        Object.DestroyImmediate(instance);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (!success || prefab == null)
        {
            Debug.LogError("147VR TABLE BUILD BLOCKED: prefab save failed: " + PREFAB);
            return;
        }

        File.WriteAllText(Path.Combine(Directory.GetParent(Application.dataPath).FullName, MARKER),
            "147VR visual prefab build PASS\nSource: " + FBX + "\nPrefab: " + PREFAB + "\nPhysics components: 0\nScene replacement: NOT PERFORMED\n");
        AssetDatabase.Refresh();
        Debug.Log("147VR_TABLE_VISUAL_V004_BUILD_PASS | Physics untouched | Scene replacement NOT PERFORMED | " + PREFAB);
    }
}
