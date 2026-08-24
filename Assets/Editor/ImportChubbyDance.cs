using System.IO;
using UnityEditor;
using UnityEngine;

public static class ImportChubbyDance
{
    const string SrcFbx = @"C:/Users/mongo/UnityProjects/147 VR/SNOOKER   VR pool table/FBX/Chubby Girl Dancing.fbx";
    const string DstFbx = "Assets/Models/Girls/Chubby Girl Dancing.fbx";

    [MenuItem("Tools/147/Import Dancing Chubby Girl")]
    public static void Run()
    {
        Directory.CreateDirectory("Assets/Models/Girls");
        File.Copy(SrcFbx, DstFbx, true);
        AssetDatabase.ImportAsset(DstFbx, ImportAssetOptions.ForceUpdate);

        ModelImporter imp = (ModelImporter)AssetImporter.GetAtPath(DstFbx);
        if (imp != null)
        {
            // Chubby (scaled 2.9m->1.57m) fails humanoid mapping; Generic works with same rig
            imp.animationType = ModelImporterAnimationType.Generic;
            imp.SaveAndReimport();
        }

        var go = AssetDatabase.LoadAssetAtPath<GameObject>(DstFbx);
        if (go != null)
        {
            foreach (var anim in go.GetComponentsInChildren<Animator>())
                Debug.Log("ANIMATOR on " + anim.gameObject.name);
            foreach (var smr in go.GetComponentsInChildren<SkinnedMeshRenderer>())
                Debug.Log($"SKINNED {smr.gameObject.name}: bones={smr.bones.Length} meshBones={smr.sharedMesh.bindposes.Length}");
            foreach (var a in AssetDatabase.LoadAllAssetsAtPath(DstFbx))
            {
                if (a is AnimationClip c && !c.name.StartsWith("__preview__") && !c.name.Contains("|"))
                    Debug.Log($"CLIP {c.name}: {c.length:F2}s");
                else if (a is Avatar av)
                    Debug.Log("AVATAR " + av.name + " isHuman=" + av.isHuman);
            }
        }
        else
        {
            Debug.LogError("FBX_IMPORT_FAILED");
        }
        Debug.Log("DONE_IMPORT_CHUBBY");
    }
}
