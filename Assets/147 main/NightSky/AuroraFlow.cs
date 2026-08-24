using UnityEngine;

/// <summary>
/// AuroraFlow
/// -----------------------------------------------------------------------
/// Slowly drifting aurora borealis with color shifting (green/purple) across the sky.
/// Creates a transparent dome overlapping the Skybox and scrolls a noise texture UV via shader.
///
/// Usage:
/// 1. Create an Empty GameObject named "AuroraController"
/// 2. Attach this script — it will auto-generate the mesh dome + material, no extra setup needed.
/// 3. Press Play — green/purple light will drift slowly across the sky.
///
/// Note: uses "Unlit/Transparent" shader with vertex color. For URP/HDRP,
/// change shaderName below to "Universal Render Pipeline/Unlit" or equivalent.
/// -----------------------------------------------------------------------
/// </summary>
public class AuroraFlow : MonoBehaviour
{
    [Header("Scroll Speed")]
    public float scrollSpeedX = 0.015f;
    public float scrollSpeedY = 0.008f;

    [Header("Aurora Colors")]
    public Color colorA = new Color(0.15f, 0.9f, 0.55f, 0.35f);   // green
    public Color colorB = new Color(0.55f, 0.25f, 0.95f, 0.30f);  // purple

    [Header("Dome Size")]
    public float domeRadius = 350f;

    [Header("Aurora band height/position (0=horizon, 1=overhead)")]
    [Range(0.3f, 0.95f)] public float bandHeight = 0.65f;

    private Material auroraMat;
    private float offsetX = 0f;
    private float offsetY = 0f;

    void Start()
    {
        BuildDome();
    }

    void BuildDome()
    {
        GameObject domeObj = new GameObject("AuroraDome");
        domeObj.transform.SetParent(transform);
        domeObj.transform.localPosition = Vector3.zero;

        MeshFilter mf = domeObj.AddComponent<MeshFilter>();
        MeshRenderer mr = domeObj.AddComponent<MeshRenderer>();
        mf.mesh = BuildDomeMesh(domeRadius, bandHeight);

        Shader shader = Shader.Find("Unlit/Transparent") ?? Shader.Find("Universal Render Pipeline/Unlit");
        auroraMat = new Material(shader);
        auroraMat.renderQueue = 3000;
        mr.material = auroraMat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;

        Texture2D noiseTex = GenerateAuroraNoiseTexture(256, 128);
        auroraMat.mainTexture = noiseTex;
        Color mixed = Color.Lerp(colorA, colorB, 0.5f);
        auroraMat.color = mixed;
    }

    Texture2D GenerateAuroraNoiseTexture(int w, int h)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Repeat;
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float n1 = Mathf.PerlinNoise(x * 0.04f, y * 0.08f);
                float n2 = Mathf.PerlinNoise(x * 0.09f + 50f, y * 0.03f + 50f);
                float v = Mathf.Clamp01((n1 * 0.6f + n2 * 0.4f));
                float band = 1f - Mathf.Abs((y / (float)h) - 0.5f) * 2.2f;
                band = Mathf.Clamp01(band);
                Color c = Color.Lerp(colorA, colorB, n1);
                c.a = v * band;
                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();
        return tex;
    }

    Mesh BuildDomeMesh(float radius, float bandHeight)
    {
        Mesh mesh = new Mesh();
        int segments = 48;
        var verts = new System.Collections.Generic.List<Vector3>();
        var uvs = new System.Collections.Generic.List<Vector2>();
        var tris = new System.Collections.Generic.List<int>();

        float thetaMin = Mathf.Acos(bandHeight) * 0.4f;
        float thetaMax = Mathf.Acos(bandHeight) * 1.6f;

        int rings = 12;
        for (int r = 0; r <= rings; r++)
        {
            float theta = Mathf.Lerp(thetaMin, thetaMax, r / (float)rings);
            for (int s = 0; s <= segments; s++)
            {
                float phi = (s / (float)segments) * Mathf.PI * 2f;
                Vector3 p = new Vector3(
                    Mathf.Sin(theta) * Mathf.Cos(phi),
                    Mathf.Cos(theta),
                    Mathf.Sin(theta) * Mathf.Sin(phi)
                ) * radius;
                verts.Add(p);
                uvs.Add(new Vector2(s / (float)segments, r / (float)rings));
            }
        }
        for (int r = 0; r < rings; r++)
        {
            for (int s = 0; s < segments; s++)
            {
                int i0 = r * (segments + 1) + s;
                int i1 = i0 + 1;
                int i2 = i0 + (segments + 1);
                int i3 = i2 + 1;
                tris.Add(i0); tris.Add(i2); tris.Add(i1);
                tris.Add(i1); tris.Add(i2); tris.Add(i3);
            }
        }
        mesh.SetVertices(verts);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    void Update()
    {
        if (auroraMat == null) return;
        offsetX += scrollSpeedX * Time.deltaTime;
        offsetY += scrollSpeedY * Time.deltaTime;
        auroraMat.mainTextureOffset = new Vector2(offsetX, offsetY);
    }
}
