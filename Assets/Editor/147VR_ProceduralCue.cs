#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Editor tool: generate a snooker/pool cue as a procedural surface-of-revolution mesh
// (no model files needed). MenuItem-only — the OWNER runs it in Unity (agent never opens Unity).
// UV layout: u wraps around the shaft, v runs along the length (tip=0 -> butt=1) so a
// procedural wood Shader Graph can run the grain lengthwise. Pairs with the next step
// (147VR/Art/Cue/... wood shader).
public static class _147VR_ProceduralCue
{
    const string DIR = "Assets/AAA/ProceduralCue";
    const string MESH_PATH = DIR + "/ProceduralCue.asset";

    // Snooker cue silhouette (metres from tip): tip ~9.5mm dia -> joint ~19mm -> butt ~24mm + bumper.
    static readonly float[] ZK = { 0.000f, 0.012f, 0.026f, 0.735f, 0.745f, 1.360f, 1.372f, 1.400f };
    static readonly float[] RK = { 0.0046f, 0.0048f, 0.0050f, 0.0095f, 0.0096f, 0.0118f, 0.0128f, 0.0122f };

    [MenuItem("147VR/Art/Cue/Generate Procedural Cue (mesh)")]
    public static void Generate()
    {
        EnsureFolder();
        Mesh mesh = BuildCue();

        if (AssetDatabase.LoadAssetAtPath<Mesh>(MESH_PATH) != null)
            AssetDatabase.DeleteAsset(MESH_PATH);
        AssetDatabase.CreateAsset(mesh, MESH_PATH);
        AssetDatabase.SaveAssets();

        GameObject go = new GameObject("ProceduralCue");
        MeshFilter mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;
        MeshRenderer mr = go.AddComponent<MeshRenderer>();
        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        if (lit != null) mr.sharedMaterial = new Material(lit);

        Selection.activeGameObject = go;
        Debug.Log("147VR/Cue: generated procedural cue -> " + MESH_PATH + "  verts=" + mesh.vertexCount);
    }

    static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder(DIR))
            AssetDatabase.CreateFolder("Assets/AAA", "ProceduralCue");
    }

    static Mesh BuildCue()
    {
        int rings = 96, seg = 28;
        float length = 1.40f;
        var verts = new List<Vector3>();
        var uvs = new List<Vector2>();
        var tris = new List<int>();

        for (int i = 0; i <= rings; i++)
        {
            float t = (float)i / rings;
            float z = t * length;
            float r = RadiusAt(z);
            for (int s = 0; s < seg; s++)
            {
                float a = (float)s / seg * Mathf.PI * 2f;
                verts.Add(new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, z));
                uvs.Add(new Vector2((float)s / seg, t));
            }
        }

        for (int i = 0; i < rings; i++)
        {
            for (int s = 0; s < seg; s++)
            {
                int v0 = i * seg + s;
                int v1 = i * seg + (s + 1) % seg;
                int v2 = (i + 1) * seg + s;
                int v3 = (i + 1) * seg + (s + 1) % seg;
                tris.Add(v0); tris.Add(v1); tris.Add(v2);
                tris.Add(v1); tris.Add(v3); tris.Add(v2);
            }
        }

        // tip cap (z=0, outward -z)
        int tipCenter = verts.Count;
        verts.Add(new Vector3(0, 0, 0)); uvs.Add(new Vector2(0.5f, 0f));
        for (int s = 0; s < seg; s++)
        {
            tris.Add(tipCenter);
            tris.Add((s + 1) % seg);
            tris.Add(s);
        }

        // butt cap (z=length, outward +z)
        int buttCenter = verts.Count;
        verts.Add(new Vector3(0, 0, length)); uvs.Add(new Vector2(0.5f, 1f));
        int lastRing = rings * seg;
        for (int s = 0; s < seg; s++)
        {
            tris.Add(buttCenter);
            tris.Add(lastRing + s);
            tris.Add(lastRing + (s + 1) % seg);
        }

        Mesh mesh = new Mesh();
        mesh.name = "ProceduralCue";
        if (verts.Count > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.SetVertices(verts);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();
        return mesh;
    }

    static float RadiusAt(float z)
    {
        if (z <= ZK[0]) return RK[0];
        for (int i = 1; i < ZK.Length; i++)
        {
            if (z <= ZK[i])
            {
                float f = (z - ZK[i - 1]) / (ZK[i] - ZK[i - 1]);
                return Mathf.Lerp(RK[i - 1], RK[i], f);
            }
        }
        return RK[RK.Length - 1];
    }
}
#endif
