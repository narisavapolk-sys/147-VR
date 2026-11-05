using UnityEngine;

// Player-selectable cue skin (VISUAL only — does not affect physics/cue-strike).
// Assign the cue Renderer + the list of cue material presets (Assets/AAA/ProceduralCue/Mats/*).
// UI option buttons call SetSkin(i) / Next() / Previous().
public class CueSkinSelector : MonoBehaviour
{
    [SerializeField] Renderer targetRenderer;
    [SerializeField] Material[] skins = new Material[0];
    [SerializeField] int defaultIndex = 0;

    public int Count => skins != null ? skins.Length : 0;
    public int CurrentIndex { get; private set; } = -1;

    public Material GetSkin(int index)
    {
        if (skins == null || index < 0 || index >= skins.Length) return null;
        return skins[index];
    }

    void Awake()
    {
        if (targetRenderer == null) targetRenderer = GetComponentInChildren<Renderer>();
        SetSkin(defaultIndex);
    }

    public void SetSkin(int index)
    {
        if (Count == 0 || targetRenderer == null) return;
        index = Mathf.Clamp(index, 0, skins.Length - 1);
        if (skins[index] == null) return;
        targetRenderer.material = skins[index]; // instance => no shared-asset mutation
        CurrentIndex = index;
    }

    public void Next()
    {
        if (Count > 0) SetSkin((CurrentIndex + 1) % Count);
    }

    public void Previous()
    {
        if (Count > 0) SetSkin((CurrentIndex - 1 + Count) % Count);
    }
}
