using System.IO;
using UnityEditor;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class M5BatchTrigger
{
    private const string ScenePath = "Assets/AAA/PhysicsCalibration/147VR_PhysicsCalibration.unity";
    private const string Marker = "Assets/AAA/PhysicsCalibration/.m5_batch_go";
    private static bool queued;

    static M5BatchTrigger()
    {
        EditorApplication.update += Tick;
    }

    private static void Tick()
    {
        if (queued || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
            return;
        if (SceneManager.GetActiveScene().path != ScenePath)
            return;
        string full = Path.Combine(System.IO.Directory.GetParent(UnityEngine.Application.dataPath).FullName,
            Marker.Replace("/", System.IO.Path.DirectorySeparatorChar.ToString()));
        if (!File.Exists(full)) return;
        queued = true;
        File.Delete(full);
        EditorApplication.delayCall += Run;
    }

    private static void Run()
    {
        try { M5BatchCI.Run(); }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogError("[M5 BATCH TRIGGER] FAILED: " + ex);
            queued = false;
        }
    }
}
