#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using PoolPhysics.Unity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VR147.AAA.Editor
{
    /// <summary>Read-only inventory for migrating the Bay2 Snooker rack from Bay1.</summary>
    public static class Bay2RackMigrationAudit
    {
        private const string ScenePath = "Assets/Scenes/147VR_MainScene_WipRoom.unity";
        private const string ReportPath = "Logs/Bay2_RackMigrationAudit_20261119.txt";

        public static void RunBatch()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid() || !scene.isLoaded)
                throw new InvalidOperationException("Could not open WipRoom for read-only Bay2 rack audit.");
            Run();
        }

        [MenuItem("147VR/Production/Audit Bay2 Rack Migration (Read Only)")]
        public static void Run()
        {
            var scene = SceneManager.GetSceneByPath(ScenePath);
            if (!scene.IsValid() || !scene.isLoaded)
                throw new InvalidOperationException("Open WipRoom before running the Bay2 rack audit.");

            var sb = new StringBuilder(16000);
            sb.AppendLine("147VR BAY2 RACK MIGRATION AUDIT");
            sb.AppendLine("Timestamp: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine("Unity: " + Application.unityVersion);
            sb.AppendLine("Scene: " + ScenePath);
            sb.AppendLine("Read-only: YES");
            sb.AppendLine();

            var runners = Resources.FindObjectsOfTypeAll<PoolPhysicsEventRunner>()
                .Where(x => x != null && x.gameObject.scene == scene)
                .OrderBy(x => x.name, StringComparer.Ordinal)
                .ToArray();

            PoolPhysicsEventRunner bay1 = runners.FirstOrDefault(x => x.name == "Bay1_PhysicsRoot");
            PoolPhysicsEventRunner bay2 = runners.FirstOrDefault(x => x.name == "Bay2_PhysicsRoot");

            foreach (var runner in runners)
            {
                sb.AppendLine("RUNNER " + runner.name);
                sb.AppendLine("  variant=" + runner.Variant +
                    " balls=" + runner.BallCount +
                    " cueIndex=" + runner.CueBallIndex +
                    " tableRoot=" + (runner.TableRootTransform != null ? GetPath(runner.TableRootTransform) : "<null>") +
                    " playfield=" + runner.PlayfieldSize);
                if (runner.TableRootTransform != null)
                {
                    var t = runner.TableRootTransform;
                    sb.AppendLine("  table worldPos=" + t.position.ToString("F5") +
                        " worldRot=" + t.rotation.eulerAngles.ToString("F3") +
                        " lossyScale=" + t.lossyScale.ToString("F4"));
                }

                for (int i = 0; i < runner.BallCount; i++)
                {
                    var ball = runner.BallTransformAt(i);
                    if (ball == null)
                    {
                        sb.AppendLine("  ball[" + i.ToString("00") + "]=<NULL>");
                        continue;
                    }

                    string parent = ball.parent != null ? GetPath(ball.parent) : "<root>";
                    string relative = runner.TableRootTransform != null
                        ? runner.TableRootTransform.InverseTransformPoint(ball.position).ToString("F5")
                        : "<no table root>";
                    var source = PrefabUtility.GetCorrespondingObjectFromSource(ball.gameObject);
                    string sourcePath = source != null ? AssetDatabase.GetAssetPath(source) : "<scene object>";
                    int renderers = ball.GetComponentsInChildren<Renderer>(true).Length;
                    int colliders = ball.GetComponentsInChildren<Collider>(true).Length;
                    int rigidbodies = ball.GetComponentsInChildren<Rigidbody>(true).Length;
                    sb.AppendLine("  ball[" + i.ToString("00") + "]=" + ball.name +
                        " | parent=" + parent +
                        " | local=" + ball.localPosition.ToString("F5") +
                        " | world=" + ball.position.ToString("F5") +
                        " | tableLocal=" + relative +
                        " | scale=" + ball.lossyScale.ToString("F4") +
                        " | renderers=" + renderers +
                        " colliders=" + colliders +
                        " rigidbodies=" + rigidbodies +
                        " | source=" + sourcePath);
                }
                sb.AppendLine();
            }

            if (bay1 == null || bay2 == null)
            {
                sb.AppendLine("ERROR: Bay1_PhysicsRoot or Bay2_PhysicsRoot is missing.");
            }
            else
            {
                sb.AppendLine("MIGRATION PRECONDITIONS");
                sb.AppendLine("  sourceCount=" + bay1.BallCount + " targetCount=" + bay2.BallCount);
                sb.AppendLine("  sourceTable=" + (bay1.TableRootTransform != null ? bay1.TableRootTransform.name : "<null>"));
                sb.AppendLine("  targetTable=" + (bay2.TableRootTransform != null ? bay2.TableRootTransform.name : "<null>"));
                sb.AppendLine("  sourceScale=" + (bay1.TableRootTransform != null ? bay1.TableRootTransform.lossyScale.ToString("F4") : "<null>"));
                sb.AppendLine("  targetScale=" + (bay2.TableRootTransform != null ? bay2.TableRootTransform.lossyScale.ToString("F4") : "<null>"));

                var targetObjects = new HashSet<UnityEngine.Object>();
                var targetBalls = new List<Transform>();
                for (int i = 0; i < bay2.BallCount; i++)
                {
                    var ball = bay2.BallTransformAt(i);
                    if (ball == null) continue;
                    targetBalls.Add(ball);
                    targetObjects.Add(ball);
                    targetObjects.Add(ball.gameObject);
                    foreach (var component in ball.GetComponents<Component>())
                        if (component != null) targetObjects.Add(component);
                }

                sb.AppendLine("  targetUniqueTransforms=" + targetBalls.Distinct().Count());
                sb.AppendLine("  targetCueFirst=" + (targetBalls.Count > 0 && SnookerBallTracker.IsCueBallName(targetBalls[0].name)));
                sb.AppendLine("  targetObjectsToReplace=" + targetObjects.Count);
                sb.AppendLine("  external serialized references to current Bay2 balls:");
                int externalCount = 0;
                foreach (var component in Resources.FindObjectsOfTypeAll<Component>()
                    .Where(c => c != null && c.gameObject.scene == scene))
                {
                    if (component is Transform) continue;
                    try
                    {
                        var so = new SerializedObject(component);
                        var iterator = so.GetIterator();
                        bool enterChildren = true;
                        while (iterator.Next(enterChildren))
                        {
                            enterChildren = true;
                            if (iterator.propertyType != SerializedPropertyType.ObjectReference) continue;
                            var referenced = iterator.objectReferenceValue;
                            if (referenced == null || !targetObjects.Contains(referenced)) continue;
                            if (component is PoolPhysicsEventRunner && component == bay2) continue;
                            externalCount++;
                            sb.AppendLine("    " + GetPath(component.transform) + " :: " +
                                component.GetType().Name + "." + iterator.propertyPath +
                                " -> " + referenced.name);
                        }
                    }
                    catch (Exception ex)
                    {
                        sb.AppendLine("    scan-warning " + GetPath(component.transform) +
                            " :: " + component.GetType().Name + " :: " + ex.GetType().Name);
                    }
                }
                sb.AppendLine("  externalReferenceCount=" + externalCount);
            }

            Directory.CreateDirectory("Logs");
            File.WriteAllText(ReportPath, sb.ToString());
            Debug.Log(sb.ToString());
        }

        private static string GetPath(Transform t)
        {
            if (t == null) return "<null>";
            var names = new Stack<string>();
            while (t != null) { names.Push(t.name); t = t.parent; }
            return string.Join("/", names);
        }
    }
}
#endif
