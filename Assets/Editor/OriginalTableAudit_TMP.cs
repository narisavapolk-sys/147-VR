using UnityEditor;
using UnityEngine;
using System;
using System.IO;
using System.Linq;

public static class OriginalTableAudit_TMP
{
    [MenuItem("Tools/147VR/Audit Original Table Assets TMP")]
    public static void Run()
    {
        string outPath = "OriginalTableAudit_TMP.txt";
        using var w = new StreamWriter(outPath, false);
        string[] paths = {
            "Assets/AAA/ImportedSnooker/147VR_Table_WPBSA_Base_v001.fbx",
            "Assets/AAA/ImportedSnooker/SnookerTable_Hi3D.fbx",
            "Assets/AAA/ImportedSnooker/Prefab_WPBSA_12Foot_Snooker.prefab"
        };
        foreach (var p in paths) Audit(p, w);
        AssetDatabase.SaveAssets();
        Debug.Log("AUDIT_DONE " + outPath);
    }

    static void Audit(string path, StreamWriter w)
    {
        w.WriteLine("=== " + path + " ===");
        if (!File.Exists(path)) { w.WriteLine("MISSING"); return; }
        var assets = AssetDatabase.LoadAllAssetsAtPath(path);
        var meshes = assets.OfType<Mesh>().ToArray();
        var gos = assets.OfType<GameObject>().ToArray();
        w.WriteLine($"SubAssets={assets.Length} GameObjects={gos.Length} Meshes={meshes.Length}");
        foreach (var go in gos.Take(20)) w.WriteLine("GO: " + go.name);
        foreach (var m in meshes.Take(100))
            w.WriteLine($"MESH: {m.name} verts={m.vertexCount} tris={m.triangles.Length/3} bounds={m.bounds.size}");

        if (path.EndsWith(".prefab"))
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try {
                var transforms = root.GetComponentsInChildren<Transform>(true);
                w.WriteLine("HierarchyTransforms=" + transforms.Length);
                foreach (var t in transforms) {
                    var mr = t.GetComponent<MeshRenderer>();
                    var mf = t.GetComponent<MeshFilter>();
                    if (mr || mf) w.WriteLine($"NODE: {t.name} mesh={(mf && mf.sharedMesh ? mf.sharedMesh.name : "-")} renderer={(mr?"Y":"-")}");
                }
            } finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        w.WriteLine();
    }
}
