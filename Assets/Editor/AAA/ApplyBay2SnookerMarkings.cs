#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Mirrors Bay1's authored WPBSA foul-line + D mesh onto Bay2.
/// The old Bay2 split marks are retained but disabled for a reversible migration.
/// </summary>
public static class ApplyBay2SnookerMarkings
{
    const string ScenePath = "Assets/Scenes/147VR_MainScene_WipRoom.unity";
    const string SourceName = "SM_FoulLineAndD";
    const string TargetRootName = "ROOT_PoolTable_02";
    const string NewName = "SM_147VR_Bay2_FoulLineAndD";
    const string OldBaulkName = "SM_147VR_SnookerMark_BaulkLine";
    const string OldDName = "SM_147VR_SnookerMark_DArc";

    [MenuItem("147VR/AAA/Apply Bay2 Foul Line And D")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("[Bay2 Markings] Refused: exit Play Mode before editing the production scene.");
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != ScenePath)
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        GameObject source = FindInScene(scene, SourceName);
        GameObject targetRoot = FindInScene(scene, TargetRootName);
        GameObject oldBaulk = FindInScene(scene, OldBaulkName);
        GameObject oldD = FindInScene(scene, OldDName);

        if (source == null || targetRoot == null || oldBaulk == null || oldD == null)
        {
            Debug.LogError("[Bay2 Markings] Refused: source, target root, or legacy Bay2 markings are missing.");
            return;
        }

        MeshFilter sourceFilter = source.GetComponent<MeshFilter>();
        MeshRenderer sourceRenderer = source.GetComponent<MeshRenderer>();
        if (sourceFilter == null || sourceFilter.sharedMesh == null ||
            sourceRenderer == null || sourceRenderer.sharedMaterial == null)
        {
            Debug.LogError("[Bay2 Markings] Refused: Bay1 reference mesh/material is incomplete.");
            return;
        }

        GameObject copy = FindInScene(scene, NewName);
        if (copy == null)
        {
            copy = Object.Instantiate(source, targetRoot.transform, false);
            Undo.RegisterCreatedObjectUndo(copy, "Add Bay2 Foul Line And D");
            copy.name = NewName;
        }
        else
        {
            Undo.RecordObject(copy.transform, "Align Bay2 Foul Line And D");
            copy.transform.SetParent(targetRoot.transform, false);
        }

        copy.transform.localPosition = Vector3.zero;
        copy.transform.localRotation = Quaternion.Euler(0f, 0f, -90f);
        copy.transform.localScale = source.transform.localScale;

        MeshFilter copyFilter = copy.GetComponent<MeshFilter>();
        MeshRenderer copyRenderer = copy.GetComponent<MeshRenderer>();
        if (copyFilter == null || copyFilter.sharedMesh != sourceFilter.sharedMesh ||
            copyRenderer == null || copyRenderer.sharedMaterial != sourceRenderer.sharedMaterial)
        {
            Debug.LogError("[Bay2 Markings] Refused: copied mesh/material does not match Bay1.");
            if (copy != null && copy != source)
                Undo.DestroyObjectImmediate(copy);
            return;
        }

        Bounds bounds = copyRenderer.bounds;
        Vector3 expectedCenter = targetRoot.transform.TransformPoint(new Vector3(-1.19f, 0f, 0.83f));
        bool boundsValid =
            Vector3.Distance(bounds.center, expectedCenter) <= 0.05f &&
            bounds.extents.x >= 0.10f &&
            bounds.extents.z >= 0.80f &&
            bounds.extents.y <= 0.03f;

        if (!boundsValid)
        {
            Debug.LogError("[Bay2 Markings] Refused: geometry bounds are not aligned. center=" +
                           bounds.center.ToString("F4") + " expected=" + expectedCenter.ToString("F4") +
                           " extents=" + bounds.extents.ToString("F4"));
            if (copy != null && copy != source)
                Undo.DestroyObjectImmediate(copy);
            return;
        }

        Undo.RecordObject(oldBaulk, "Disable legacy Bay2 baulk line");
        Undo.RecordObject(oldD, "Disable legacy Bay2 D arc");
        oldBaulk.SetActive(false);
        oldD.SetActive(false);
        copy.SetActive(true);

        EditorSceneManager.MarkSceneDirty(scene);
        bool saved = EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = copy;

        Debug.Log("[Bay2 Markings] " + (saved ? "SAVED" : "SAVE FAILED") +
                  " | source=" + source.name +
                  " | target=" + copy.name +
                  " | mesh=" + copyFilter.sharedMesh.name +
                  " | material=" + copyRenderer.sharedMaterial.name +
                  " | localRotation=" + copy.transform.localEulerAngles.ToString("F2") +
                  " | boundsCenter=" + bounds.center.ToString("F4") +
                  " | boundsExtents=" + bounds.extents.ToString("F4") +
                  " | legacy marks retained inactive=" + (!oldBaulk.activeSelf && !oldD.activeSelf));
    }

    static GameObject FindInScene(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform t in transforms)
                if (t.name == objectName)
                    return t.gameObject;
        }
        return null;
    }
}
#endif
