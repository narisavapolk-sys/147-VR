#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Apply a panoramic arena image as the scene Skybox -> continuous environment + ceiling all
// around (fixes patchy billboards + gray gaps + open ceiling). MenuItem-only.
public static class _147VR_Skybox
{
    const string TEX = "Assets/AAA/ImportedSnooker/Venue/Skybox/arena_panorama.png";
    const string MAT = "Assets/AAA/ImportedSnooker/Venue/Skybox/ArenaSkybox.mat";

    [MenuItem("147VR/Art/Apply Arena Skybox (panorama)")]
    public static void Apply()
    {
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(TEX);
        if (tex == null) { Debug.LogError("147VR/Skybox: panorama not found at " + TEX); return; }

        var shader = Shader.Find("Skybox/Panoramic");
        if (shader == null) { Debug.LogError("147VR/Skybox: Skybox/Panoramic shader missing"); return; }

        var mat = AssetDatabase.LoadAssetAtPath<Material>(MAT);
        if (mat == null) { mat = new Material(shader); AssetDatabase.CreateAsset(mat, MAT); }
        mat.shader = shader;
        mat.SetTexture("_MainTex", tex);
        if (mat.HasProperty("_Tint")) mat.SetColor("_Tint", Color.white);
        if (mat.HasProperty("_Exposure")) mat.SetFloat("_Exposure", 1f);
        if (mat.HasProperty("_Rotation")) mat.SetFloat("_Rotation", 0f);
        EditorUtility.SetDirty(mat);

        RenderSettings.skybox = mat;
        DynamicGI.UpdateEnvironment();
        EditorSceneManager.MarkAllScenesDirty();
        Debug.Log("147VR/Skybox: applied arena_panorama as panoramic skybox. Adjust _Rotation on ArenaSkybox.mat to align the stairs side.");
    }

    [MenuItem("147VR/Art/Clear Skybox")]
    public static void Clear()
    {
        RenderSettings.skybox = null;
        DynamicGI.UpdateEnvironment();
        EditorSceneManager.MarkAllScenesDirty();
        Debug.Log("147VR/Skybox: cleared (back to default sky).");
    }
}
#endif
