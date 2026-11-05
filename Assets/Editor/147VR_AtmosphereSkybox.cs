#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.IO;
using System.Linq;

// Editor helper for the switchable venue atmospheres. Mirrors the MenuItem-only style of
// 147VR_Skybox.cs. Builds a Skybox/Panoramic material for every .hdr in the Atmospheres folder,
// lets you preview each one in the Scene view, and can drop a pre-wired AtmosphereSkyboxSwitcher
// GameObject into the active scene. The agent never opens Unity — the OWNER runs these menus.
public static class _147VR_AtmosphereSkybox
{
    const string ATMOS_DIR = "Assets/AAA/ImportedSnooker/Venue/Skybox/Atmospheres";
    const string MAT_DIR   = "Assets/AAA/ImportedSnooker/Venue/Skybox/Atmospheres/Mats";

    [MenuItem("147VR/Art/Atmosphere/Build Skybox Materials (from Atmospheres/*.hdr)")]
    public static void BuildMaterials()
    {
        if (!AssetDatabase.IsValidFolder(ATMOS_DIR))
        {
            Debug.LogError("147VR/Atmosphere: folder not found: " + ATMOS_DIR);
            return;
        }
        if (!AssetDatabase.IsValidFolder(MAT_DIR))
            AssetDatabase.CreateFolder(ATMOS_DIR, "Mats");

        var shader = Shader.Find("Skybox/Panoramic");
        if (shader == null)
        {
            Debug.LogError("147VR/Atmosphere: Skybox/Panoramic shader missing");
            return;
        }

        var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { ATMOS_DIR });
        int made = 0;
        foreach (var g in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(g);
            if (!path.ToLowerInvariant().EndsWith(".hdr")) continue;

            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex == null) continue;

            string matName = Path.GetFileNameWithoutExtension(path); // e.g. Atmos_WarmBar_2k
            string matPath = MAT_DIR + "/" + matName + ".mat";

            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, matPath);
            }
            mat.shader = shader;
            mat.SetTexture("_MainTex", tex);
            if (mat.HasProperty("_Tint"))    mat.SetColor("_Tint", Color.white);
            if (mat.HasProperty("_Exposure")) mat.SetFloat("_Exposure", 1f);
            if (mat.HasProperty("_Rotation")) mat.SetFloat("_Rotation", 0f);
            EditorUtility.SetDirty(mat);
            made++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"147VR/Atmosphere: built/updated {made} panoramic skybox material(s) under {MAT_DIR}.");
    }

    [MenuItem("147VR/Art/Atmosphere/Preview — Warm Bar")]
    public static void PreviewWarmBar() => Preview("Atmos_WarmBar_2k");

    [MenuItem("147VR/Art/Atmosphere/Preview — Reception Dinner")]
    public static void PreviewReception() => Preview("Atmos_ReceptionDinner_2k");

    [MenuItem("147VR/Art/Atmosphere/Preview — Country Club")]
    public static void PreviewCountryClub() => Preview("Atmos_CountryClub_2k");

    static void Preview(string matName)
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(MAT_DIR + "/" + matName + ".mat");
        if (mat == null)
        {
            Debug.LogError($"147VR/Atmosphere: '{matName}.mat' not found. Run 'Build Skybox Materials' first.");
            return;
        }
        RenderSettings.skybox = mat;
        DynamicGI.UpdateEnvironment();
        EditorSceneManager.MarkAllScenesDirty();
        Debug.Log("147VR/Atmosphere: previewing " + matName);
    }

    [MenuItem("147VR/Art/Atmosphere/Add Switcher GameObject (auto-wire materials)")]
    public static void AddSwitcher()
    {
        if (!AssetDatabase.IsValidFolder(MAT_DIR))
        {
            Debug.LogError("147VR/Atmosphere: no Mats folder. Run 'Build Skybox Materials' first.");
            return;
        }

        var mats = AssetDatabase.FindAssets("t:Material", new[] { MAT_DIR })
            .Select(g => AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(m => m != null)
            .OrderBy(m => m.name)
            .ToArray();

        if (mats.Length == 0)
        {
            Debug.LogError("147VR/Atmosphere: no atmosphere materials found. Run 'Build Skybox Materials' first.");
            return;
        }

        var go = new GameObject("AtmosphereSkyboxSwitcher");
        var sw = go.AddComponent<AtmosphereSkyboxSwitcher>();

        var so = new SerializedObject(sw);
        var arr = so.FindProperty("atmospheres");
        arr.arraySize = mats.Length;
        for (int i = 0; i < mats.Length; i++)
        {
            var el = arr.GetArrayElementAtIndex(i);
            el.FindPropertyRelative("label").stringValue = NiceName(mats[i].name);
            el.FindPropertyRelative("skyboxMat").objectReferenceValue = mats[i];
        }
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkAllScenesDirty();
        Selection.activeGameObject = go;

        Debug.Log($"147VR/Atmosphere: added AtmosphereSkyboxSwitcher with {mats.Length} atmospheres. " +
                  "Call SetAtmosphere(i) / Next() / Previous() from your UI.");
    }

    static string NiceName(string matName)
    {
        var s = matName.Replace("Atmos_", "").Replace("_2k", "").Replace("_1k", "").Replace("_4k", "");
        // Insert spaces before capital letters: Atmos_WarmBar -> Warm Bar
        s = System.Text.RegularExpressions.Regex.Replace(s, "(?<!^)(?=[A-Z])", " ").Trim();
        return s;
    }
}
#endif
