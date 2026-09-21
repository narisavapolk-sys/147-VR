using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using XRInputDevice = UnityEngine.XR.InputDevice;
using System.Collections.Generic;

/// <summary>
/// VR Tablet Options Menu ÔÇö opens when the left Meta Quest controller's
/// menu/secondary button is pressed.  The panel floats in front of the
/// player and contains:
///   ÔÇó Movement Speed slider (1ÔÇô10)
///   ÔÇó Player Height slider (160ÔÇô185 cm)
///   ÔÇó Volume slider (0ÔÇô100) with Mute toggle
///   ÔÇó Scene selector (MR MODE / NightSky / Dreamy_OLED / ConcertRoom / promDance)
///
/// All values are persisted via PlayerPrefs.
/// </summary>
public sealed class TabletOptionsMenu : MonoBehaviour
{
    // ÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇ
    //  Persistence keys
    // ÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇ
    private const string PrefsMoveSpeed   = "Tablet.MoveSpeed";
    private const string PrefsHeight      = "Tablet.Height";
    private const string PrefsVolume      = "Tablet.Volume";
    private const string PrefsMuted       = "Tablet.Muted";
    private const string PrefsScene       = "Tablet.Scene";

    // ÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇ
    //  Inspector fields
    // ÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇ
    [Header("Layout")]
    [Tooltip("Offset from the left controller when the menu opens (local space).")]
    public Vector3 panelOffset = new Vector3(0f, 0.15f, 0.25f);
    [Tooltip("Panel size in world units.")]
    public Vector2 panelSize = new Vector2(0.32f, 0.44f);

    [Header("Colors")]
    public Color bgColor       = new Color(0.06f, 0.07f, 0.10f, 0.94f);
    public Color accentColor   = new Color(0.25f, 0.55f, 0.90f, 1f);
    public Color textPrimary   = Color.white;
    public Color textSecondary = new Color(0.7f, 0.72f, 0.78f);
    public Color sliderFill    = new Color(0.25f, 0.55f, 0.90f, 1f);
    public Color sliderBg      = new Color(0.15f, 0.16f, 0.20f, 1f);
    public Color buttonActive  = new Color(0.20f, 0.60f, 0.30f, 1f);
    public Color buttonInactive = new Color(0.16f, 0.18f, 0.22f, 1f);
    public Color muteOnColor   = new Color(0.85f, 0.25f, 0.25f, 1f);
    public Color muteOffColor  = new Color(0.16f, 0.18f, 0.22f, 1f);

    [Header("Font")]
    public int titleFontSize   = 18;
    public int labelFontSize   = 12;
    public int valueFontSize   = 11;
    public int buttonFontSize  = 13;

    [Header("Dependencies (auto-found if null)")]
    [Tooltip("Transform of the XR camera rig root (OVRCameraRig).")]
    public Transform xrCameraRig;
    [Tooltip("Optional AudioSource whose volume is controlled by the slider.")]
    public AudioSource musicSource;

    // ÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇ
    //  Scene names (must match your Unity Build Settings)
    // ÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇ
    private readonly string[] _sceneNames =
    {
        "MR MODE",
        "NightSky",
        "Dreamy_OLED",
        "ConcertRoom",
        "promDance"
    };

    // ÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇ
    //  Runtime state
    // ÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇ
    private Canvas _canvas;
    private GameObject _panel;
    private bool _isOpen;

    // Slider value displays
    private Text _moveSpeedVal;
    private Text _heightVal;
    private Text _volumeVal;

    // Mute button
    private Button _muteBtn;
    private Text   _muteLabel;
    private bool   _muted;

    // Scene buttons
    private readonly Button[]   _sceneBtns   = new Button[5];
    private readonly Text[]     _sceneLabels = new Text[5];
    private int _selectedScene;

    // Current values
    private float _moveSpeed = 3f;
    private float _height    = 170f;
    private float _volume    = 0.8f;

    // ÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇ
    //  Lifecycle
    // ÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇ
    private void Start()
    {
        LoadPrefs();
        EnsureCanvas();
        BuildPanel();
        _panel.SetActive(false);

        if (xrCameraRig == null)
        {
            GameObject rig = GameObject.Find("OVRCameraRig");
            if (rig != null) xrCameraRig = rig.transform;
        }
    }

