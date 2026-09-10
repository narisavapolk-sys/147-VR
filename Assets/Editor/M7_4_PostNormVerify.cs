using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.IO;
using System.Text;
using System.Linq;

public static class M7_4_PostNormVerify
{
    public static void Run()
    {
        const string scenePath="Assets/Scenes/147VR_MainScene.unity";
        const string visualPrefab="Assets/BlenderTest/147VR_Table_WPBSA_12ft_VISUAL_CLEAN_v004.prefab";
        const string visualFbx="Assets/BlenderTest/147VR_Table_WPBSA_12ft_VISUAL_CLEAN_v006.fbx";
        var sb=new StringBuilder(); sb.AppendLine("M7.4 POST NORMALIZATION VERIFICATION");
        var mi=AssetImporter.GetAtPath(visualFbx) as ModelImporter;
        sb.AppendLine($"FBX_IMPORT globalScale={mi?.globalScale} useFileScale={mi?.useFileScale}");
        var fbxObjs=AssetDatabase.LoadAllAssetsAtPath(visualFbx).OfType<Mesh>().ToArray();
        sb.AppendLine($"FBX_MESHES={fbxObjs.Length}");
        foreach(var m in fbxObjs) sb.AppendLine($"FBX_MESH {m.name} size={m.bounds.size} center={m.bounds.center}");
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(visualPrefab);
        if(prefab!=null){var inst=PrefabUtility.InstantiatePrefab(prefab) as GameObject; if(inst!=null){var rs=inst.GetComponentsInChildren<Renderer>(true); Bounds b=new Bounds(); bool has=false; foreach(var r in rs){if(!has){b=r.bounds;has=true;}else b.Encapsulate(r.bounds);} sb.AppendLine($"V004_PREFAB renderers={rs.Length} boundsCenter={b.center} boundsSize={b.size} rootScale={inst.transform.localScale}"); Object.DestroyImmediate(inst);}}
        var scene=EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        var roots=scene.GetRootGameObjects(); sb.AppendLine($"SCENE_ROOTS={roots.Length}");
        foreach(var go in roots.Where(g=>g.name.Contains("147VR_Table")||g.name.Contains("Visual")||g.name.Contains("Physics Table")||g.name.Contains("Bed_Collider"))) sb.AppendLine($"ROOT name={go.name} pos={go.transform.position} rot={go.transform.eulerAngles} scale={go.transform.lossyScale}");
        var allBoxes=Object.FindObjectsByType<BoxCollider>(FindObjectsInactive.Include,FindObjectsSortMode.None); sb.AppendLine($"ALL_BOX_COLLIDERS={allBoxes.Length}"); foreach(var c in allBoxes.Where(c=>c.enabled).Take(80)) sb.AppendLine($"ACTIVE_BOX name={c.name} size={c.size} center={c.center} worldScale={c.transform.lossyScale} worldPos={c.transform.position} parent={c.transform.parent?.name}"); var beds=allBoxes.Where(c=>c.name=="Bed_Collider").ToArray();
        sb.AppendLine($"BED_COLLIDERS={beds.Length}"); foreach(var c in beds) sb.AppendLine($"BED size={c.size} center={c.center} worldScale={c.transform.lossyScale} enabled={c.enabled} parent={c.transform.parent?.name}");
        var pockets=Object.FindObjectsByType<SnookerPocketCatch>(FindObjectsInactive.Include,FindObjectsSortMode.None); sb.AppendLine($"POCKET_CATCH_COMPONENTS={pockets.Length}"); foreach(var x in pockets) sb.AppendLine($"POCKET name={x.name} enabled={x.enabled} pos={x.transform.position} scale={x.transform.lossyScale} parent={x.transform.parent?.name}");
        File.WriteAllText("Docs/M7_4_POST_NORMALIZATION_VERIFY.txt",sb.ToString()); Debug.Log(sb.ToString());
        EditorSceneManager.CloseScene(scene,false);
        EditorApplication.Exit(0);
    }
}

