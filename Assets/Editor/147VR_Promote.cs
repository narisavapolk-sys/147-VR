#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Promote the assembled V008_TEST scene into the real MainScene, backing up the original first.
public static class _147VR_Promote
{
    const string MAIN = "Assets/Scenes/147VR_MainScene.unity";
    const string TEST = "Assets/Scenes/147VR_MainScene_V008_TEST.unity";
    const string BACKUP = "Assets/Scenes/147VR_MainScene_PRE_V008.unity";

    [MenuItem("147VR/Art/Promote V008_TEST -> MainScene (with backup)")]
    public static void Promote()
    {
        // 1) back up current MainScene (once)
        if (System.IO.File.Exists(MAIN) && !System.IO.File.Exists(BACKUP))
        {
            EditorSceneManager.OpenScene(MAIN, OpenSceneMode.Single);
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), BACKUP);
            Debug.Log("147VR/Promote: original MainScene backed up to " + BACKUP);
        }
        // 2) open the assembled test scene and save it AS MainScene
        var t = EditorSceneManager.OpenScene(TEST, OpenSceneMode.Single);
        if (!t.IsValid()) { Debug.LogError("147VR/Promote: test scene not found " + TEST); return; }
        bool ok = EditorSceneManager.SaveScene(t, MAIN);
        Debug.Log("147VR/Promote: " + (ok ? "MainScene now = V008 assembled (backup kept). Revert via git or load " + BACKUP : "save failed"));
    }
}
#endif
