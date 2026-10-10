#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// 147VR ART staging helper. MenuItem-only (NO [InitializeOnLoad], no auto-run).
// Place the new v006-DARC table beside the current one to evaluate scale/shape/materials
// before doing the real non-destructive swap.
public static class _147VR_ArtStage_Tools
{
    const string FBX = "Assets/AAA/ImportedSnooker/V006_DARC_V04/147VR_Table_WPBSA_V006_DARC_V04_GAME.fbx";

    [MenuItem("147VR/Art/Place New Table (test beside old)")]
    public static void PlaceNewTableTest()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FBX);
        if (prefab == null)
        {
            Debug.LogError("147VR/Art: FBX not found at " + FBX);
            return;
        }
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.name = "TEST_NewTable_V006_DARC";
        // Place to the side so it doesn't overlap the existing table (compare side by side).
        go.transform.position = new Vector3(6f, 0f, 0f);
        go.transform.rotation = Quaternion.identity;
        Undo.RegisterCreatedObjectUndo(go, "Place New Table Test");
        Selection.activeGameObject = go;
        EditorGUIUtility.PingObject(go);
        Debug.Log("147VR/Art: placed 'TEST_NewTable_V006_DARC' at x=+6. " +
                  "Compare footprint (~3.57 x 1.78 m) and felt height vs old table. " +
                  "Materials look flat until baked. Delete this test object when done.");
    }

    [MenuItem("147VR/Art/Measure Selected World Bounds")]
    public static void MeasureSelected()
    {
        var go = Selection.activeGameObject;
        if (go == null) { Debug.LogWarning("147VR/Art: select an object first."); return; }
        var rends = go.GetComponentsInChildren<Renderer>();
        if (rends.Length == 0) { Debug.LogWarning("147VR/Art: no renderers under selection."); return; }
        Bounds b = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
        Debug.Log(string.Format(
            "147VR/Art bounds '{0}': center={1:F3} size={2:F3}  (topY={3:F3})",
            go.name, b.center, b.size, b.max.y));
    }
}
#endif
