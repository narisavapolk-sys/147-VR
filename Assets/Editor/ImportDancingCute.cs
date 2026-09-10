using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Imports "Cute Girl Dancing.fbx" into the project, configures Humanoid rig,
/// and reports the imported animation clips + skinned mesh state.
/// </summary>
public static class ImportDancingCute
{
    const string SrcFbx = @"C:/Users/mongo/UnityProjects/147 VR/SNOOKER   VR pool table/FBX/Cute Girl Original Dancing.fbx";
    const string DstFbx = "Assets/Models/Girls/Cute Girl Dancing.fbx";

    [MenuItem("Tools/147/Import Dancing Cute Girl")]
    public static void Run()
    {
        // Copy FBX if not already there (or always to refresh)
        Directory.CreateDirectory("Assets/Models/Girls");
        File.Copy(SrcFbx, DstFbx, true);
        AssetDatabase.ImportAsset(DstFbx, ImportAssetOptions.ForceUpdate);

        // Configure rig: humanoid
        ModelImporter imp = (ModelImporter)AssetImporter.GetAtPath(DstFbx);
        if (imp != null)
        {
            imp.animationType = ModelImporterAnimationType.Human;
            imp.SaveAndReimport();
        }

        // List imported clips
        var go = AssetDatabase.LoadAssetAtPath<GameObject>(DstFbx);
        if (go != null)
        {
            foreach (var anim in go.GetComponentsInChildren<Animator>())
                Debug.Log("ANIMATOR on " + anim.gameObject.name);
            foreach (var smr in go.GetComponentsInChildren<SkinnedMeshRenderer>())
                Debug.Log($"SKINNED {smr.gameObject.name}: bones={smr.bones.Length} meshBones={smr.sharedMesh.bindposes.Length}");

            // Clips embedded in the FBX model (sub-assets)
            var all = AssetDatabase.LoadAllAssetsAtPath(DstFbx);
            foreach (var a in all)
            {
                if (a is AnimationClip clip)
                    Debug.Log($"CLIP {clip.name}: {clip.length:F2}s ({(int)(clip.length * 60)} frames) legacy={clip.legacy}");
                else if (a is Avatar av)
                    Debug.Log("AVATAR " + av.name + " isHuman=" + av.isHuman);
            }
        }
        else
        {
            Debug.LogError("FBX_IMPORT_FAILED");
        }
        Debug.Log("DONE_IMPORT_DANCING");
    }
}
