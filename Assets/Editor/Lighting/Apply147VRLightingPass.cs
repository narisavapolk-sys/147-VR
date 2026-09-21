using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class Apply147VRLightingPass
{
    private const string ProfilePath = "Assets/Settings/147VR_Quest_LightingProfile.asset";
    private const string SkyboxPath = "Assets/Settings/147VR_PromDance_Skybox.mat";
    private const string HdriPath = "Assets/147 main/promDance _Hdri/PromDance_HDRI.exr";

    [MenuItem("147 VR/Lighting/Apply Quest Lighting Pass")]
    public static void Apply()
    {
        EnsureAssets();
        ApplyToScene("Assets/Scenes/PoolTable_8Ball.unity");
        ApplyToScene("Assets/Scenes/PoolTable_9Ball.unity");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[147VR Lighting] Quest lighting pass completed for 8 Ball and 9 Ball.");
    }
    private static void EnsureAssets()
    {
        var hdri = AssetDatabase.LoadAssetAtPath<Texture2D>(HdriPath);
        if (hdri == null)
            throw new System.InvalidOperationException("PromDance HDRI not found: " + HdriPath);

        var skybox = AssetDatabase.LoadAssetAtPath<Material>(SkyboxPath);
        if (skybox == null)
        {
            skybox = new Material(Shader.Find("Skybox/Panoramic"))
            {
                name = "147VR_PromDance_Skybox"
            };
            AssetDatabase.CreateAsset(skybox, SkyboxPath);
        }
        skybox.SetTexture("_MainTex", hdri);
        skybox.SetFloat("_Exposure", 0.8f);
        skybox.SetFloat("_Rotation", 0f);
        skybox.SetColor("_Tint", Color.white);
        EditorUtility.SetDirty(skybox);

        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "147VR_Quest_LightingProfile";
            AssetDatabase.CreateAsset(profile, ProfilePath);
        }
        ConfigureProfile(profile);
        EditorUtility.SetDirty(profile);
    }
    private static void ConfigureProfile(VolumeProfile profile)
    {
        var tonemap = GetOrAdd<Tonemapping>(profile);
        tonemap.active = true;
        tonemap.mode.overrideState = true;
        tonemap.mode.value = TonemappingMode.Neutral;

        var color = GetOrAdd<ColorAdjustments>(profile);
        color.active = true;
        color.postExposure.overrideState = true;
        color.postExposure.value = 0.15f;
        color.contrast.overrideState = true;
        color.contrast.value = 8f;
        color.saturation.overrideState = true;
        color.saturation.value = 5f;

        var bloom = GetOrAdd<Bloom>(profile);
        bloom.active = true;
        bloom.threshold.overrideState = true;
        bloom.threshold.value = 1.2f;
        bloom.intensity.overrideState = true;
        bloom.intensity.value = 0.08f;
        bloom.scatter.overrideState = true;
        bloom.scatter.value = 0.2f;
        bloom.highQualityFiltering.overrideState = true;
        bloom.highQualityFiltering.value = false;
        bloom.downscale.overrideState = true;
        bloom.downscale.value = BloomDownscaleMode.Quarter;
        bloom.dirtIntensity.overrideState = true;
        bloom.dirtIntensity.value = 0f;

        var vignette = GetOrAdd<Vignette>(profile);
        vignette.active = false;

        var motionBlur = GetOrAdd<MotionBlur>(profile);
        motionBlur.active = false;
    }

    private static T GetOrAdd<T>(VolumeProfile profile) where T : VolumeComponent
    {
        if (!profile.TryGet<T>(out var component))
            component = profile.Add<T>(true);
        return component;
    }
    private static void ApplyToScene(string scenePath)
    {
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        var hdri = AssetDatabase.LoadAssetAtPath<Texture2D>(HdriPath);
        var skybox = AssetDatabase.LoadAssetAtPath<Material>(SkyboxPath);
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);

        RenderSettings.skybox = skybox;
        RenderSettings.ambientMode = AmbientMode.Skybox;
        RenderSettings.ambientIntensity = 0.9f;
        RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
        RenderSettings.defaultReflectionResolution = 128;
        RenderSettings.reflectionIntensity = 0.85f;
        RenderSettings.reflectionBounces = 1;
        RenderSettings.sun = null;
        DynamicGI.UpdateEnvironment();

        var key = FindOrCreateKeyLight();
        key.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        key.intensity = 1.2f;
        key.color = Color.white;
        key.shadows = LightShadows.Hard;
        key.shadowStrength = 1f;
        RenderSettings.sun = key;

        var volumeGo = FindOrCreate("147VR Global Volume");
        var volume = volumeGo.GetComponent<Volume>() ?? volumeGo.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 10f;
        volume.weight = 1f;
        volume.sharedProfile = profile;

        ConfigureReflectionProbe(scene);
        ConfigureCharacterFill(key);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static Light FindOrCreateKeyLight()
    {
        foreach (var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (light.type == LightType.Directional && light.gameObject.name == "Directional Light")
                return light;

        var go = new GameObject("Directional Light");
        var lightComponent = go.AddComponent<Light>();
        lightComponent.type = LightType.Directional;
        return lightComponent;
    }
    private static void ConfigureReflectionProbe(Scene scene)
    {
        var probeGo = FindOrCreate("147VR Table Reflection Probe");
        var probe = probeGo.GetComponent<ReflectionProbe>() ?? probeGo.AddComponent<ReflectionProbe>();
        var bounds = CalculateTableBounds();
        probe.transform.position = bounds.center + Vector3.up * 0.25f;
        probe.size = bounds.size + new Vector3(1.0f, 1.5f, 1.0f);
        probe.center = Vector3.zero;
        probe.mode = ReflectionProbeMode.Realtime;
        probe.refreshMode = ReflectionProbeRefreshMode.OnAwake;
        probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.AllFacesAtOnce;
        probe.resolution = 64;
        probe.hdr = true;
        probe.intensity = 0.85f;
        probe.boxProjection = true;
        probe.nearClipPlane = 0.1f;
        probe.farClipPlane = 100f;
        probe.cullingMask = -1;
        probe.clearFlags = ReflectionProbeClearFlags.Skybox;
        probe.importance = 10;
    }

    private static Bounds CalculateTableBounds()
    {
        var renderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
        var bounds = new Bounds(Vector3.zero, Vector3.one);
        var found = false;
        foreach (var renderer in renderers)
        {
            if (renderer.gameObject.name.IndexOf("PREFAB POoL table", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                bounds = found ? Encapsulate(bounds, renderer.bounds) : renderer.bounds;
                found = true;
            }
        }
        if (!found)
        {
            foreach (var renderer in renderers)
            {
                if (renderer.bounds.size.x > 1.5f && renderer.bounds.size.z > 1.5f)
                {
                    bounds = found ? Encapsulate(bounds, renderer.bounds) : renderer.bounds;
                    found = true;
                }
            }
        }
        return found ? bounds : new Bounds(Vector3.zero, new Vector3(3f, 2f, 5f));
    }

    private static Bounds Encapsulate(Bounds a, Bounds b)
    {
        a.Encapsulate(b.min);
        a.Encapsulate(b.max);
        return a;
    }
    private static void ConfigureCharacterFill(Light key)
    {
        var fillGo = FindOrCreate("147VR Character Fill");
        var fill = fillGo.GetComponent<Light>() ?? fillGo.AddComponent<Light>();
        fill.type = LightType.Directional;
        fill.transform.rotation = Quaternion.LookRotation(-key.transform.forward, Vector3.up);
        fill.color = new Color(0.72f, 0.82f, 1f);
        fill.intensity = 0.22f;
        fill.shadows = LightShadows.None;
        fill.renderMode = LightRenderMode.Auto;
        fill.range = 10f;
        EnsureAdditionalLightData(fillGo);
    }

    private static void EnsureAdditionalLightData(GameObject go)
    {
        if (go.GetComponent<UniversalAdditionalLightData>() == null)
            go.AddComponent<UniversalAdditionalLightData>();
    }

    private static GameObject FindOrCreate(string objectName)
    {
        var existing = GameObject.Find(objectName);
        if (existing != null)
            return existing;
        return new GameObject(objectName);
    }
}
