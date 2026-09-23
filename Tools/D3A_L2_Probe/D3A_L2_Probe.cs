using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

public static class D3A_L2_Probe
{
    private static ListRequest _list;
    private static SearchRequest _search;
    private static object _registries;
    private static double _deadline;

    public static void Run()
    {
        Debug.Log("[D3A L2] BEGIN");
        _list = Client.List();
        _search = Client.SearchAll();
        _registries = InvokeInternalRegistryClient();
        _deadline = EditorApplication.timeSinceStartup + 45.0;
        EditorApplication.update += Poll;
    }

    private static object InvokeInternalRegistryClient()
    {
        var containerType = Type.GetType("UnityEditor.PackageManager.UI.Internal.ServicesContainer, UnityEditor");
        var instanceProperty = typeof(ScriptableSingleton<>).MakeGenericType(containerType)
            .GetProperty("instance", BindingFlags.Static | BindingFlags.Public);
        var container = instanceProperty.GetValue(null);
        var resolve = containerType.GetMethod("Resolve", BindingFlags.Instance | BindingFlags.Public);
        var clientType = Type.GetType("UnityEditor.PackageManager.UI.Internal.UpmRegistryClient, UnityEditor");
        var service = resolve.MakeGenericMethod(clientType).Invoke(container, null);
        if (service == null) throw new Exception("UpmRegistryClient service resolved null.");
        var prop = clientType.GetProperty("getRegistriesOperation",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (prop == null) throw new Exception("UpmRegistryClient.getRegistriesOperation not found.");
        var op = prop.GetValue(service);
        if (op == null) throw new Exception("getRegistriesOperation returned null.");
        Debug.Log("[D3A L2] registry operation type=" + op.GetType().FullName);

        var check = clientType.GetMethod("CheckRegistriesChanged",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (check == null) throw new Exception("UpmRegistryClient.CheckRegistriesChanged not found.");
        check.Invoke(service, null);
        Debug.Log("[D3A L2] CheckRegistriesChanged INVOKED via ServicesContainer.Resolve<UpmRegistryClient>");
        return op;
    }

    private static void Poll()
    {
        if (_list != null && _list.IsCompleted)
        {
            Debug.Log($"[D3A L2] List COMPLETED status={_list.Status} error={_list.Error}");
            _list = null;
        }
        if (_search != null && _search.IsCompleted)
        {
            Debug.Log($"[D3A L2] SearchAll COMPLETED status={_search.Status} error={_search.Error}");
            _search = null;
        }

        if (_registries != null)
        {
            var completed = GetValue(_registries, "IsCompleted");
            if (completed is bool b && b)
            {
                Debug.Log($"[D3A L2] GetRegistries COMPLETED status={GetValue(_registries, "Status")} error={GetValue(_registries, "Error")}");
                _registries = null;
            }
        }

        if ((_list == null && _search == null && _registries == null) ||
            EditorApplication.timeSinceStartup >= _deadline)
        {
            Debug.Log("[D3A L2] END");
            EditorApplication.update -= Poll;
            EditorApplication.Exit(0);
        }
    }

    private static object GetValue(object target, string name)
    {
        var t = target.GetType();
        var p = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (p != null) return p.GetValue(target);
        var f = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        return f?.GetValue(target);
    }
}
