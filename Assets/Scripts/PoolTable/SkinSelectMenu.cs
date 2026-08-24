using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Pre-game skin selection menu. Builds a simple uGUI overlay at runtime
/// (no scene canvas needed) with three skin buttons and a Start button.
/// Picking a skin swaps the table live behind the menu (via
/// TableSkinManager); Start hides the menu and begins the game.
/// </summary>
public class SkinSelectMenu : MonoBehaviour
{
    [Tooltip("Manager that owns/swaps the table. Found on this object or in the scene if empty.")]
    public TableSkinManager skinManager;

    [Tooltip("Felt colors shown as swatches on the buttons (Navy, Walnut, Blue).")]
    public Color[] skinColors = new Color[3]
    {
        new Color(0.70f, 0.11f, 0.15f), // Navy & Gold -> burgundy felt
        new Color(0.10f, 0.45f, 0.22f), // Walnut -> forest green felt
        new Color(0.10f, 0.22f, 0.45f), // Blue -> navy felt
    };

    [Tooltip("Skin display names.")]
    public string[] skinNames = { "Navy & Gold", "Walnut", "Dark Wood" };

    private Canvas canvas;
    private GameObject panel;
    private readonly List<Button> skinButtons = new List<Button>();
    private GameObject selectedHighlight;
    private Text statusText;

    private void Start()
    {
        if (skinManager == null)
            skinManager = GetComponent<TableSkinManager>() ?? FindAnyObjectByType<TableSkinManager>();
        if (skinManager == null)
        {
            Debug.LogError("[SkinSelectMenu] No TableSkinManager found in scene.", this);
            return;
        }
        BuildMenu();
    }

    private void BuildMenu()
    {
        // Canvas
        var canvasGO = new GameObject("SkinSelectCanvas");
        canvasGO.transform.SetParent(transform, false);
        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGO.AddComponent<GraphicRaycaster>();
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        // Panel
        panel = new GameObject("MenuPanel");
        panel.transform.SetParent(canvasGO.transform, false);
        var img = panel.AddComponent<Image>();
        img.color = new Color(0.05f, 0.06f, 0.08f, 0.92f);
        var rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(460f, 560f);
        rt.anchoredPosition = Vector2.zero;

        var layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(28, 28, 28, 28);
        layout.spacing = 16f;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        // Title
        AddText("Select Table Skin", 30, FontStyle.Bold).color = Color.white;

        // Skin buttons
        for (int i = 0; i < 3 && i < skinManager.tablePrefabs.Length; i++)
        {
            Button b = MakeSkinButton(i);
            skinButtons.Add(b);
        }

        statusText = AddText("", 16, FontStyle.Normal);
        statusText.color = new Color(0.6f, 0.9f, 0.6f, 1f);

        // Start button
        var start = MakeButton("START GAME", new Color(0.20f, 0.55f, 0.25f));
        start.onClick.AddListener(() =>
        {
            panel.SetActive(false);
            Debug.Log("[SkinSelectMenu] Game started with skin " + skinManager.ActiveSkin);
        });

        RefreshStatus();
    }

    private Button MakeSkinButton(int index)
    {
        var go = new GameObject("Skin" + index);
        go.transform.SetParent(panel.transform, false);
        var b = go.AddComponent<Button>();
        var img = go.AddComponent<Image>();
        img.color = new Color(0.16f, 0.18f, 0.22f, 1f);

        var hLayout = go.AddComponent<HorizontalLayoutGroup>();
        hLayout.padding = new RectOffset(16, 16, 12, 12);
        hLayout.spacing = 14f;
        hLayout.childForceExpandWidth = false;
        hLayout.childForceExpandHeight = true;
        hLayout.childAlignment = TextAnchor.MiddleLeft;

        // Color swatch
        var swatch = new GameObject("Swatch");
        swatch.transform.SetParent(go.transform, false);
        var sw = swatch.AddComponent<Image>();
        sw.color = skinColors[index];
        var swRt = swatch.GetComponent<RectTransform>();
        swRt.sizeDelta = new Vector2(44f, 44f);

        // Label
        var label = AddText(skinNames[index], 22, FontStyle.Bold);
        label.transform.SetParent(go.transform, false);
        label.color = Color.white;
        var lrt = label.GetComponent<RectTransform>();
        lrt.sizeDelta = new Vector2(280f, 44f);

        int captured = index;
        b.onClick.AddListener(() =>
        {
            skinManager.SetSkin(captured);
            RefreshStatus();
        });
        return b;
    }

    private Button MakeButton(string label, Color color)
    {
        var go = new GameObject(label.Replace(" ", ""));
        go.transform.SetParent(panel.transform, false);
        var b = go.AddComponent<Button>();
        var img = go.AddComponent<Image>();
        img.color = color;
        AddText(label, 24, FontStyle.Bold).transform.SetParent(go.transform, false);
        return b;
    }

    private Text AddText(string content, int size, FontStyle style)
    {
        var go = new GameObject("Text");
        go.transform.SetParent(panel.transform, false);
        var txt = go.AddComponent<Text>();
        txt.text = content;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = size;
        txt.fontStyle = style;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        txt.horizontalOverflow = HorizontalWrapMode.Overflow;
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(380f, 40f);
        return txt;
    }

    private void RefreshStatus()
    {
        if (skinManager == null) return;
        statusText.text = "Selected: " + skinNames[(int)skinManager.ActiveSkin];
    }
}
