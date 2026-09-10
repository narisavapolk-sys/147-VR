using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

/// <summary>
/// Runtime environment selector for the 147 VR scene.
/// The cycle action is mapped to the Quest Touch secondary button (B/Y).
/// </summary>
public class RotatingPromSkybox : MonoBehaviour
{
    public enum EnvironmentMode { Prom = 0, Bokeh = 1, NightSky = 2, MR = 3 }

    [Header("Start Environment")]
    public EnvironmentMode startMode = EnvironmentMode.Prom;

    [Header("HDRI per Mode")]
    public Texture2D promHdri;
    public Texture2D bokehHdri;
    public Texture2D nightSkyHdri;

    [Header("HDRI Rotation")]
    [Range(0f, 10f)] public float rotationSpeed = 1.2f;
    public bool rotateSkybox = true;

    [Header("Quest 2 Controller Mapping")]
    [Tooltip("Press B on right hand or Y on left hand to cycle environments")]
    public bool enableControllerSwitch = true;

    private Material runtimeSkybox;
    private InputAction cycleEnvironmentAction;
    private float currentRotation;
    private EnvironmentMode currentMode;
    private static readonly int MainTexProperty = Shader.PropertyToID("_MainTex");
    private static readonly int RotationProperty = Shader.PropertyToID("_Rotation");
    private static readonly int ExposureProperty = Shader.PropertyToID("_Exposure");

    void Awake()
    {
        // Quest Touch: secondaryButton is B on the right controller and Y on the left.
        cycleEnvironmentAction = new InputAction("Cycle Environment", InputActionType.Button,
            "<XRController>/secondaryButton");
        cycleEnvironmentAction.AddBinding("<XRController>{LeftHand}/secondaryButton");
        cycleEnvironmentAction.AddBinding("<XRController>{RightHand}/secondaryButton");
    }

    void OnEnable()
    {
        if (enableControllerSwitch)
            cycleEnvironmentAction?.Enable();
    }

    void OnDisable()
    {
        cycleEnvironmentAction?.Disable();
    }

    void OnDestroy()
    {
        cycleEnvironmentAction?.Dispose();
        if (runtimeSkybox != null)
            Destroy(runtimeSkybox);
    }

    void Start()
    {
        ApplyMode(startMode);
    }

    void Update()
    {
        if (enableControllerSwitch && cycleEnvironmentAction != null && cycleEnvironmentAction.WasPressedThisFrame())
            ApplyMode((EnvironmentMode)(((int)currentMode + 1) % 4));

        if (runtimeSkybox != null && rotateSkybox && currentMode != EnvironmentMode.MR)
        {
            currentRotation = Mathf.Repeat(currentRotation + rotationSpeed * Time.deltaTime, 360f);
            if (runtimeSkybox.HasProperty(RotationProperty))
                runtimeSkybox.SetFloat(RotationProperty, currentRotation);
        }
    }

    public void SetProm() => ApplyMode(EnvironmentMode.Prom);
    public void SetBokeh() => ApplyMode(EnvironmentMode.Bokeh);
    public void SetNightSky() => ApplyMode(EnvironmentMode.NightSky);
    public void SetMR() => ApplyMode(EnvironmentMode.MR);

    public void ApplyMode(EnvironmentMode mode)
    {
        currentMode = mode;
        currentRotation = 0f;

        if (runtimeSkybox != null)
            Destroy(runtimeSkybox);

        if (mode == EnvironmentMode.MR)
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = Color.black;
            SetCameraForMR();
            Debug.Log("[RotatingPromSkybox] MR selected. Real passthrough requires OpenXR/Meta XR passthrough.");
            return;
        }

        Texture2D hdri = GetHdri(mode);
        if (hdri == null)
        {
            Debug.LogWarning($"[RotatingPromSkybox] No HDRI assigned for {mode}; keeping current environment.");
            return;
        }

        Shader shader = Shader.Find("Skybox/Panoramic");
        if (shader == null)
        {
            Debug.LogError("[RotatingPromSkybox] Skybox/Panoramic shader is unavailable.");
            return;
        }

        runtimeSkybox = new Material(shader) { name = $"Runtime_{mode}_Skybox" };
        runtimeSkybox.SetTexture(MainTexProperty, hdri);
        runtimeSkybox.SetFloat(ExposureProperty, mode == EnvironmentMode.Prom ? 0.8f : 0.7f);
        runtimeSkybox.SetColor("_Tint", Color.white);
        runtimeSkybox.SetFloat(RotationProperty, 0f);
        RenderSettings.skybox = runtimeSkybox;
        RenderSettings.ambientMode = AmbientMode.Skybox;
        DynamicGI.UpdateEnvironment();
        Debug.Log($"[RotatingPromSkybox] Environment changed to {mode}.");
    }

    private Texture2D GetHdri(EnvironmentMode mode)
    {
        switch (mode)
        {
            case EnvironmentMode.Prom: return promHdri;
            case EnvironmentMode.Bokeh: return bokehHdri;
            case EnvironmentMode.NightSky: return nightSkyHdri;
            default: return null;
        }
    }

    private static void SetCameraForMR()
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
    }
}
