using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Opens every scene in EditorBuildSettings and reports missing scripts / broken components.
/// Runnable from the menu (Tools > Validate Quest MR Setup) or Unity batch mode:
///   Unity.exe -batchmode -quit -projectPath &lt;path&gt; -executeMethod ProjectValidator.Validate -logFile validate.log
/// </summary>
public static class ProjectValidator
{
    public static void Validate()
    {
        int missingCount = 0;
        int rootCount = 0;

        foreach (EditorBuildSettingsScene buildScene in EditorBuildSettings.scenes)
        {
            if (buildScene == null || !buildScene.enabled)
                continue;

            Scene scene = EditorSceneManager.OpenScene(buildScene.path, OpenSceneMode.Single);
            Debug.Log($"[Validator] Opened scene: {buildScene.path}");

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                rootCount++;
                Debug.Log($"[Validator]   Root: {root.name}");
                foreach (MonoBehaviour component in root.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (component == null)
                    {
                        missingCount++;
                        Debug.LogError($"[Validator]   Missing script component under '{root.name}' (check .meta guids)");
                    }
                }
            }
        }

        Debug.Log($"[Validator] Done. Roots={rootCount} MissingScripts={missingCount}");

        if (missingCount > 0)
        {
            Debug.LogError("[Validator] FAILED: scene contains missing script references.");
            EditorApplication.Exit(1);
        }
        else
        {
            Debug.Log("[Validator] PASSED: no missing scripts found.");
            EditorApplication.Exit(0);
        }
    }
}
