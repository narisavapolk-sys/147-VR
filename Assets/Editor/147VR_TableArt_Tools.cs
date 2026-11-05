#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Art polish tools for the v008 table. MenuItem-only; the OWNER runs them with the scene open.
public static class _147VR_TableArt_Tools
{
    static GameObject FindByName(string name)
    {
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
            if (t.name == name) return t.gameObject;
        return null;
    }

    // (ก) Assign a varnished-wood material (plain wood, no cue sections) to TABLE FRAME.
    [MenuItem("147VR/Art/Table/Apply Varnished Wood to TABLE FRAME")]
    public static void ApplyFrameWood()
    {
        GameObject frame = FindByName("TABLE FRAME");
        if (frame == null) { Debug.LogError("147VR/TableArt: 'TABLE FRAME' not found in the open scene."); return; }
        Shader shader = Shader.Find("147VR/Cue/Procedural Wood");
        if (shader == null) { Debug.LogError("147VR/TableArt: shader '147VR/Cue/Procedural Wood' not found."); return; }

        string path = "Assets/AAA/ImportedSnooker/Materials/V008/M_TableFrame_Wood.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null) { mat = new Material(shader); AssetDatabase.CreateAsset(mat, path); }
        mat.shader = shader;
        mat.SetFloat("_UseSections", 0f);                       // plain wood, no tip/ferrule/bumper
        mat.SetColor("_ShaftColor", new Color(0.30f, 0.13f, 0.07f)); // mahogany
        mat.SetColor("_ButtColor",  new Color(0.22f, 0.09f, 0.05f));
        mat.SetColor("_GrainColor", new Color(0.10f, 0.04f, 0.02f));
        mat.SetFloat("_RingScale", 6f);
        mat.SetFloat("_GrainScale", 5f);
        mat.SetFloat("_Distortion", 0.5f);
        mat.SetFloat("_Varnish", 0.82f);
        mat.SetFloat("_ClearCoat", 0.7f);
        mat.SetFloat("_Fresnel", 0.25f);
        EditorUtility.SetDirty(mat);
        AssetDatabase.SaveAssets();

        MeshRenderer mr = frame.GetComponent<MeshRenderer>();
        if (mr == null) { Debug.LogError("147VR/TableArt: TABLE FRAME has no MeshRenderer."); return; }
        mr.sharedMaterial = mat;
        EditorSceneManager.MarkSceneDirty(frame.scene);
        Debug.Log("147VR/TableArt: applied varnished wood to TABLE FRAME (" + path + ").");
    }

    // (ข) Lift the DARC lights up above the table (fixes a light sitting on the cloth).
    [MenuItem("147VR/Art/Table/Move DARC Lights Above Table")]
    public static void LiftDarcLights()
    {
        GameObject surface = FindByName("TABLE SURFACE");
        if (surface == null) { Debug.LogError("147VR/TableArt: 'TABLE SURFACE' not found (need it to compute the table top)."); return; }
        Vector3 center = surface.transform.position;
        const float height = 2.3f;

        string[] names = { "DARC_Key", "DARC_Fill", "DARC_RimL", "DARC_RimR" };
        Vector3[] offs =
        {
            new Vector3( 0.0f, height,      -1.3f),
            new Vector3( 0.0f, height,       1.3f),
            new Vector3(-1.8f, height * 0.8f, 0.0f),
            new Vector3( 1.8f, height * 0.8f, 0.0f),
        };

        int n = 0;
        for (int i = 0; i < names.Length; i++)
        {
            GameObject l = FindByName(names[i]);
            if (l == null) continue;
            l.transform.position = center + offs[i];
            l.transform.LookAt(center);
            n++;
        }
        EditorSceneManager.MarkAllScenesDirty();
        Debug.Log("147VR/TableArt: repositioned " + n + " DARC lights above the table (center=" + center + ", height=" + height + "). Tweak in Scene view if needed.");
    }
}
#endif
