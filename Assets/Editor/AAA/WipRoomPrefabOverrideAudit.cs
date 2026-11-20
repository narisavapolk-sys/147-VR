#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VR147.AAA.Editor
{
    /// <summary>
    /// Read-only prefab override/dependency audit for the production WipRoom scene.
    /// This tool never applies/reverts overrides and never saves the scene.
    /// </summary>
    public static class WipRoomPrefabOverrideAudit
    {
        private const string ScenePath = "Assets/Scenes/147VR_MainScene_WipRoom.unity";
        private const string ReportPath = "Logs/WipRoom_PrefabOverrideAudit_20261119.txt";

        public static void RunBatch()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid() || !scene.isLoaded)
                throw new InvalidOperationException("Could not open WipRoom for prefab override audit.");
            Run();
        }

        [MenuItem("Tools/147/Production/Audit WipRoom Prefab Overrides (Read Only)")]
        public static void Run()
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            if (!scene.IsValid() || !scene.isLoaded)
                throw new InvalidOperationException("Open WipRoom before running this read-only audit.");

            var report = new StringBuilder(65536);
            report.AppendLine("147VR WIPROOM PREFAB OVERRIDE AUDIT");
            report.AppendLine("Timestamp: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            report.AppendLine("Unity: " + Application.unityVersion);
            report.AppendLine("Scene: " + ScenePath);
            report.AppendLine("READ ONLY: no Apply, no Revert, no Save");
            report.AppendLine();

            var allObjects = Resources.FindObjectsOfTypeAll<GameObject>()
                .Where(go => go != null && go.scene == scene).ToArray();
            var instanceRoots = new HashSet<GameObject>();
            foreach (GameObject go in allObjects)
            {
                GameObject root = PrefabUtility.GetNearestPrefabInstanceRoot(go);
                if (root != null && root.scene == scene)
                    instanceRoots.Add(root);
            }

            int totalPropertyOverrides = 0;
            int totalAddedComponents = 0;
            int totalRemovedComponents = 0;
            int totalAddedGameObjects = 0;
            int totalRemovedGameObjects = 0;
            int connected = 0, missing = 0, disconnected = 0;

            report.AppendLine("SCENE OBJECTS=" + allObjects.Length);
            report.AppendLine("PREFAB INSTANCE ROOTS=" + instanceRoots.Count);
            report.AppendLine();

            foreach (GameObject root in instanceRoots.OrderBy(GetHierarchyPath, StringComparer.Ordinal))
            {
                string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(root);
                var status = PrefabUtility.GetPrefabInstanceStatus(root);
                if (status == PrefabInstanceStatus.Connected) connected++;
                else if (status == PrefabInstanceStatus.MissingAsset) missing++;
                else if (status == PrefabInstanceStatus.Disconnected) disconnected++;

                PropertyModification[] mods = PrefabUtility.GetPropertyModifications(root) ?? Array.Empty<PropertyModification>();
                totalPropertyOverrides += mods.Length;

                report.AppendLine("PREFAB INSTANCE: " + GetHierarchyPath(root));
                report.AppendLine("  source=" + (string.IsNullOrEmpty(prefabPath) ? "<unknown>" : prefabPath));
                report.AppendLine("  status=" + status + " propertyOverrides=" + mods.Length);

                foreach (PropertyModification mod in mods)
                {
                    string targetName = mod.target != null ? mod.target.name : "<null-target>";
                    string targetType = mod.target != null ? mod.target.GetType().Name : "<null-type>";
                    string value = mod.objectReference != null
                        ? "objectReference=" + mod.objectReference.name + " (" + AssetDatabase.GetAssetPath(mod.objectReference) + ")"
                        : "value=" + (mod.value ?? "");
                    report.AppendLine("    PROPERTY " + targetType + " " + targetName + "." + mod.propertyPath + " => " + value);
                }

                totalAddedComponents += AppendReflectedEntries(report, root, "GetAddedComponents", "ADDED_COMPONENT");
                totalRemovedComponents += AppendReflectedEntries(report, root, "GetRemovedComponents", "REMOVED_COMPONENT");
                totalAddedGameObjects += AppendReflectedEntries(report, root, "GetAddedGameObjects", "ADDED_GAMEOBJECT");
                totalRemovedGameObjects += AppendReflectedEntries(report, root, "GetRemovedGameObjects", "REMOVED_GAMEOBJECT");
                report.AppendLine();
            }

            report.AppendLine("SUMMARY");
            report.AppendLine("  connectedPrefabRoots=" + connected);
            report.AppendLine("  missingAssetPrefabRoots=" + missing);
            report.AppendLine("  disconnectedPrefabRoots=" + disconnected);
            report.AppendLine("  propertyOverrides=" + totalPropertyOverrides);
            report.AppendLine("  addedComponents=" + totalAddedComponents);
            report.AppendLine("  removedComponents=" + totalRemovedComponents);
            report.AppendLine("  addedGameObjects=" + totalAddedGameObjects);
            report.AppendLine("  removedGameObjects=" + totalRemovedGameObjects);
            report.AppendLine();

            report.AppendLine("SCENE DEPENDENCIES");
            string[] dependencies = AssetDatabase.GetDependencies(ScenePath, true)
                .Where(path => path != ScenePath)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
            report.AppendLine("  dependencyCount=" + dependencies.Length);
            foreach (string dependency in dependencies)
                report.AppendLine("  DEP " + dependency);

            report.AppendLine();
            report.AppendLine("SCENE MODIFIED/SAVED: NO");

            Directory.CreateDirectory("Logs");
            File.WriteAllText(ReportPath, report.ToString(), Encoding.UTF8);
            Debug.Log("[WipRoom Prefab Override Audit] Complete. prefabRoots=" + instanceRoots.Count +
                " overrides=" + totalPropertyOverrides + " report=" + ReportPath);
        }

        private static int AppendReflectedEntries(StringBuilder report, GameObject root, string methodName, string label)
        {
            MethodInfo method = typeof(PrefabUtility).GetMethod(methodName,
                BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(GameObject) }, null);
            if (method == null)
            {
                report.AppendLine("    " + label + " API unavailable in this Unity version.");
                return 0;
            }

            object result;
            try { result = method.Invoke(null, new object[] { root }); }
            catch (Exception exception)
            {
                report.AppendLine("    " + label + " scan error: " + exception.GetBaseException().Message);
                return 0;
            }

            IEnumerable enumerable = result as IEnumerable;
            if (enumerable == null) return 0;

            int count = 0;
            foreach (object entry in enumerable)
            {
                count++;
                report.AppendLine("    " + label + " " + DescribeEntry(entry));
            }
            return count;
        }

        private static string DescribeEntry(object entry)
        {
            if (entry == null) return "<null>";
            Type type = entry.GetType();
            var values = new List<string>();
            foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                object value = field.GetValue(entry);
                values.Add(field.Name + "=" + DescribeValue(value));
            }
            foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!property.CanRead || property.GetIndexParameters().Length != 0) continue;
                if (values.Any(value => value.StartsWith(property.Name + "=", StringComparison.Ordinal))) continue;
                try { values.Add(property.Name + "=" + DescribeValue(property.GetValue(entry, null))); }
                catch { }
            }
            return type.Name + " { " + string.Join(", ", values) + " }";
        }

        private static string DescribeValue(object value)
        {
            if (value == null) return "<null>";
            if (value is UnityEngine.Object unityObject)
                return unityObject.name + " [" + unityObject.GetType().Name + "] " + AssetDatabase.GetAssetPath(unityObject);
            return value.ToString();
        }

        private static string GetHierarchyPath(GameObject go)
        {
            if (go == null) return "<null>";
            var names = new Stack<string>();
            Transform current = go.transform;
            while (current != null)
            {
                names.Push(current.name);
                current = current.parent;
            }
            return string.Join("/", names);
        }
    }
}
#endif
