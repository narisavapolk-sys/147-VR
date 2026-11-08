#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace _147VR.EditorTools
{
    /// <summary>
    /// Repairs the two proven material defects through the Material API only.
    ///
    /// Why not edit .mat text: the previous attempt did that and produced
    ///   "  - - _EMISSION"
    /// which made Unity fail to parse 6 materials (Parser Failure at line 18/19/31/32). All of
    /// those files were reverted with git. This tool never writes YAML by hand - Unity serialises
    /// the assets itself, and the tool re-reads every asset from disk afterwards to prove the
    /// change actually landed.
    ///
    /// Evidence it works from (all paths verified on disk before writing this file):
    ///  * 20 Rebound materials have a _BaseMap GUID that resolves to NO asset anywhere, all with
    ///    tint (1,1,1), i.e. they render white: BarPultA/B, BCStoolsAndShelves, BottleHolders,
    ///    CashDesk, Sampagne, WhiskeyBottles, WineBottles, MP_Carpet (two name variants each).
    ///    Mat_PannelFrame also has a dead GUID but no name-matching texture exists, so it is
    ///    reported and left alone rather than guessed.
    ///  * MP_Carpet: the room FBX .fbm folder ships Carpet_01/Carpet_02, which already belong to
    ///    MP8_Carpet_8BP_A/B. Binding Carpet_01 to MP_Carpet was tried and the owner rejected the
    ///    pattern, so the owner chose the MiraclePool carpet instead.
    ///  * Emission was zeroed on the scene-bound copies while the single-prefix sibling in the
    ///    same folder (or the prepared counterpart) still holds the rip-authored values, so the
    ///    numbers are COPIED from a real asset, never invented here.
    ///
    /// Touches material assets only. No Scene, Prefab, mesh or ProjectSettings change.
    /// </summary>
    public static class WipRoomMaterialRepair
    {
        private const string Rebound = "Assets/AAA/Imports/naris_vintage/Rebound/";
        private const string RoomTex = "Assets/147 main/RoomTextures/";
        private const string BarTex = "Assets/AAA/ClubVenue/Bowlliards2_bar/textures/";
        private const string PrepTex = "Assets/AAA/Imports/NeonVintageArtPass/textures/";
        private const string PrepMat = "Assets/AAA/Imports/NeonVintageArtPass/Materials/";
        private const string ReportPath = "Logs/WipRoom_Material_Repair.txt";

        /// <summary>material -> albedo texture, both naming variants of each broken material.</summary>
        private static readonly (string Material, string Texture, string Label)[] Rebinds =
        {
            (Rebound + "147VR_147VR_147VR_Mat_BarPultA.mat", RoomTex + "0191_T_BarPultA_BaseColor.png", "bar counter A"),
            (Rebound + "147VR_147VR_Mat_BarPultA.mat",       RoomTex + "0191_T_BarPultA_BaseColor.png", "bar counter A sibling"),
            (Rebound + "147VR_147VR_147VR_Mat_BarPultB.mat", RoomTex + "0139_T_BarPultB_BaseColor.png", "bar counter B"),
            (Rebound + "147VR_147VR_Mat_BarPultB.mat",       RoomTex + "0139_T_BarPultB_BaseColor.png", "bar counter B sibling"),
            (Rebound + "147VR_147VR_147VR_Mat_BCStoolsAndShelves.mat", RoomTex + "0151_T_BCStoolsAndShelves_BaseColor.png", "bar stools + shelves"),
            (Rebound + "147VR_147VR_Mat_BCStoolsAndShelves.mat",       RoomTex + "0151_T_BCStoolsAndShelves_BaseColor.png", "bar stools + shelves sibling"),
            (Rebound + "147VR_147VR_147VR_Mat_BottleHolders.mat", BarTex + "0173_T_BottleHolders_1_BaseColor.png", "bottle holders"),
            (Rebound + "147VR_147VR_Mat_BottleHolders.mat",       BarTex + "0173_T_BottleHolders_1_BaseColor.png", "bottle holders sibling"),
            (Rebound + "147VR_147VR_147VR_Mat_CashDesk.mat",   BarTex + "0203_T_CashDesk_DefaultMaterial_BaseColor.png", "cash desk"),
            (Rebound + "147VR_147VR_Mat_CashDesk.mat",         BarTex + "0203_T_CashDesk_DefaultMaterial_BaseColor.png", "cash desk sibling"),
            (Rebound + "147VR_147VR_147VR_Mat_Sampagne.mat",   PrepTex + "0187_T_Sampagne_1_BaseColor.png", "champagne"),
            (Rebound + "147VR_147VR_Mat_Sampagne.mat",         PrepTex + "0187_T_Sampagne_1_BaseColor.png", "champagne sibling"),
            (Rebound + "147VR_147VR_147VR_Mat_WhiskeyBottles.mat", BarTex + "0194_T_WhiskeyBottles_1_BaseColor.png", "whiskey bottles"),
            (Rebound + "147VR_147VR_Mat_WhiskeyBottles.mat",       BarTex + "0194_T_WhiskeyBottles_1_BaseColor.png", "whiskey bottles sibling"),
            (Rebound + "147VR_147VR_147VR_Mat_WineBottles.mat", BarTex + "0128_T_WineBottles_1_BaseColor.png", "wine bottles"),
            (Rebound + "147VR_147VR_Mat_WineBottles.mat",       BarTex + "0128_T_WineBottles_1_BaseColor.png", "wine bottles sibling"),
            // Owner decision 2026-11-08: MP_Carpet gets the MiraclePool carpet, NOT Carpet_01
            // (that one already belongs to MP8_Carpet_8BP_A and the owner rejected the pattern).
            (Rebound + "147VR_147VR_MP_Carpet.mat", "Assets/AAA/ClubVenue/MiraclePool_vintage/textures/Carpet.png", "main hall carpet (owner choice)"),
            (Rebound + "147VR_MP_Carpet.mat",       "Assets/AAA/ClubVenue/MiraclePool_vintage/textures/Carpet.png", "main hall carpet sibling"),
        };

        /// <summary>Deliberately skipped: dead GUID but no name-matching texture exists anywhere.</summary>
        private static readonly string[] SkipNoTexture =
        {
            Rebound + "147VR_147VR_147VR_Mat_PannelFrame.mat",
            Rebound + "147VR_147VR_Mat_PannelFrame.mat",
        };

        /// <summary>target -> source whose emission colour/keywords are copied verbatim.</summary>
        private static readonly (string Target, string Source, string EmissiveTexture, string Label)[] Emissions =
        {
            (Rebound + "147VR_147VR_lightbulb_01_glass.mat", Rebound + "147VR_lightbulb_01_glass.mat",
             RoomTex + "lightbulb_01_emissive.png", "bulb glass x12"),
            (Rebound + "147VR_147VR_lightbulb_01_base.mat",  Rebound + "147VR_lightbulb_01_base.mat",
             RoomTex + "lightbulb_01_emissive.png", "bulb filament x12"),
            (Rebound + "147VR_147VR_147vr_mat_glow_Bay1.mat", Rebound + "147VR_147vr_mat_glow_Bay1.mat", null, "under-table neon bay1"),
            (Rebound + "147VR_147VR_147vr_mat_glow_Bay2.mat", Rebound + "147VR_147vr_mat_glow_Bay2.mat", null, "under-table neon bay2"),
            (Rebound + "147VR_147VR_147vr_mat_glow_Bay3.mat", Rebound + "147VR_147vr_mat_glow_Bay3.mat", null, "under-table neon bay3"),
            (Rebound + "147VR_147VR_147vr_mat_glow_Bay4.mat", Rebound + "147VR_147vr_mat_glow_Bay4.mat", null, "under-table neon bay4"),
            (Rebound + "147VR_147VR Stage _ Warm Marquee.mat", PrepMat + "147VR Stage _ Warm Marquee.mat", null, "marquee neon"),
        };

        [MenuItem("147VR/AAA/NeonVintage/WipRoom/12) Repair Dead Texture Refs + Emission (Assets Only)")]
        public static void Repair()
        {
            var sb = new StringBuilder();
            sb.AppendLine("147VR MATERIAL REPAIR (Material API only - no hand-written YAML)");
            sb.AppendLine("Time: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine("Scope: material assets only. No Scene/Prefab/mesh/ProjectSettings change.");
            sb.AppendLine();

            var touched = new List<Material>();
            int rebound = 0, emissionFixed = 0, missing = 0;

            sb.AppendLine("A) DEAD _BaseMap REFERENCES");
            foreach ((string matPath, string texPath, string label) in Rebinds)
            {
                Material material = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                Texture texture = AssetDatabase.LoadAssetAtPath<Texture>(texPath);
                if (material == null) { sb.AppendLine("  MISSING MATERIAL  " + matPath); missing++; continue; }
                if (texture == null) { sb.AppendLine("  MISSING TEXTURE   " + texPath + " for " + Path.GetFileName(matPath)); missing++; continue; }

                Texture before = material.GetTexture("_BaseMap");
                Undo.RecordObject(material, "147VR Material Repair");
                material.SetTexture("_BaseMap", texture);
                if (material.HasProperty("_Color")) material.SetColor("_Color", texture ? Color.white : material.GetColor("_Color"));
                if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
                EditorUtility.SetDirty(material);
                touched.Add(material);
                rebound++;
                sb.AppendLine("  REBIND " + Path.GetFileName(matPath) + " [" + label + "]");
                sb.AppendLine("        before=" + (before != null ? before.name + " (guid resolves)" : "<none or dead reference>") +
                              " -> after=" + texture.name + " (" + texPath + ")");
            }
            foreach (string skip in SkipNoTexture)
                sb.AppendLine("  SKIPPED (no name-matching texture exists) " + Path.GetFileName(skip));

            sb.AppendLine();
            sb.AppendLine("B) EMISSION COPIED FROM THE UNCORRUPTED COUNTERPART");
            foreach ((string targetPath, string sourcePath, string emissivePath, string label) in Emissions)
            {
                Material target = AssetDatabase.LoadAssetAtPath<Material>(targetPath);
                Material source = AssetDatabase.LoadAssetAtPath<Material>(sourcePath);
                if (target == null || source == null)
                {
                    sb.AppendLine("  MISSING " + Path.GetFileName(targetPath) +
                                  " (target=" + (target != null) + " source=" + (source != null) + ")");
                    missing++;
                    continue;
                }

                Color emission = source.HasProperty("_EmissionColor") ? source.GetColor("_EmissionColor") : Color.black;
                if (emission.maxColorComponent <= 0.001f)
                {
                    sb.AppendLine("  SKIP (source has no emission to copy) " + Path.GetFileName(targetPath));
                    continue;
                }

                Undo.RecordObject(target, "147VR Material Repair");
                target.SetColor("_EmissionColor", emission);

                Texture emissiveMap = string.IsNullOrEmpty(emissivePath) ? null
                                    : AssetDatabase.LoadAssetAtPath<Texture>(emissivePath);
                if (emissiveMap != null) target.SetTexture("_EmissionMap", emissiveMap);

                // Build the wanted keyword list EXPLICITLY. Do not trust source.shaderKeywords:
                // a previous run stripped _EMISSION from the single-prefix sources as well, so
                // copying from them silently produced an empty list and the bulbs stayed black.
                // URP Lit gates emission behind "shader_feature_local_fragment _EMISSION"
                // (Lit.shader lines 128/265/437), so a non-black _EmissionColor REQUIRES it.
                var wantedKeywords = new List<string>();
                if (emission.maxColorComponent > 0.001f) wantedKeywords.Add("_EMISSION");
                foreach (string keyword in source.shaderKeywords)
                    if (!wantedKeywords.Contains(keyword)) wantedKeywords.Add(keyword);
                wantedKeywords.RemoveAll(k => k == "_NORMALMAP" && target.GetTexture("_BumpMap") == null);

                // Material.EnableKeyword() does NOT persist keywords into an asset .mat file.
                // Proof from the previous run: _EmissionColor landed but m_ValidKeywords stayed
                // [] and the bulbs/glows remained dark. Write the array through SerializedObject
                // instead - that is what Unity's own MaterialEditor does - and let Unity
                // serialise the file.
                var serialized = new SerializedObject(target);
                SerializedProperty keywordList = serialized.FindProperty("m_ValidKeywords");
                int addedKeywords = 0;
                if (keywordList != null && keywordList.isArray)
                {
                    foreach (string keyword in wantedKeywords)
                    {
                        bool present = false;
                        for (int i = 0; i < keywordList.arraySize; i++)
                            if (string.Equals(keywordList.GetArrayElementAtIndex(i).stringValue, keyword, StringComparison.Ordinal))
                            {
                                present = true;
                                break;
                            }
                        if (present) continue;
                        keywordList.InsertArrayElementAtIndex(keywordList.arraySize);
                        keywordList.GetArrayElementAtIndex(keywordList.arraySize - 1).stringValue = keyword;
                        addedKeywords++;
                    }
                    serialized.ApplyModifiedProperties();
                }
                EditorUtility.SetDirty(target);
                touched.Add(target);
                emissionFixed++;

                sb.AppendLine("  EMISSION " + Path.GetFileName(targetPath) + " [" + label + "]");
                sb.AppendLine("        copied (" + emission.r.ToString("F3") + ", " + emission.g.ToString("F3") + ", " +
                              emission.b.ToString("F3") + ", a=" + emission.a.ToString("F3") + ") from " + Path.GetFileName(sourcePath) +
                              " | wanted keywords=" + string.Join(",", wantedKeywords) + " (inserted " + addedKeywords + ")" +
                              " | emissiveMap=" + (emissiveMap != null ? emissiveMap.name : "none"));
            }

            if (touched.Count > 0) AssetDatabase.SaveAssets();

            sb.AppendLine();
            sb.AppendLine("C) SELF-CHECK (reloaded from disk after SaveAssets)");
            int failed = 0;
            foreach ((string matPath, string texPath, string label) in Rebinds)
            {
                Material m = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                Texture want = AssetDatabase.LoadAssetAtPath<Texture>(texPath);
                bool ok = m != null && want != null && m.GetTexture("_BaseMap") == want;
                if (!ok) failed++;
                sb.AppendLine("  " + (ok ? "PASS " : "FAIL ") + Path.GetFileName(matPath) + " _BaseMap=" +
                              (m != null && m.GetTexture("_BaseMap") != null ? m.GetTexture("_BaseMap").name : "<none>"));
            }
            foreach ((string targetPath, string sourcePath, string emissivePath, string label) in Emissions)
            {
                Material m = AssetDatabase.LoadAssetAtPath<Material>(targetPath);
                Material s = AssetDatabase.LoadAssetAtPath<Material>(sourcePath);
                if (m == null || s == null) { failed++; sb.AppendLine("  FAIL " + Path.GetFileName(targetPath) + " (asset missing)"); continue; }

                // Read the SAVED FILE, not the in-memory asset: after SaveAssets the loaded
                // Material can still report stale keyword state, which is precisely how the
                // previous run hid the fact that _EMISSION never landed.
                string targetText = File.Exists(targetPath) ? File.ReadAllText(targetPath) : "";
                string sourceText = File.Exists(sourcePath) ? File.ReadAllText(sourcePath) : "";
                string targetKeywords = FirstGroup(targetText, @"m_ValidKeywords:\n((?:  - .*\n)*)");
                bool colourLanded = Regex.IsMatch(targetText, @"_EmissionColor: \{r: " +
                    m.GetColor("_EmissionColor").r.ToString("F3", System.Globalization.CultureInfo.InvariantCulture).Replace('.', '\\') + @"[0-9]");
                bool emissionKeywordOnDisk = targetKeywords.Contains("_EMISSION");
                bool sourceStillHasItsKeyword = string.IsNullOrEmpty(sourceText) || sourceText.Contains("- _EMISSION")
                                                || !Regex.IsMatch(sourceText, @"_EmissionColor: \{r: (?!0, 0, 0)");
                bool ok = emissionKeywordOnDisk && m.GetColor("_EmissionColor").maxColorComponent > 0.001f;
                if (!ok) failed++;
                sb.AppendLine("  " + (ok ? "PASS " : "FAIL ") + Path.GetFileName(targetPath) +
                              " | on-disk keywords=" + (string.IsNullOrWhiteSpace(targetKeywords) ? "[]" : targetKeywords.Trim().Replace("\n", " ")) +
                              " | emissionColour=" + m.GetColor("_EmissionColor") +
                              (colourLanded ? " (written)" : "") +
                              (sourceStillHasItsKeyword ? "" : " | *** SOURCE LOST _EMISSION, RESTORE IT"));
            }

            sb.AppendLine();
            sb.AppendLine("SUMMARY rebound=" + rebound + " emission=" + emissionFixed + " missingInputs=" + missing +
                          " selfCheckFailures=" + failed + " assetsSaved=" + touched.Count);
            sb.AppendLine("Scene was NOT saved. Undo is grouped per material under '147VR Material Repair'.");

            Directory.CreateDirectory("Logs");
            File.WriteAllText(ReportPath, sb.ToString());
            Debug.Log("[MaterialRepair] rebound=" + rebound + " emission=" + emissionFixed +
                      " missing=" + missing + " selfCheckFailures=" + failed + " -> " + ReportPath);
            EditorUtility.DisplayDialog("147VR Material Repair",
                "Rebound textures: " + rebound + "\nEmission restored: " + emissionFixed +
                "\nMissing inputs: " + missing + "\nSelf-check failures: " + failed +
                "\n\nReport: " + ReportPath + "\nScene not saved. Focus the Scene view to see the result.", "OK");
        }

        /// <summary>First capture group of the pattern in the text, or empty.</summary>
        private static string FirstGroup(string text, string pattern)
        {
            Match m = Regex.Match(text, pattern);
            return m.Success && m.Groups.Count > 1 ? m.Groups[1].Value : string.Empty;
        }

        /// <summary>True when the serialized _BaseMap GUID no longer resolves to an asset.</summary>
        private static bool HasDeadReference(Material material)
        {
            string path = AssetDatabase.GetAssetPath(material);
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return false;
            string txt = File.ReadAllText(path);
            var match = System.Text.RegularExpressions.Regex.Match(
                txt, @"_BaseMap:\s*\n\s*m_Texture: \{fileID: 2800000, guid: ([0-9a-f]{32})");
            if (!match.Success) return false;
            return string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(match.Groups[1].Value));
        }
    }
}
#endif
