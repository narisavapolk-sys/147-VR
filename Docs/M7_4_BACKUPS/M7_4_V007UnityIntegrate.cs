using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class M7_4_V007UnityIntegrate
{
    const string Fbx = "Assets/AAA/ImportedSnooker/147VR_Table_WPBSA_Visual_Clean_v007.fbx";
    const string Prefab = "Assets/AAA/ImportedSnooker/147VR_Table_WPBSA_12ft_VISUAL_CLEAN_v007.prefab";
    const string ScenePath = "Assets/Scenes/147VR_MainScene.unity";
    const string OldName = "147VR_Table_WPBSA_12ft_VISUAL_CLEAN_v004";

    public static void Run()
    {
        AssetDatabase.ImportAsset(Fbx, ImportAssetOptions.ForceUpdate);
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(Fbx);
        if (!model) throw new Exception("V007 FBX not found");
        var temp = PrefabUtility.InstantiatePrefab(model) as GameObject;
        if (!temp) throw new Exception("V007 FBX instantiate failed");
        PrefabUtility.UnpackPrefabInstance(temp, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        bool ok; PrefabUtility.SaveAsPrefabAsset(temp, Prefab, out ok);
        UnityEngine.Object.DestroyImmediate(temp);
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        if (!ok) throw new Exception("V007 prefab save failed");

        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var old = FindByName(scene, OldName);
        if (!old) throw new Exception("Old visual table instance not found");
        var parent = old.transform.parent;
        var pos = old.transform.localPosition; var rot = old.transform.localRotation; var scale = old.transform.localScale;
        var bed = FindByName(scene, "Bed_Collider");
        if (!bed) throw new Exception("Bed_Collider not found");
        bed.transform.SetParent(null, true);
        var replacement = PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab), scene) as GameObject;
        replacement.transform.SetParent(parent, false); replacement.transform.localPosition=pos; replacement.transform.localRotation=rot; replacement.transform.localScale=scale; replacement.name=OldName; replacement.SetActive(false);
        bed.transform.SetParent(replacement.transform, false);
        UnityEngine.Object.DestroyImmediate(old);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("M7.4 V007 UNITY INTEGRATION PASS: prefab + MainScene swapped; Bed_Collider preserved.");
        EditorApplication.Exit(0);
    }

    static GameObject FindByName(Scene scene, string name)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            var hit = Find(root.transform, name);
            if (hit) return hit.gameObject;
        }
        return null;
    }
    static Transform Find(Transform t, string name)
    {
        if (t.name == name) return t;
        foreach (Transform c in t) { var hit=Find(c,name); if(hit) return hit; }
        return null;
    }
}
