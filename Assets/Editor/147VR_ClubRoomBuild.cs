#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

// ONE-CLICK club room builder (v2). Replaces the separate menus. Does, in order:
//  - removes previous generated objects (idempotent) + leftover venue roots (KEEPS table+physics+XR rig)
//  - turns OFF the HDRI background (no skybox, flat warm interior ambient) and adds a CEILING
//  - builds floor + 4 walls + ceiling + a low center dais
//  - adds a chandelier + warm point light over the table
//  - places CC0 props around the room
// MenuItem-only; the OWNER runs it. Table + physics are left untouched.
public static class _147VR_ClubRoomBuild
{
    const string PROPS = "Assets/AAA/ClubProps";
    const float W = 16f, D = 12f, H = 3.6f;

    [MenuItem("147VR/Art/Club/★ Build Full Club Room (one-click)")]
    public static void BuildAll()
    {
        var scene = EditorSceneManager.GetActiveScene();

        foreach (var r in scene.GetRootGameObjects())
            if (r.name == "ClubRoomShell" || r.name == "ClubDressing" || r.name == "ClubPropsGroup" || IsVenue(r.name))
                Object.DestroyImmediate(r);

        Vector3 c = TableCenter();

        RenderSettings.skybox = null;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.16f, 0.13f, 0.10f);

        BuildShell(c);
        AddChandelier(c);
        PlaceProps(c);

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("147VR/ClubRoom: full build done — shell+ceiling, HDRI background OFF, chandelier+light, props placed. Table+physics kept. Turn on 'Post Processing' in the Scene view to judge.");
    }

    static bool IsVenue(string n) =>
        n.StartsWith("147VR_VENUE") || n.StartsWith("Crowd_BB") || n.StartsWith("Ceil_BB");

    static Vector3 TableCenter()
    {
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
            if (t.name == "TABLE SURFACE") return t.position;
        return Vector3.zero;
    }

    static void BuildShell(Vector3 c)
    {
        GameObject shell = new GameObject("ClubRoomShell");

        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Floor"; floor.transform.SetParent(shell.transform);
        floor.transform.position = c + new Vector3(0f, -0.02f, 0f);
        floor.transform.localScale = new Vector3(W / 10f, 1f, D / 10f);
        Tint(floor, 0.20f, 0.12f, 0.07f);

        Wall(shell, "Wall_N", c + new Vector3(0f, H / 2f, D / 2f), new Vector3(W, H, 0.2f));
        Wall(shell, "Wall_S", c + new Vector3(0f, H / 2f, -D / 2f), new Vector3(W, H, 0.2f));
        Wall(shell, "Wall_E", c + new Vector3(W / 2f, H / 2f, 0f), new Vector3(0.2f, H, D));
        Wall(shell, "Wall_W", c + new Vector3(-W / 2f, H / 2f, 0f), new Vector3(0.2f, H, D));

        GameObject ceil = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ceil.name = "Ceiling"; ceil.transform.SetParent(shell.transform);
        ceil.transform.position = c + new Vector3(0f, H, 0f);
        ceil.transform.localScale = new Vector3(W, 0.2f, D);
        Tint(ceil, 0.10f, 0.07f, 0.05f);

        GameObject dais = GameObject.CreatePrimitive(PrimitiveType.Cube);
        dais.name = "CenterDais"; dais.transform.SetParent(shell.transform);
        dais.transform.position = c + new Vector3(0f, 0.03f, 0f);
        dais.transform.localScale = new Vector3(5.0f, 0.06f, 3.2f);
        Tint(dais, 0.14f, 0.08f, 0.05f);
    }

    static void Wall(GameObject p, string n, Vector3 pos, Vector3 s)
    {
        GameObject w = GameObject.CreatePrimitive(PrimitiveType.Cube);
        w.name = n; w.transform.SetParent(p.transform); w.transform.position = pos; w.transform.localScale = s;
        Tint(w, 0.24f, 0.13f, 0.08f);
    }

    static void Tint(GameObject go, float r, float g, float b)
    {
        var mr = go.GetComponent<MeshRenderer>();
        if (mr == null || mr.sharedMaterial == null) return;
        Material m = mr.material;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", new Color(r, g, b));
        else m.color = new Color(r, g, b);
    }

    static void AddChandelier(Vector3 c)
    {
        GameObject grp = new GameObject("ClubPropsGroup");
        GameObject chan = InstantiateProp(grp, "Chandelier_01", c + new Vector3(0f, 3.0f, 0f), 0f);
        if (chan != null) chan.transform.localScale = Vector3.one * 1.2f;

        GameObject lo = new GameObject("ChandelierLight"); lo.transform.SetParent(grp.transform);
        lo.transform.position = c + new Vector3(0f, 2.7f, 0f);
        Light l = lo.AddComponent<Light>();
        l.type = LightType.Point; l.color = new Color(1f, 0.85f, 0.6f);
        l.intensity = 1.6f; l.range = 12f;
    }

    static void PlaceProps(Vector3 c)
    {
        GameObject grp = GameObject.Find("ClubPropsGroup");
        if (grp == null) grp = new GameObject("ClubPropsGroup");

        InstantiateProp(grp, "Sofa_01",            c + new Vector3(0f, 0f, 5.0f), 180f);
        InstantiateProp(grp, "ArmChair_01",        c + new Vector3(-3.0f, 0f, 4.0f), 135f);
        InstantiateProp(grp, "ArmChair_01",        c + new Vector3(3.0f, 0f, 4.0f), 225f);
        InstantiateProp(grp, "CoffeeTable_01",     c + new Vector3(0f, 0f, 4.2f), 0f);
        InstantiateProp(grp, "Shelf_01",           c + new Vector3(7.4f, 0f, -2.0f), 270f);
        InstantiateProp(grp, "bar_chair_round_01", c + new Vector3(-7.0f, 0f, -2.0f), 90f);
        InstantiateProp(grp, "bar_chair_round_01", c + new Vector3(-7.0f, 0f, 0.0f), 90f);
        InstantiateProp(grp, "calathea_orbifolia_01", c + new Vector3(7.2f, 0f, 3.0f), 0f);
        InstantiateProp(grp, "bronze_ray_statue",  c + new Vector3(-7.2f, 0f, 3.4f), 0f);
    }

    static GameObject InstantiateProp(GameObject parent, string id, Vector3 worldPos, float yRot)
    {
        string fbx = PROPS + "/" + id + "/" + id + ".fbx";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
        if (prefab == null) { Debug.LogWarning("147VR/ClubRoom: missing " + fbx); return null; }
        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.transform.SetParent(parent.transform, false);
        go.transform.position = worldPos;
        go.transform.rotation = Quaternion.Euler(0f, yRot, 0f);
        return go;
    }
}
#endif
