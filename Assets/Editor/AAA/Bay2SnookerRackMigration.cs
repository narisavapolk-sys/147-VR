#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using PoolPhysics.Unity;
using PoolPhysics.Sim;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VR147.AAA.Editor
{
    /// <summary>
    /// Replaces Bay2's short 17-ball Snooker rack with a scene-local copy of Bay1's
    /// validated 22-ball rack. Preserves Bay2's TableSpec, physics profile and runner.
    /// </summary>
    public static class Bay2SnookerRackMigration
    {
        private const string ScenePath = "Assets/Scenes/147VR_MainScene_WipRoom.unity";
        private const string ReportPath = "Logs/Bay2_RackMigration_20261119.txt";
        private const float RadiusTolerance = 0.001f;
        private const float MinimumBallSeparation = 0.045f;

        private sealed class Plan
        {
            public Scene Scene;
            public PoolPhysicsEventRunner Source;
            public PoolPhysicsEventRunner Target;
            public Transform SourceTable;
            public Transform TargetTable;
            public Transform SourceGroup;
            public Transform TargetGroup;
            public Transform TargetParent;
            public Transform[] SourceBalls;
            public Transform[] TargetBalls;
            public Transform[] TargetGroupChildren;
            public Vector3[] TargetWorldPositions;
            public Quaternion[] TargetWorldRotations;
            public Vector3[] TargetWorldScales;
            public int TargetSiblingIndex;
            public readonly List<string> Errors = new List<string>();
            public readonly StringBuilder Report = new StringBuilder(16000);
        }

        public static void DryRunBatch()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var plan = BuildPlan(scene);
            WriteReport(plan, "DRY RUN - NO SCENE MODIFICATIONS");
            if (plan.Errors.Count > 0)
                throw new InvalidOperationException("Bay2 rack dry-run failed. See " + ReportPath);
            Debug.Log("Bay2 rack dry-run passed. No scene changes were made. " + ReportPath);
        }

        public static void ApplyBatch()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var plan = BuildPlan(scene);
            WriteReport(plan, "APPLY REQUESTED - VALIDATING BEFORE MUTATION");
            if (plan.Errors.Count > 0)
                throw new InvalidOperationException("Bay2 rack migration aborted before mutation. See " + ReportPath);

            int undoGroup = Undo.GetCurrentGroup();
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Replace Bay2 17-ball rack with Bay1 standard 22-ball Snooker rack");

            GameObject newGroup = null;
            var newBalls = new List<Transform>(22);
            try
            {
                newGroup = new GameObject("ROOT_147vr_Bay2_0_SnookerBalls_22_TEMP");
                Undo.RegisterCreatedObjectUndo(newGroup, "Create Bay2 22-ball rack root");
                newGroup.transform.SetParent(plan.TargetParent, false);
                newGroup.transform.localPosition = Vector3.zero;
                newGroup.transform.localRotation = Quaternion.identity;
                newGroup.transform.localScale = Vector3.one;
                newGroup.layer = plan.TargetGroup.gameObject.layer;
                newGroup.tag = plan.TargetGroup.gameObject.tag;

                for (int i = 0; i < plan.SourceBalls.Length; i++)
                {
                    Transform sourceBall = plan.SourceBalls[i];
                    GameObject clone = UnityEngine.Object.Instantiate(sourceBall.gameObject);
                    Undo.RegisterCreatedObjectUndo(clone, "Copy Bay1 ball into Bay2");
                    clone.name = sourceBall.name.Replace("Bay1", "Bay2");
                    clone.transform.SetParent(newGroup.transform, true);
                    clone.transform.position = plan.TargetWorldPositions[i];
                    clone.transform.rotation = plan.TargetWorldRotations[i];
                    clone.transform.localScale = plan.TargetWorldScales[i];
                    clone.layer = sourceBall.gameObject.layer;
                    clone.tag = sourceBall.gameObject.tag;
                    clone.SetActive(sourceBall.gameObject.activeSelf);

                    if (clone.GetComponent<MeshFilter>() == null ||
                        clone.GetComponent<MeshRenderer>() == null ||
                        clone.GetComponent<SphereCollider>() == null ||
                        clone.GetComponent<Rigidbody>() == null)
                        throw new InvalidOperationException("Cloned ball lost a required component: " + clone.name);

                    newBalls.Add(clone.transform);
                }

                var runnerObject = new SerializedObject(plan.Target);
                SerializedProperty ballList = runnerObject.FindProperty("ballTransforms");
                if (ballList == null || !ballList.isArray)
                    throw new InvalidOperationException("Bay2 runner serialized ballTransforms list was not found.");

                Undo.RecordObject(plan.Target, "Rewire Bay2 Physics Runner to 22-ball rack");
                ballList.arraySize = newBalls.Count;
                for (int i = 0; i < newBalls.Count; i++)
                    ballList.GetArrayElementAtIndex(i).objectReferenceValue = newBalls[i];
                runnerObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(plan.Target);

                // Only destroy the old group after the runner points at all 22 replacement balls.
                Undo.DestroyObjectImmediate(plan.TargetGroup.gameObject);
                newGroup.name = "ROOT_147vr_Bay2_0_SnookerBalls";
                newGroup.transform.SetSiblingIndex(plan.TargetSiblingIndex);
                EditorSceneManager.MarkSceneDirty(plan.Scene);

                ValidatePostApply(plan, newGroup.transform, newBalls);
                if (!EditorSceneManager.SaveScene(plan.Scene))
                    throw new IOException("Unity failed to save WipRoom after Bay2 rack migration.");

                Undo.CollapseUndoOperations(Undo.GetCurrentGroup());

                plan.Report.AppendLine();
                plan.Report.AppendLine("RESULT: SUCCESS");
                plan.Report.AppendLine("Saved scene: " + ScenePath);
                plan.Report.AppendLine("Old Bay2 17-ball group removed after runner references were rewired.");
                plan.Report.AppendLine("New Bay2 group: " + newGroup.name);
                plan.Report.AppendLine("New ball count: " + newBalls.Count);
                for (int i = 0; i < newBalls.Count; i++)
                    plan.Report.AppendLine("  [" + i.ToString("00") + "] " + newBalls[i].name +
                        " world=" + newBalls[i].position.ToString("F5"));
                WriteReport(plan, "APPLIED AND SAVED");
                Debug.Log("Bay2 standard Snooker rack migration succeeded. " + ReportPath);
            }
            catch
            {
                Undo.RevertAllDownToGroup(undoGroup);
                throw;
            }
        }

        private static Plan BuildPlan(Scene scene)
        {
            var plan = new Plan { Scene = scene };
            plan.Report.AppendLine("147VR BAY2 SNOOKER RACK MIGRATION");
            plan.Report.AppendLine("Timestamp: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            plan.Report.AppendLine("Unity: " + Application.unityVersion);
            plan.Report.AppendLine("Scene: " + ScenePath);
            plan.Report.AppendLine("Mutation: " + (Application.isBatchMode ? "batch editor" : "interactive editor"));
            plan.Report.AppendLine();

            if (!scene.IsValid() || !scene.isLoaded)
                plan.Errors.Add("WipRoom is not loaded.");

            var runners = Resources.FindObjectsOfTypeAll<PoolPhysicsEventRunner>()
                .Where(x => x != null && x.gameObject.scene == scene).ToArray();
            plan.Source = runners.SingleOrDefault(x => x.name == "Bay1_PhysicsRoot");
            plan.Target = runners.SingleOrDefault(x => x.name == "Bay2_PhysicsRoot");
            if (plan.Source == null) plan.Errors.Add("Bay1_PhysicsRoot runner missing or duplicated.");
            if (plan.Target == null) plan.Errors.Add("Bay2_PhysicsRoot runner missing or duplicated.");
            if (plan.Source == null || plan.Target == null)
                return plan;

            plan.SourceTable = plan.Source.TableRootTransform;
            plan.TargetTable = plan.Target.TableRootTransform;
            if (plan.Source.Variant != GameVariant.Snooker || plan.Target.Variant != GameVariant.Snooker)
                plan.Errors.Add("Both Bay1 and Bay2 must remain Snooker variants.");
            if (plan.SourceTable == null || plan.TargetTable == null)
                plan.Errors.Add("Source or target TableRootTransform is null.");
            if (plan.Source.BallCount != 22)
                plan.Errors.Add("Bay1 source must contain exactly 22 balls; found " + plan.Source.BallCount + ".");
            if (plan.Target.BallCount != 17)
                plan.Errors.Add("Bay2 target must contain exactly 17 balls before migration; found " + plan.Target.BallCount + ".");

            plan.SourceBalls = ReadBalls(plan.Source);
            plan.TargetBalls = ReadBalls(plan.Target);
            if (plan.SourceBalls.Any(x => x == null)) plan.Errors.Add("Bay1 contains a null ball transform.");
            if (plan.TargetBalls.Any(x => x == null)) plan.Errors.Add("Bay2 contains a null ball transform.");
            if (plan.SourceBalls.Distinct().Count() != plan.SourceBalls.Length)
                plan.Errors.Add("Bay1 source ball transforms are not unique.");
            if (plan.TargetBalls.Distinct().Count() != plan.TargetBalls.Length)
                plan.Errors.Add("Bay2 target ball transforms are not unique.");

            if (plan.SourceBalls.Length > 0 && plan.SourceBalls.All(x => x != null))
                plan.SourceGroup = plan.SourceBalls[0].parent;
            if (plan.TargetBalls.Length > 0 && plan.TargetBalls.All(x => x != null))
                plan.TargetGroup = plan.TargetBalls[0].parent;

            if (plan.SourceGroup == null) plan.Errors.Add("Bay1 balls do not share a parent group.");
            if (plan.TargetGroup == null) plan.Errors.Add("Bay2 balls do not share a parent group.");
            if (plan.SourceGroup != null && plan.SourceBalls.Any(x => x != null && x.parent != plan.SourceGroup))
                plan.Errors.Add("Bay1 balls do not share one direct parent.");
            if (plan.TargetGroup != null && plan.TargetBalls.Any(x => x != null && x.parent != plan.TargetGroup))
                plan.Errors.Add("Bay2 balls do not share one direct parent.");

            if (plan.SourceGroup != null && plan.SourceGroup.childCount != plan.SourceBalls.Length)
                plan.Errors.Add("Bay1 ball parent has additional children; refusing partial group copy.");
            if (plan.TargetGroup != null)
            {
                plan.TargetGroupChildren = Enumerable.Range(0, plan.TargetGroup.childCount).Select(plan.TargetGroup.GetChild).ToArray();
                if (plan.TargetGroupChildren.Length != 22)
                    plan.Errors.Add("Bay2 group must contain 22 ball objects before replacement; found " + plan.TargetGroupChildren.Length + ".");
                                var wiredTargetBalls = new HashSet<Transform>(plan.TargetBalls ?? Array.Empty<Transform>());
                foreach (var child in plan.TargetGroupChildren)
                {
                    bool hasMesh = child.GetComponent<MeshFilter>() != null && child.GetComponent<MeshRenderer>() != null;
                    bool hasPhysics = child.GetComponent<SphereCollider>() != null && child.GetComponent<Rigidbody>() != null;
                    bool recognizedBall = SnookerBallTracker.PointsForSimName(child.name) >= 0;
                    if (!hasMesh || !recognizedBall)
                        plan.Errors.Add("Bay2 group contains a non-ball or unrecognized child; refusing to delete it: " + child.name);
                    if (wiredTargetBalls.Contains(child) && !hasPhysics)
                        plan.Errors.Add("A currently wired Bay2 ball is missing Collider/Rigidbody: " + child.name);
                }
                int visibleCues = plan.TargetGroupChildren.Count(x => SnookerBallTracker.IsCueBallName(x.name));
                int visibleReds = plan.TargetGroupChildren.Count(x => SnookerBallTracker.PointsForSimName(x.name) == 1);
                int visibleColours = plan.TargetGroupChildren.Count(x =>
                {
                    int points = SnookerBallTracker.PointsForSimName(x.name);
                    return points >= 2 && points <= 7;
                });
                plan.Report.AppendLine("Bay2 visible group identity: cue=" + visibleCues + " reds=" + visibleReds + " colours=" + visibleColours);
                if (visibleCues != 1 || visibleReds != 15 || visibleColours != 6)
                    plan.Errors.Add("Bay2 visible group is not a complete 22-ball Snooker set.");
            }

            if (plan.SourceGroup != null && plan.SourceGroup.GetComponents<Component>().Any(x => !(x is Transform)))
                plan.Errors.Add("Bay1 ball parent contains non-Transform components; review before copying.");
            if (plan.TargetGroup != null && plan.TargetGroup.GetComponents<Component>().Any(x => !(x is Transform)))
                plan.Errors.Add("Bay2 ball parent contains non-Transform components; refusing to delete it.");

            if (plan.TargetGroup != null)
            {
                plan.TargetParent = plan.TargetGroup.parent;
                plan.TargetSiblingIndex = plan.TargetGroup.GetSiblingIndex();
                if (plan.TargetParent == null)
                    plan.Errors.Add("Bay2 ball parent has no table parent.");
                else if ((plan.TargetParent.lossyScale - Vector3.one).sqrMagnitude > 0.000001f)
                    plan.Errors.Add("Bay2 table parent scale is not 1; refusing world-scale conversion.");
            }

            ValidateRackIdentity(plan, plan.SourceBalls, "Bay1 source", 22, 15, 6);
            ValidateRackIdentity(plan, plan.TargetBalls, "Bay2 current", 17, 15, 1);

            if (plan.SourceTable != null && plan.TargetTable != null)
            {
                if ((plan.SourceTable.lossyScale - Vector3.one).sqrMagnitude > 0.000001f)
                    plan.Errors.Add("Bay1 physics table scale is not 1.");
                if ((plan.TargetTable.lossyScale - Vector3.one).sqrMagnitude > 0.000001f)
                    plan.Errors.Add("Bay2 physics table scale is not 1.");
                if (Mathf.Abs(plan.Source.BallRadius - plan.Target.BallRadius) > RadiusTolerance)
                    plan.Errors.Add("Bay1 and Bay2 nominal ball radii differ.");
                if (plan.Source.Spec == null || plan.Target.Spec == null)
                    plan.Errors.Add("Bay1 or Bay2 TableSpec is missing.");
            }

            ValidateRunnerCompatibility(plan);

                        if (plan.SourceTable != null && plan.TargetTable != null &&
                plan.SourceBalls.Length == 22 && plan.SourceBalls.All(x => x != null) &&
                plan.TargetGroupChildren != null && plan.TargetGroupChildren.Length == 22)
            {
                plan.TargetWorldPositions = new Vector3[plan.SourceBalls.Length];
                plan.TargetWorldRotations = new Quaternion[plan.SourceBalls.Length];
                plan.TargetWorldScales = new Vector3[plan.SourceBalls.Length];
                Vector3[] targetLocal = new Vector3[plan.SourceBalls.Length];
                Vector2 size = plan.Target.PlayfieldSize;
                float targetReferenceRadius = plan.TargetBalls
                    .Select(x => x.GetComponent<SphereCollider>().radius * x.lossyScale.x)
                    .Average();
                var targetByKey = new Dictionary<string, Transform>(StringComparer.OrdinalIgnoreCase);
                foreach (var targetBall in plan.TargetGroupChildren)
                {
                    string key = BuildBallKey(targetBall.name);
                    if (string.IsNullOrEmpty(key) || !targetByKey.TryAdd(key, targetBall))
                        plan.Errors.Add("Bay2 existing ball identity is missing or duplicated: " + targetBall.name);
                }

                plan.Report.AppendLine("Placement policy: copy Bay1 ball objects/components, preserve Bay2's existing per-identity positions and scale.");
                plan.Report.AppendLine("Bay2 reference physical ball radius: " + targetReferenceRadius.ToString("F5") + " m");
                var sourceKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < plan.SourceBalls.Length; i++)
                {
                    Transform sourceBall = plan.SourceBalls[i];
                    string key = BuildBallKey(sourceBall.name);
                    if (string.IsNullOrEmpty(key) || !sourceKeys.Add(key) || !targetByKey.TryGetValue(key, out Transform targetBall))
                    {
                        plan.Errors.Add("No unique Bay2 position match for source ball: " + sourceBall.name);
                        continue;
                    }

                    plan.TargetWorldPositions[i] = targetBall.position;
                    plan.TargetWorldRotations[i] = targetBall.rotation;
                    plan.TargetWorldScales[i] = targetBall.lossyScale;
                    targetLocal[i] = plan.TargetTable.InverseTransformPoint(targetBall.position);

                    var sourceCollider = sourceBall.GetComponent<SphereCollider>();
                    float copiedWorldRadius = sourceCollider.radius * targetBall.lossyScale.x;
                    if (Mathf.Abs(copiedWorldRadius - targetReferenceRadius) > 0.0001f)
                        plan.Errors.Add("Source ball radius would not match Bay2 rack scale: " + sourceBall.name +
                            " radius=" + copiedWorldRadius.ToString("F5"));

                    if (targetLocal[i].x < targetReferenceRadius ||
                        targetLocal[i].x > size.x - targetReferenceRadius ||
                        targetLocal[i].z < targetReferenceRadius ||
                        targetLocal[i].z > size.y - targetReferenceRadius)
                        plan.Errors.Add("Existing Bay2 position falls outside playfield bounds: " + sourceBall.name);

                    plan.Report.AppendLine("  MAP " + key + " :: " + sourceBall.name + " -> " + targetBall.name +
                        " position=" + targetBall.position.ToString("F5") +
                        " scale=" + targetBall.lossyScale.ToString("F4"));
                }

                if (sourceKeys.Count != 22 || targetByKey.Count != 22)
                    plan.Errors.Add("Source/target canonical ball identity maps are not a complete 22-ball set.");

                float minDistance = float.MaxValue;
                for (int i = 0; i < targetLocal.Length; i++)
                for (int j = i + 1; j < targetLocal.Length; j++)
                    minDistance = Mathf.Min(minDistance, Vector3.Distance(targetLocal[i], targetLocal[j]));
                plan.Report.AppendLine("Bay2 mapped minimum centre separation: " + minDistance.ToString("F5") +
                    " m; expected diameter=" + (targetReferenceRadius * 2f).ToString("F5") + " m");
                if (minDistance < targetReferenceRadius * 2f - 0.00025f)
                    plan.Errors.Add("Existing Bay2 rack positions overlap at the copied ball radius.");
            }

            CountExternalReferences(plan);
            AppendPlanDetails(plan);
            return plan;
        }

        private static Transform[] ReadBalls(PoolPhysicsEventRunner runner)
        {
            if (runner == null) return Array.Empty<Transform>();
            var result = new Transform[runner.BallCount];
            for (int i = 0; i < result.Length; i++)
                result[i] = runner.BallTransformAt(i);
            return result;
        }

        private static string BuildBallKey(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            int points = SnookerBallTracker.PointsForSimName(name);
            if (points == 0) return "Cue";
            if (points >= 2 && points <= 7) return "Colour:" + points;
            if (points != 1) return null;

            int end = name.Length - 1;
            while (end >= 0 && char.IsDigit(name[end])) end--;
            if (end >= 0 && end < name.Length - 1 &&
                (name[end] == '.' || name[end] == '_' || name[end] == '-'))
            {
                if (int.TryParse(name.Substring(end + 1), out int number))
                    return "Red:" + number;
            }
            return "Red:0";
        }
        private static void ValidateRackIdentity(Plan plan, Transform[] balls, string label,
            int expectedTotal, int expectedReds, int expectedColours)
        {
            if (balls == null || balls.Any(x => x == null)) return;
            int cues = balls.Count(x => SnookerBallTracker.IsCueBallName(x.name));
            int reds = balls.Count(x => SnookerBallTracker.PointsForSimName(x.name) == 1);
            int colours = balls.Count(x =>
            {
                int points = SnookerBallTracker.PointsForSimName(x.name);
                return points >= 2 && points <= 7;
            });
            plan.Report.AppendLine(label + " identity: total=" + balls.Length +
                " cue=" + cues + " reds=" + reds + " colours=" + colours);
            if (balls.Length != expectedTotal || cues != 1 ||
                reds != expectedReds || colours != expectedColours)
                plan.Errors.Add(label + " identity mismatch. Expected total/cue/reds/colours=" +
                    expectedTotal + "/1/" + expectedReds + "/" + expectedColours +
                    ", found " + balls.Length + "/" + cues + "/" + reds + "/" + colours + ".");
            if (balls.Length > 0 && !SnookerBallTracker.IsCueBallName(balls[0].name))
                plan.Errors.Add(label + " cue ball is not at serialized index 0.");
        }

        private static void ValidateRunnerCompatibility(Plan plan)
        {
            var sourceSO = new SerializedObject(plan.Source);
            var targetSO = new SerializedObject(plan.Target);
            var sourceProfile = sourceSO.FindProperty("profileSO");
            var targetProfile = targetSO.FindProperty("profileSO");
            if (sourceProfile == null || targetProfile == null ||
                sourceProfile.objectReferenceValue != targetProfile.objectReferenceValue)
                plan.Errors.Add("Bay1 and Bay2 do not share the same CueVRPhysicsProfile.");

            var sourceMass = sourceSO.FindProperty("defaultBallMass");
            var targetMass = targetSO.FindProperty("defaultBallMass");
            if (sourceMass == null || targetMass == null ||
                Mathf.Abs(sourceMass.floatValue - targetMass.floatValue) > 0.00001f)
                plan.Errors.Add("Bay1 and Bay2 default ball masses differ.");

            var drivers = targetSO.FindProperty("legacyDrivers");
            int driverCount = drivers != null && drivers.isArray ? drivers.arraySize : -1;
            plan.Report.AppendLine("Bay2 legacyDrivers count: " + driverCount);
            if (drivers != null && drivers.isArray)
            {
                var oldObjects = CollectObjectTree(plan.TargetGroup);
                for (int i = 0; i < drivers.arraySize; i++)
                {
                    var driver = drivers.GetArrayElementAtIndex(i).objectReferenceValue;
                    if (driver != null && oldObjects.Contains(driver))
                        plan.Errors.Add("Bay2 legacyDrivers references an old ball component: " + driver.name);
                }
            }
        }

        private static HashSet<UnityEngine.Object> CollectObjectTree(Transform root)
        {
            var objects = new HashSet<UnityEngine.Object>();
            if (root == null) return objects;
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                objects.Add(t);
                objects.Add(t.gameObject);
                foreach (var component in t.GetComponents<Component>())
                    if (component != null) objects.Add(component);
            }
            return objects;
        }

        private static void CountExternalReferences(Plan plan)
        {
            if (plan.TargetGroup == null) return;
            var oldObjects = CollectObjectTree(plan.TargetGroup);
            var external = new List<string>();
            foreach (var component in Resources.FindObjectsOfTypeAll<Component>()
                .Where(c => c != null && c.gameObject.scene == plan.Scene))
            {
                if (component == plan.Target || component is Transform) continue;
                if (component.transform == plan.TargetGroup || component.transform.IsChildOf(plan.TargetGroup))
                    continue;
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
                        if (referenced != null && oldObjects.Contains(referenced))
                            external.Add(component.GetType().Name + "." + iterator.propertyPath +
                                " on " + GetPath(component.transform) + " -> " + referenced.name);
                    }
                }
                catch (Exception ex)
                {
                    plan.Errors.Add("Reference scan failed on " + component.GetType().Name +
                        " at " + GetPath(component.transform) + ": " + ex.GetType().Name);
                }
            }
            plan.Report.AppendLine("External serialized references to old Bay2 group: " + external.Count);
            foreach (string item in external) plan.Report.AppendLine("  " + item);
            if (external.Count > 0)
                plan.Errors.Add("Old Bay2 balls/group have external serialized references; migration refused.");
        }

        private static void AppendPlanDetails(Plan plan)
        {
            plan.Report.AppendLine();
            plan.Report.AppendLine("SOURCE GROUP: " + GetPath(plan.SourceGroup));
            AppendTransform(plan.Report, plan.SourceGroup);
            plan.Report.AppendLine("TARGET GROUP: " + GetPath(plan.TargetGroup));
            AppendTransform(plan.Report, plan.TargetGroup);
            if (plan.TargetGroupChildren != null)
            {
                var wired = new HashSet<Transform>(plan.TargetBalls ?? Array.Empty<Transform>());
                plan.Report.AppendLine("TARGET GROUP CHILD INVENTORY (22 expected):");
                foreach (var child in plan.TargetGroupChildren)
                {
                    var collider = child.GetComponent<SphereCollider>();
                    float radius = collider != null ? collider.radius * child.lossyScale.x : -1f;
                    plan.Report.AppendLine("  " + child.name + " wired=" + wired.Contains(child) + " mesh=" + (child.GetComponent<MeshFilter>() != null && child.GetComponent<MeshRenderer>() != null) + " collider=" + (collider != null) + " rigidbody=" + (child.GetComponent<Rigidbody>() != null) + " worldRadius=" + radius.ToString("F5") + " position=" + child.position.ToString("F5"));
                }
            }
            plan.Report.AppendLine("TARGET PARENT: " + GetPath(plan.TargetParent));
            AppendTransform(plan.Report, plan.TargetParent);
            plan.Report.AppendLine("SOURCE TABLE: " + GetPath(plan.SourceTable) +
                " playfield=" + plan.Source.PlayfieldSize);
            AppendTransform(plan.Report, plan.SourceTable);
            plan.Report.AppendLine("TARGET TABLE: " + GetPath(plan.TargetTable) +
                " playfield=" + plan.Target.PlayfieldSize);
            AppendTransform(plan.Report, plan.TargetTable);
            plan.Report.AppendLine();
            plan.Report.AppendLine("SOURCE BALL ORDER");
            for (int i = 0; i < plan.SourceBalls.Length; i++)
            {
                var ball = plan.SourceBalls[i];
                if (ball == null) continue;
                bool required = ball.GetComponent<MeshFilter>() != null &&
                    ball.GetComponent<MeshRenderer>() != null &&
                    ball.GetComponent<SphereCollider>() != null &&
                    ball.GetComponent<Rigidbody>() != null;
                if (!required) plan.Errors.Add("Bay1 source ball missing required same-object components: " + ball.name);
                plan.Report.AppendLine("  [" + i.ToString("00") + "] " + ball.name +
                    " componentsOK=" + required + " worldScale=" + ball.lossyScale.ToString("F4") + " worldRadius=" + (ball.GetComponent<SphereCollider>().radius * ball.lossyScale.x).ToString("F5"));
            }
            plan.Report.AppendLine();
            plan.Report.AppendLine("ERRORS: " + plan.Errors.Count);
            foreach (string error in plan.Errors) plan.Report.AppendLine("  ERROR: " + error);
        }

        private static void AppendTransform(StringBuilder sb, Transform t)
        {
            if (t == null) { sb.AppendLine("  <null>"); return; }
            sb.AppendLine("  localPos=" + t.localPosition.ToString("F5") +
                " localRot=" + t.localRotation.eulerAngles.ToString("F3") +
                " localScale=" + t.localScale.ToString("F4") +
                " lossyScale=" + t.lossyScale.ToString("F4") +
                " childCount=" + t.childCount +
                " components=" + string.Join(",", t.GetComponents<Component>().Select(x => x.GetType().Name)));
        }

        private static void ValidatePostApply(Plan plan, Transform newGroup, List<Transform> newBalls)
        {
            if (plan.Target.BallCount != 22)
                throw new InvalidOperationException("Post-apply runner count is not 22.");
            if (newGroup == null || newGroup.childCount != 22)
                throw new InvalidOperationException("New Bay2 group does not contain exactly 22 children.");
            if (plan.Target.BallTransformAt(0) != newBalls[0] ||
                !SnookerBallTracker.IsCueBallName(newBalls[0].name))
                throw new InvalidOperationException("Post-apply cue ball is not at index 0.");

            int cues = newBalls.Count(x => SnookerBallTracker.IsCueBallName(x.name));
            int reds = newBalls.Count(x => SnookerBallTracker.PointsForSimName(x.name) == 1);
            int colours = newBalls.Count(x =>
            {
                int points = SnookerBallTracker.PointsForSimName(x.name);
                return points >= 2 && points <= 7;
            });
            if (cues != 1 || reds != 15 || colours != 6)
                throw new InvalidOperationException("Post-apply identity is not cue + 15 reds + 6 colours.");

            for (int i = 0; i < newBalls.Count; i++)
            {
                if (plan.Target.BallTransformAt(i) != newBalls[i])
                    throw new InvalidOperationException("Runner list mismatch at index " + i + ".");
                if (newBalls[i] == null || newBalls[i].parent != newGroup)
                    throw new InvalidOperationException("New ball hierarchy mismatch at index " + i + ".");
                if (newBalls[i].GetComponent<MeshFilter>() == null ||
                    newBalls[i].GetComponent<MeshRenderer>() == null ||
                    newBalls[i].GetComponent<SphereCollider>() == null ||
                    newBalls[i].GetComponent<Rigidbody>() == null)
                    throw new InvalidOperationException("New ball missing Mesh/Collider/Rigidbody at index " + i + ".");
            }
        }

        private static string GetPath(Transform t)
        {
            if (t == null) return "<null>";
            var names = new Stack<string>();
            while (t != null) { names.Push(t.name); t = t.parent; }
            return string.Join("/", names);
        }

        private static void WriteReport(Plan plan, string status)
        {
            Directory.CreateDirectory("Logs");
            plan.Report.AppendLine();
            plan.Report.AppendLine("STATUS: " + status);
            File.WriteAllText(ReportPath, plan.Report.ToString());
        }
    }
}
#endif





