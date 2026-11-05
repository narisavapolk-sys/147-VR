#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// One-click club lighting setup for the clean scene: warm HDRI skybox (reflections+ambient),
// a spotlight over the table, a URP Volume (ACES tonemap + bloom + color + vignette), and a
// brightness fix on the Table_Mahogany materials. MenuItem-only; the OWNER runs it.
public static class _147VR_ClubLighting
{
    const string HDRI = "Assets/AAA/ImportedSnooker/Venue/Skybox/Atmospheres/Atmos_WarmBar_2k.hdr";
    const string ATMOS_DIR = "Assets/AAA/ImportedSnooker/Venue/Skybox/Atmospheres";

    [MenuItem("147VR/Art/Club/Setup Club Lighting (HDRI + table light + post + fix mahogany)")]
    public static void Setup()
    {
        ApplySkybox();
        AddTableLight();
        CreateVolume();
        FixMahogany();
        EditorSceneManager.MarkAllScenesDirty();
        AssetDatabase.SaveAssets();
        Debug.Log("147VR/Lighting: done. Now enable 'Post Processing' in the Scene view (top-left dropdown) or check the Game view.");
    }

    static void ApplySkybox()
    {
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(HDRI);
        if (tex == null) { Debug.LogWarning("147VR/Lighting: HDRI not found: " + HDRI); return; }
        Shader shader = Shader.Find("Skybox/Panoramic");
        if (shader == null) { Debug.LogWarning("147VR/Lighting: Skybox/Panoramic missing."); return; }

        string matPath = ATMOS_DIR + "/Skybox_WarmBar.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null) { mat = new Material(shader); AssetDatabase.CreateAsset(mat, matPath); }
        mat.shader = shader;
        mat.SetTexture("_MainTex", tex);
        if (mat.HasProperty("_Exposure")) mat.SetFloat("_Exposure", 1f);
        EditorUtility.SetDirty(mat);
        RenderSettings.skybox = mat;
        DynamicGI.UpdateEnvironment();
    }

    static void AddTableLight()
    {
        Vector3 center = Vector3.zero;
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
            if (t.name == "TABLE SURFACE") { center = t.position; break; }

        GameObject go = new GameObject("Club_TableLight");
        Light l = go.AddComponent<Light>();
        l.type = LightType.Spot;
        l.color = new Color(1f, 0.95f, 0.85f);
        l.intensity = 3.5f;
        l.range = 12f;
        l.spotAngle = 80f;
        l.shadows = LightShadows.Soft;
        go.transform.position = center + new Vector3(0f, 4.5f, 0f);
        go.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // point straight down
    }

    static void CreateVolume()
    {
        string dir = AssetDatabase.IsValidFolder("Assets/Settings") ? "Assets/Settings" : "Assets";
        string path = dir + "/ClubVolumeProfile.asset";
        if (AssetDatabase.LoadAssetAtPath<VolumeProfile>(path) != null) AssetDatabase.DeleteAsset(path);

        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(profile, path);

        Tonemapping tm = profile.Add<Tonemapping>(true); tm.mode.Override(TonemappingMode.ACES);
        Bloom bloom = profile.Add<Bloom>(true); bloom.intensity.Override(0.25f); bloom.threshold.Override(1.0f);
        ColorAdjustments ca = profile.Add<ColorAdjustments>(true); ca.postExposure.Override(0.25f); ca.saturation.Override(6f);
        Vignette vg = profile.Add<Vignette>(true); vg.intensity.Override(0.2f);

        foreach (VolumeComponent c in new VolumeComponent[] { tm, bloom, ca, vg })
        {
            EditorUtility.SetDirty(c);
            AssetDatabase.AddObjectToAsset(c, profile);
        }
        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();

        Volume vol = null;
        foreach (var v in Object.FindObjectsByType<Volume>(FindObjectsInactive.Include, FindObjectsSortMode.None)) { vol = v; break; }
        if (vol == null) vol = new GameObject("Global Volume").AddComponent<Volume>();
        vol.isGlobal = true;
        vol.priority = 10f;
        vol.sharedProfile = profile;
    }

    static void FixMahogany()
    {
        string[] guids = AssetDatabase.FindAssets("t:Material Table_Mahogany");
        int n = 0;
        foreach (var g in guids)
        {
            Material m = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g));
            if (m == null) continue;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", new Color(0.42f, 0.20f, 0.12f, 1f));
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.65f);
            EditorUtility.SetDirty(m); n++;
        }
        Debug.Log("147VR/Lighting: brightened " + n + " Table_Mahogany material asset(s)." +
                  (n == 0 ? " (0 found = materials are embedded in the FBX; extract them via the model's Materials tab > Extract, then re-run.)" : ""));
    }
}
#endif
