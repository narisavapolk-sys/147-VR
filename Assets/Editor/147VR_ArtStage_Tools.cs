#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// Small reusable editor utility; old V006 staging/swap actions were removed with that table variant.
public static class _147VR_ArtStage_Tools
{
    [MenuItem("147VR/Art/Measure Selected World Bounds")]
    public static void MeasureSelected()
    {
        var go = Selection.activeGameObject;
        if (go == null) { Debug.LogWarning("147VR/Art: select an object first."); return; }
        var rends = go.GetComponentsInChildren<Renderer>(true);
        if (rends.Length == 0) { Debug.LogWarning("147VR/Art: no renderers under selection."); return; }
        Bounds b = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
        Debug.Log(string.Format(
            "147VR/Art bounds '{0}': center={1:F3} size={2:F3} (topY={3:F3})",
            go.name, b.center, b.size, b.max.y));
    }
}
#endif
