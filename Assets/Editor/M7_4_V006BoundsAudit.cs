using UnityEditor;
using UnityEngine;
using System.IO;
using System.Text;
public static class M7_4_V006BoundsAudit
{
    [InitializeOnLoadMethod] static void Run()
    {
        const string marker = "Library/M7_4_V006BoundsAudit.done";
        if (File.Exists(marker)) return;
        Directory.CreateDirectory("Library"); File.WriteAllText(marker,"1");
        var sb=new StringBuilder(); sb.AppendLine("M7.4 V006 BOUNDS AUDIT");
        foreach(var path in new[]{"Assets/AAA/ImportedSnooker/147VR_Table_WPBSA_Visual_Clean_v006.fbx","Assets/AAA/ImportedSnooker/147VR_Table_WPBSA_Visual_Clean_v004.fbx"})
        {
            sb.AppendLine("ASSET="+path); var objs=AssetDatabase.LoadAllAssetsAtPath(path); int n=0; Bounds t=new Bounds(); bool h=false;
            foreach(var o in objs) if(o is Mesh m){ n++; var b=m.bounds; sb.AppendLine($"MESH {m.name} center={b.center} size={b.size}"); if(!h){t=b;h=true;} else {t.Encapsulate(b.min);t.Encapsulate(b.max);} }
            sb.AppendLine($"MESH_COUNT={n} LOCAL_TOTAL_CENTER={t.center} LOCAL_TOTAL_SIZE={t.size}");
        }
        var go=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/AAA/ImportedSnooker/Prefabs/147VR_Table_WPBSA_12ft_VISUAL_CLEAN_v006.prefab");
        if(go){var root=PrefabUtility.InstantiatePrefab(go) as GameObject;if(root){var b=GetBounds(root);sb.AppendLine($"PREFAB_WORLD_BOUNDS_CENTER={b.center} SIZE={b.size}");Object.DestroyImmediate(root);}}
        File.WriteAllText("Docs/M7_4_V006_BOUNDS_AUDIT.txt",sb.ToString()); Debug.Log(sb.ToString());
    }
    static Bounds GetBounds(GameObject root){var rs=root.GetComponentsInChildren<Renderer>(true);Bounds b=new Bounds(root.transform.position,Vector3.zero);bool h=false;foreach(var r in rs){if(!h){b=r.bounds;h=true;}else b.Encapsulate(r.bounds);}return b;}
}
