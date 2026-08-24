using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Places "Cute Girl Dancing" (6 dance clips) into SampleScene with an AnimatorController.
/// </summary>
public static class AddDancingCuteToScene
{
    const string ScenePath = "Assets/Scenes/SampleScene.unity";
    const string FbxPath = "Assets/Models/Girls/Cute Girl Dancing.fbx";
    const string ChubbyFbxPath = "Assets/Models/Girls/Chubby Girl Dancing.fbx";
    const string ControllerPath = "Assets/Models/Girls/CuteDance.controller";

    static readonly string[] ClipNames =
    {
        "Cute_Arms_Hip_Hop_Dance",
        "Cute_Booty_Hip_Hop_Dance",
        "Cute_Dancing_Twerk",
        "Cute_Hip_Hop_Dancing",
        "Cute_Hip_Hop_Dancing_1",
        "Cute_Rumba_Dancing",
    };

    [MenuItem("Tools/147/Add Dancing Cute Girl to Scene")]
    public static void Run()
    {
        // --- 1. Build AnimatorController with 6 states ---
        var controller = BuildController();

        // --- 2. Open scene, remove old instances, add new ones ---
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Scene scene = SceneManager.GetActiveScene();

        foreach (GameObject go in scene.GetRootGameObjects())
        {
            if (go.name.Contains("SLIM") || go.name.Contains("Dancing") || go.name.Contains("CuteGirl"))
                Object.DestroyImmediate(go);
        }

        // Cute Girl on the left
        AddGirl(FbxPath, "CuteGirl_Dancing", new Vector3(-2.3f, 0f, 0.8f), controller, "DANCE_CUTE_ADDED");
        // Chubby Girl on the right
        AddGirl(ChubbyFbxPath, "ChubbyGirl_Dancing", new Vector3(2.3f, 0f, 0.8f), controller, "DANCE_CHUBBY_ADDED");

        EditorSceneManager.SaveScene(scene, ScenePath);
        Debug.Log("SCENE_SAVED " + ScenePath);
    }

    static void AddGirl(string fbxPath, string name, Vector3 pos, RuntimeAnimatorController controller, string logTag)
    {
        Object prefab = AssetDatabase.LoadMainAssetAtPath(fbxPath);
        if (prefab == null)
        {
            Debug.LogError("FBX_MISSING " + fbxPath);
            return;
        }
        GameObject girl = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        girl.name = name;
        girl.transform.position = pos;
        girl.transform.rotation = Quaternion.identity;

        var animator = girl.GetComponentInChildren<Animator>();
        if (animator == null)
        {
            Debug.LogError("NO_ANIMATOR on " + name);
            return;
        }
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;

        var dancer = girl.AddComponent<CuteDancer>();
        dancer.danceClips = ClipNames;
        dancer.autoCycle = true;
        dancer.switchAfter = 0f; // play full clip, then next

        Debug.Log($"{logTag} at {pos} clips={ClipNames.Length}");
    }

    static AnimatorController BuildController()
    {
        var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (existing != null)
            AssetDatabase.DeleteAsset(ControllerPath);

        var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        var sm = controller.layers[0].stateMachine;

        // Collect clips from the FBX sub-assets
        var clips = new Dictionary<string, AnimationClip>();
        foreach (var a in AssetDatabase.LoadAllAssetsAtPath(FbxPath))
        {
            if (a is AnimationClip c && !c.name.StartsWith("__preview__") && !c.name.Contains("|"))
            {
                clips[c.name] = c;
                Debug.Log("CLIP_FOUND " + c.name);
            }
        }

        AnimatorState first = null;
        foreach (string name in ClipNames)
        {
            AnimationClip clip;
            if (!clips.TryGetValue(name, out clip))
            {
                // try longer names
                clip = clips[name + " (UnityEngine.AnimationClip)"];
            }
            if (clip == null)
            {
                Debug.LogWarning("CLIP_MISSING " + name);
                continue;
            }
            clip.wrapMode = WrapMode.Loop;
            var state = sm.AddState(name);
            state.motion = clip;
            if (first == null)
                first = state;
        }
        if (first != null)
            sm.defaultState = first;

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        return controller;
    }
}
