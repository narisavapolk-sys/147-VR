using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class LunaPhase3MainSceneBallAudit
{
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/147VR_MainScene.unity", OpenSceneMode.Single);
        var names = new[] { "White_CueBall", "Red", "Yellow", "Green", "Brown", "Blue", "Pink", "Black" };
        var all = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var found = new List<Transform>();
        foreach (var t in all)
        {
            foreach (var n in names)
                if (t.name == n || (n == "Red" && t.name.StartsWith("Red", StringComparison.Ordinal)))
                { found.Add(t); break; }
        }
        Debug.Log($"[PHASE3 MAINSCENE] candidateBallObjects={found.Count}");
        foreach (var t in found)
            Debug.Log($"[PHASE3 MAINSCENE] {t.name} world={t.position:F6} active={t.gameObject.activeInHierarchy} parent={t.parent?.name}");
        EditorApplication.Exit(0);
    }
}
