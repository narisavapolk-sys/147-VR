#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// Editor tool: create several procedural-wood cue material presets from the
// "147VR/Cue/Procedural Wood" shader. MenuItem-only; the OWNER runs it in Unity.
public static class _147VR_CueMaterials
{
    const string PARENT = "Assets/AAA/ProceduralCue";
    const string DIR = PARENT + "/Mats";
    const string SHADER = "147VR/Cue/Procedural Wood";

    struct Preset
    {
        public string name;
        public Color shaft, butt, grain;
        public float ring, grainScale, varnish, coat;
    }

    static readonly Preset[] Presets = new Preset[]
    {
        new Preset { name = "Cue_Ash_Rosewood",  shaft = new Color(0.82f,0.70f,0.48f), butt = new Color(0.28f,0.12f,0.06f), grain = new Color(0.20f,0.09f,0.04f), ring = 24, grainScale = 8,  varnish = 0.85f, coat = 0.6f },
        new Preset { name = "Cue_Maple_Black",   shaft = new Color(0.86f,0.77f,0.56f), butt = new Color(0.14f,0.14f,0.16f), grain = new Color(0.45f,0.32f,0.16f), ring = 20, grainScale = 10, varnish = 0.88f, coat = 0.7f },
        new Preset { name = "Cue_Rosewood",      shaft = new Color(0.55f,0.30f,0.16f), butt = new Color(0.30f,0.12f,0.07f), grain = new Color(0.18f,0.07f,0.03f), ring = 28, grainScale = 7,  varnish = 0.82f, coat = 0.5f },
        new Preset { name = "Cue_Ebony",         shaft = new Color(0.13f,0.11f,0.10f), butt = new Color(0.06f,0.05f,0.05f), grain = new Color(0.26f,0.22f,0.20f), ring = 30, grainScale = 12, varnish = 0.90f, coat = 0.8f },
        new Preset { name = "Cue_Bubinga_Burst", shaft = new Color(0.60f,0.26f,0.15f), butt = new Color(0.35f,0.10f,0.08f), grain = new Color(0.14f,0.05f,0.03f), ring = 26, grainScale = 9,  varnish = 0.85f, coat = 0.6f },
        new Preset { name = "Cue_Padauk_Sunset", shaft = new Color(0.75f,0.38f,0.28f), butt = new Color(0.45f,0.16f,0.10f), grain = new Color(0.28f,0.10f,0.06f), ring = 22, grainScale = 9,  varnish = 0.86f, coat = 0.6f },
    };

    [MenuItem("147VR/Art/Cue/Create Cue Material Presets")]
    public static void Create()
    {
        Shader shader = Shader.Find(SHADER);
        if (shader == null)
        {
            Debug.LogError("147VR/CueMat: shader '" + SHADER + "' not found. Make sure CueProceduralWood.shader is imported.");
            return;
        }
        if (!AssetDatabase.IsValidFolder(PARENT))
            AssetDatabase.CreateFolder("Assets/AAA", "ProceduralCue");
        if (!AssetDatabase.IsValidFolder(DIR))
            AssetDatabase.CreateFolder(PARENT, "Mats");

        foreach (var p in Presets)
        {
            string path = DIR + "/" + p.name + ".mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = shader;
            mat.SetColor("_ShaftColor", p.shaft);
            mat.SetColor("_ButtColor", p.butt);
            mat.SetColor("_GrainColor", p.grain);
            mat.SetFloat("_RingScale", p.ring);
            mat.SetFloat("_GrainScale", p.grainScale);
            mat.SetFloat("_Varnish", p.varnish);
            mat.SetFloat("_ClearCoat", p.coat);
            EditorUtility.SetDirty(mat);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("147VR/CueMat: created/updated " + Presets.Length + " cue material presets in " + DIR);
    }
}
#endif
