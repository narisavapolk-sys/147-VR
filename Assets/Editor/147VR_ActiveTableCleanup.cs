#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Safe, undoable cleanup for the main V008 table scene.
/// It only rewires known tableRoot fields and removes the inactive legacy prefab instance
/// when no serialized references remain. The scene is marked dirty but never auto-saved.
/// </summary>
public static class _147VR_ActiveTableCleanup
{
    private const string MainScenePath = "Assets/Scenes/147VR_MainScene.unity";
    private const string ActiveTableName = "147VR_Table_V008_OPT_VISUAL";
    private const string LegacyPrefabPath = "Assets/AAA/ImportedSnooker/Prefab_WPBSA_12Foot_Snooker.prefab";

    [MenuItem("147VR/Scene Cleanup/Validate Main Table Wiring")]
    public static void ValidateMainTableWiring()
    {
        Scene scene = EditorSceneManager.GetActiveScene();
        if (!IsMainScene(scene)) return;

        GameObject table = FindUniqueSceneObject(scene, ActiveTableName);
        Renderer surface = FindTableSurface(table);
        int pockets = CountPocketPads(table);
        int ballBodies = CountBallLikeRigidbodies(scene);
        GameObject legacy = FindLegacyPrefabRoot(scene);
        GameObject[] bedMarkers = FindSceneGameObjects(scene)
            .Where(go => go.name == "Bed_Collider" && (legacy == null || !IsWithin(go, legacy.transform)))
            .ToArray();

        Debug.Log(
            "[147VR Table Audit] " +
            "scene=" + scene.path +
            ", activeTable=" + (table != null) +
            ", surfaceRenderer=" + (surface != null) +
            ", pocketPads=" + pockets +
            ", ballLikeRigidbodies=" + ballBodies +
            ", legacyPrefabInstance=" + (legacy != null ? legacy.name + " (active=" + legacy.activeInHierarchy + ")" : "none") +
            ", externalLegacyRefs=" + (legacy != null ? FindExternalReferences(scene, legacy.transform).Count : 0) +
            ", rootBedColliderMarkers=" + bedMarkers.Length);

        if (ballBodies == 0)
            Debug.LogWarning("[147VR Table Audit] No active ball-sized SphereCollider + Rigidbody set found in this scene. This tool does not create or restore balls.");
    }

