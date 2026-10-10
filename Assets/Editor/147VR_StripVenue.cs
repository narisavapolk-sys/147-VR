#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Strip the scene down to the table + physics + lights + camera, hiding the 3D venue,
// crowd billboards and ceiling planes, so the panoramic skybox becomes the whole arena.
// Deactivates (not deletes) -> reversible. MenuItem-only.
public static class _147VR_StripVenue
{
    [MenuItem("147VR/Art/Strip Venue -> Table Only (for skybox)")]
    public static void Strip()
    {
        var scene = EditorSceneManager.GetActiveScene();
        int n = 0;
        foreach (var t in GameObject.FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            string nm = t.name;
            if (nm == "147VR_VENUE_CRUCIBLE" || nm.StartsWith("Crowd_BB_") || nm.StartsWith("Ceil_BB_"))
            {
                if (t.gameObject.activeSelf) { t.gameObject.SetActive(false); n++; }
            }
        }
        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("147VR/Strip: hid " + n + " venue/billboard/ceiling objects; kept table + physics + lights + camera. Now run 'Apply Arena Skybox (panorama)'.");
    }

    [MenuItem("147VR/Art/Restore Venue (un-hide)")]
    public static void Restore()
    {
        var scene = EditorSceneManager.GetActiveScene();
        int n = 0;
        foreach (var t in GameObject.FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            string nm = t.name;
            if (nm == "147VR_VENUE_CRUCIBLE" || nm.StartsWith("Crowd_BB_") || nm.StartsWith("Ceil_BB_"))
            {
                if (!t.gameObject.activeSelf) { t.gameObject.SetActive(true); n++; }
            }
        }
        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("147VR/Restore: re-enabled " + n + " venue/billboard/ceiling objects.");
    }
}
#endif
