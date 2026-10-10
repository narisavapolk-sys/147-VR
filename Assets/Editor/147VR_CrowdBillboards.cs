#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Crowd billboards v2: 3 audience sides with 3 DIFFERENT crowd textures (no repeat),
// aspect-correct tiling (no stretched people), dark ceiling fill above the crowd to close
// the gap, and hides the blocky 3D Spectator_* + chairs. Keeps stairs/scoreboard (entry +Z).
// Re-runnable (clears its own previous Crowd_BB_/Ceil_BB_ first). MenuItem-only.
public static class _147VR_CrowdBillboards
{
    const string DIR = "Assets/AAA/ImportedSnooker/Venue/Crowd/";
    // wall geometry (tweak here): half-extents of the arena around the table
    static readonly Vector3 FAR_POS = new Vector3(0f, 5f, -18f),   FAR_EULER = new Vector3(90f, 0f, 0f);   const float FAR_W = 36f;
    static readonly Vector3 LFT_POS = new Vector3(-16f, 5f, 0f),   LFT_EULER = new Vector3(90f, 90f, 0f);  const float LFT_W = 32f;
    static readonly Vector3 RGT_POS = new Vector3( 16f, 5f, 0f),   RGT_EULER = new Vector3(90f, -90f, 0f); const float RGT_W = 32f;
    const float CROWD_H = 9f, CEIL_H = 8f;

    [MenuItem("147VR/Art/Crowd Billboards v2 (3 sides + ceiling, hide spectators/chairs)")]
    public static void Place()
    {
        var scene = EditorSceneManager.GetActiveScene();
        foreach (var t in GameObject.FindObjectsByType<Transform>(FindObjectsSortMode.None))
            if (t.name.StartsWith("Crowd_BB_") || t.name.StartsWith("Ceil_BB_")) Object.DestroyImmediate(t.gameObject);

        int hidden = 0;
        foreach (var t in GameObject.FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            string n = t.name.ToLower();
            if (t.name.StartsWith("Spectator_") || n.Contains("chair") || n.Contains("seat") || n.Contains("stool") || n.Contains("bench"))
            { t.gameObject.SetActive(false); hidden++; }
        }

        var left = LoadTex("crowd_left.png"); var right = LoadTex("crowd_right.png"); var far = LoadTex("crowd_far.png");
        Side("Crowd_BB_Far", FAR_POS, FAR_EULER, FAR_W, far);
        Side("Crowd_BB_Left", LFT_POS, LFT_EULER, LFT_W, left);
        Side("Crowd_BB_Right", RGT_POS, RGT_EULER, RGT_W, right);

        // dark ceiling fill above each crowd to close the gray gap
        var dark = DarkMat();
        Side("Ceil_BB_Far", FAR_POS + new Vector3(0, (CROWD_H + CEIL_H) / 2f, 0), FAR_EULER, FAR_W, null, dark, CEIL_H);
        Side("Ceil_BB_Left", LFT_POS + new Vector3(0, (CROWD_H + CEIL_H) / 2f, 0), LFT_EULER, LFT_W, null, dark, CEIL_H);
        Side("Ceil_BB_Right", RGT_POS + new Vector3(0, (CROWD_H + CEIL_H) / 2f, 0), RGT_EULER, RGT_W, null, dark, CEIL_H);

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log(string.Format("147VR/Crowd v2: hid {0} spectators/chairs; 3 distinct sides + ceilings. Nudge constants at top of script if needed.", hidden));
    }

    static Texture2D LoadTex(string f)
    {
        var t = AssetDatabase.LoadAssetAtPath<Texture2D>(DIR + f);
        if (t == null) Debug.LogWarning("147VR/Crowd: missing " + f);
        return t;
    }

    static void Side(string name, Vector3 pos, Vector3 euler, float w, Texture2D crowdTex)
    {
        Side(name, pos, euler, w, crowdTex, null, CROWD_H);
    }
    static void Side(string name, Vector3 pos, Vector3 euler, float w, Texture2D crowdTex, Material darkMat, float h)
    {
        var p = GameObject.CreatePrimitive(PrimitiveType.Plane);
        p.name = name;
        var col = p.GetComponent<Collider>(); if (col != null) Object.DestroyImmediate(col);
        p.transform.position = pos; p.transform.eulerAngles = euler;
        p.transform.localScale = new Vector3(w / 10f, 1f, h / 10f);
        var r = p.GetComponent<Renderer>();
        if (crowdTex != null)
        {
            var mat = UnlitMat();
            mat.SetTexture("_BaseMap", crowdTex);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
            else mat.color = Color.white;
            // aspect-correct horizontal tiling: texture 1792x1024 -> keep people upright
            float texAspect = crowdTex.width / (float)crowdTex.height;
            float repeatX = w / (h * texAspect);
            mat.SetTextureScale("_BaseMap", new Vector2(Mathf.Max(1f, Mathf.Round(repeatX)), 1f));
            r.sharedMaterial = mat;
        }
        else if (darkMat != null) r.sharedMaterial = darkMat;
    }

    static Material UnlitMat()
    {
        var s = Shader.Find("Universal Render Pipeline/Unlit");
        if (s == null) s = Shader.Find("Unlit/Texture");
        if (s == null) s = Shader.Find("Standard");
        return new Material(s);
    }
    static Material DarkMat()
    {
        var m = UnlitMat();
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", new Color(0.02f, 0.02f, 0.04f, 1f));
        else m.color = new Color(0.02f, 0.02f, 0.04f, 1f);
        return m;
    }
}
#endif
