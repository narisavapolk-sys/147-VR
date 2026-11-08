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
    /// Owner-clicked, undoable material upgrade for the real Build Index 0 WipRoom scene.
    /// Geometry, colliders, rigidbodies, XR, lighting, and Build Settings are not replaced.
    /// The operation marks the Scene dirty but never saves it.
    /// </summary>
    public static class WipRoomArtPassTools
    {
        private const string ScenePath = "Assets/Scenes/147VR_MainScene_WipRoom.unity";
        private const string RoomRootName = "NarisVintage_Room";
        private const string MaterialsRoot = "Assets/AAA/Imports/NeonVintageArtPass/Materials";

        [MenuItem("147VR/AAA/NeonVintage/WipRoom/0) Open Actual Game Scene") ]
        public static void OpenActualGameScene()
        {
            if (!File.Exists(ScenePath))
            {
                EditorUtility.DisplayDialog("147VR WipRoom", "Actual game Scene not found:\n" + ScenePath, "OK");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Debug.Log("[WipRoomArtPass] Opened actual Build Index 0 Scene. No Scene content was modified.");
        }

        [MenuItem("147VR/AAA/NeonVintage/WipRoom/1) Apply Prepared Materials (Undoable / No Save)") ]
        public static void ApplyPreparedMaterials()
        {
            Scene scene;
            GameObject room;
            if (!RequireWipRoom(out scene, out room)) return;

            int ambiguous;
            Dictionary<string, Material> map = BuildMaterialMap(out ambiguous);
            if (map.Count == 0)
            {
                EditorUtility.DisplayDialog("147VR WipRoom", "No unique prepared URP materials found. Nothing was changed.", "OK");
                return;
            }

            Renderer[] renderers = room.GetComponentsInChildren<Renderer>(true);
            int totalSlots = renderers.Sum(r => r.sharedMaterials.Length);
            int matchedSlots = 0;
            int matchedRenderers = 0;
            foreach (Renderer renderer in renderers)
            {
                if (renderer.sharedMaterials.Any(m => m != null && map.ContainsKey(Normalize(m.name))))
                    matchedRenderers++;
                matchedSlots += renderer.sharedMaterials.Count(m => m != null && map.ContainsKey(Normalize(m.name)));
            }

            if (matchedSlots == 0)
            {
                EditorUtility.DisplayDialog("147VR WipRoom", "Prepared materials did not match any room slots. Nothing was changed.", "OK");
                return;
            }

            bool confirmed = EditorUtility.DisplayDialog(
                "147VR WipRoom — Material Upgrade",
                "Apply the prepared NeonVintage URP materials to the existing NarisVintage_Room?\n\n" +
                "Matched slots: " + matchedSlots + " / " + totalSlots + "\n" +
                "Matched renderers: " + matchedRenderers + " / " + renderers.Length + "\n" +
                "Ambiguous material keys skipped: " + ambiguous + "\n\n" +
                "This changes material overrides only. It does NOT change geometry, colliders, rigidbodies, XR, lights, or Build Settings.\n" +
                "Undo is available. The Scene will be left dirty and will NOT be saved.",
                "Apply Materials", "Cancel");
            if (!confirmed) return;

            int collidersBefore = room.GetComponentsInChildren<Collider>(true).Length;
            int bodiesBefore = room.GetComponentsInChildren<Rigidbody>(true).Length;
            int transformsBefore = room.GetComponentsInChildren<Transform>(true).Length;
            int lightsBefore = room.GetComponentsInChildren<Light>(true).Length;
            int appliedSlots = 0;
            int changedRenderers = 0;

            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("147VR WipRoom NeonVintage Material Upgrade");
            try
            {
                foreach (Renderer renderer in renderers)
                {
                    Material[] current = renderer.sharedMaterials;
                    bool changed = false;
                    for (int i = 0; i < current.Length; i++)
                    {
                        Material source = current[i];
                        if (source == null) continue;
                        Material replacement;
                        if (map.TryGetValue(Normalize(source.name), out replacement) && replacement != source)
                        {
                            current[i] = replacement;
                            appliedSlots++;
                            changed = true;
                        }
                    }
                    if (!changed) continue;
                    Undo.RecordObject(renderer, "Apply NeonVintage URP material override");
                    renderer.sharedMaterials = current;
                    EditorUtility.SetDirty(renderer);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                    changedRenderers++;
                }

                int collidersAfter = room.GetComponentsInChildren<Collider>(true).Length;
                int bodiesAfter = room.GetComponentsInChildren<Rigidbody>(true).Length;
                int transformsAfter = room.GetComponentsInChildren<Transform>(true).Length;
                int lightsAfter = room.GetComponentsInChildren<Light>(true).Length;
                if (collidersBefore != collidersAfter || bodiesBefore != bodiesAfter || transformsBefore != transformsAfter || lightsBefore != lightsAfter)
                    throw new InvalidOperationException("Boundary invariant changed during material remap; Undo rollback requested.");

                EditorSceneManager.MarkSceneDirty(scene);
                Undo.CollapseUndoOperations(undoGroup);
                string report =
                    "147VR WipRoom NeonVintage Material Upgrade\n" +
                    "Scene: " + scene.path + "\n" +
                    "Room root: " + room.name + "\n" +
                    "Prepared unique material keys: " + map.Count + "\n" +
                    "Ambiguous keys skipped: " + ambiguous + "\n" +
                    "Applied material slots: " + appliedSlots + " / " + totalSlots + "\n" +
                    "Changed renderers: " + changedRenderers + " / " + renderers.Length + "\n" +
                    "Colliders: " + collidersBefore + " -> " + collidersAfter + "\n" +
                    "Rigidbodies: " + bodiesBefore + " -> " + bodiesAfter + "\n" +
                    "Transforms: " + transformsBefore + " -> " + transformsAfter + "\n" +
                    "Lights: " + lightsBefore + " -> " + lightsAfter + "\n" +
                    "Scene saved: NO (review and save manually after render inspection).\n";
                Directory.CreateDirectory("Logs");
                File.WriteAllText("Logs/WipRoom_NeonVintage_Material_Upgrade.txt", report);
                Debug.Log("[WipRoomArtPass] " + report);
                EditorUtility.DisplayDialog("147VR WipRoom",
                    "Material upgrade applied.\n\nSlots changed: " + appliedSlots + "\nRenderers changed: " + changedRenderers +
                    "\nPhysics boundary unchanged.\n\nScene is NOT saved. Review the rendered scene, then save manually if approved.\nUndo is available.", "OK");
            }
            catch (Exception exception)
            {
                Undo.RevertAllDownToGroup(undoGroup);
                Debug.LogError("[WipRoomArtPass] Material upgrade rolled back: " + exception);
                EditorUtility.DisplayDialog("147VR WipRoom", "Material upgrade failed and was rolled back:\n" + exception.Message, "OK");
            }
        }

        [MenuItem("147VR/AAA/NeonVintage/WipRoom/2) Audit Physics Boundary") ]
        public static void AuditPhysicsBoundary()
        {
            Scene scene;
            GameObject room;
            if (!RequireWipRoom(out scene, out room)) return;

            var roots = scene.GetRootGameObjects();
            var sb = new StringBuilder();
            sb.AppendLine("147VR WipRoom Physics Boundary Audit");
            sb.AppendLine("Scene: " + scene.path);
            sb.AppendLine("Room transforms: " + room.GetComponentsInChildren<Transform>(true).Length);
            sb.AppendLine("Room renderers: " + room.GetComponentsInChildren<Renderer>(true).Length);
            sb.AppendLine("Room colliders: " + room.GetComponentsInChildren<Collider>(true).Length);
            sb.AppendLine("Room rigidbodies: " + room.GetComponentsInChildren<Rigidbody>(true).Length);
            sb.AppendLine("Room lights: " + room.GetComponentsInChildren<Light>(true).Length);
            foreach (string name in new[] { "Bay1_PhysicsRoot", "Bay2_PhysicsRoot", "Bay3_PhysicsRoot", "Bay4_PhysicsRoot", "Quest Setup", "OVRCameraRig", "Bed_Collider", "Global Volume", "147VR_LightRig" })
            {
                GameObject found = roots.FirstOrDefault(g => string.Equals(g.name, name, StringComparison.Ordinal));
                sb.AppendLine(name + ": " + (found == null ? "MISSING" : (found.activeSelf ? "ACTIVE" : "INACTIVE")));
            }
            foreach (var volume in roots.SelectMany(g => g.GetComponentsInChildren<UnityEngine.Rendering.Volume>(true)))
            {
                var profile = volume.sharedProfile;
                sb.AppendLine("Volume " + volume.name + ": global=" + volume.isGlobal + ", priority=" + volume.priority + ", profile=" + (profile != null ? profile.name : "MISSING") +
                              ", components=" + (profile != null ? string.Join(",", profile.components.Where(c => c != null).Select(c => c.GetType().Name)) : "none"));
            }
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/WipRoom_PhysicsBoundary_Audit.txt", sb.ToString());
            Debug.Log("[WipRoomArtPass] " + sb);
            EditorUtility.DisplayDialog("147VR WipRoom", "Physics boundary audit written to Logs/WipRoom_PhysicsBoundary_Audit.txt.\nNo Scene changes were made.", "OK");
        }

        private static bool RequireWipRoom(out Scene scene, out GameObject room)
        {
            scene = SceneManager.GetActiveScene();
            room = null;
            if (!scene.IsValid() || !scene.isLoaded || !string.Equals(scene.path, ScenePath, StringComparison.OrdinalIgnoreCase))
            {
                EditorUtility.DisplayDialog("147VR WipRoom", "Safety stop. Open the actual game Scene first:\n" + ScenePath, "OK");
                return false;
            }
            room = scene.GetRootGameObjects().FirstOrDefault(g => string.Equals(g.name, RoomRootName, StringComparison.Ordinal));
            if (room == null)
            {
                EditorUtility.DisplayDialog("147VR WipRoom", "NarisVintage_Room root was not found. Nothing was changed.", "OK");
                return false;
            }
            return true;
        }

        private static Dictionary<string, Material> BuildMaterialMap(out int ambiguousCount)
        {
            var map = new Dictionary<string, Material>(StringComparer.OrdinalIgnoreCase);
            var ambiguous = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!AssetDatabase.IsValidFolder(MaterialsRoot))
            {
                ambiguousCount = 0;
                return map;
            }
            foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { MaterialsRoot }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null || material.shader == null || !material.shader.name.Contains("Universal Render Pipeline")) continue;
                string key = Normalize(material.name);
                if (string.IsNullOrEmpty(key) || ambiguous.Contains(key)) continue;
                if (map.ContainsKey(key))
                {
                    map.Remove(key);
                    ambiguous.Add(key);
                    Debug.LogWarning("[WipRoomArtPass] Ambiguous material key skipped: " + key);
                }
                else map.Add(key, material);
            }
            ambiguousCount = ambiguous.Count;
            return map;
        }

        private static string Normalize(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            string normalized = new string(value.Replace(" (Instance)", string.Empty).Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
            while (normalized.StartsWith("147vr", StringComparison.OrdinalIgnoreCase)) normalized = normalized.Substring(5);
            return normalized;
        }
    }
}
#endif