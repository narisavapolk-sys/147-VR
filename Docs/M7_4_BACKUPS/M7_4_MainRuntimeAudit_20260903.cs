using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;

public static class M7_4_MainRuntimeAudit
{
    [InitializeOnLoadMethod]
    private static void Boot()
    {
        EditorApplication.delayCall -= Run;
        EditorApplication.delayCall += Run;
    }

    [MenuItem("Tools/147VR/M7.4 Main Runtime Audit")]
    public static void Run()
    {
        EditorApplication.delayCall -= Run;
        var s=EditorSceneManager.OpenScene("Assets/Scenes/147VR_MainScene.unity",OpenSceneMode.Single);
        if(!s.IsValid()) throw new Exception("MainScene invalid");
        var table=GameObject.Find("147VR_Table_WPBSA_12ft_VISUAL_CLEAN_v004");
        var bed=GameObject.Find("147VR_Table_WPBSA_12ft_VISUAL_CLEAN_v004/Bed_Collider");
        var setup=UnityEngine.Object.FindAnyObjectByType<SnookerPhysicsSetup>();
        Debug.Log($"M7.4 MAIN AUDIT | scene={s.path} roots={s.rootCount} V004={(table?"1":"0")} V003={(GameObject.Find("147VR_Table_WPBSA_12ft_VISUAL_CLEAN_v003")?"1":"0")} Bed={(bed?"1":"0")} Setup={(setup?"1":"0")}");
        if(!table||!bed||!setup) throw new Exception("Required MainScene object missing");
        var bc=bed.GetComponent<BoxCollider>();
        Debug.Log($"M7.4 ALIGN | Bed world center={bc.bounds.center} size={bc.bounds.size} top={bc.bounds.max.y:F6}");
        foreach(var r in table.GetComponentsInChildren<Renderer>(true)) Debug.Log($"M7.4 VISUAL | {r.name} center={r.bounds.center} size={r.bounds.size}");
        setup.EnsurePhysics();
        Physics.SyncTransforms();
        Debug.Log($"M7.4 RUNTIME | SurfaceTopY={setup.SurfaceTopY:F6} PhysicsTableBounds={setup.TableBounds}");
        EditorSceneManager.SaveScene(s);
    }
}
