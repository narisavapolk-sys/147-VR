using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Builds ready-to-play pool scenes: table + racked balls + cue ball
/// + cues + camera/light + rigidbody physics on all balls.
///
/// Run headless:
///   Unity.exe -batchmode -projectPath <proj> -executeMethod BuildPoolScene.Build -quit
///   Unity.exe -batchmode -projectPath <proj> -executeMethod BuildPoolScene.Build8Ball -quit
/// </summary>
public static class BuildPoolScene
{
    private const string TablePrefab = "Assets/Prefabs/PoolTable/PREFAB POoL table.prefab";
    private const string BallsPrefab = "Assets/Prefabs/PoolTable/PREFAB POoL Balls.prefab";
    private const string CuesPrefab = "Assets/Prefabs/PoolTable/PREFAB POoL Cues.prefab";
    private const string PhysicMatPath = "Assets/Models/PoolTable/PoolBallPhysics.physicsMaterial";
    private const string OutScene9 = "Assets/Scenes/PoolTable_9Ball.unity";
    private const string OutScene8 = "Assets/Scenes/PoolTable_8Ball.unity";

    // Pool ball physics: high bounciness, low rolling friction (cloth-like).
    private const float BallBounciness = 0.92f;
    private const float BallDynamicFriction = 0.18f;
    private const float BallStaticFriction = 0.22f;
    private const float BallMass = 0.17f; // 170g standard pool ball
    private const float BallRadius = 0.028575f; // half of 57.15mm

    public static void Build()
    {
        BuildScene(OutScene9, true);
    }

    public static void Build8Ball()
    {
        BuildScene(OutScene8, false);
    }

    private static void BuildScene(string scenePath, bool nineBall)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Physics material (shared across all balls).
        var physMat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(PhysicMatPath);
        if (physMat == null)
        {
            physMat = new PhysicsMaterial("PoolBallPhysics")
            {
                bounciness = BallBounciness,
                dynamicFriction = BallDynamicFriction,
                staticFriction = BallStaticFriction,
                bounceCombine = PhysicsMaterialCombine.Maximum,
                frictionCombine = PhysicsMaterialCombine.Average,
            };
            AssetDatabase.CreateAsset(physMat, PhysicMatPath);
        }

        // Table
        var table = PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(TablePrefab)) as GameObject;
        if (table == null) { Debug.LogError("SCENE: table prefab missing"); return; }
        table.transform.position = Vector3.zero;

        // Balls + rack them
        var balls = PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(BallsPrefab)) as GameObject;
        if (balls == null) { Debug.LogError("SCENE: balls prefab missing"); return; }
        var rack = balls.AddComponent<BallRack>();
        rack.shuffle = true;

        // Add physics to every ball child.
        AddPhysicsToBalls(balls, physMat);

        if (nineBall)
            rack.RackNineBall();
        else
            rack.RackEightBall();

        // Cues (rest on the table)
        var cues = PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(CuesPrefab)) as GameObject;
        if (cues != null) cues.transform.position = Vector3.zero;

        // Camera framing the table
        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        camGo.AddComponent<AudioListener>();
        cam.transform.position = new Vector3(0f, 3.0f, 3.4f);
        cam.transform.LookAt(new Vector3(0f, 0.8f, 0f));

        // Key light
        var lightGo = new GameObject("Directional Light");
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.2f;
        lightGo.transform.rotation = Quaternion.Euler(55f, -35f, 0f);

        EditorSceneManager.SaveScene(scene, scenePath);
        AssetDatabase.SaveAssets();
        Debug.Log($"SCENE_BUILT: {scenePath} ({balls.transform.childCount} ball objects, physics=on)");
    }

    /// <summary>
    /// Adds Rigidbody + SphereCollider with pool-ball physics to every direct child
    /// of the given root that has a MeshRenderer. Uses kinematic = false so they
    /// react to gravity and collisions.
    /// </summary>
    private static void AddPhysicsToBalls(GameObject ballsRoot, PhysicsMaterial physMat)
    {
        int count = 0;
        foreach (Transform child in ballsRoot.transform)
        {
            if (child.GetComponent<MeshRenderer>() == null)
                continue;

            // Sphere collider exactly matching the pool ball diameter.
            var sc = child.GetComponent<SphereCollider>();
            if (sc == null) sc = child.gameObject.AddComponent<SphereCollider>();
            sc.radius = BallRadius;
            sc.center = Vector3.zero;
            sc.material = physMat;

            // Rigidbody at rest (no initial velocity).
            var rb = child.GetComponent<Rigidbody>();
            if (rb == null) rb = child.gameObject.AddComponent<Rigidbody>();
            rb.mass = BallMass;
            rb.useGravity = true;
            rb.isKinematic = false;
            rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
            rb.angularDamping = 0.6f;
            rb.linearDamping = 0.15f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            count++;
        }
        Debug.Log($"SCENE: added Rigidbody+SphereCollider to {count} balls");
    }
}