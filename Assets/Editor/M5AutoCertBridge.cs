using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class M5AutoCertBridge
{
    private const string Marker = "Assets/AAA/PhysicsCalibration/.m5_go";
    private static bool queued;

    static M5AutoCertBridge()
    {
        EditorApplication.update += Tick;
    }

    private static void Tick()
    {
        if (queued || EditorApplication.isPlayingOrWillChangePlaymode)
            return;
        if (!File.Exists(Marker))
            return;
        queued = true;
        File.Delete(Marker);
        EditorApplication.update -= Tick;
        EditorApplication.delayCall += Run;
    }

    private static void Run()
    {
        try
        {
            M5FinalCertification.Run();
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[M5 AUTO CERT] " + ex);
            queued = false;
            EditorApplication.update += Tick;
        }
    }
}
