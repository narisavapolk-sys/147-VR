#if UNITY_EDITOR
// NeonVintageArtPassImporter.cs — Owner-clicked pipeline for Blender ArtPass import.
// 147VR/AAA/NeonVintage/1) = configure importers + create URP materials from manifest (safe, repeatable)
// 147VR/AAA/NeonVintage/2) = place in scene + archive old WipRoom content (asks before each destructive step)
//
// Data source: Assets/AAA/Imports/NeonVintageArtPass/manifest_flat.json
// (flattened from Blender export manifest.json by Docs/AAA/pipeline/rebind_manifest.ps1)
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace _147VR.EditorTools
{
    public static class NeonVintageArtPassImporter
    {
        const string Root = "Assets/AAA/Imports/NeonVintageArtPass";
        const string FlatManifest = Root + "/manifest_flat.json";
        const string SceneRootName = "147VR_AAA_NeonVintage";

        [System.Serializable] class MatTex { public string baseTex = ""; public string normalTex = ""; public string roughTex = ""; public string metalTex = ""; public string emissionTex = ""; public string specTex = ""; public string clearcoatTex = ""; }
        [System.Serializable] class MatData { public string name; public float[] baseColor; public float[] emissionColor; public float metallic; public float roughness; public float alpha; public float emissionStrength; public MatTex tex; }
        [System.Serializable] class TexData { public string name; public string file; public int perc_width; public int perc_height; }
        [System.Serializable] class FbxData { public string name; public string collection; public int tris_after; }
        [System.Serializable] class LightData { public string name; public string collection; public string type; public float[] color; public float energy; public float[] loc; public float[] rot_euler; public int vis; public float spot_size; public float spot_blend; public float[] size; }
        [System.Serializable] class WorldData { public float[] color; public float strength; }
        [System.Serializable] class ManifestFlat { public List<MatData> materials; public List<TexData> textures; public List<FbxData> fbx; public List<LightData> lights; public WorldData world; }

        static ManifestFlat M;
        static Dictionary<string, Material> builtMaps;
        static int matCount, texTouched, fbxTouched;

        // ---------- menus ----------
        [MenuItem("147VR/AAA/NeonVintage/1) Prepare Assets (importers + materials)")]
        public static void MenuPrepareAssets()
        {
            if (!LoadManifest()) return;
            try { ConfigureAll(); } finally { EditorUtility.ClearProgressBar(); }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[NeonVintage] Prepare OK — materials {matCount}, textures touched {texTouched}, fbx touched {fbxTouched}");
        }

        [MenuItem("147VR/AAA/NeonVintage/LEGACY - Disabled unsafe replace path")]
        public static void MenuPlaceInScene()
        {
            EditorUtility.DisplayDialog("NeonVintage",
                "Legacy placement path is disabled because it deletes existing ArtPass children and archives scene roots. Use 147VR/AAA/NeonVintage/Safe/3) Stage Static + Dynamic FBX (additive) on the AAA working copy.",
                "OK");
            Debug.LogWarning("[NeonVintage] Legacy destructive placement path blocked. Use NeonVintageSafeStager.");
        }

        [MenuItem("147VR/AAA/NeonVintage/LEGACY - Disabled incomplete PostProcess fix")]
        public static void MenuFixPost()
        {
            EditorUtility.DisplayDialog("NeonVintage",
                "Legacy PostProcess path is disabled because it does not repair existing VolumeProfile sub-assets. Use 147VR/AAA/NeonVintage/Safe/4) Repair PostProcess Profile (new v002 asset) on the AAA working copy.",
                "OK");
            Debug.LogWarning("[NeonVintage] Legacy incomplete PostProcess path blocked. Use NeonVintageSafeStager.");
        }

        // ---------- loading ----------
        static bool LoadManifest()
        {
            if (!File.Exists(FlatManifest))
            {
                EditorUtility.DisplayDialog("NeonVintage", "manifest_flat.json หายไป — รัน Docs/AAA/pipeline/rebind_manifest.ps1 ก่อน", "ปิด");
                return false;
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            M = JsonUtility.FromJson<ManifestFlat>(File.ReadAllText(FlatManifest));
            if (M == null || M.materials == null || M.fbx == null || M.fbx.Count == 0)
            {
                EditorUtility.DisplayDialog("NeonVintage", "manifest_flat.json อ่านไม่ได้/ว่าง", "ปิด");
                return false;
            }
            return true;
        }

        // ---------- step 1: configure everything ----------
        static void ConfigureAll()
        {
            matCount = texTouched = fbxTouched = 0;
            builtMaps = null;

            var texRoles = new Dictionary<string, string>(); // texture file -> role
            foreach (var md in M.materials)
            {
                if (md.tex == null) continue;
                Bind(texRoles, md.tex.normalTex, "normal");
                Bind(texRoles, md.tex.metalTex, "linear");
                Bind(texRoles, md.tex.roughTex, "linear");
                Bind(texRoles, md.tex.specTex, "linear");
                Bind(texRoles, md.tex.baseTex, "albedo");
                Bind(texRoles, md.tex.emissionTex, "albedo");
            }

            var files = M.textures;
            for (int i = 0; i < files.Count; i++)
            {
                var t = files[i];
                if (string.IsNullOrEmpty(t.file)) continue;
                EditorUtility.DisplayProgressBar("NeonVintage", $"Texture {i + 1}/{files.Count} {t.name}", (float)i / files.Count);
                string p = Root + "/" + t.file;
                var ti = AssetImporter.GetAtPath(p) as TextureImporter;
                if (ti == null) continue;
                string role; texRoles.TryGetValue(t.file, out role);
                bool changed = false;
                if (role == "normal" && ti.textureType != TextureImporterType.NormalMap)
                { ti.textureType = TextureImporterType.NormalMap; changed = true; }
                else if (role == "linear" && ti.sRGBTexture)
                { ti.sRGBTexture = false; changed = true; }
                else if (role == "albedo" && !ti.sRGBTexture)
                { ti.sRGBTexture = true; changed = true; }
                if (ti.maxTextureSize > 2048) { ti.maxTextureSize = 2048; changed = true; }
                if (changed) { ti.SaveAndReimport(); texTouched++; }
            }

            var fbxs = M.fbx;
            for (int i = 0; i < fbxs.Count; i++)
            {
                var f = fbxs[i];
                EditorUtility.DisplayProgressBar("NeonVintage", $"FBX {i + 1}/{fbxs.Count} {f.name}", (float)i / fbxs.Count);
                string p = Root + "/" + f.name;
                var mi = AssetImporter.GetAtPath(p) as ModelImporter;
                if (mi == null) { Debug.LogWarning("[NeonVintage] no importer " + f.name); continue; }
                bool changed = false;
                if (mi.importAnimation) { mi.importAnimation = false; changed = true; }
                if (!mi.generateSecondaryUV) { mi.generateSecondaryUV = true; changed = true; } // UV2 สำหรับ lightmap
                if (mi.isReadable) { mi.isReadable = false; changed = true; }
                if (changed) { mi.SaveAndReimport(); fbxTouched++; }
            }

            BuildMaterials();
        }

        static void Bind(Dictionary<string, string> roles, string file, string role)
        {
            if (string.IsNullOrEmpty(file)) return;
            string cur;
            roles.TryGetValue(file, out cur);
            int prio(string r) { return r == "normal" ? 3 : r == "linear" ? 2 : 1; }
            if (!roles.ContainsKey(file) || prio(role) > prio(cur)) roles[file] = role;
        }

        static string Sane(string s)
        {
            if (string.IsNullOrEmpty(s)) return "mat";
            var sb = new char[s.Length];
            int j = 0;
            foreach (char ch in s)
            {
                bool ok = char.IsLetterOrDigit(ch) || ch == '_' || ch == '-' || ch == '.' || ch == ' ';
                sb[j++] = ok ? ch : '_';
            }
            var name = new string(sb, 0, j).Replace("  ", " ").Trim();
            if (name.Length > 80) name = name.Substring(0, 80);
            if (name.Length == 0) name = "mat";
            return name;
        }

        static string SaneLower(string s) { return Sane(s).Replace(" ", "").ToLower(); }

        static float Sc(float[] a, int i, float def) { return a != null && a.Length > i ? a[i] : def; }

        static Texture2D TexOrNull(string rel)
        {
            if (string.IsNullOrEmpty(rel)) return null;
            return AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/" + rel);
        }

        static void BuildMaterials()
        {
            string folder = Root + "/" + "Materials";
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder(Root, "Materials");

            var litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (litShader == null) { Debug.LogError("[NeonVintage] URP/Lit shader not found"); return; }

            var used = new Dictionary<string, string>();
            var byName = new Dictionary<string, Material>();
            var list = M.materials;
            matCount = 0;

            for (int i = 0; i < list.Count; i++)
            {
                var md = list[i];
                EditorUtility.DisplayProgressBar("NeonVintage", $"Material {i + 1}/{list.Count} {md.name}", (float)i / list.Count);

                string sane = Sane(md.name);
                string key = sane.ToLower();
                string assetName = sane;
                if (used.ContainsKey(key))
                {
                    int k = 2;
                    while (true)
                    {
                        assetName = sane + "_" + k;
                        string k2 = assetName.ToLower();
                        if (!used.ContainsKey(k2)) { key = k2; break; }
                        k++;
                    }
                }
                used[key] = assetName;

                string path = folder + "/" + assetName + ".mat";
                var m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (m == null)
                {
                    m = new Material(litShader);
                    AssetDatabase.CreateAsset(m, path);
                }
                else m.shader = litShader;

                SetupMaterial(m, md);

                byName[md.name.ToLower()] = m;
                string norm = SaneLower(md.name);
                if (!byName.ContainsKey(norm)) byName[norm] = m;
                matCount++;
            }

            builtMaps = byName;
        }

        static void SetupMaterial(Material m, MatData md)
        {
            var c = new Color(Sc(md.baseColor, 0, 1f), Sc(md.baseColor, 1, 1f), Sc(md.baseColor, 2, 1f), Sc(md.baseColor, 3, 1f));
            m.SetColor("_BaseColor", c);

            var baseT = TexOrNull(md.tex != null ? md.tex.baseTex : null);
            if (baseT != null) m.SetTexture("_BaseMap", baseT);

            var normalT = TexOrNull(md.tex != null ? md.tex.normalTex : null);
            if (normalT != null) { m.SetTexture("_BumpMap", normalT); m.EnableKeyword("_NORMALMAP"); }

            m.SetFloat("_Metallic", Mathf.Clamp01(md.metallic));
            m.SetFloat("_Smoothness", Mathf.Clamp01(1f - md.roughness));

            // GLTF-style metallicRoughness textures (G=rough B=metal) ไม่ตรง channel Unity — ใช้ค่า scalar แทน (visual pass จะชี้ตัวที่ต้องฟิต)
            float es = md.emissionStrength;
            if (es > 0.0005f)
            {
                var ecol = new Color(Sc(md.emissionColor, 0, 0f), Sc(md.emissionColor, 1, 0f), Sc(md.emissionColor, 2, 0f), 1f);
                m.SetColor("_EmissionColor", ecol * es);
                m.EnableKeyword("_EMISSION");
            }

            if (md.alpha < 0.999f)
            {
                m.SetFloat("_Surface", 1f);
                m.SetFloat("_Blend", 0f);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.renderQueue = 3000;
            }
        }

        // ---------- step 2: instantiate + replace ----------
        static void InstanceAll()
        {
            ConfigureAll();
            if (builtMaps == null) return;

            var active = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!active.isLoaded) { EditorUtility.DisplayDialog("NeonVintage", "ไม่มีฉากที่เปิดอยู่", "ปิด"); return; }

            GameObject artGo = active.GetRootGameObjects().FirstOrDefault(g => g.name == SceneRootName);
            Transform artpassRoot = artGo != null ? artGo.transform : null;

            bool existing = artpassRoot != null;
            if (existing)
            {
                if (!EditorUtility.DisplayDialog("NeonVintage", $"พบ root เดิม \"{SceneRootName}\" อยู่แล้ว\nต่อ = เคลียร์ี่ลูกเดิม + วางใหม่ + remap ใหม่", "ต่อ", "ยกเลิก"))
                    return;
                for (int i = artpassRoot.childCount - 1; i >= 0; i--)
                    Object.DestroyImmediate(artpassRoot.GetChild(i).gameObject);
            }
            else
            {
                var go = new GameObject(SceneRootName);
                Undo.RegisterCreatedObjectUndo(go, "NeonVintage root");
                artpassRoot = go.transform;
            }

            int fbxOk = 0;
            var fbxs = M.fbx;
            for (int i = 0; i < fbxs.Count; i++)
            {
                var f = fbxs[i];
                EditorUtility.DisplayProgressBar("NeonVintage", $"Place {i + 1}/{fbxs.Count} {f.name}", (float)i / fbxs.Count);
                string p = Root + "/" + f.name;
                if (!File.Exists(p)) { Debug.LogWarning("[NeonVintage] missing " + f.name); continue; }
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(p);
                if (prefab == null) { Debug.LogWarning("[NeonVintage] load fail " + p); continue; }
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, active);
                inst.transform.SetParent(artpassRoot, false);
                inst.name = f.name.EndsWith(".fbx") ? f.name.Remove(f.name.Length - 4) : f.name;
                var flags = (StaticEditorFlags)~0;
                foreach (var t in inst.GetComponentsInChildren<Transform>(true))
                    GameObjectUtility.SetStaticEditorFlags(t.gameObject, flags);
                fbxOk++;
            }

            int remapOk = 0, remapMiss = 0;
            var missSet = new List<string>();
            foreach (var r in artpassRoot.GetComponentsInChildren<Renderer>(false))
            {
                var cur = r.sharedMaterials;
                bool touched = false;
                for (int i = 0; i < cur.Length; i++)
                {
                    var cm = cur[i];
                    if (cm == null) continue;
                    Material rep;
                    string ln = cm.name.ToLower();
                    if (!builtMaps.TryGetValue(ln, out rep))
                    {
                        string nn = SaneLower(cm.name);
                        if (!builtMaps.TryGetValue(nn, out rep))
                        {
                            remapMiss++;
                            if (missSet.Count < 25) missSet.Add(cm.name);
                            continue;
                        }
                    }
                    if (rep != cm) { cur[i] = rep; touched = true; remapOk++; }
                }
                if (touched) r.sharedMaterials = cur;
            }

            PlaceLights(artpassRoot);
            ArchiveOldRoots(active, artpassRoot);
            SetupPostProcess(artpassRoot);
            SetupAmbient();

            Selection.activeGameObject = artpassRoot.gameObject;
            Debug.Log($"[NeonVintage] placed fbx {fbxOk}/{fbxs.Count}; remap ok {remapOk}, miss {remapMiss} ({string.Join(", ", missSet.Take(18))})\nขั้นถัดไป: เทียบภาพ + Bake GI (menu 147VR/Lighting)");
        }

        static void ArchiveOldRoots(UnityEngine.SceneManagement.Scene active, Transform artpassRoot)
        {
            var keepPref = new[]
            {
                "XR", "Camera", "Player", "Ball", "Table", "Cue", "Game", "Manager", "Physic",
                "Pool", "UI", "Canvas", "EventSystem", "Input", "Lighting", "Bake", "Volume",
                "Spawn", "Network", "Grid", "Oculus", "Meta", "OpenXR", "Scene", SceneRootName
            };
            var oldOnes = active.GetRootGameObjects()
                .Where(g => g.transform != artpassRoot &&
                            !keepPref.Any(k => g.name.StartsWith(k, System.StringComparison.OrdinalIgnoreCase)))
                .ToList();
            if (oldOnes.Count == 0) return;

            if (!EditorUtility.DisplayDialog("NeonVintage",
                    $"จะปิดเนื้อหาเดิมในฉาก {oldOnes.Count} อัน (ไม่ลบ — ย้ายไป \"WipRoom_Old_Backup\" แล้วปิด)\nเผลอปิดอะไรที่ต้องใช้ ลากกลับมาได้\n\n" + string.Join("\n", oldOnes.Take(12).Select(o => o.name)),
                    "เก็บและปิด", "ยกเลิก"))
                return;

            var archive = new GameObject("WipRoom_Old_Backup");
            archive.transform.SetParent(artpassRoot.parent, true);
            foreach (var g in oldOnes)
            {
                Undo.SetTransformParent(g.transform, archive.transform, "NeonVintage archive");
                g.SetActive(false);
            }
            archive.SetActive(false);
            Debug.Log($"[NeonVintage] archived {oldOnes.Count} roots: {string.Join(", ", oldOnes.Select(o => o.name))}");
        }

        static void SetupPostProcess(Transform artpassRoot)
        {
            if (artpassRoot.Find("147VR_AAA_PostProcess") != null) return;
            var pgo = new GameObject("147VR_AAA_PostProcess");
            pgo.transform.SetParent(artpassRoot, false);
            // Unity 6: .profile ห้าม CreateAsset ใช้ .asset แทน + save ก่อนค่อยผูกกับ Volume
            const string ppPath = Root + "/ArtpassPost.asset";
            AssetDatabase.DeleteAsset(Root + "/ArtpassPost.profile");
            var prof = ScriptableObject.CreateInstance<VolumeProfile>();
            try
            {
                var tm = prof.Add<Tonemapping>(true);
                tm.mode.Override(TonemappingMode.ACES);
                var bl = prof.Add<Bloom>(true);
                bl.intensity.Override(0.35f);
                bl.threshold.Override(1.0f);
            }
            catch (System.Exception e) { Debug.LogWarning("[NeonVintage] post setup: " + e.Message); }
            AssetDatabase.CreateAsset(prof, ppPath);
            var vol = pgo.AddComponent<Volume>();
            vol.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ppPath);
            vol.isGlobal = true;
        }

        static void SetupAmbient()
        {
            if (M.world == null) return;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            var c = new Color(Sc(M.world.color, 0, 1f), Sc(M.world.color, 1, 1f), Sc(M.world.color, 2, 1f));
            RenderSettings.ambientLight = c * Mathf.Clamp01(M.world.strength);
        }

        // ---------- lights ----------
        static void PlaceLights(Transform artpassRoot)
        {
            var lg = artpassRoot.Find("147VR_AAA_NeonVintage_Lights");
            if (lg == null)
            {
                var g = new GameObject("147VR_AAA_NeonVintage_Lights");
                g.transform.SetParent(artpassRoot, false);
                lg = g.transform;
            }
            for (int i = lg.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(lg.GetChild(i).gameObject);

            var top = M.lights.Where(l => l.vis == 1).OrderByDescending(l => l.energy).ToList();
            int take = Mathf.Min(8, top.Count);
            for (int i = 0; i < take; i++)
            {
                var L = top[i];
                var go = new GameObject("L_" + Sane(L.name));
                go.transform.SetParent(lg, false);
                // Blender (x,y,z) -> Unity (x, z, -y)
                go.transform.position = new Vector3(Sc(L.loc, 0, 0f), Sc(L.loc, 2, 0f), -Sc(L.loc, 1, 0f));

                var l = go.AddComponent<Light>();
                l.color = new Color(Sc(L.color, 0, 1f), Sc(L.color, 1, 1f), Sc(L.color, 2, 1f));
                l.intensity = Mathf.Clamp(L.energy * 0.05f, 0.05f, 6f); // TODO: จูนตอน visual pass

                if (L.type == "SPOT")
                {
                    l.type = LightType.Spot;
                    float angRad = L.spot_size > 0 ? L.spot_size : 60f * Mathf.Deg2Rad;
                    float ang = Mathf.Clamp(angRad * Mathf.Rad2Deg, 5f, 160f);
                    l.spotAngle = ang;
                    if (L.spot_blend > 0)
                        l.innerSpotAngle = Mathf.Clamp(ang * (1f - Mathf.Clamp01(L.spot_blend)), 1f, ang - 1f);
                    var dirBl = EulerXYZDir(Sc(L.rot_euler, 0, 0f), Sc(L.rot_euler, 1, 0f), Sc(L.rot_euler, 2, 0f));
                    var dirU = new Vector3(dirBl.x, dirBl.z, -dirBl.y).normalized;
                    go.transform.rotation = Quaternion.LookRotation(dirU);
                }
                else l.type = LightType.Point;

                l.range = Mathf.Lerp(5f, 24f, Mathf.InverseLerp(1f, 300f, L.energy));
                l.shadows = i == 0 ? LightShadows.Soft : LightShadows.None; // 1 dynamic shadow (Quest budget)
                l.shadowStrength = 0.7f;
            }
            Debug.Log($"[NeonVintage] lights placed {take}/{M.lights.Count} (top by energy) — intensity/range จูนที่ visual pass");
        }

        // Blender euler 'XYZ' (rad): R = Rz @ Ry @ Rx in right-handed Blender coords; dir = local -Z
        static Vector3 EulerXYZDir(float rx, float ry, float rz)
        {
            var v = new Vector3(0f, 0f, -1f);
            // X
            float c = Mathf.Cos(rx), s = Mathf.Sin(rx);
            v = new Vector3(v.x, v.y * c - v.z * s, v.y * s + v.z * c);
            // Y
            c = Mathf.Cos(ry); s = Mathf.Sin(ry);
            v = new Vector3(v.x * c + v.z * s, v.y, -v.x * s + v.z * c);
            // Z
            c = Mathf.Cos(rz); s = Mathf.Sin(rz);
            v = new Vector3(v.x * c - v.y * s, v.x * s + v.y * c, v.z);
            return v;
        }
    }
}
#endif
