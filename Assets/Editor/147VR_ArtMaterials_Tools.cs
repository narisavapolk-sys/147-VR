#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.Collections.Generic;

// One-click, self-contained V008 table setup:
//  opens original MainScene -> saves AS a non-destructive test scene -> places the
//  optimized v008 game FBX at the old table's slot -> builds+applies URP/Lit materials
//  (FELT textured) -> adds a DARC-style 3-light rig -> disables old visual (physics kept)
//  -> saves the test scene. MenuItem-only (no auto-run).
public static class _147VR_ArtMaterials_Tools
{
    const string FBX = "Assets/AAA/ImportedSnooker/V008_OPT/147VR_Table_V008_OPT_GAME.fbx";
    const string MatDir = "Assets/AAA/ImportedSnooker/Materials/V008";
    const string FeltTex = "Assets/AAA/ImportedSnooker/Textures/Green_Felt_Texture_V007.png";
    const string OLD_TABLE = "Prefab_WPBSA_12Foot_Snooker";
    const string ORIG_SCENE = "Assets/Scenes/147VR_MainScene.unity";
    const string TEST_SCENE = "Assets/Scenes/147VR_MainScene_V008_TEST.unity";

    class MatDef { public float r, g, b, metal, smooth; public bool felt;
        public MatDef(float r,float g,float b,float m,float s,bool f){this.r=r;this.g=g;this.b=b;this.metal=m;this.smooth=s;this.felt=f;} }

    static readonly Dictionary<string, MatDef> Mats = new Dictionary<string, MatDef>
    {
        { "0I_Gold_mqm",              new MatDef(0.55f,0.30f,0.055f, 0.82f,0.78f,false) },
        { "147VR_Pocket_Brass",       new MatDef(0.55f,0.30f,0.055f, 0.82f,0.76f,false) },
        { "147VR_Pocket_Leather",     new MatDef(0.05f,0.03f,0.02f,  0.00f,0.57f,false) },
        { "147VR_Pocket_Leather.001", new MatDef(0.05f,0.03f,0.02f,  0.00f,0.57f,false) },
        { "147VR_Pocket_Net",         new MatDef(0.72f,0.66f,0.52f,  0.00f,0.25f,false) },
        { "147VR_Pocket_Stitch",      new MatDef(0.30f,0.20f,0.05f,  0.00f,0.50f,false) },
        { "147VR_Pocket_Throat",      new MatDef(0.018f,0.012f,0.009f,0.00f,0.30f,false) },
        { "147VR_Pocket_Throat_Dark", new MatDef(0.004f,0.002f,0.001f,0.00f,0.42f,false) },
        { "Table Mahogany",           new MatDef(0.095f,0.018f,0.012f,0.00f,0.73f,false) },
        { "Table Mahogany.001",       new MatDef(0.16f,0.035f,0.018f,0.00f,0.70f,false) },
        { "cw-FABRIC-01",             new MatDef(0.0495f,0.0495f,0.0495f,0.0f,0.79f,false) },
        { "sy_dark_matte",            new MatDef(0.02f,0.02f,0.02f,  0.00f,0.10f,false) },
        { "4C_Leather_Black_mqm",     new MatDef(0.02f,0.015f,0.01f, 0.00f,0.30f,false) },
        { "1B_Concrete_Smooth_mqm",   new MatDef(0.35f,0.35f,0.36f,  0.00f,0.05f,false) },
        { "Black",                    new MatDef(0.006f,0.006f,0.006f,0.0f,0.78f,false) },
        { "Dots Stroke",              new MatDef(0.8f,0.8f,0.8f,     0.00f,0.50f,false) },
        { "Material",                 new MatDef(0.8f,0.8f,0.8f,     0.00f,0.50f,false) },
        { "Material.001",             new MatDef(0.8f,0.8f,0.8f,     0.00f,0.50f,false) },
        { "FELT",                     new MatDef(1f,1f,1f,           0.00f,0.28f,true ) },
    };

    [MenuItem("147VR/Art/Setup V008 Table (full: open+save+place+mat+light)")]
    public static void SetupV008()
    {
        // 1) open original, 2) save-as test scene (non-destructive)
        var opened = EditorSceneManager.OpenScene(ORIG_SCENE, OpenSceneMode.Single);
        if (!opened.IsValid()) { Debug.LogError("147VR/Setup: cannot open " + ORIG_SCENE); return; }
        EditorSceneManager.SaveScene(opened, TEST_SCENE);
        var scene = EditorSceneManager.GetActiveScene();

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FBX);
        if (prefab == null) { Debug.LogError("147VR/Setup: v008 FBX not found at " + FBX); return; }
        var lit = Shader.Find("Universal Render Pipeline/Lit");
        if (lit == null) { Debug.LogError("147VR/Setup: URP/Lit shader not found."); return; }
        var felt = AssetDatabase.LoadAssetAtPath<Texture2D>(FeltTex);
        if (felt == null) Debug.LogWarning("147VR/Setup: FELT tex missing at " + FeltTex);

        GameObject old = null;
        foreach (var r in scene.GetRootGameObjects()) { old = FindDeep(r.transform, OLD_TABLE); if (old != null) break; }

        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.name = "147VR_Table_V008_OPT_VISUAL";
        if (old != null)
        {
            go.transform.position = old.transform.position;
            go.transform.rotation = old.transform.rotation;
            go.transform.localScale = old.transform.localScale;
        }
        else go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

