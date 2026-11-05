using UnityEngine;

// Switchable HDRI "atmosphere" skyboxes for the 147-VR venue (warm club / reception / country club).
// Purely VISUAL: swaps RenderSettings.skybox at runtime. Assign the Skybox/Panoramic materials in the
// Inspector (create them with the menu "147VR/Art/Atmosphere/Build Skybox Materials", or use
// "Add Switcher GameObject (auto-wire)") and call SetAtmosphere(index) from your UI buttons.
//
// NOTE (URP): ambient/reflections may be overridden by a Volume in the scene. If the environment
// tint does not follow the skybox, either disable the Volume's Ambient override or drive ambient
// from the same atmosphere profile.
public class AtmosphereSkyboxSwitcher : MonoBehaviour
{
    [System.Serializable]
    public struct Atmosphere
    {
        public string label;        // name shown in the UI
        public Material skyboxMat;  // a Skybox/Panoramic material built from an .hdr
    }

    [Tooltip("Atmosphere profiles in display order.")]
    public Atmosphere[] atmospheres = new Atmosphere[0];

    [Tooltip("Index applied on Awake.")]
    public int defaultIndex = 0;

    [Tooltip("Also refresh real-time ambient/GI from the newly selected skybox.")]
    public bool updateAmbient = true;

    public int Count => atmospheres != null ? atmospheres.Length : 0;
    public int CurrentIndex { get; private set; } = -1;

    void Awake()
    {
        SetAtmosphere(defaultIndex);
    }

    public void SetAtmosphere(int index)
    {
        if (Count == 0)
        {
            Debug.LogWarning("AtmosphereSkyboxSwitcher: no atmospheres assigned.");
            return;
        }

        index = Mathf.Clamp(index, 0, atmospheres.Length - 1);
        var a = atmospheres[index];

        if (a.skyboxMat == null)
        {
            Debug.LogWarning($"AtmosphereSkyboxSwitcher: atmosphere[{index}] '{a.label}' has no material.");
            return;
        }

        RenderSettings.skybox = a.skyboxMat;
        CurrentIndex = index;

        if (updateAmbient)
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
            DynamicGI.UpdateEnvironment();
        }
    }

    public void Next()
    {
        if (Count > 0) SetAtmosphere((CurrentIndex + 1) % Count);
    }

    public void Previous()
    {
        if (Count > 0) SetAtmosphere((CurrentIndex - 1 + Count) % Count);
    }
}
