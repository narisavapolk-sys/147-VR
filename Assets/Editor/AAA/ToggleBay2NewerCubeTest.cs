using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ToggleBay2NewerCubeTest
{
    private const string RootName = "ROOT_PoolTable_02";
    private const string TargetName = "SM_Cube.018";
    private const string ScenePath = "Assets/Scenes/147VR_MainScene_WipRoom.unity";
    [MenuItem("147VR/AAA/Debug/Temporarily Disable Bay2 Newer Cube")]
    private static void DisableNewerCube()
    {
        var root = GameObject.Find(RootName);
        if (root == null) { Debug.LogError("[Bay2 Cube Test] ROOT not found."); return; }
        var matches = root.GetComponentsInChildren<Transform>(true)
            .Where(t => t.name == TargetName).Select(t => t.gameObject).ToArray();
        if (matches.Length != 1)
        {
            Debug.LogError($"[Bay2 Cube Test] Expected one {TargetName}; found {matches.Length}. No change.");
            return;
        }
        var target = matches[0];
        if (!target.activeSelf)
        {
            WriteReport(target, "ALREADY_INACTIVE");
            Debug.Log($"[Bay2 Cube Test] {TargetName} already inactive.");
            return;
        }
        Undo.RegisterCompleteObjectUndo(target, "Temporarily Disable Bay2 SM_Cube.018");
        target.SetActive(false);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        WriteReport(target, "DISABLED_TEMPORARILY");
        Debug.LogWarning($"[Bay2 Cube Test] Temporarily disabled {TargetName}. Scene NOT saved. Restore menu available.");
    }

    [MenuItem("147VR/AAA/Debug/Restore Bay2 Newer Cube")]
    private static void RestoreNewerCube()
    {
        var root = GameObject.Find(RootName);
        if (root == null) { Debug.LogError("[Bay2 Cube Test] ROOT not found."); return; }
        var matches = root.GetComponentsInChildren<Transform>(true)
            .Where(t => t.name == TargetName).Select(t => t.gameObject).ToArray();
        if (matches.Length != 1)
        {
            Debug.LogError($"[Bay2 Cube Test] Expected one {TargetName}; found {matches.Length}. No change.");
            return;
        }
        var target = matches[0];
        Undo.RegisterCompleteObjectUndo(target, "Restore Bay2 SM_Cube.018");
        target.SetActive(true);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        WriteReport(target, "RESTORED_ACTIVE");
        Debug.Log($"[Bay2 Cube Test] Restored {TargetName}. Scene NOT saved.");
    }

    private static void WriteReport(GameObject target, string action)
    {
        var projectRoot = Directory.GetParent(Application.dataPath).FullName;
        var logs = Path.Combine(projectRoot, "Logs");
        Directory.CreateDirectory(logs);
        File.WriteAllText(Path.Combine(logs, "Bay2_NewerCube_Test.txt"),
            $"Time: {DateTime.Now:O}\nScene: {SceneManager.GetActiveScene().name}\nAction: {action}\n" +
            $"Target: {target.name}\nActiveSelf: {target.activeSelf}\n" +
            $"ActiveInHierarchy: {target.activeInHierarchy}\nSceneSaved: false (test only)\n");
    }
}
