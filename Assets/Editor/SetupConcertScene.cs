using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Assembles the main game scene per "step to do.txt":
/// 1. Instantiate ConcertRoom_WithTable.fbx at origin
/// 2. Set Skybox to Dreamy_OLED_HDRI.exr (Skybox/Panoramic)
/// 3. Create Empty GO "ConcertSmoke" at room center, near floor
/// 4. Attach DriftingConcertSmoke.cs + ParticleSystem + SmokeParticle_Texture.png
/// </summary>
public static class SetupConcertScene
{
    const string ScenePath = "Assets/Scenes/SampleScene.unity";
    const string RoomFbxPath = "Assets/147 main/ConcertRoom_WithTable.fbx";
    const string HdriPath = "Assets/147 main/Dreamy_OLED_HDRI.exr";
    const string SmokeTexPath = "Assets/147 main/SmokeParticle_Texture.png";
    const string SmokeScriptPath = "Assets/147 main/DriftingConcertSmoke.cs";

    [MenuItem("Tools/147/Setup Concert Scene")]
    public static void Run()
    {
        // ---- Open scene ----
        if (!System.IO.File.Exists(ScenePath))
        {
            Debug.LogError($"SCENE_MISSING {ScenePath}");
            return;
        }
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        // ---- Remove existing ConcertRoom / ConcertSmoke if present (idempotent) ----
        foreach (GameObject go in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (go.name.StartsWith("ConcertRoom") || go.name == "ConcertSmoke")
                Object.DestroyImmediate(go);
        }

        // ---- 1. Room FBX ----
        Object roomPrefab = AssetDatabase.LoadMainAssetAtPath(RoomFbxPath);
        if (roomPrefab == null) { Debug.LogError($"ROOM_FBX_MISSING {RoomFbxPath}"); return; }
        GameObject room = (GameObject)PrefabUtility.InstantiatePrefab(roomPrefab);
        room.name = "ConcertRoom";
        room.transform.position = Vector3.zero;
        room.transform.rotation = Quaternion.identity;
        Debug.Log($"ROOM_ADDED {room.name}");

        // ---- 2. Skybox from HDRI ----
        Texture2D hdri = AssetDatabase.LoadAssetAtPath<Texture2D>(HdriPath);
        if (hdri == null) { Debug.LogError($"HDRI_MISSING {HdriPath}"); }
        else
        {
            Shader skyShader = Shader.Find("Skybox/Panoramic");
            if (skyShader == null) skyShader = Shader.Find("Skybox/Cubemap");
            if (skyShader != null)
            {
                Material sky = new Material(skyShader);
                sky.name = "Dreamy_OLED_Skybox";
                if (sky.HasProperty("_MainTex"))
                {
                    sky.SetTexture("_MainTex", hdri);
                    sky.SetFloat("_Exposure", 1f);
                    sky.SetFloat("_Rotation", 0f);
                }
                RenderSettings.skybox = sky;
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
                Debug.Log($"SKYBOX_SET {sky.name} (shader={skyShader.name})");
            }
            else Debug.LogError("SKYBOX_SHADER_MISSING");
        }

        // ---- 3+4+5. ConcertSmoke ----
        GameObject smoke = new GameObject("ConcertSmoke");
        smoke.transform.position = new Vector3(0f, 0.25f, 0f); // room center, near floor
        var ps = smoke.AddComponent<ParticleSystem>();
        var psr = smoke.AddComponent<ParticleSystemRenderer>();

        MonoScript ms = AssetDatabase.LoadAssetAtPath<MonoScript>(SmokeScriptPath);
        if (ms == null || ms.GetClass() == null) { Debug.LogError($"SMOKE_SCRIPT_MISSING {SmokeScriptPath}"); }
        else
        {
            var comp = (DriftingConcertSmoke)smoke.AddComponent(ms.GetClass());
            Texture2D smokeTex = AssetDatabase.LoadAssetAtPath<Texture2D>(SmokeTexPath);
            if (smokeTex != null)
            {
                comp.smokeTexture = smokeTex;
                Debug.Log($"SMOKE_TEXTURE_SET {smokeTex.name}");
            }
            else Debug.LogError($"SMOKE_TEX_MISSING {SmokeTexPath}");
            Debug.Log($"CONCERT_SMOKE_ADDED pos={smoke.transform.position}");
        }

        // ---- Save ----
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePath);
        Debug.Log($"SCENE_SAVED {ScenePath}");
    }
}