    [MenuItem("147VR/Scene Cleanup/Rewire V008 + Remove Inactive Legacy Table")]
    public static void RewireAndRemoveLegacyTable()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("[147VR Table Cleanup] Stop Play Mode before editing the scene.");
            return;
        }

        Scene scene = EditorSceneManager.GetActiveScene();
        if (!IsMainScene(scene)) return;

        GameObject table = FindUniqueSceneObject(scene, ActiveTableName);
        Renderer surface = FindTableSurface(table);
        int pockets = CountPocketPads(table);
        if (table == null || !table.activeInHierarchy || surface == null || pockets < 4)
        {
            Debug.LogError("[147VR Table Cleanup] Aborted: active V008 table, enabled TABLE SURFACE renderer, or pocket-pad hierarchy is missing.");
            return;
        }

        PlayerViewManager viewManager = FindSingleSceneComponent<PlayerViewManager>(scene);
        SnookerBallTracker ballTracker = FindSingleSceneComponent<SnookerBallTracker>(scene);
        SnookerPhysicsSetup physicsSetup = FindSingleSceneComponent<SnookerPhysicsSetup>(scene);
        if (viewManager == null || ballTracker == null || physicsSetup == null)
        {
            Debug.LogError("[147VR Table Cleanup] Aborted: expected exactly one PlayerViewManager, SnookerBallTracker, and SnookerPhysicsSetup in the active scene.");
            return;
        }

        GameObject legacy = FindLegacyPrefabRoot(scene);
        if (legacy != null && legacy.activeInHierarchy)
        {
            Debug.LogError("[147VR Table Cleanup] Aborted: the legacy table prefab instance is active; this tool only removes an inactive instance.");
            return;
        }

        int ballBodies = CountBallLikeRigidbodies(scene);
        string warning = ballBodies == 0
            ? "\n\nWARNING: no active ball-sized Rigidbody set was found. This operation does not create balls; verify the snooker ball rack/spawn workflow separately."
            : "";
        string prompt =
            "Rewire PlayerViewManager, SnookerBallTracker, and SnookerPhysicsSetup to the active V008 table.\n" +
            "Remove the inactive legacy prefab instance only if no serialized references remain.\n" +
            "Remove a separate disabled Bed_Collider marker only if it has no references.\n\n" +
            "Pocket pads: " + pockets + "\n" +
            "Ball-like rigidbodies: " + ballBodies + warning +
            "\n\nThe scene will be marked dirty but NOT saved. Undo remains available.";
        if (!EditorUtility.DisplayDialog("147VR Main Table Cleanup", prompt, "Apply (Undoable)", "Cancel"))
            return;

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Rewire main table to V008");

        Undo.RecordObject(viewManager, "Rewire PlayerViewManager table root");
        Undo.RecordObject(ballTracker, "Rewire SnookerBallTracker table root");
        Undo.RecordObject(physicsSetup, "Rewire SnookerPhysicsSetup table root");
        viewManager.tableRoot = table.transform;
        ballTracker.tableRoot = table.transform;
        physicsSetup.tableRoot = table.transform;
        EditorUtility.SetDirty(viewManager);
        EditorUtility.SetDirty(ballTracker);
        EditorUtility.SetDirty(physicsSetup);

        bool legacyRemoved = false;
        if (legacy != null)
        {
            List<string> refs = FindExternalReferences(scene, legacy.transform);
            if (refs.Count == 0)
            {
                Undo.RegisterFullObjectHierarchyUndo(legacy, "Remove inactive legacy table instance");
                Undo.DestroyObjectImmediate(legacy);
                legacyRemoved = true;
            }
            else
            {
                Debug.LogWarning("[147VR Table Cleanup] Kept legacy prefab instance because serialized references remain: " + string.Join(", ", refs.Take(8)));
            }
        }

        GameObject[] bedMarkers = FindSceneGameObjects(scene)
            .Where(go => go.name == "Bed_Collider" && (legacy == null || !IsWithin(go, legacy.transform)))
            .ToArray();
        bool bedMarkerRemoved = false;
        if (bedMarkers.Length == 1)
        {
            GameObject bed = bedMarkers[0];
            Collider[] colliders = bed.GetComponentsInChildren<Collider>(true);
            Renderer[] renderers = bed.GetComponentsInChildren<Renderer>(true);
            bool allDisabled = colliders.Length > 0 && colliders.All(c => !c.enabled);
            List<string> refs = FindExternalReferences(scene, bed.transform);
            if (allDisabled && renderers.Length == 0 && refs.Count == 0)
            {
                Undo.RegisterFullObjectHierarchyUndo(bed, "Remove unused disabled Bed_Collider marker");
                Undo.DestroyObjectImmediate(bed);
                bedMarkerRemoved = true;
            }
            else
            {
                Debug.LogWarning("[147VR Table Cleanup] Kept Bed_Collider marker; it is not proven unused (disabled=" + allDisabled + ", renderers=" + renderers.Length + ", externalRefs=" + refs.Count + ").");
            }
        }
        else if (bedMarkers.Length > 1)
        {
            Debug.LogWarning("[147VR Table Cleanup] Kept Bed_Collider markers because more than one candidate exists.");
        }

        EditorSceneManager.MarkSceneDirty(scene);
        Undo.CollapseUndoOperations(undoGroup);
        Debug.Log("[147VR Table Cleanup] Applied. tableRoot fields now target " + table.name + "; legacyRemoved=" + legacyRemoved + "; disabledBedMarkerRemoved=" + bedMarkerRemoved + "; ballLikeRigidbodies=" + ballBodies + ". Scene NOT saved.");
        EditorUtility.DisplayDialog(
            "147VR Cleanup Applied",
            "Scene is marked dirty. Inspect the Hierarchy and Console, then save manually if correct.\n\n" +
            "Legacy prefab removed: " + legacyRemoved + "\n" +
            "Disabled Bed_Collider removed: " + bedMarkerRemoved + "\n" +
            "Ball-like rigidbodies found: " + ballBodies + "\n\n" +
            "No ball objects were created or modified.",
            "OK");
    }

    private static bool IsMainScene(Scene scene)
    {
        if (scene.IsValid() && scene.isLoaded && string.Equals(scene.path, MainScenePath, StringComparison.OrdinalIgnoreCase))
            return true;
        Debug.LogError("[147VR Table Cleanup] Open " + MainScenePath + " as the active scene first. No changes made.");
        return false;
    }

    private static GameObject FindUniqueSceneObject(Scene scene, string objectName)
    {
        GameObject[] matches = FindSceneGameObjects(scene).Where(go => go.name == objectName).ToArray();
        if (matches.Length != 1)
        {
            Debug.LogError("[147VR Table Cleanup] Expected one scene object named " + objectName + "; found " + matches.Length + ".");
            return null;
        }
        return matches[0];
    }

    private static Renderer FindTableSurface(GameObject table)
    {
        if (table == null) return null;
        return table.GetComponentsInChildren<Renderer>(true)
            .FirstOrDefault(r => r.name == "TABLE SURFACE" && r.enabled && r.gameObject.activeInHierarchy);
    }

    private static int CountPocketPads(GameObject table)
    {
        if (table == null) return 0;
        return table.GetComponentsInChildren<Transform>(true)
            .Count(t => t.gameObject.activeInHierarchy && t.name.StartsWith("Pocket Pad", StringComparison.Ordinal));
    }

    private static int CountBallLikeRigidbodies(Scene scene)
    {
        return FindSceneObjects<Rigidbody>(scene).Count(rb =>
        {
            if (rb == null || !rb.gameObject.activeInHierarchy) return false;
            SphereCollider sphere = rb.GetComponent<SphereCollider>();
            return sphere != null && sphere.radius >= 0.02f && sphere.radius <= 0.04f;
        });
    }

    private static T FindSingleSceneComponent<T>(Scene scene) where T : Component
    {
        T[] matches = FindSceneObjects<T>(scene);
        if (matches.Length != 1)
        {
            Debug.LogError("[147VR Table Cleanup] Expected one " + typeof(T).Name + "; found " + matches.Length + ".");
            return null;
        }
        return matches[0];
    }

    private static GameObject FindLegacyPrefabRoot(Scene scene)
    {
        GameObject[] roots = FindSceneGameObjects(scene)
            .Select(go => PrefabUtility.GetOutermostPrefabInstanceRoot(go))
            .Where(go => go != null && go.scene == scene &&
                string.Equals(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go), LegacyPrefabPath, StringComparison.OrdinalIgnoreCase))
            .Distinct()
            .ToArray();
        if (roots.Length > 1)
        {
            Debug.LogWarning("[147VR Table Cleanup] Found multiple legacy prefab instances; none will be removed automatically.");
            return null;
        }
        return roots.Length == 1 ? roots[0] : null;
    }

    private static GameObject[] FindSceneGameObjects(Scene scene)
    {
        return Resources.FindObjectsOfTypeAll<GameObject>()
            .Where(go => go != null && go.scene == scene)
            .ToArray();
    }

    private static T[] FindSceneObjects<T>(Scene scene) where T : Component
    {
        return Resources.FindObjectsOfTypeAll<T>()
            .Where(component => component != null && component.gameObject.scene == scene)
            .ToArray();
    }

    private static List<string> FindExternalReferences(Scene scene, Transform targetRoot)
    {
        var references = new List<string>();
        foreach (Component component in FindSceneObjects<Component>(scene))
        {
            if (component == null || IsWithin(component, targetRoot)) continue;
            try
            {
                var serialized = new SerializedObject(component);
                SerializedProperty property = serialized.GetIterator();
                while (property.Next(true))
                {
                    if (property.propertyType == SerializedPropertyType.ObjectReference &&
                        IsWithin(property.objectReferenceValue, targetRoot))
                    {
                        references.Add(component.gameObject.name + "." + property.propertyPath);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[147VR Table Cleanup] Could not inspect serialized references on " + component.GetType().Name + ": " + ex.Message);
            }
        }
        return references.Distinct().ToList();
    }

    private static bool IsWithin(UnityEngine.Object candidate, Transform root)
    {
        if (candidate == null || root == null) return false;
        Transform candidateTransform = null;
        if (candidate is GameObject go) candidateTransform = go.transform;
        else if (candidate is Component component) candidateTransform = component.transform;
        return candidateTransform != null && (candidateTransform == root || candidateTransform.IsChildOf(root));
    }
}
#endif