        int applied = 0;
        foreach (var r in go.GetComponentsInChildren<Renderer>(true))
        {
            var sm = r.sharedMaterials;
            for (int i = 0; i < sm.Length; i++)
            {
                if (sm[i] == null) continue;
                MatDef p;
                if (!Mats.TryGetValue(sm[i].name, out p)) continue;
                var mat = GetOrCreate(lit, sm[i].name, p, felt);
                if (mat != null) { sm[i] = mat; applied++; }
            }
            r.sharedMaterials = sm;
        }

        if (old != null) old.SetActive(false);
        SetupLights(go.transform.position);

        Selection.activeGameObject = go;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log(string.Format("147VR/Setup: saved '{0}' with V008 placed, {1} material slots, felt={2}, old visual disabled (physics kept).",
            TEST_SCENE, applied, felt != null ? "OK" : "MISSING"));
    }

    [MenuItem("147VR/Art/Cleanup Strays + Place Venue")]
    public static void CleanupAndVenue()
    {
        var scene = EditorSceneManager.GetActiveScene();
        int removed = 0;
        var table = GameObject.Find("147VR_Table_V008_OPT_VISUAL");
        if (table != null)
        {
            if (PrefabUtility.IsPartOfAnyPrefab(table))
                PrefabUtility.UnpackPrefabInstance(table, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            var kill = new List<GameObject>();
            foreach (var t in table.GetComponentsInChildren<Transform>(true))
                if (t != null && t.name.StartsWith("Cube")) kill.Add(t.gameObject);
            foreach (var g in kill) if (g != null) { Object.DestroyImmediate(g); removed++; }
        }
        else Debug.LogWarning("147VR/Cleanup: table '147VR_Table_V008_OPT_VISUAL' not found; run Setup V008 first.");

        GameObject venueGo = null;
        if (GameObject.Find("147VR_VENUE_CRUCIBLE") == null)
        {
            var venue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/AAA/ImportedSnooker/Venue/147VR_Venue_Crucible.fbx");
            if (venue != null)
            {
                venueGo = (GameObject)PrefabUtility.InstantiatePrefab(venue);
                venueGo.name = "147VR_VENUE_CRUCIBLE";
                venueGo.transform.position = Vector3.zero;
            }
            else Debug.LogWarning("147VR/Cleanup: venue FBX not found in Assets");
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log(string.Format("147VR/Cleanup: removed {0} stray Cube(s); venue={1}. Scene saved.",
            removed, venueGo != null ? "placed" : "skipped/present"));
    }

    [MenuItem("147VR/Audio/Configure SFX Import (Vorbis + Streaming)")]
    public static void ConfigureSfxImport()
    {
        string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Audio/SFX" });
        int n = 0;
        foreach (var g in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(g);
            var imp = AssetImporter.GetAtPath(path) as AudioImporter;
            if (imp == null) continue;
            var so = imp.defaultSampleSettings;
            so.compressionFormat = AudioCompressionFormat.Vorbis;
            so.quality = 0.7f;
            bool big = new System.IO.FileInfo(path).Length > 150 * 1024;
            so.loadType = big ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
            imp.defaultSampleSettings = so;
            imp.SaveAndReimport();
            n++;
        }
        AssetDatabase.SaveAssets();
        Debug.Log("147VR/Audio: configured " + n + " SFX clips (Vorbis; >150KB = Streaming).");
    }

    static void SetupLights(Vector3 c)
    {
        float top = 1.6f;
        CreateLight("DARC_Key", LightType.Directional, new Color(1f,0.93f,0.82f), 1.4f, new Vector3(50f,-35f,0f), c, 0f);
        CreateLight("DARC_RimL", LightType.Point, new Color(0.6f,0.75f,1f), 2.2f, Vector3.zero, c + new Vector3(-2.4f, top, 0f), 6f);
        CreateLight("DARC_RimR", LightType.Point, new Color(1f,0.6f,0.35f), 2.0f, Vector3.zero, c + new Vector3(2.4f, top, 0f), 6f);
        CreateLight("DARC_Fill", LightType.Point, new Color(0.9f,0.95f,1f), 0.8f, Vector3.zero, c + new Vector3(0f, top+0.6f, 2.2f), 8f);
    }

    static void CreateLight(string name, LightType t, Color col, float inten, Vector3 euler, Vector3 pos, float range)
    {
        var go = new GameObject(name);
        go.transform.position = pos;
        go.transform.eulerAngles = euler;
        var l = go.AddComponent<Light>();
        l.type = t; l.color = col; l.intensity = inten; l.shadows = LightShadows.Soft;
        if (t != LightType.Directional && range > 0f) l.range = range;
    }

    static Material GetOrCreate(Shader lit, string name, MatDef p, Texture feltTex)
    {
        EnsureDir();
        string path = MatDir + "/" + name.Replace(" ", "_").Replace(".", "_") + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null) { mat = new Material(lit); AssetDatabase.CreateAsset(mat, path); }
        mat.shader = lit;
        mat.SetColor("_BaseColor", new Color(p.r, p.g, p.b, 1f));
        mat.SetFloat("_Metallic", p.metal);
        mat.SetFloat("_Smoothness", p.smooth);
        if (p.felt && feltTex != null) mat.SetTexture("_BaseMap", feltTex);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static void EnsureDir()
    {
        if (!AssetDatabase.IsValidFolder("Assets/AAA/ImportedSnooker/Materials"))
            AssetDatabase.CreateFolder("Assets/AAA/ImportedSnooker", "Materials");
        if (!AssetDatabase.IsValidFolder(MatDir))
            AssetDatabase.CreateFolder("Assets/AAA/ImportedSnooker/Materials", "V008");
    }

    static GameObject FindDeep(Transform t, string name)
    {
        if (t.name == name) return t.gameObject;
        foreach (Transform c in t) { var r = FindDeep(c, name); if (r != null) return r; }
        return null;
    }
}
#endif
