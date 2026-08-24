using UnityEditor;
using UnityEngine;

public static class DebugChubbyImport
{
    const string FbxPath = "Assets/Models/Girls/Chubby Girl Dancing.fbx";

    [MenuItem("Tools/147/Debug Chubby Import")]
    public static void Run()
    {
        var all = AssetDatabase.LoadAllAssetsAtPath(FbxPath);
        foreach (var a in all)
        {
            Debug.Log($"ASSET {a.GetType().Name}: {a.name}");
        }

        var imp = (ModelImporter)AssetImporter.GetAtPath(FbxPath);
        if (imp != null)
        {
            Debug.Log("animationType=" + imp.animationType);
            Debug.Log("avatarSetup=" + imp.avatarSetup);
            var desc = imp.humanDescription;
            Debug.Log("human bones count=" + desc.human.Length);
            foreach (var h in desc.human)
                Debug.Log($"  HUMAN {h.humanName} -> bone '{h.boneName}'");
            Debug.Log("skeleton bones=" + desc.skeleton.Length);
        }
        Debug.Log("DONE_DEBUG_CHUBBY");
    }
}
