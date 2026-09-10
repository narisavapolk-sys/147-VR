using System;
using System.Reflection;
using UnityEngine;

/// <summary>
/// MR bridge. It intentionally has no compile-time Meta XR dependency so the project
/// remains importable before Meta XR SDK is installed.
/// </summary>
public sealed class QuestPassthroughBridge : MonoBehaviour
{
    public bool enableOnStart;
    public bool makeSkyboxTransparent = true;

    private void Start()
    {
        if (enableOnStart)
            SetPassthrough(true);
    }

    public bool SetPassthrough(bool enabled)
    {
        if (enabled && makeSkyboxTransparent)
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientLight = Color.black;
        }

        Type managerType = FindType("OVRManager");
        if (managerType == null)
        {
            Debug.LogWarning("[QuestPassthroughBridge] Meta XR SDK is not installed; MR remains unavailable.");
            return false;
        }

        try
        {
            object instance = managerType.GetProperty("instance", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
            PropertyInfo property = managerType.GetProperty("isInsightPassthroughEnabled", BindingFlags.Public | BindingFlags.Instance);
            if (instance != null && property != null && property.CanWrite)
            {
                property.SetValue(instance, enabled);
                Debug.Log($"[QuestPassthroughBridge] Passthrough {(enabled ? "enabled" : "disabled")}.");
                return true;
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[QuestPassthroughBridge] Could not toggle Meta passthrough: {exception.Message}");
        }

        return false;
    }

    private static Type FindType(string typeName)
    {
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type type = assembly.GetType(typeName, false);
            if (type != null)
                return type;
        }
        return null;
    }
}
