#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _147VR.EditorTools
{
    /// <summary>
    /// Places the WELCOME sign asset that exists on disk but was never put into the Scene.
    ///
    /// Evidence for the defect (2026-11-08): the FBX
    ///   Assets/AAA/Imports/NeonVintageArtPass/147 VR ผ้าม่าน welcome.fbx  (433,260 bytes)
    /// and its material 147VR_Mat_WelcomeGold.mat (gold tint, _EMISSION on,
    /// _EmissionColor (3, 2.1, 0.75)) are referenced ZERO times in
    /// 147VR_MainScene_WipRoom.unity and zero times in the ArtPassPreview Scene. So the sign
    /// "disappeared" because it was never placed, not because a texture went missing.
    ///
    /// Owner decision for this pass: place it straight from the FBX. The room art-pass FBX
    /// exports are authored in room space with the transform baked (the flag row behaves the
    /// same way), so the instance is created at identity and inherits its authored placement.
    /// The menu then MEASURES where it actually landed - world bounds, the nearest Bay root and
    /// the room shell - and reports it, because a sign placed behind a wall would look exactly
    /// like the flags did. Nothing is saved; everything is Undoable.
    /// </summary>
    public static class WipRoomWelcomeSignPlacer
    {
        private const string SourceScenePath = "Assets/Scenes/147VR_MainScene_WipRoom.unity";
        private const string FbxPath = "Assets/AAA/Imports/NeonVintageArtPass/147 VR ผ้าม่าน welcome.fbx";
        private const string RootName = "147VR_AAA_WelcomeSign";
        private const string ReportPath = "Logs/WipRoom_WelcomeSign_Place.txt";

        [MenuItem("147VR/AAA/NeonVintage/WipRoom/11) Place WELCOME Sign From FBX (Undoable / No Save)")]
        public static void Place()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded ||
                !string.Equals(scene.path, SourceScenePath, StringComparison.OrdinalIgnoreCase))
            {
                EditorUtility.DisplayDialog("147VR WELCOME",
                    "Safety stop. Open the actual gameplay Scene first:\n" + SourceScenePath, "OK");
                return;
            }

            if (scene.GetRootGameObjects().Any(g => g.name == RootName))
            {
                EditorUtility.DisplayDialog("147VR WELCOME",
                    RootName + " already exists in this Scene. Nothing was added.\n" +
                    "Use Ctrl+Z to remove it first if you want to re-place it.", "OK");
                return;
            }

            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);
            if (asset == null)
            {
                EditorUtility.DisplayDialog("147VR WELCOME",
                    "The FBX could not be loaded:\n" + FbxPath +
                    "\nCheck that the Thai-named asset path still matches the file on disk.", "OK");
                return;
            }

            int collidersBefore = Count<Collider>(scene);
            int bodiesBefore = Count<Rigidbody>(scene);
            int lightsBefore = Count<Light>(scene);

            var sb = new StringBuilder();
            sb.AppendLine("147VR WELCOME SIGN PLACEMENT");
            sb.AppendLine("Scene: " + scene.path + " | Time: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine("Source asset: " + FbxPath);

            GameObject instance = PrefabUtility.InstantiatePrefab(asset, scene) as GameObject;
            if (instance == null)
            {
                EditorUtility.DisplayDialog("147VR WELCOME", "InstantiatePrefab returned null. Nothing was added.", "OK");
                return;
            }
            Undo.RegisterCreatedObjectUndo(instance, "147VR Place WELCOME Sign");
            instance.name = RootName;
            instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            instance.transform.localScale = Vector3.one;

            var renderers = instance.GetComponentsInChildren<Renderer>(true);
            Bounds bounds = new Bounds(instance.transform.position, Vector3.zero);
            foreach (Renderer r in renderers) bounds.Encapsulate(r.bounds);

            sb.AppendLine();
            sb.AppendLine("CREATED " + RootName);
            sb.AppendLine("  child transforms=" + instance.GetComponentsInChildren<Transform>(true).Length +
                          " | renderers=" + renderers.Length);
            foreach (Renderer r in renderers)
            {
                Material m = r.sharedMaterials.Length > 0 ? r.sharedMaterials[0] : null;
                sb.AppendLine("  renderer " + r.name +
                              " | mat=" + (m != null ? m.name : "<none>") +
                              " | active=" + r.gameObject.activeInHierarchy +
                              " | enabled=" + r.enabled +
                              " | boundsCentre=" + Fmt(r.bounds.center) + " size=" + Fmt(r.bounds.size));
                if (m != null)
                {
                    Texture baseMap = m.GetTexture("_BaseMap") != null ? m.GetTexture("_BaseMap") : m.mainTexture;
                    string emis = m.HasProperty("_EmissionColor") ? m.GetColor("_EmissionColor").ToString() : "-";
                    sb.AppendLine("             baseMap=" + (baseMap != null ? baseMap.name + " " + baseMap.width + "x" + baseMap.height : "<NONE>") +
                                  " | emission=" + emis +
                                  " | keywords=" + (m.shaderKeywords.Length > 0 ? string.Join(",", m.shaderKeywords) : "(NONE)") +
                                  " | cull=" + (m.HasProperty("_Cull") ? m.GetFloat("_Cull").ToString("F0") : "-"));
                }
            }

            sb.AppendLine();
            sb.AppendLine("PLACEMENT MEASUREMENT");
            sb.AppendLine("  sign world bounds: min=" + Fmt(bounds.min) + " max=" + Fmt(bounds.max) + " size=" + Fmt(bounds.size));
            if (bounds.size.sqrMagnitude < 1e-6f)
                sb.AppendLine("  *** ZERO-SIZE BOUNDS - the FBX has no renderable geometry, report this");

            foreach (string bayName in new[] { "Bay1_PhysicsRoot", "Bay2_PhysicsRoot", "Bay3_PhysicsRoot", "Bay4_PhysicsRoot" })
            {
                GameObject bay = GameObject.Find(bayName);
                if (bay == null) { sb.AppendLine("  " + bayName + ": NOT FOUND"); continue; }
                float d = Vector3.Distance(bay.transform.position, bounds.center);
                sb.AppendLine("  distance to " + bayName + " = " + d.ToString("F2") + " m");
            }

            // Is the sign inside the room shell, or outside/behind a wall like the flags were?
            List<Renderer> walls = scene.GetRootGameObjects()
                .SelectMany(g => g.GetComponentsInChildren<Renderer>(true))
                .Where(r => r != null && !r.gameObject.transform.IsChildOf(instance.transform) &&
                            (r.name.IndexOf("Wall", StringComparison.OrdinalIgnoreCase) >= 0 ||
                             r.name.Equals("SM_0020_Cube.004", StringComparison.OrdinalIgnoreCase)))
                .ToList();
            foreach (Renderer wall in walls.Take(8))
                sb.AppendLine("  wall " + wall.name + " bounds min=" + Fmt(wall.bounds.min) + " max=" + Fmt(wall.bounds.max));
            bool insideShell = walls.Any(w => w.bounds.Contains(bounds.center));
            sb.AppendLine("  sign centre inside a wall renderer bound? " + insideShell +
                          (insideShell ? "  (expected for a wall-mounted sign)" : "  (check it is not floating in the middle of the room)"));

            int collidersAfter = Count<Collider>(scene);
            int bodiesAfter = Count<Rigidbody>(scene);
            int lightsAfter = Count<Light>(scene);
            sb.AppendLine();
            sb.AppendLine("PHYSICS INVARIANT");
            sb.AppendLine("  Colliders: " + collidersBefore + " -> " + collidersAfter);
            sb.AppendLine("  Rigidbodies: " + bodiesBefore + " -> " + bodiesAfter);
            sb.AppendLine("  Lights: " + lightsBefore + " -> " + lightsAfter);
            sb.AppendLine("  (a decorative sign must not add colliders/rigidbodies/lights)");

            EditorSceneManager.MarkSceneDirty(scene);
            Directory.CreateDirectory("Logs");
            File.WriteAllText(ReportPath, sb.ToString());

            SceneView view = SceneView.lastActiveSceneView;
            if (view != null)
            {
                view.orthographic = false;
                view.pivot = bounds.center;
                view.size = Mathf.Clamp(bounds.extents.magnitude * 3f, 2f, 20f);
                view.rotation = Quaternion.LookRotation((Vector3.zero - bounds.center).normalized == Vector3.zero
                                                            ? Vector3.back : (Vector3.zero - bounds.center).normalized,
                                                        Vector3.up);
                view.Repaint();
            }

            string headline = "Placed " + RootName + ": renderers=" + renderers.Length +
                              " | bounds centre " + Fmt(bounds.center) + " size " + Fmt(bounds.size) +
                              " | physics delta " + (collidersAfter - collidersBefore) + "/" +
                              (bodiesAfter - bodiesBefore) + "/" + (lightsAfter - lightsBefore);
            Debug.Log("[WELCOME] " + headline + "\nFull report: " + ReportPath);
            EditorUtility.DisplayDialog("147VR WELCOME",
                headline + "\n\nScene view is now framed on the sign." +
                "\nScene is DIRTY but NOT SAVED: inspect the render, then Ctrl+S to keep or Ctrl+Z to remove." +
                "\nReport: " + ReportPath, "OK");
        }

        private static int Count<T>(Scene scene) where T : Component =>
            scene.GetRootGameObjects().Sum(g => g.GetComponentsInChildren<T>(true).Length);

        private static string Fmt(Vector3 v) =>
            "(" + v.x.ToString("F2") + ", " + v.y.ToString("F2") + ", " + v.z.ToString("F2") + ")";
    }
}
#endif
