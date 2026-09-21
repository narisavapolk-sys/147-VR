using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VR147.Input;

public static class M5TwoHandCueContract_TMP
{
    const string Scene = "Assets/Scenes/147VR_MainScene.unity";
    public static void Run()
    {
        EditorSceneManager.OpenScene(Scene, OpenSceneMode.Single);
        var source = UnityEngine.Object.FindFirstObjectByType<VR147TwoHandCuePoseSource>(FindObjectsInactive.Include);
        var dominant = UnityEngine.Object.FindFirstObjectByType<VR147DominantHand>(FindObjectsInactive.Include);
        var cue = UnityEngine.Object.FindFirstObjectByType<SnookerCueController>(FindObjectsInactive.Include);
        if (source == null || dominant == null || cue == null) throw new Exception($"Missing source={source!=null} dominant={dominant!=null} cue={cue!=null}");
        var so = new SerializedObject(cue);
        var en = so.FindProperty("useTwoHandCuePose");
        var sp = so.FindProperty("twoHandCuePoseSource");
        if (en == null || sp == null || !en.boolValue || sp.objectReferenceValue != source)
            throw new Exception("Cue two-hand opt-in/reference is not persisted");

        var left = new GameObject("TMP_LeftFallback").transform;
        var right = new GameObject("TMP_RightFallback").transform;
        left.position = new Vector3(-0.5f, 1f, 0f);
        right.position = new Vector3(0.5f, 1f, 0f);
        var sso = new SerializedObject(source);
        sso.FindProperty("leftFallbackAnchor").objectReferenceValue = left;
        sso.FindProperty("rightFallbackAnchor").objectReferenceValue = right;
        sso.ApplyModifiedPropertiesWithoutUndo();
        var update = typeof(VR147TwoHandCuePoseSource).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);
        if (update == null) throw new Exception("Source Update missing");
        Check(source, dominant, update, VR147Hand.Right, left.position, right.position, new Vector3(-1f,0f,0f));
        Check(source, dominant, update, VR147Hand.Left, right.position, left.position, new Vector3(1f,0f,0f));
        UnityEngine.Object.DestroyImmediate(left.gameObject);
        UnityEngine.Object.DestroyImmediate(right.gameObject);
        Debug.Log("[M5 XR TWO-HAND] CONTRACT PASS right+left role mapping, fallback validity, separation and cue axis.");
        EditorApplication.Exit(0);
    }
    static void Check(VR147TwoHandCuePoseSource s, VR147DominantHand d, MethodInfo update, VR147Hand hand, Vector3 bridge, Vector3 stroke, Vector3 axis)
    {
        d.SetDominantHand(hand);
        update.Invoke(s, null);
        if (!s.IsValid) throw new Exception($"Invalid pose {hand}");
        if (Vector3.Distance(s.BridgePosition, bridge) > .0001f) throw new Exception($"Bridge mismatch {hand}");
        if (Vector3.Distance(s.StrokePosition, stroke) > .0001f) throw new Exception($"Stroke mismatch {hand}");
        if (Mathf.Abs(s.HandSeparation - 1f) > .0001f) throw new Exception($"Separation mismatch {hand}");
        if (Vector3.Distance(s.CueAxis, axis) > .0001f) throw new Exception($"Axis mismatch {hand}: {s.CueAxis}");
    }
}
