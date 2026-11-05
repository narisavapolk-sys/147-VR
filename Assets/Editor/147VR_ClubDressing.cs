#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Additive club-dressing tool: instantiates the CC0 props (Assets/AAA/ClubProps/*.fbx) around the
// snooker table in a multiplayer-friendly layout. MenuItem-only; the OWNER runs it. Non-destructive
// (everything is parented under "ClubDressing" — delete that object to undo).
public static class _147VR_ClubDressing
{
    const string PROPS = "Assets/AAA/ClubProps";

    struct Place { public string id; public Vector3 pos; public float yRot; }

    // Layout assumes the table's long axis runs along Z. If yours runs along X, rotate the
    // "ClubDressing" parent 90deg in the editor. Positions are metres from the table centre.
    static readonly Place[] Layout = new Place[]
    {
        new Place { id = "Chandelier_01",          pos = new Vector3( 0.0f, 3.2f,  0.0f), yRot = 0 },
        new Place { id = "Sofa_01",                pos = new Vector3( 0.0f, 0.0f,  5.0f), yRot = 180 },
        new Place { id = "ArmChair_01",            pos = new Vector3(-2.4f, 0.0f,  3.4f), yRot = 135 },
        new Place { id = "ArmChair_01",            pos = new Vector3( 2.4f, 0.0f,  3.4f), yRot = 225 },
        new Place { id = "CoffeeTable_01",         pos = new Vector3( 0.0f, 0.0f,  3.4f), yRot = 0 },
        new Place { id = "bar_chair_round_01",     pos = new Vector3(-4.6f, 0.0f, -1.6f), yRot = 90 },
        new Place { id = "bar_chair_round_01",     pos = new Vector3(-4.6f, 0.0f,  0.4f), yRot = 90 },
        new Place { id = "Shelf_01",               pos = new Vector3( 4.7f, 0.0f, -1.2f), yRot = 270 },
        new Place { id = "calathea_orbifolia_01",  pos = new Vector3( 4.7f, 0.0f,  2.2f), yRot = 0 },
        new Place { id = "bronze_ray_statue",      pos = new Vector3(-4.7f, 0.0f,  2.8f), yRot = 0 },
    };

    [MenuItem("147VR/Art/Club/Place CC0 Props Around Table (additive)")]
    public static void PlaceProps()
    {
        Vector3 center = TableCenter();
        GameObject parent = new GameObject("ClubDressing");
        parent.transform.position = center;

        int ok = 0, miss = 0;
        foreach (var p in Layout)
        {
            string fbx = PROPS + "/" + p.id + "/" + p.id + ".fbx";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
            if (prefab == null) { Debug.LogWarning("147VR/Club: missing " + fbx); miss++; continue; }

            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = p.pos;
            go.transform.localRotation = Quaternion.Euler(0f, p.yRot, 0f);
            go.transform.localScale = Vector3.one;
            ok++;
        }

        EditorSceneManager.MarkAllScenesDirty();
        Selection.activeGameObject = parent;
        Debug.Log("147VR/Club: placed " + ok + " props (" + miss + " missing) under 'ClubDressing'. " +
                  "If the table's long axis is X, rotate ClubDressing 90deg. Delete ClubDressing to undo.");
    }

    static Vector3 TableCenter()
    {
        string[] names = { "TABLE SURFACE", "147VR_Table_V008_OPT_VISUAL", "Bed_Collider" };
        foreach (var n in names)
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
                if (t.name == n) return t.position;
        Debug.LogWarning("147VR/Club: table not found; placing at world origin.");
        return Vector3.zero;
    }
}
#endif
