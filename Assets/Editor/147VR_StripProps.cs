#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.Collections.Generic;

// Hide non-table props (cue rack / cue / triangle under 147VR_PROPS_ROOT) and destroy the
// stray 2m placeholder "Cube" that ships inside the table FBX. Deactivate where possible
// (reversible); the placeholder Cube is destroyed. MenuItem-only.
public static class _147VR_StripProps
{
    [MenuItem("147VR/Art/Strip Props + Placeholder Cube")]
    public static void Strip()
    {
        var scene = EditorSceneManager.GetActiveScene();
        int hidden = 0, killed = 0;
        foreach (var t in GameObject.FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            string nm = t.name;
            if (nm == "147VR_PROPS_ROOT" || nm.ToLower().Contains("cuerack") || nm.ToLower().Contains("cue_rack")
                || nm.ToLower().Contains("triangle") || nm.ToLower().Contains("ballrack") || nm.ToLower().Contains("ball_rack"))
            {
                if (t.gameObject.activeSelf) { t.gameObject.SetActive(false); hidden++; }
            }
            // stray default cube placeholder (exact name "Cube", ~2m, at origin)
            if (nm == "Cube" && t.GetComponent<Renderer>() != null)
            {
                var b = t.GetComponent<Renderer>().bounds;
                if (b.size.x > 1.5f && b.size.x < 2.5f && b.size.y > 1.5f && b.size.z > 1.5f)
                { Object.DestroyImmediate(t.gameObject); killed++; }
            }
        }
        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log(string.Format("147VR/StripProps: hid {0} prop objects (PROPS_ROOT/rack/triangle); destroyed {1} placeholder Cube(s).", hidden, killed));
    }

    [MenuItem("147VR/Art/Restore Props")]
    public static void Restore()
    {
        var scene = EditorSceneManager.GetActiveScene();
        int n = 0;
        foreach (var t in GameObject.FindObjectsByType<Transform>(FindObjectsSortMode.None))
            if (t.name == "147VR_PROPS_ROOT" && !t.gameObject.activeSelf) { t.gameObject.SetActive(true); n++; }
        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("147VR/RestoreProps: re-enabled " + n + " (note: destroyed placeholder Cube is not restored).");
    }
}
#endif
