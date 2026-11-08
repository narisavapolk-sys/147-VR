#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _147VR.EditorTools
{
    /// <summary>
    /// Read-only flag-texture forensics for the WipRoom / ArtPass-Preview Scenes.
    ///
    /// Why this exists: the on-disk evidence already proves the 26 flag PNGs are true-colour
    /// 360x240, that both Rebound material sets bind the correct country texture, that the
    /// shader is the same URP Lit as the known-good curtain gold, and that 147VR_AAA_Flags.fbx
    /// meshes carry full 0..1 UVs. The only thing file inspection cannot answer is which mesh
    /// family the live renderers actually draw (the rip's FlagMesh_* versus the staged
    /// SM_147VR_AAA_Flag_NN_&lt;country&gt;), what each slot is really bound to at runtime, and
    /// whether baked-GI/lightmap state is washing the albedo out.
    ///
    /// This tool only reads. It never assigns materials, never marks anything dirty, never
    /// saves a Scene, and writes a single report file.
    /// </summary>
    public static class WipRoomFlagTextureAudit
    {
        private const string ReportPath = "Logs/WipRoom_Flag_Texture_Audit.txt";
        private const int MaxMeshVerticesForUvScan = 300000;

        private static readonly string[] Countries =
        {
            "argentina", "austria", "brazil", "canada", "denmark", "finland",
            "france", "germany", "greece", "india", "ireland", "italy",
            "japan", "netherlands", "norway", "poland", "portugal", "russia",
            "southafrica", "spain", "switzerland", "thailand", "turkey",
            "uk", "ukraine", "usa"
        };

        [MenuItem("147VR/AAA/NeonVintage/WipRoom/5) Audit Flag Materials (Read-Only)")]
        public static void Audit()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || !scene.path.Contains("WipRoom"))
            {
                EditorUtility.DisplayDialog("147VR WipRoom",
                    "Safety stop. Open one of the WipRoom Scenes first (production or ArtPassPreview).\nCurrent: "
                    + scene.name, "OK");
                return;
            }

            var renderers = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Renderer>(true))
                .Where(IsFlagRenderer)
                .ToList();

            var sb = new StringBuilder();
            sb.AppendLine("147VR WipRoom Flag Texture Audit (READ-ONLY - nothing was modified)");
            sb.AppendLine("Scene: " + scene.path);
            sb.AppendLine("Time: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine("Flag-ish renderers: " + renderers.Count);
            sb.AppendLine();

            var familyCounts = new Dictionary<string, int>();
            var problems = new List<string>();
            var flagBounds = new List<Bounds>();
            int textureless = 0, uvDegenerate = 0, lightmapped = 0, disabled = 0;
            int cullBackFlags = 0, instanced = 0, facingIntoWall = 0, zeroScale = 0;

            Vector3 roomCentre = ResolveRoomCentre(scene);
            sb.AppendLine("Room centre reference: " + Fmt(roomCentre));
            sb.AppendLine();

            foreach (Renderer renderer in renderers)
            {
                string path = GetPath(renderer.transform);
                string family = ClassifyFamily(renderer.name);
                familyCounts[family] = familyCounts.GetValueOrDefault(family) + 1;

                Mesh mesh = renderer is SkinnedMeshRenderer skinned ? skinned.sharedMesh
                                                                       : (renderer.GetComponent<MeshFilter>()?.sharedMesh);
                string meshPath = mesh != null ? AssetDatabase.GetAssetPath(mesh) : "<no mesh>";
                string expectedCountry = ExtractCountry(renderer.name);

                sb.AppendLine("RENDERER " + path);
                sb.AppendLine("  name=" + renderer.name + " | family=" + family +
                              " | activeInHierarchy=" + renderer.gameObject.activeInHierarchy +
                              " | enabled=" + renderer.enabled);
                if (!renderer.gameObject.activeInHierarchy || !renderer.enabled) disabled++;
                sb.AppendLine("  mesh=" + (mesh != null ? mesh.name : "<null>") +
                              " | verts=" + (mesh != null ? mesh.vertexCount.ToString() : "-") +
                              " | subMeshes=" + (mesh != null ? mesh.subMeshCount.ToString() : "-") +
                              " | asset=" + (string.IsNullOrEmpty(meshPath) ? "<generated/unknown>" : meshPath));
                sb.AppendLine("  lightmapIndex=" + renderer.lightmapIndex +
                              " | staticFlags=" + GameObjectUtility.GetStaticEditorFlags(renderer.gameObject) +
                              " | expectedCountry=" + (expectedCountry ?? "<not in name>"));
                if (renderer.lightmapIndex >= 0) lightmapped++;

                Material[] slots = renderer.sharedMaterials;
                for (int i = 0; i < slots.Length; i++)
                {
                    Material material = slots[i];
                    if (material == null)
                    {
                        sb.AppendLine("  slot[" + i + "]: NULL MATERIAL");
                        problems.Add(path + " slot[" + i + "] has no material");
                        continue;
                    }

                    Texture baseMap = material.GetTexture("_BaseMap") != null ? material.GetTexture("_BaseMap")
                                                                               : material.mainTexture;
                    string texPath = baseMap != null ? AssetDatabase.GetAssetPath(baseMap) : "";
                    string texName = baseMap != null ? baseMap.name : "<none>";

                    bool srgb = true;
                    if (!string.IsNullOrEmpty(texPath) && AssetImporter.GetAtPath(texPath) is TextureImporter importer)
                        srgb = importer.sRGBTexture;

                    sb.AppendLine("  slot[" + i + "] mat=" + material.name +
                                  " | shader=" + material.shader.name +
                                  " | baseMap=" + texName +
                                  " | dims=" + (baseMap != null ? baseMap.width + "x" + baseMap.height : "-") +
                                  " | sRGB=" + srgb +
                                  " | texPath=" + (string.IsNullOrEmpty(texPath) ? "<none/external>" : texPath));

                    if (baseMap == null)
                    {
                        textureless++;
                        problems.Add(path + " slot[" + i + "] material '" + material.name + "' has NO albedo texture");
                    }
                    else
                    {
                        string texCountry = ExtractCountry(texName);
                        if (expectedCountry != null && texCountry != null &&
                            !string.Equals(expectedCountry, texCountry, StringComparison.OrdinalIgnoreCase))
                        {
                            problems.Add(path + " expects '" + expectedCountry + "' but is bound to texture '" +
                                         texName + "' (" + texCountry + ")");
                        }
                    }

                    string uvInfo = DescribeSubmeshUv(mesh, i);
                    sb.AppendLine("             uv[" + i + "]=" + uvInfo);
                    if (uvInfo.StartsWith("DEGENERATE")) uvDegenerate++;

                    if (material.HasProperty("_Cull"))
                    {
                        float cull = material.GetFloat("_Cull");
                        string cullName = cull <= 0f ? "Off" : cull <= 1f ? "Front(back faces kept)" : "Back(front faces only)";
                        sb.AppendLine("             _Cull=" + cull + " (" + cullName + ")" +
                                      " | _Surface=" + material.GetFloat("_Surface") +
                                      " | baseColorTint=" + material.GetColor("_BaseColor"));
                        if (cull >= 2f) cullBackFlags++;
                    }
                    if (material.enableInstancing) instanced++;
                }

                Bounds worldBounds = renderer.bounds;
                flagBounds.Add(worldBounds);
                Vector3 lossy = renderer.transform.lossyScale;
                float volume = worldBounds.size.x * worldBounds.size.y * worldBounds.size.z;
                sb.AppendLine("  WORLD bounds center=" + Fmt(worldBounds.center) + " size=" + Fmt(worldBounds.size) +
                              " volume=" + volume.ToString("F4") + " m3");
                sb.AppendLine("  lossyScale=" + Fmt(lossy) + " | layer=" + LayerMask.LayerToName(renderer.gameObject.layer) +
                              "(" + renderer.gameObject.layer + ") | lightProbeUsage=" + renderer.lightProbeUsage +
                              " | reflectionProbeUsage=" + renderer.reflectionProbeUsage);
                if (volume < 0.0001f)
                {
                    zeroScale++;
                    problems.Add(path + " world volume is ~" + volume.ToString("F6") + " m3 - the flag is scaled to (near) nothing");
                }

                Vector3 toRoomCentre = roomCentre - worldBounds.center;
                toRoomCentre.y = 0f;
                if (toRoomCentre.sqrMagnitude > 0.0001f)
                {
                    toRoomCentre.Normalize();
                    Vector3 averageNormal = AverageWorldNormal(renderer, mesh);
                    if (averageNormal != Vector3.zero)
                    {
                        float facing = Vector3.Dot(averageNormal, toRoomCentre);
                        sb.AppendLine("  facing: avgWorldNormal=" + Fmt(averageNormal) +
                                      " dot(toward room centre)=" + facing.ToString("F3") +
                                      (facing > 0.15f ? "  -> visible side faces the room"
                                       : facing < -0.15f ? "  -> *** FRONT FACE POINTS INTO THE WALL"
                                       : "  -> edge-on to the room"));
                        if (facing < -0.15f)
                        {
                            facingIntoWall++;
                            problems.Add(path + " front face points into the wall (dot=" + facing.ToString("F2") +
                                         ") - with _Cull=Back it renders only from behind the wall");
                        }
                    }
                }
                sb.AppendLine();
            }

            // Cross-check the country -> material usage census for this scene.
            sb.AppendLine("COUNTRY MATERIAL USAGE CENSUS (renderers in this Scene per country material)");
            var usedByCountry = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (Renderer renderer in renderers)
                foreach (Material material in renderer.sharedMaterials.Where(m => m != null))
                {
                    string country = ExtractCountry(material.name);
                    if (country != null && Countries.Contains(country, StringComparer.OrdinalIgnoreCase))
                        usedByCountry[country] = usedByCountry.GetValueOrDefault(country) + 1;
                }
            foreach (string country in Countries)
            {
                int count = usedByCountry.GetValueOrDefault(country);
                sb.AppendLine("  " + country + ": " + count + (count == 1 ? "" : count == 0 ? "   <-- NOT USED" : "   <-- USED " + count + "x"));
            }

            sb.AppendLine();
            sb.AppendLine("SUMMARY");
            foreach (KeyValuePair<string, int> pair in familyCounts.OrderBy(p => p.Key))
                sb.AppendLine("  family " + pair.Key + ": " + pair.Value + " renderers");
            sb.AppendLine("  textureless slots: " + textureless);
            sb.AppendLine("  degenerate UV submeshes: " + uvDegenerate);
            sb.AppendLine("  renderers with a lightmap index (baked GI can wash albedo): " + lightmapped);
            sb.AppendLine("  hidden/disabled flag renderers: " + disabled);
            sb.AppendLine("  near-zero-volume flags (scaled away): " + zeroScale);
            sb.AppendLine("  materials using _Cull=Back: " + cullBackFlags + " (a flag facing into the wall is then invisible from the room)");
            sb.AppendLine("  front faces pointing into the wall: " + facingIntoWall);
            sb.AppendLine("  materials with GPU instancing on: " + instanced);
            if (flagBounds.Count > 0)
            {
                Bounds row = flagBounds[0];
                for (int i = 1; i < flagBounds.Count; i++) row.Encapsulate(flagBounds[i]);
                sb.AppendLine("  FLAG ROW EXTENT (world): min=" + Fmt(row.min) + " max=" + Fmt(row.max) +
                              " size=" + Fmt(row.size));
                sb.AppendLine("  row Y range tells whether the flags sit in the visible damask band or above/behind the cornice");
            }
            sb.AppendLine("  binding problems: " + problems.Count);
            foreach (string problem in problems.Take(60)) sb.AppendLine("    - " + problem);
            if (problems.Count > 60) sb.AppendLine("    ... " + (problems.Count - 60) + " more");

            System.IO.Directory.CreateDirectory("Logs");
            System.IO.File.WriteAllText(ReportPath, sb.ToString());

            string headline = "Flag renderers: " + renderers.Count +
                              " | textureless slots: " + textureless +
                              " | degenerate UVs: " + uvDegenerate +
                              " | lightmapped: " + lightmapped +
                              " | binding problems: " + problems.Count;
            Debug.Log("[WipRoomFlagTextureAudit] " + headline + "\nFull report: " + ReportPath);
            EditorUtility.DisplayDialog("147VR WipRoom", headline + "\n\nReport written to " + ReportPath +
                                        "\nNo scene data was modified.", "OK");
        }

        private static bool IsFlagRenderer(Renderer renderer)
        {
            if (renderer.name.IndexOf("flag", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return renderer.sharedMaterials.Any(m => m != null &&
                m.name.IndexOf("flagmat", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static string ClassifyFamily(string objectName)
        {
            if (objectName.StartsWith("FlagMesh", StringComparison.OrdinalIgnoreCase)) return "FlagMesh_* (raw rip)";
            if (objectName.IndexOf("SM_147VR_AAA_Flag", StringComparison.OrdinalIgnoreCase) >= 0) return "SM_147VR_AAA_Flag_* (staged)";
            if (objectName.IndexOf("flag", StringComparison.OrdinalIgnoreCase) >= 0) return "other flag-named";
            return "material-only match";
        }

        /// <summary>Pulls the country token out of a mesh/material/texture name.
        /// Longest names are tried first and only whole-token matches count, so
        /// "uk" can never steal a match from "ukraine" or "turkey".</summary>
        private static string ExtractCountry(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            string lower = name.ToLowerInvariant();

            foreach (string country in Countries.OrderByDescending(c => c.Length))
            {
                int idx = lower.IndexOf(country, StringComparison.Ordinal);
                while (idx >= 0)
                {
                    bool leftOk = idx == 0 || !char.IsLetterOrDigit(lower[idx - 1]);
                    int end = idx + country.Length;
                    bool rightOk = end == lower.Length || !char.IsLetterOrDigit(lower[end]);
                    if (leftOk && rightOk) return country;
                    idx = lower.IndexOf(country, idx + 1, StringComparison.Ordinal);
                }
            }
            return null;
        }

        private static string DescribeSubmeshUv(Mesh mesh, int subMesh)
        {
            if (mesh == null || mesh.uv == null || mesh.uv.Length == 0) return "NO UV0 CHANNEL";
            if (mesh.vertexCount > MaxMeshVerticesForUvScan) return "SKIPPED (mesh too large: " + mesh.vertexCount + " verts)";
            if (subMesh >= mesh.subMeshCount) return "SUBMESH OUT OF RANGE";

            var triangles = mesh.GetTriangles(subMesh);
            if (triangles.Length == 0) return "EMPTY SUBMESH";

            var uvs = mesh.uv;
            float minU = float.MaxValue, minV = float.MaxValue, maxU = float.MinValue, maxV = float.MinValue;
            foreach (int index in triangles)
            {
                if (index >= uvs.Length) continue;
                Vector2 uv = uvs[index];
                if (uv.x < minU) minU = uv.x;
                if (uv.x > maxU) maxU = uv.x;
                if (uv.y < minV) minV = uv.y;
                if (uv.y > maxV) maxV = uv.y;
            }

            float spanU = maxU - minU, spanV = maxV - minV;
            string detail = "u[" + minU.ToString("F3") + ".." + maxU.ToString("F3") + "] " +
                            "v[" + minV.ToString("F3") + ".." + maxV.ToString("F3") + "]";
            string span = " (span " + spanU.ToString("F3") + " x " + spanV.ToString("F3") + ")";
            if (spanU < 0.05f || spanV < 0.05f)
                return "DEGENERATE UVs " + detail + span +
                       " -> the texture collapses to a sliver, so albedo reads as one flat colour";
            if (spanU > 1.5f || spanV > 1.5f)
                return "TILED/OVERSCAN " + detail + span;
            return "OK " + detail + span;
        }

        /// <summary>Compact world-space vector text.</summary>
        private static string Fmt(Vector3 v) =>
            "(" + v.x.ToString("F2") + ", " + v.y.ToString("F2") + ", " + v.z.ToString("F2") + ")";

        /// <summary>Horizontal reference point the flags should face. The room is authored around
        /// its own root, so the root position is the most honest "inside the room" point; if the
        /// root is absent, fall back to the centre of every renderer bound in the Scene.</summary>
        private static Vector3 ResolveRoomCentre(Scene scene)
        {
            GameObject root = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "NarisVintage_Room");
            if (root != null) return root.transform.position;

            var all = scene.GetRootGameObjects()
                .SelectMany(g => g.GetComponentsInChildren<Renderer>(true))
                .ToList();
            if (all.Count == 0) return Vector3.zero;
            Bounds union = all[0].bounds;
            for (int i = 1; i < all.Count; i++) union.Encapsulate(all[i].bounds);
            return union.center;
        }

        /// <summary>Area-weighted world normal of the first sampled triangles (0 when unavailable).</summary>
        private static Vector3 AverageWorldNormal(Renderer renderer, Mesh mesh)
        {
            if (mesh == null) return Vector3.zero;
            int[] triangles = mesh.triangles;
            Vector3[] normals = mesh.normals;
            if (triangles.Length < 3 || normals.Length == 0) return Vector3.zero;

            Matrix4x4 localToWorld = renderer.transform.localToWorldMatrix;
            Vector3 sum = Vector3.zero;
            int samples = 0;
            for (int i = 0; i + 2 < triangles.Length && samples < 240; i += 3)
            {
                if (triangles[i] >= normals.Length || triangles[i + 1] >= normals.Length || triangles[i + 2] >= normals.Length)
                    continue;
                Vector3 a = mesh.vertices[triangles[i]];
                Vector3 b = mesh.vertices[triangles[i + 1]];
                Vector3 c = mesh.vertices[triangles[i + 2]];
                Vector3 face = Vector3.Cross(b - a, c - a);
                if (face.sqrMagnitude < 1e-12f) continue;

                Vector3 nObj = normals[triangles[i]];
                if (nObj.sqrMagnitude < 1e-12f) continue;
                if (Vector3.Dot(nObj, face) < 0f) nObj = -nObj;   // follow the winding, like the rasteriser

                Vector3 n = localToWorld.MultiplyVector(nObj);
                if (n.sqrMagnitude < 1e-12f) continue;
                sum += n.normalized;
                samples++;
            }
            return samples == 0 ? Vector3.zero : sum.normalized;
        }

        private static string GetPath(Transform transform)
        {
            var parts = new List<string>();
            Transform current = transform;
            while (current != null) { parts.Insert(0, current.name); current = current.parent; }
            return string.Join("/", parts);
        }
    }
}
#endif
