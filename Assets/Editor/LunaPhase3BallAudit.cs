using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class LunaPhase3BallAudit
{
    public static void Run()
    {
        const string path = "Assets/AAA/ImportedSnooker/Prefab_WPBSA_12Foot_Snooker.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var names = new[] { "White_CueBall", "Red", "Yellow", "Green", "Brown", "Blue", "Pink", "Black" };
            var all = root.GetComponentsInChildren<Transform>(true);
            var found = new List<Transform>();
            foreach (var t in all)
            {
                foreach (var n in names)
                    if (t.name == n || (n == "Red" && t.name.StartsWith("Red", StringComparison.Ordinal)))
                    { found.Add(t); break; }
            }
            Debug.Log($"[PHASE3 STATIC] source={path} candidateBallObjects={found.Count}");
            foreach (var t in found)
                Debug.Log($"[PHASE3 STATIC] {t.name} local={t.localPosition:F6} world={t.position:F6} parent={t.parent?.name}");
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        EditorApplication.Exit(0);
    }
}
