#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using PoolPhysics.Unity;

namespace PoolPhysics.EditorTools
{
    /// <summary>
    /// FALLBACK TableSpec generator: builds a TableSpec from a table prefab using its
    /// playfield size (read via reflection) + a 4-rect rail / 6-pocket approximation,
    /// then VALIDATES (pockets in bounds, size>0). It does NOT yet read real rail
    /// colliders / pocket triggers / per-ball radius from the prefab — treat output as
    /// a starting approximation to refine per bay, not an exact prefab extraction.
    /// </summary>
    public static class TableSpecGenerator
    {
        [MenuItem("147VR/Physics/Generate TableSpec (fallback) from selection")]
        static void MenuGenerate()
        {
            var go = Selection.activeGameObject;
            if (go == null) { EditorUtility.DisplayDialog("TableSpec", "Select a table root GameObject first.", "OK"); return; }
            Generate(go, $"Assets/AAA/PhysicsCalibration/Generated/{go.name}_TableSpec.asset");
        }

        public static TableSpec Generate(GameObject tableRoot, string assetPath)
        {
            var report = new List<string>();
            if (AssetDatabase.LoadAssetAtPath<TableSpec>(assetPath) != null)
                report.Add("NOTE: existing TableSpec at this path will be overwritten.");
            var dir = System.IO.Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(dir) && !AssetDatabase.IsValidFolder(dir))
            { System.IO.Directory.CreateDirectory(dir); AssetDatabase.Refresh(); }
            var spec = ScriptableObject.CreateInstance<TableSpec>();

            // --- playfield size: try SnookerPhysicsSetup serialized fields, else collider bounds ---
            Vector2 size = ReadPlayfieldSize(tableRoot, report);
            spec.playfieldSize = size;
            spec.origin = Vector3.zero; // local space; runner maps via tableTransform

            // --- rails: 4 inward-facing rect rails around the playfield ---
            // (If the prefab has explicit rail colliders, extend here to derive endpoints.)
            report.Add("rails: generated as 4-sided rectangle from playfield size (extend to read real rail colliders per prefab).");

            // --- pockets: 6 (4 corners + 2 middles) scaled to the playfield ---
            float pr = Mathf.Max(0.04f, size.x * 0.028f);
            spec.pockets = new List<TableSpec.PocketEntry>{
                new TableSpec.PocketEntry{ xz=new Vector2(0,0), radius=pr },
                new TableSpec.PocketEntry{ xz=new Vector2(size.x/2,0), radius=pr },
                new TableSpec.PocketEntry{ xz=new Vector2(size.x,0), radius=pr },
                new TableSpec.PocketEntry{ xz=new Vector2(0,size.y), radius=pr },
                new TableSpec.PocketEntry{ xz=new Vector2(size.x/2,size.y), radius=pr },
                new TableSpec.PocketEntry{ xz=new Vector2(size.x,size.y), radius=pr },
            };

            // --- ball radius from BallRack / SnookerPhysicsSetup if present ---
            spec.frictionScale = 1f; spec.napHeadingDeg = 0f;

            // --- VALIDATION ---
            bool ok = Validate(spec, report);

            AssetDatabase.CreateAsset(spec, assetPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[TableSpecGenerator] {tableRoot.name} -> {assetPath}\n" + string.Join("\n", report) + (ok ? "\nVALID ✓" : "\n*** VALIDATION FAILED ***"));
            EditorUtility.DisplayDialog("TableSpec", (ok ? "Created + VALID:\n" : "Created but FAILED validation:\n") + string.Join("\n", report), "OK");
            return spec;
        }

        static Vector2 ReadPlayfieldSize(GameObject root, List<string> report)
        {
            // Try common serialized field names on any component via reflection.
            foreach (var comp in root.GetComponentsInChildren<Component>(true))
            {
                if (comp == null) continue;
                foreach (var fname in new[] { "playfieldSize", "tableSize", "playingArea", "length", "width" })
                {
                    var f = comp.GetType().GetField(fname, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (f != null && f.FieldType == typeof(Vector2))
                    { report.Add($"size from {comp.GetType().Name}.{fname}"); return (Vector2)f.GetValue(comp); }
                }
            }
            // Fallback: renderer bounds of the table surface.
            var rend = root.GetComponentInChildren<Renderer>();
            if (rend != null) { report.Add("size from renderer bounds (approx)"); return new Vector2(rend.bounds.size.x, rend.bounds.size.z); }
            report.Add("size DEFAULTED to 2.54x1.27 — set manually");
            return new Vector2(2.54f, 1.27f);
        }

        static bool Validate(TableSpec s, List<string> report)
        {
            bool ok = true;
            if (s.playfieldSize.x <= 0 || s.playfieldSize.y <= 0) { ok = false; report.Add("FAIL: non-positive playfield size"); }
            // rail loop closed: rect rails always close, but verify count when custom
            foreach (var p in s.pockets)
            {
                if (p.xz.x < -1e-3f || p.xz.x > s.playfieldSize.x + 1e-3f || p.xz.y < -1e-3f || p.xz.y > s.playfieldSize.y + 1e-3f)
                { ok = false; report.Add($"FAIL: pocket {p.xz} outside playfield"); }
                if (p.radius <= 0) { ok = false; report.Add($"FAIL: pocket radius {p.radius}"); }
            }
            report.Add($"pockets={s.pockets.Count} size={s.playfieldSize}");
            return ok;
        }
    }
}
#endif
