using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// In-game one-press skin switcher: presses the cycle key (default Tab) or
/// clicks the small on-screen button to cycle the table through
/// Navy → Walnut → Blue (any number of skins, actually).
/// Shows a short toast with the current skin name.
/// </summary>
public class SkinCycler : MonoBehaviour
{
    [Tooltip("Manager that owns/swaps the table.")]
    public TableSkinManager skinManager;

    [Tooltip("Press this key to cycle to the next skin.")]
    public KeyCode cycleKey = KeyCode.Tab;

    [Tooltip("Create a small on-screen button that cycles when clicked.")]
    public bool createUiButton = true;

    [Tooltip("Show a short toast with the current skin name.")]
    public bool showToast = true;

    [Tooltip("Display names for the skins, in the same order as the prefabs.")]
    public string[] skinNames = { "Navy & Gold", "Walnut", "Dark Wood" };

    private Canvas canvas;
    private Text buttonLabel;
    private Text toast;
    private float toastTimer;

    private void Start()
    {
        if (skinManager == null)
            skinManager = GetComponent<TableSkinManager>() ?? FindAnyObjectByType<TableSkinManager>();
        if (skinManager == null || skinManager.tablePrefabs == null || skinManager.tablePrefabs.Length == 0)
        {
            Debug.LogError("[SkinCycler] No TableSkinManager (or empty prefab list) — disabling.", this);
            enabled = false;
            return;
        }

        if (createUiButton) BuildButton();
        if (showToast) BuildToast();
    }

    private void Update()
    {
        if (Input.GetKeyDown(cycleKey))
            Cycle();

        if (toast != null && toast.gameObject.activeSelf)
        {
            toastTimer -= Time.deltaTime;
            if (toastTimer <= 0f)
                toast.gameObject.SetActive(false);
        }
    }

    /// <summary>Cycles to the next skin (wraps around). Callable from any UI.</summary>
    public void Cycle()
    {
        int count = Mathf.Max(1, skinManager.tablePrefabs.Length);
        int next = ((int)skinManager.ActiveSkin + 1) % count;
        skinManager.SetSkin(next);
        string name = SkinName(next);
        if (buttonLabel != null) buttonLabel.text = "Skin: " + name;
        if (toast != null)
        {
            toast.text = "Skin: " + name;
            toast.gameObject.SetActive(true);
            toastTimer = 1.5f;
        }
    }

    private string SkinName(int index)
    {
        if (skinNames != null && index >= 0 && index < skinNames.Length)
            return skinNames[index];
        return ((TableSkinManager.Skin)index).ToString();
    }

    private void BuildButton()
    {
        canvas = EnsureCanvas();
        var btnGo = new GameObject("SkinCycleButton");
        btnGo.transform.SetParent(canvas.transform, false);
        var btn = btnGo.AddComponent<Button>();
        var img = btnGo.AddComponent<Image>();
        img.color = new Color(0.12f, 0.14f, 0.18f, 0.9f);
        var rt = btnGo.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(1f, 0f);
        rt.anchoredPosition = new Vector2(-24f, 24f);
        rt.sizeDelta = new Vector2(170f, 46f);

        buttonLabel = AddLabel(btnGo.transform, "Skin: " + SkinName((int)skinManager.ActiveSkin), 18);
        btn.onClick.AddListener(Cycle);
    }

    private void BuildToast()
    {
        canvas = EnsureCanvas();
        var toastGo = new GameObject("SkinToast");
        toastGo.transform.SetParent(canvas.transform, false);
        toast = toastGo.AddComponent<Text>();
        toast.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        toast.fontSize = 26;
        toast.fontStyle = FontStyle.Bold;
        toast.alignment = TextAnchor.MiddleCenter;
        toast.color = new Color(1f, 0.85f, 0.3f, 1f);
        toast.horizontalOverflow = HorizontalWrapMode.Overflow;
        var rt = toastGo.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -80f);
        rt.sizeDelta = new Vector2(600f, 60f);
        toastGo.SetActive(false);
    }

    private Canvas EnsureCanvas()
    {
        if (canvas != null) return canvas;
        var canvasGO = new GameObject("SkinCyclerCanvas");
        canvasGO.transform.SetParent(transform, false);
        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGO.AddComponent<GraphicRaycaster>();
        if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
        return canvas;
    }

    private Text AddLabel(Transform parent, string content, int size)
    {
        var go = new GameObject("Label");
        go.transform.SetParent(parent, false);
        var txt = go.AddComponent<Text>();
        txt.text = content;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = size;
        txt.fontStyle = FontStyle.Bold;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return txt;
    }
}
