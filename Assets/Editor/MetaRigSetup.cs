using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Adds OVRCameraRig + OVRPassthroughLayer to SampleScene and enables QuestPassthroughBridge.
///
/// Written with reflection only, so it compiles and runs even BEFORE the Meta XR SDK is
/// installed. Run it again after installing Meta XR All-in-One SDK.
///
///   Menu:  Tools > Meta XR > Setup MR Rig (SampleScene)
///   Batch: Unity.exe -batchmode -quit -projectPath &lt;path&gt; -executeMethod MetaRigSetup.SetupMrRig
/// </summary>
public static class MetaRigSetup
{
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";

    [MenuItem("Tools/Meta XR/Setup MR Rig (SampleScene)")]
    public static void SetupMrRig()
    {
        bool hasMetaSdk = FindType("OVRManager") != null;

        if (!hasMetaSdk)
        {
            Debug.LogWarning("[MetaRigSetup] Meta XR SDK is not installed yet — skipping rig creation and camera removal.");
            EnsurePlayerView();
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        // 1. Remove a previously added rig so this is idempotent.
        GameObject oldRig = GameObject.Find("OVRCameraRig");
        if (oldRig != null)
            UnityEngine.Object.DestroyImmediate(oldRig);

        // 2. Remove the default Main Camera (OVRCameraRig ships its own stereo cameras).
        GameObject mainCamera = GameObject.Find("Main Camera");
        if (mainCamera != null)
            UnityEngine.Object.DestroyImmediate(mainCamera);

        // 3. Instantiate the SDK's OVRCameraRig prefab if the SDK is present.
        GameObject rig = null;
        if (hasMetaSdk)
        {
            rig = InstantiateOvrcameraRig();
            if (rig == null)
                Debug.LogWarning("[MetaRigSetup] OVRCameraRig.prefab not found in the SDK; rig must be added manually.");
        }

        // Position: standing just south of the table (table at origin), eye height 1.6 m, facing -Z.
        if (rig != null)
        {
            rig.transform.SetPositionAndRotation(new Vector3(0f, 1.6f, 2.6f), Quaternion.Euler(0f, 180f, 0f));

            // Make sure the rig's centre eye has an AudioListener (replaces the deleted one).
            Transform centerEye = rig.transform.Find("TrackingSpace/CenterEyeAnchor");
            if (centerEye != null && centerEye.GetComponent<AudioListener>() == null)
                centerEye.gameObject.AddComponent<AudioListener>();

            if (hasMetaSdk)
                EnablePassthrough(rig);
        }

        // 4. Flip QuestPassthroughBridge to auto-enable at runtime.
        GameObject questSetup = GameObject.Find("Quest Setup");
        if (questSetup != null)
        {
            Component bridge = questSetup.GetComponent("QuestPassthroughBridge");
            if (bridge != null)
            {
                FieldInfo field = bridge.GetType().GetField("enableOnStart", BindingFlags.Public | BindingFlags.Instance);
                if (field != null)
                {
                    field.SetValue(bridge, true);
                    Debug.Log("[MetaRigSetup] QuestPassthroughBridge.enableOnStart = true");
                }
            }
            else
            {
                Debug.LogWarning("[MetaRigSetup] Quest Setup has no QuestPassthroughBridge component.");
            }
        }

        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[MetaRigSetup] Done. Meta SDK present: {hasMetaSdk}. Check the Scene view, then press Play.");
    }

    /// <summary>
    /// Attaches PlayerViewManager to "Quest Setup" (with the ConcertRoom as table root) so
    /// the rig/camera is positioned for the active player at runtime. Safe to run without the
    /// Meta XR SDK.
    /// </summary>
    [MenuItem("Tools/Meta XR/Setup Player Views (SampleScene)")]
    public static void EnsurePlayerView()
    {
        bool hasMetaSdk = FindType("OVRManager") != null;
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        GameObject questSetup = GameObject.Find("Quest Setup");
        if (questSetup == null)
        {
            Debug.LogError("[MetaRigSetup] 'Quest Setup' not found in scene.");
            return;
        }

        Type managerType = Type.GetType("PlayerViewManager, Assembly-CSharp");
        Component manager = questSetup.GetComponent("PlayerViewManager");
        if (manager == null && managerType != null)
        {
            manager = questSetup.AddComponent(managerType);
            Debug.Log("[MetaRigSetup] Added PlayerViewManager to Quest Setup");
        }

        if (manager != null)
        {
            GameObject room = GameObject.Find("ConcertRoom");
            FieldInfo tableRootField = manager.GetType().GetField("tableRoot", BindingFlags.Public | BindingFlags.Instance);
            if (tableRootField != null && room != null)
                tableRootField.SetValue(manager, room.transform);
        }

        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[MetaRigSetup] Player views ready. Meta SDK present: {hasMetaSdk}. Press Play and use keys 1 / 2 to switch sides.");
    }

    private static GameObject InstantiateOvrcameraRig()
    {
        string[] guids = AssetDatabase.FindAssets("OVRCameraRig t:Prefab");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.EndsWith("OVRCameraRig.prefab", StringComparison.OrdinalIgnoreCase))
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    GameObject rig = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    rig.name = "OVRCameraRig";
                    Debug.Log($"[MetaRigSetup] Instantiated {path}");
                    return rig;
                }
            }
        }
        return null;
    }

    private static void EnablePassthrough(GameObject rig)
    {
        // OVRManager.isInsightPassthroughEnabled = true
        Type managerType = FindType("OVRManager");
        if (managerType != null)
        {
            PropertyInfo instance = managerType.GetProperty("instance", BindingFlags.Public | BindingFlags.Static);
            PropertyInfo enabled = managerType.GetProperty("isInsightPassthroughEnabled", BindingFlags.Public | BindingFlags.Instance);
            if (instance != null && enabled != null && enabled.CanWrite)
            {
                object ovrManager = instance.GetValue(null);
                if (ovrManager != null)
                    enabled.SetValue(ovrManager, true);
            }
        }

        // OVRPassthroughLayer component on the rig (renders the camera passthrough).
        Type layerType = FindType("OVRPassthroughLayer");
        if (layerType != null && rig.GetComponent(layerType) == null)
        {
            rig.AddComponent(layerType);
            Debug.Log("[MetaRigSetup] Added OVRPassthroughLayer to OVRCameraRig");
        }
    }

    private static Type FindType(string typeName)
    {
        foreach (System.Reflection.Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type type = assembly.GetType(typeName, false);
            if (type != null)
                return type;
        }
        return null;
    }
}
