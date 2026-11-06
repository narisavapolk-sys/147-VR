#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace _147VR.Wiring
{
    /// <summary>
    /// Pass A of the scene swap (headless): open 147VR_MainScene, dump every root
    /// GameObject to Logs/scene_roots.txt, instantiate the imported naris FBX as
    /// 'NarisVintage_Room' (idempotent), save. Nothing is disabled yet — the root
    /// dump decides that in Pass B.
    /// </summary>
    public static class SceneSwapPassA
    {
        const string ScenePath = "Assets/Scenes/147VR_MainScene.unity";
        const string FbxPath   = "Assets/AAA/Imports/naris_vintage/naris_vintage.fbx";

        public static void Run()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid()) { Debug.LogError("[PassA] cannot open scene"); return; }

            var sb = new StringBuilder("ROOTS:\n");
            foreach (var go in scene.GetRootGameObjects())
                sb.AppendLine($"{(go.activeSelf ? "ON " : "OFF")} {go.name} | tag={go.tag} | children={go.transform.childCount} | pos={go.transform.position}");
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/scene_roots.txt", sb.ToString());
            Debug.Log("[PassA] roots dumped:\n" + sb);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);
            if (prefab == null) { Debug.LogError("[PassA] FBX prefab not found at " + FbxPath); return; }

            if (GameObject.Find("NarisVintage_Room") == null)
            {
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                inst.name = "NarisVintage_Room";
                inst.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                Debug.Log("[PassA] instantiated NarisVintage_Room, children=" + inst.transform.childCount);
            }
            else Debug.Log("[PassA] NarisVintage_Room already present (idempotent)");

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[PassA] DONE saved");
        }
    }
}
#endif
