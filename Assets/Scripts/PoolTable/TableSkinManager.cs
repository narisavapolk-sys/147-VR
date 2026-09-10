using UnityEngine;

/// <summary>
/// Owns the pool table in the scene and swaps it between the three skins
/// (Navy / Walnut / Blue). The selection persists via PlayerPrefs so the
/// choice made in the pre-game menu survives scene loads / app restarts.
///
/// Any UI (SkinSelectMenu or your own) can call <see cref="SetSkin(int)"/>.
/// Balls and cues are NOT touched — all table prefabs share identical
/// geometry/origin, so racked balls stay in place after a swap.
/// </summary>
public class TableSkinManager : MonoBehaviour
{
    public enum Skin { Navy = 0, Walnut = 1, Blue = 2 }

    [Tooltip("Table prefabs, in order: Navy, Walnut, Blue.")]
    public GameObject[] tablePrefabs = new GameObject[3];

    public const string PrefsKey = "PoolTable.Skin";

    /// <summary>Last chosen skin index (0-2). -1 = nothing chosen yet this session.</summary>
    public static int SelectedSkin { get; private set; } = -1;

    /// <summary>Currently instantiated table.</summary>
    public GameObject CurrentTable { get; private set; }

    public Skin ActiveSkin => (Skin)Mathf.Clamp(SelectedSkin, 0, tablePrefabs.Length - 1);

    private void Awake()
    {
        if (SelectedSkin < 0)
            SelectedSkin = Mathf.Clamp(PlayerPrefs.GetInt(PrefsKey, 0), 0, 2);
        // Remove any static layout table baked into the scene at edit time
        // (all skins share identical geometry, so racked balls stay valid).
        RemoveStaticTables();
        ApplySkin(SelectedSkin, false);
    }

    private void RemoveStaticTables()
    {
        var all = Object.FindObjectsOfType<Transform>(true);
        foreach (var t in all)
        {
            if (t.parent == null && (t.name.StartsWith("PREFAB POoL table") || t.name.StartsWith("PoolTable(")))
            {
                if (Application.isPlaying) Destroy(t.gameObject);
                else DestroyImmediate(t.gameObject);
            }
        }
    }

    /// <summary>Switches to the given skin (0 = Navy, 1 = Walnut, 2 = Blue).</summary>
    public void SetSkin(int index)
    {
        if (tablePrefabs == null || tablePrefabs.Length == 0)
        {
            Debug.LogWarning("[TableSkin] No table prefabs assigned.", this);
            return;
        }
        SelectedSkin = Mathf.Clamp(index, 0, tablePrefabs.Length - 1);
        PlayerPrefs.SetInt(PrefsKey, SelectedSkin);
        PlayerPrefs.Save();
        ApplySkin(SelectedSkin, true);
    }

    public void SetSkin(Skin skin) => SetSkin((int)skin);

    private void ApplySkin(int index, bool isRuntime)
    {
        if (tablePrefabs == null || index < 0 || index >= tablePrefabs.Length)
            return;

        if (CurrentTable != null)
        {
            if (Application.isPlaying) Destroy(CurrentTable);
            else DestroyImmediate(CurrentTable);
        }

        GameObject prefab = tablePrefabs[index];
        if (prefab == null)
        {
            Debug.LogWarning($"[TableSkin] Prefab for skin {index} is null.", this);
            return;
        }

        GameObject go = Instantiate(prefab, transform.position, transform.rotation);
        go.name = "PoolTable(" + ((Skin)index).ToString() + ")";
        if (isRuntime)
            go.transform.SetParent(transform, true);
        CurrentTable = go;
        Debug.Log($"[TableSkin] Applied skin {((Skin)index)}", this);
    }
}
