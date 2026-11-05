#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Imports the Miracle Pool "vintage" room (loose OBJ meshes + PNG textures) into the scene,
// assigns heuristic materials, and scales it up so the longest side ~ TARGET_LONGEST metres.
// MenuItem-only; the OWNER runs it. Saves a reusable prefab too.
public static class _147VR_VintageRoomImport
{
    const string MESHDIR = "Assets/AAA/ClubVenue/MiraclePool_vintage/meshes";
    const string TEXDIR  = "Assets/AAA/ClubVenue/MiraclePool_vintage/textures";
    const string MATDIR  = "Assets/AAA/ClubVenue/Mats";
    const string OUT     = "Assets/AAA/ClubVenue/VintageRoom_Scaled.prefab";
    const float TARGET_LONGEST = 18f;

    static readonly Dictionary<string, Material> _mats = new Dictionary<string, Material>();

    [MenuItem("147VR/Art/Club/Import Vintage Room (Miracle Pool, scaled)")]
    public static void Import()
    {
        EnsureFolder(MATDIR);
        GameObject parent = new GameObject("VintageRoom");
        var rends = new List<Renderer>();

        foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { MESHDIR }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string clean = Regex.Replace(Path.GetFileNameWithoutExtension(path), @"^\d+_", "");
            Mesh mesh = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Mesh>().FirstOrDefault();
            if (mesh == null) continue;
            GameObject go = new GameObject(clean);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = MatFor(clean);
            go.transform.SetParent(parent.transform, false);
            rends.Add(go.GetComponent<Renderer>());
        }

        if (rends.Count == 0) { Debug.LogError("147VR/VintageRoom: no meshes found (import OBJ first?)"); UnityEngine.Object.DestroyImmediate(parent); return; }

        Bounds b = rends[0].bounds;
        foreach (var r in rends) b.Encapsulate(r.bounds);
        foreach (Transform t in parent.transform) t.position -= b.center;
        float longest = Mathf.Max(b.size.x, b.size.z);
        float s = longest > 0.01f ? TARGET_LONGEST / longest : 1f;
        parent.transform.localScale = Vector3.one * s;

        // lift so the lowest point sits on y=0
        b = parent.GetComponentInChildren<Renderer>().bounds;
        foreach (var r in parent.GetComponentsInChildren<Renderer>()) b.Encapsulate(r.bounds);
        parent.transform.position += new Vector3(0f, -b.min.y, 0f);

        if (File.Exists(OUT)) AssetDatabase.DeleteAsset(OUT);
        PrefabUtility.SaveAsPrefabAsset(parent, OUT);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = parent;
        Debug.Log("147VR/VintageRoom: imported " + rends.Count + " parts, scaled x" + s.ToString("F2") + " (longest->" + TARGET_LONGEST + "m), saved " + OUT);
    }

    static Material MatFor(string name)
    {
        string tex = PickTex(name);
        string key = name.ToLower();
        if (tex == null) key = "none";
        if (_mats.TryGetValue(key, out var m) && m != null) return m;

        m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        if (tex != null)
        {
            var t = AssetDatabase.LoadAssetAtPath<Texture2D>(tex);
            if (t != null) m.SetTexture("_BaseMap", t);
        }
        else m.SetColor("_BaseColor", new Color(0.4f, 0.3f, 0.22f));
        m.SetFloat("_Smoothness", 0.4f);

        string mp = MATDIR + "/VRM_" + Sanitize(key) + ".mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(mp);
        if (existing != null) { _mats[key] = existing; return existing; }
        AssetDatabase.CreateAsset(m, mp);
        _mats[key] = m;
        return m;
    }

    static string PickTex(string n)
    {
        string l = n.ToLower();
        if (l.Contains("floor") || l.Contains("parquet")) return Find("Parquette");
        if (l.Contains("carpet") || l.Contains("rug")) return Find("Carpet");
        if (l.Contains("book") || l.Contains("shelf") || l.Contains("storage") || l.Contains("closet") || l.Contains("wardrobe") || l.Contains("komod")) return Find("Books");
        if (l.Contains("sofa") || l.Contains("fotel") || l.Contains("pouffle") || l.Contains("chair") || l.Contains("leather")) return Find("Leather");
        if (l.Contains("picture") || l.Contains("frame")) return Find("Picture_eiffel");
        if (l.Contains("metal") || l.Contains("lamp") || l.Contains("phonograph") || l.Contains("brass")) return Find("Metal_antique");
        if (l.Contains("wall") || l.Contains("wood") || l.Contains("column") || l.Contains("window") || l.Contains("table") || l.Contains("door") || l.Contains("bed")) return Find("Wood_warm");
        return Find("Wood_light");
    }
    static string Find(string part)
    {
        foreach (var f in Directory.GetFiles(TEXDIR, "*.png"))
            if (Path.GetFileName(f).IndexOf(part, System.StringComparison.OrdinalIgnoreCase) >= 0)
                return f.Replace('\\', '/');
        return null;
    }
    static string Sanitize(string s) => Regex.Replace(s, @"[^a-zA-Z0-9_]", "_");
    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string[] parts = path.Split('/'); string cur = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = cur + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(cur, parts[i]);
            cur = next;
        }
    }
}
#endif