    private void Update()
    {
        // Toggle menu with left controller menu button (Meta primaryButton)
        if (Input.GetKeyDown(KeyCode.M) || MenuButtonPressed())
            ToggleMenu();

        if (_isOpen)
            FollowController();
    }

    // ÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇ
    //  Menu button detection
    // ÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇ
    private bool MenuButtonPressed()
    {
        // Meta Quest left controller primary button (menu button)
        var leftHand = new List<XRInputDevice>();
        InputDevices.GetDevicesAtXRNode(XRNode.LeftHand, leftHand);
        foreach (XRInputDevice device in leftHand)
        {
            if (device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton, out bool pressed) && pressed)
                return true;
        }
        return false;
    }

    public void ToggleMenu()
    {
        _isOpen = !_isOpen;
        _panel.SetActive(_isOpen);
        if (_isOpen)
            FollowController();
    }

    // ÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇ
    //  Follow the left controller
    // ÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇ
    private void FollowController()
    {
        Transform cam = xrCameraRig != null ? xrCameraRig : Camera.main?.transform;
        if (cam == null) return;

        Vector3 forward = cam.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude > 0.001f)
            forward.Normalize();

        _panel.transform.position = cam.position + forward * panelOffset.z + Vector3.up * panelOffset.y;
        _panel.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
    }

    // ÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇ
    //  Build UI
    // ÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇ
    private void EnsureCanvas()
    {
        GameObject canvasGO = new GameObject("TabletOptionsMenu_Canvas");
        canvasGO.transform.SetParent(transform, false);
        _canvas = canvasGO.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.WorldSpace;
        _canvas.sortingOrder = 100;

        RectTransform crt = canvasGO.GetComponent<RectTransform>();
        crt.sizeDelta = new Vector2(1000, 1400);

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 300;
        canvasGO.AddComponent<GraphicRaycaster>();

        // Ensure EventSystem exists
        if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
    }

    private void BuildPanel()
    {
        // Panel root
        _panel = new GameObject("OptionsPanel");
        _panel.transform.SetParent(_canvas.transform, false);
        RectTransform panelRT = _panel.AddComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.5f, 0.5f);
        panelRT.anchorMax = new Vector2(0.5f, 0.5f);
        panelRT.sizeDelta = new Vector2(1000, 1400);
        panelRT.anchoredPosition = Vector2.zero;
        Image panelBg = _panel.AddComponent<Image>();
        panelBg.color = bgColor;

        // Make the panel face camera
        _panel.transform.localScale = Vector3.one * 0.001f; // World-space scale

        float y = 640f; // Start from top

        // ÔöÇÔöÇ Title ÔöÇÔöÇ
        y = AddLabel(_panel.transform, "ÔÜÖ  OPTIONS", titleFontSize, TextAnchor.MiddleCenter, ref y, 80f);

        // ÔöÇÔöÇ Divider ÔöÇÔöÇ
        y = AddDivider(_panel.transform, ref y);

        // ÔöÇÔöÇ MOVEMENT SPEED ÔöÇÔöÇ
        y = AddLabel(_panel.transform, "MOVEMENT SPEED", labelFontSize, TextAnchor.MiddleLeft, ref y, 40f);
        float moveVal = PlayerPrefs.GetFloat(PrefsMoveSpeed, 3f);
        y = AddSlider(_panel.transform, "MoveSpeed", 1f, 10f, moveVal, OnMoveSpeedChanged, ref y);

        // ÔöÇÔöÇ PLAYER HEIGHT ÔöÇÔöÇ
        y = AddLabel(_panel.transform, "PLAYER HEIGHT (CM)", labelFontSize, TextAnchor.MiddleLeft, ref y, 40f);
        float heightVal = PlayerPrefs.GetFloat(PrefsHeight, 170f);
        y = AddSlider(_panel.transform, "Height", 160f, 185f, heightVal, OnHeightChanged, ref y);

        // ÔöÇÔöÇ Divider ÔöÇÔöÇ
        y = AddDivider(_panel.transform, ref y);

        // ÔöÇÔöÇ VOLUME ÔöÇÔöÇ
        y = AddLabel(_panel.transform, "VOLUME", labelFontSize, TextAnchor.MiddleLeft, ref y, 40f);
        float volVal = PlayerPrefs.GetFloat(PrefsVolume, 0.8f);
        y = AddSlider(_panel.transform, "Volume", 0f, 1f, volVal, OnVolumeChanged, ref y);

        // ÔöÇÔöÇ MUTE BUTTON ÔöÇÔöÇ
        y = AddMuteButton(_panel.transform, ref y);

        // ÔöÇÔöÇ Divider ÔöÇÔöÇ
        y = AddDivider(_panel.transform, ref y);

        // ÔöÇÔöÇ SCENE SELECTOR ÔöÇÔöÇ
        y = AddLabel(_panel.transform, "SCENE", labelFontSize, TextAnchor.MiddleLeft, ref y, 40f);
        _selectedScene = PlayerPrefs.GetInt(PrefsScene, 0);
        for (int i = 0; i < _sceneNames.Length; i++)
        {
            int idx = i; // capture
            y = AddSceneButton(_panel.transform, _sceneNames[i], idx, ref y);
        }
    }

    // ÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇ
    //  UI element builders
    // ÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇ
    private float AddLabel(Transform parent, string text, int size, TextAnchor align, ref float y, float height)
    {
        GameObject go = new GameObject("Label_" + text.Replace(" ", ""));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.05f, 1f);
        rt.anchorMax = new Vector2(0.95f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, y);
        rt.sizeDelta = new Vector2(0f, height);

        Text t = go.AddComponent<Text>();
        t.text = text;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = size;
        t.fontStyle = FontStyle.Bold;
        t.color = textPrimary;
        t.alignment = align;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;

        y -= height + 8f;
        return y;
    }

    private float AddDivider(Transform parent, ref float y)
    {
        GameObject go = new GameObject("Divider");
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.1f, 1f);
        rt.anchorMax = new Vector2(0.9f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, y);
        rt.sizeDelta = new Vector2(0f, 2f);

        Image img = go.AddComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.12f);

        y -= 14f;
        return y;
    }

    private float AddSlider(Transform parent, string name, float min, float max, float val,
        UnityEngine.Events.UnityAction<float> onChange, ref float y)
    {
        float sliderHeight = 36f;
        float totalHeight = sliderHeight + 22f; // slider + value label

        // Slider background
        GameObject sliderGO = new GameObject("Slider_" + name);
        sliderGO.transform.SetParent(parent, false);
        RectTransform sliderRT = sliderGO.AddComponent<RectTransform>();
        sliderRT.anchorMin = new Vector2(0.08f, 1f);
        sliderRT.anchorMax = new Vector2(0.92f, 1f);
        sliderRT.pivot = new Vector2(0.5f, 1f);
        sliderRT.anchoredPosition = new Vector2(0f, y);
        sliderRT.sizeDelta = new Vector2(0f, sliderHeight);

        Slider slider = sliderGO.AddComponent<Slider>();
        slider.minValue = min;
        slider.maxValue = max;
        slider.value = val;
        slider.onValueChanged.AddListener(onChange);

        // Background image
        GameObject bgGO = new GameObject("Background");
        bgGO.transform.SetParent(sliderGO.transform, false);
        RectTransform bgRT = bgGO.AddComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.sizeDelta = Vector2.zero;
        Image bgImg = bgGO.AddComponent<Image>();
        bgImg.color = sliderBg;

        // Fill area
        GameObject fillAreaGO = new GameObject("Fill Area");
        fillAreaGO.transform.SetParent(sliderGO.transform, false);
        RectTransform fillAreaRT = fillAreaGO.AddComponent<RectTransform>();
        fillAreaRT.anchorMin = new Vector2(0f, 0.25f);
        fillAreaRT.anchorMax = new Vector2(1f, 0.75f);
        fillAreaRT.sizeDelta = Vector2.zero;

        GameObject fillGO = new GameObject("Fill");
        fillGO.transform.SetParent(fillAreaGO.transform, false);
        RectTransform fillRT = fillGO.AddComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = new Vector2(0f, 1f);
        fillRT.sizeDelta = Vector2.zero;
        Image fillImg = fillGO.AddComponent<Image>();
        fillImg.color = sliderFill;

        // Handle slide area
        GameObject handleAreaGO = new GameObject("Handle Slide Area");
        handleAreaGO.transform.SetParent(sliderGO.transform, false);
        RectTransform handleAreaRT = handleAreaGO.AddComponent<RectTransform>();
        handleAreaRT.anchorMin = Vector2.zero;
        handleAreaRT.anchorMax = Vector2.one;
        handleAreaRT.sizeDelta = Vector2.zero;

        GameObject handleGO = new GameObject("Handle");
        handleGO.transform.SetParent(handleAreaGO.transform, false);
        RectTransform handleRT = handleGO.AddComponent<RectTransform>();
        handleRT.sizeDelta = new Vector2(20f, 20f);
        Image handleImg = handleGO.AddComponent<Image>();
        handleImg.color = Color.white;

        slider.fillRect = fillRT;
        slider.handleRect = handleRT;

        // Value label
        y -= sliderHeight + 4f;
        GameObject valGO = new GameObject("Value_" + name);
        valGO.transform.SetParent(parent, false);
        RectTransform valRT = valGO.AddComponent<RectTransform>();
        valRT.anchorMin = new Vector2(0.08f, 1f);
        valRT.anchorMax = new Vector2(0.92f, 1f);
        valRT.pivot = new Vector2(0.5f, 1f);
        valRT.anchoredPosition = new Vector2(0f, y);
        valRT.sizeDelta = new Vector2(0f, 20f);

        Text valText = valGO.AddComponent<Text>();
        valText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        valText.fontSize = valueFontSize;
        valText.color = textSecondary;
        valText.alignment = TextAnchor.MiddleRight;

        // Store reference for live update
        if (name == "MoveSpeed") { _moveSpeedVal = valText; UpdateMoveSpeedLabel(val); }
        else if (name == "Height") { _heightVal = valText; UpdateHeightLabel(val); }
        else if (name == "Volume") { _volumeVal = valText; UpdateVolumeLabel(val); }

        y -= 22f;
        return y;
    }

    private float AddMuteButton(Transform parent, ref float y)
    {
        float btnHeight = 40f;

        GameObject go = new GameObject("MuteButton");
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.08f, 1f);
        rt.anchorMax = new Vector2(0.92f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, y);
        rt.sizeDelta = new Vector2(0f, btnHeight);

        _muteBtn = go.AddComponent<Button>();
        Image img = go.AddComponent<Image>();
        _muted = PlayerPrefs.GetInt(PrefsMuted, 0) == 1;
        img.color = _muted ? muteOnColor : muteOffColor;

        _muteBtn.onClick.AddListener(ToggleMute);

        // Label
        GameObject labelGO = new GameObject("MuteLabel");
        labelGO.transform.SetParent(go.transform, false);
        RectTransform labelRT = labelGO.AddComponent<RectTransform>();
        labelRT.anchorMin = Vector2.zero;
        labelRT.anchorMax = Vector2.one;
        labelRT.sizeDelta = Vector2.zero;
        _muteLabel = labelGO.AddComponent<Text>();
        _muteLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _muteLabel.fontSize = buttonFontSize;
        _muteLabel.fontStyle = FontStyle.Bold;
        _muteLabel.color = textPrimary;
        _muteLabel.alignment = TextAnchor.MiddleCenter;
        _muteLabel.text = _muted ? "­ƒöç  UNMUTE" : "­ƒöè  MUTE";

        y -= btnHeight + 12f;
        return y;
    }

    private float AddSceneButton(Transform parent, string sceneName, int index, ref float y)
    {
        float btnHeight = 42f;

        GameObject go = new GameObject("Scene_" + sceneName);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.08f, 1f);
        rt.anchorMax = new Vector2(0.92f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, y);
        rt.sizeDelta = new Vector2(0f, btnHeight);

        Button btn = go.AddComponent<Button>();
        Image img = go.AddComponent<Image>();
        _sceneBtns[index] = btn;
        btn.onClick.AddListener(() => OnSceneSelected(index));

        // Label
        GameObject labelGO = new GameObject("Label");
        labelGO.transform.SetParent(go.transform, false);
        RectTransform labelRT = labelGO.AddComponent<RectTransform>();
        labelRT.anchorMin = Vector2.zero;
        labelRT.anchorMax = Vector2.one;
        labelRT.sizeDelta = Vector2.zero;
        Text label = labelGO.AddComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = buttonFontSize;
        label.fontStyle = FontStyle.Bold;
        label.color = textPrimary;
        label.alignment = TextAnchor.MiddleCenter;
        label.text = sceneName;
        _sceneLabels[index] = label;

        // Update visual
        UpdateSceneButtonVisual(index);

        y -= btnHeight + 6f;
        return y;
    }

    // ÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇ
    //  Callbacks
    // ÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇ
    private void OnMoveSpeedChanged(float val)
    {
        _moveSpeed = val;
        PlayerPrefs.SetFloat(PrefsMoveSpeed, val);
        UpdateMoveSpeedLabel(val);
    }

    private void OnHeightChanged(float val)
    {
        _height = val;
        PlayerPrefs.SetFloat(PrefsHeight, val);
        UpdateHeightLabel(val);
        ApplyHeight(val);
    }

    private void OnVolumeChanged(float val)
    {
        _volume = val;
        PlayerPrefs.SetFloat(PrefsVolume, val);
        UpdateVolumeLabel(val);
        ApplyVolume(val);
    }

    private void ToggleMute()
    {
        _muted = !_muted;
        PlayerPrefs.SetInt(PrefsMuted, _muted ? 1 : 0);

        _muteBtn.GetComponent<Image>().color = _muted ? muteOnColor : muteOffColor;
        _muteLabel.text = _muted ? "­ƒöç  UNMUTE" : "­ƒöè  MUTE";

        ApplyVolume(_volume);
    }

    private void OnSceneSelected(int index)
    {
        _selectedScene = index;
        PlayerPrefs.SetInt(PrefsScene, index);

        for (int i = 0; i < _sceneBtns.Length; i++)
            UpdateSceneButtonVisual(i);

        Debug.Log($"[TabletMenu] Scene selected: {_sceneNames[index]} ÔÇö load this scene to apply.");
    }

    // ÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇ
    //  Apply settings
    // ÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇ
    private void ApplyHeight(float cm)
    {
        float meters = cm / 100f;
        Camera cam = Camera.main;
        if (cam != null)
        {
            Vector3 pos = cam.transform.localPosition;
            pos.y = meters;
            cam.transform.localPosition = pos;
        }
    }

    private void ApplyVolume(float vol)
    {
        float effectiveVol = _muted ? 0f : vol;
        if (musicSource != null)
            musicSource.volume = effectiveVol;

        AudioListener.volume = effectiveVol;
    }

    // ÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇ
    //  Label helpers
    // ÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇ
    private void UpdateMoveSpeedLabel(float val)
    {
        if (_moveSpeedVal != null)
            _moveSpeedVal.text = $"{val:F1}x";
    }

    private void UpdateHeightLabel(float val)
    {
        if (_heightVal != null)
            _heightVal.text = $"{val:F0} cm";
    }

    private void UpdateVolumeLabel(float val)
    {
        if (_volumeVal != null)
            _volumeVal.text = $"{(val * 100f):F0}%";
    }

    private void UpdateSceneButtonVisual(int index)
    {
        if (_sceneBtns[index] == null) return;
        bool selected = index == _selectedScene;
        _sceneBtns[index].GetComponent<Image>().color = selected ? accentColor : buttonInactive;
        if (_sceneLabels[index] != null)
            _sceneLabels[index].fontStyle = selected ? FontStyle.BoldAndItalic : FontStyle.Bold;
    }

    // ÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇ
    //  Prefs
    // ÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇ
    private void LoadPrefs()
    {
        _moveSpeed = PlayerPrefs.GetFloat(PrefsMoveSpeed, 3f);
        _height    = PlayerPrefs.GetFloat(PrefsHeight, 170f);
        _volume    = PlayerPrefs.GetFloat(PrefsVolume, 0.8f);
        _muted     = PlayerPrefs.GetInt(PrefsMuted, 0) == 1;
        _selectedScene = PlayerPrefs.GetInt(PrefsScene, 0);
    }

    // ÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇ
    //  Public API
    // ÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇ
    public float MoveSpeed => _moveSpeed;
    public float PlayerHeight => _height;
    public float Volume => _muted ? 0f : _volume;
    public bool IsMuted => _muted;
    public int SelectedSceneIndex => _selectedScene;
    public string SelectedSceneName => _selectedScene >= 0 && _selectedScene < _sceneNames.Length
        ? _sceneNames[_selectedScene] : "";
}
