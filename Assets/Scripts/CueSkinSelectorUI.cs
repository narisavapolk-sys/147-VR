using UnityEngine;
using UnityEngine.UI;

// Runtime UI builder: auto-creates one button per cue skin and wires them to a CueSkinSelector.
// Drop this on a UI object under a Canvas. It builds a vertical list of buttons (labelled by the
// material name). No scene-authoring needed. (Legacy UI only — no TextMeshPro dependency.)
[RequireComponent(typeof(CueSkinSelector))]
public class CueSkinSelectorUI : MonoBehaviour
{
    [Tooltip("If empty, a panel with a VerticalLayoutGroup is created under this object.")]
    public RectTransform contentRoot;
    public Vector2 buttonSize = new Vector2(280, 64);
    public int fontSize = 26;

    CueSkinSelector selector;

    void Start()
    {
        selector = GetComponent<CueSkinSelector>();
        if (selector == null || selector.Count == 0) return;

        Transform parent = EnsureContent();
        Font font = GetFont();

        for (int i = 0; i < selector.Count; i++)
        {
            int index = i; // capture
            Material m = selector.GetSkin(i);
            string label = m != null ? Nicify(m.name) : ("Skin " + i);
            BuildButton(parent, label, font, () => selector.SetSkin(index));
        }
    }

    Transform EnsureContent()
    {
        if (contentRoot != null) return contentRoot;
        var panel = new GameObject("CueSkinButtons", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        panel.transform.SetParent(transform, false);
        var vlg = panel.GetComponent<VerticalLayoutGroup>();
        vlg.spacing = 10; vlg.childControlWidth = false; vlg.childControlHeight = false;
        vlg.childForceExpandWidth = false; vlg.childAlignment = TextAnchor.UpperCenter;
        var csf = panel.GetComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        contentRoot = panel.GetComponent<RectTransform>();
        return contentRoot;
    }

    void BuildButton(Transform parent, string label, Font font, System.Action onClick)
    {
        var go = new GameObject("Btn_" + label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        go.transform.SetParent(parent, false);

        var le = go.GetComponent<LayoutElement>();
        le.minWidth = buttonSize.x; le.minHeight = buttonSize.y;

        var img = go.GetComponent<Image>();
        img.color = new Color(0.16f, 0.16f, 0.18f, 1f);

        var btn = go.GetComponent<Button>();
        var colors = btn.colors;
        colors.highlightedColor = new Color(0.3f, 0.3f, 0.35f);
        colors.pressedColor = new Color(0.1f, 0.1f, 0.12f);
        btn.colors = colors;
        btn.onClick.AddListener(() => onClick());

        var txtGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
        txtGo.transform.SetParent(go.GetComponent<RectTransform>(), false);
        var trt = (RectTransform)txtGo.transform;
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(8, 0); trt.offsetMax = new Vector2(-8, 0);
        var txt = txtGo.GetComponent<Text>();
        txt.text = label; txt.fontSize = fontSize; txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white; txt.font = font; txt.raycastTarget = false;
    }

    Font GetFont()
    {
        Font f = null;
        try { f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { }
        if (f == null) { try { f = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { } }
        return f;
    }

    static string Nicify(string matName)
    {
        string s = matName.Replace("Cue_", "").Replace("_", " ");
        return s;
    }
}
