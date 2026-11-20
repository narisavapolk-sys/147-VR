#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VR147.EditorTools
{
    /// <summary>
    /// Drops the prebuilt LightRig A (12 TableKey lamps + sconces/lofts/uplights) into the ACTIVE
    /// scene and names the root "147VR_LightRig" so BakeGI's lights-baked / bake steps can find it.
    ///
    /// WHY: WipRoom has no ripped light-fixture renderers, so LightRigAB.Apply("A") would
    /// "[ABORT] no ripped fixtures found" there. MainScene already produced the rig as a prefab,
    /// so we instantiate that prefab instead of rebuilding. The rig's light positions were authored
    /// at the shared Blender world coords, so they should land on the tables in WipRoom too — EYEBALL
    /// them in the Scene view after this, before baking.
    ///
    /// Owner-click menu. Marks the scene DIRTY but does NOT save (Ctrl+S is yours).
    /// </summary>
    public static class WipRoomRigInstantiate
    {
        const string PrefabPath  = "Assets/Prefabs/Lighting/PREFAB_147VR_LightRig_A.prefab";
        const string RigRootName = "147VR_LightRig";
        const string ReportPath  = "Logs/WipRoom_RigInstantiate.txt";

        [MenuItem("147 VR/Lighting/Bake GI/0 Instantiate LightRig A (into active scene)")]
        public static void MenuRun()
        {
            Run();
            EditorUtility.DisplayDialog("LightRig A -> scene",
                "Report: " + ReportPath +
                "\n\nEyeball the lamps land on the tables, then Ctrl+S, then run " +
                "'2 Mark Contribute GI' and '4 Bake Now (Baked GI)'.", "OK");
        }

        /// <summary>ToolRunner entry point: "rig-instantiate".</summary>
        public static void Run()
        {
            var log = new StringBuilder();
            log.AppendLine("RIG INSTANTIATE - " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

            Scene scene = SceneManager.GetActiveScene();
            log.AppendLine("active scene: " + scene.path);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                log.AppendLine("REFUSED: prefab not found at " + PrefabPath);
                Write(log);
                return;
            }

            // Idempotent: remove a prior rig (renamed root or a plain prefab instance) first.
            foreach (var n in new[] { RigRootName, "PREFAB_147VR_LightRig_A", "PREFAB_147VR_LightRig_A (1)" })
            {
                var old = GameObject.Find(n);
                if (old != null) { Undo.DestroyObjectImmediate(old); log.AppendLine("  removed prior " + n); }
            }

            var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Undo.RegisterCreatedObjectUndo(inst, "Instantiate LightRig A");
            inst.name = RigRootName;
            inst.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            int lights = inst.GetComponentsInChildren<Light>(true).Length;
            int tableKey = 0;
            foreach (var l in inst.GetComponentsInChildren<Light>(true))
                if (l.name.StartsWith("TableKey_")) tableKey++;
            log.AppendLine("  instantiated -> '" + RigRootName + "'  lights=" + lights + "  TableKey=" + tableKey);
            log.AppendLine("  (rig authored at shared Blender world coords; confirm lamps sit on the tables in Scene view)");

            EditorSceneManager.MarkSceneDirty(scene);
            log.AppendLine("Scene marked DIRTY (NOT saved). Ctrl+S, then gi-flags + bake.");
            Write(log);
        }

        static void Write(StringBuilder log)
        {
            Directory.CreateDirectory("Logs");
            File.WriteAllText(ReportPath, log.ToString());
            Debug.Log("[RigInstantiate] done -> " + ReportPath);
        }
    }
}
#endif
