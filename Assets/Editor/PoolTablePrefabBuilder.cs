using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Creates official .prefab assets from the pool-table FBX imports under
/// Assets/Models/PoolTable and assigns URP/Lit materials to all renderers.
///
/// Material assets are stored per-prefab (prefixed by the FBX name) so the two
/// table skins (main + walnut) which share material names don't collide.
/// PBR textures (albedo + normal) extracted from the FBX embedded data are
/// assigned by name. Run headless:
///   Unity.exe -batchmode -projectPath <proj> -executeMethod PoolTablePrefabBuilder.Build -quit
/// </summary>
public static class PoolTablePrefabBuilder
{
    private const string SourceDir = "Assets/Models/PoolTable";
    private const string PrefabDir = "Assets/Prefabs/PoolTable";

    private static readonly string[] FbxFiles =
    {
        "PREFAB POoL table.fbx",
        "PREFAB POoL table Walnut.fbx",
        "PREFAB POoL table Blue.fbx",
        "PREFAB POoL Balls.fbx",
        "PREFAB POoL Cues.fbx",
    };

    private sealed class TexInfo
    {
        public string Base;
        public string Bump;
        public string Gloss;
    }

    public static void Build()
    {
        if (!Directory.Exists(PrefabDir))
            Directory.CreateDirectory(PrefabDir);

        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        if (lit == null)
        {
            Debug.LogError("URP Lit shader not found — is URP package installed?");
            EditorApplication.Exit(1);
            return;
        }

        foreach (string fbx in FbxFiles)
        {
            string src = Path.Combine(SourceDir, fbx);
            if (!File.Exists(src))
            {
                Debug.LogWarning("Missing FBX: " + src);
                continue;
            }

            string fbxBase = Path.GetFileNameWithoutExtension(fbx).Replace(" ", "");

            AssetDatabase.ImportAsset(src, ImportAssetOptions.ForceUpdate);
            var go = (GameObject)AssetDatabase.LoadAssetAtPath(src, typeof(GameObject));
            if (go == null)
            {
                Debug.LogError("Could not load model: " + src);
                continue;
            }

            var instance = Object.Instantiate(go);
            instance.name = go.name;

            int fixedMat = 0, texCount = 0;
            foreach (Renderer r in instance.GetComponentsInChildren<Renderer>(true))
            {
                Material[] newMats = new Material[r.sharedMaterials.Length];
                for (int i = 0; i < r.sharedMaterials.Length; i++)
                {
                    Material old = r.sharedMaterials[i];
                    if (old == null)
                    {
                        newMats[i] = CreateLit(lit, "Material", Color.white);
                        fixedMat++;
                        continue;
                    }

                    Color baseColor = Color.white;
                    if (old.HasProperty("_BaseColor")) baseColor = old.GetColor("_BaseColor");
                    else if (old.HasProperty("_Color")) baseColor = old.GetColor("_Color");

                    TexInfo tex = FindTextures(fbxBase, old.name);

                    // Per-prefab material asset so skins sharing names don't collide.
                    string matPath = Path.Combine(PrefabDir, fbxBase + "__" + old.name + ".mat");
                    Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                    bool created = false;
                    if (mat == null)
                    {
                        mat = new Material(lit);
                        created = true;
                    }

                    if (tex != null && tex.Base != null)
                    {
                        var albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(Path.Combine(SourceDir, "Textures", tex.Base));
                        if (albedo != null)
                        {
                            mat.SetTexture("_BaseMap", albedo);
                            mat.SetTexture("_MainTex", albedo);
                            mat.SetColor("_BaseColor", Color.white);
                            texCount++;
                        }
                        else
                        {
                            mat.SetColor("_BaseColor", baseColor);
                        }
                        if (tex.Bump != null)
                        {
                            var bump = AssetDatabase.LoadAssetAtPath<Texture2D>(Path.Combine(SourceDir, "Textures", tex.Bump));
                            if (bump != null)
                            {
                                mat.SetTexture("_BumpMap", bump);
                                mat.EnableKeyword("_NORMALMAP");
                                mat.SetFloat("_BumpScale", 1.0f);
                                texCount++;
                            }
                        }
                        // Metallic-gloss map (R=metallic, A=smoothness from roughness texture).
                        if (tex.Gloss != null)
                        {
                            var gloss = AssetDatabase.LoadAssetAtPath<Texture2D>(Path.Combine(SourceDir, "Textures", tex.Gloss));
                            if (gloss != null)
                            {
                                mat.SetTexture("_MetallicGlossMap", gloss);
                                mat.SetFloat("_SmoothnessTextureChannel", 1f); // use metallic alpha
                                mat.SetFloat("_Metallic", 1f);
                                mat.SetFloat("_Smoothness", 1f);
                                texCount++;
                            }
                        }
                    }
                    else
                    {
                        mat.SetColor("_BaseColor", baseColor);
                    }

                    // Smoothness: keep the FBX-imported value when present, else sensible defaults.
                    float smooth = old.HasProperty("_Smoothness") ? old.GetFloat("_Smoothness") : 0.5f;
                    if (old.name.Contains("Felt") || old.name.Contains("felt")) smooth = 0.12f;
                    else if (old.name.Contains("Wood") || old.name.Contains("wood")) smooth = 0.35f;
                    mat.SetFloat("_Smoothness", smooth);

                    float metal = 0f;
                    if (old.HasProperty("_Metallic")) metal = old.GetFloat("_Metallic");
                    if (old.name.Contains("Metal") || old.name.Contains("chrome") || old.name.Contains("Brass")
                        || old.name.Contains("bronze") || old.name.Contains("gold") || old.name.Contains("Plaque"))
                        metal = 1f;
                    mat.SetFloat("_Metallic", metal);

                    if (created)
                        AssetDatabase.CreateAsset(mat, matPath);
                    newMats[i] = mat;
                    fixedMat++;
                }
                r.sharedMaterials = newMats;
            }

            string prefabPath = Path.Combine(PrefabDir, go.name + ".prefab");
            bool success;
            PrefabUtility.SaveAsPrefabAsset(instance, prefabPath, out success);
            Debug.Log($"POOL: {prefabPath} success={success} mats={fixedMat} textures={texCount}");
            Object.DestroyImmediate(instance);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("POOL_BUILD_DONE");
    }

    private static TexInfo FindTextures(string fbxBase, string matName)
    {
        var t = new TexInfo();
        if (matName.StartsWith("ballN"))
        {
            t.Base = "n" + matName.Substring(5) + ".png";
            return t;
        }
        if (matName == "ballCue")
        {
            t.Base = "cueball.png";
            return t;
        }
        if (matName == "AAA_Plaque")
        {
            t.Base = "plaque_naris.png";
            return t;
        }

        // Determine skin from FBX filename.
        bool isWalnut = fbxBase.StartsWith("PREFABPOoLtableWalnut");
        bool isBlue = fbxBase.StartsWith("PREFABPOoLtableBlue");
        string skinTag = isWalnut ? "walnut" : (isBlue ? "blue" : "main");
        string woodKey = isWalnut ? "walnut" : "navy"; // blue and main share navy wood diffuse

        if (matName == "AAA_Felt")
        {
            t.Base = $"felt_albedo_{skinTag}.png";
            t.Bump = "felt_nor_gl.jpg";
            t.Gloss = "felt_metallic_gloss.png";
            return t;
        }
        if (matName == "AAA_Wood")
        {
            t.Base = $"wood_albedo_{skinTag}.png";
            t.Bump = $"wood_{woodKey}_nor_gl.jpg";
            t.Gloss = $"wood_{woodKey}_metallic_gloss.png";
            return t;
        }
        return null;
    }

    private static Material CreateLit(Shader lit, string name, Color color)
    {
        var m = new Material(lit);
        m.name = name;
        m.SetColor("_BaseColor", color);
        return m;
    }
}
