using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Wires the table skin manager + pre-game menu + cycler into pool table scenes.
/// Run headless:
///   Unity.exe -batchmode -projectPath <proj> -executeMethod SetupGameScene.Run -quit
///   Unity.exe -batchmode -projectPath <proj> -executeMethod SetupGameScene.AddCycler -quit
/// </summary>
public static class SetupGameScene
{
    private static readonly string[] ScenePaths =
    {
        "Assets/Scenes/PoolTable_9Ball.unity",
        "Assets/Scenes/PoolTable_8Ball.unity",
    };

    public static void Run()
    {
        foreach (string scenePath in ScenePaths)
            SetupOne(scenePath);
    }

    private static void SetupOne(string scenePath)
    {
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        // Keep the static table for edit-time layout (balls rack correctly, visible table).
        // TableSkinManager.Awake removes stale instances at runtime before spawning its own.

        GameObject[] prefabs =
        {
            AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PoolTable/PREFAB POoL table.prefab"),
            AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PoolTable/PREFAB POoL table Walnut.prefab"),
            AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PoolTable/PREFAB POoL table Blue.prefab"),
        };

        // TableSkinManager (if not already present)
        var mgr = Object.FindAnyObjectByType<TableSkinManager>();
        GameObject mgrGO;
        if (mgr == null)
        {
            mgrGO = new GameObject("TableSkinManager");
            mgr = mgrGO.AddComponent<TableSkinManager>();
        }
        else
        {
            mgrGO = mgr.gameObject;
        }
        mgr.tablePrefabs = prefabs;

        // SkinSelectMenu (if not already present)
        if (Object.FindAnyObjectByType<SkinSelectMenu>() == null)
        {
            var menuGO = new GameObject("SkinSelectMenu");
            var menu = menuGO.AddComponent<SkinSelectMenu>();
            menu.skinManager = mgr;
        }

        // SkinCycler (one-press cycle)
        if (Object.FindAnyObjectByType<SkinCycler>() == null)
        {
            var cycGO = new GameObject("SkinCycler");
            var cycler = cycGO.AddComponent<SkinCycler>();
            cycler.skinManager = mgr;
        }

        EditorSceneManager.SaveScene(scene, scenePath);
        Debug.Log("SCENE_SETUP_DONE: " + scenePath);
    }

    public static void AddCycler()
    {
        foreach (string scenePath in ScenePaths)
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var mgr = Object.FindAnyObjectByType<TableSkinManager>();
            if (mgr == null)
            {
                Debug.LogError("CYCLER: no TableSkinManager in " + scenePath);
                continue;
            }
            if (Object.FindAnyObjectByType<SkinCycler>() != null)
            {
                Debug.Log("CYCLER_EXISTS in " + scenePath);
                continue;
            }
            var go = new GameObject("SkinCycler");
            var cycler = go.AddComponent<SkinCycler>();
            cycler.skinManager = mgr;
            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log("CYCLER_ADDED: " + scenePath);
        }
    }
}