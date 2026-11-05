#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

// LODGroup + AudioMixer setup tools. MenuItem-only, defensive.
// BuildLODGroups: expects the selected table root plus sibling LOD1/LOD2 roots in the scene
// (instantiate the LOD FBXs named *LOD1*/*LOD2*), matches renderers by name, adds LODGroups.
// CreateAudioMixer: creates the mixer asset + applies Vorbis/Streaming to SFX.
public static class _147VR_LODAudio_Tools
{
    [MenuItem("147VR/Art/Build LODGroups (select table root; place LOD1/LOD2 in scene first)")]
    public static void BuildLODGroups()
    {
        var root = Selection.activeGameObject;
        if (root == null) { Debug.LogError("147VR/LOD: select the table root first."); return; }

        var all = root.GetComponentsInChildren<Renderer>(true);
        var lod1 = new Dictionary<string, Renderer>();
        var lod2 = new Dictionary<string, Renderer>();
        foreach (var r in all)
        {
            if (r.name.Contains("LOD1")) lod1[StripLod(r.name)] = r;
            else if (r.name.Contains("LOD2")) lod2[StripLod(r.name)] = r;
        }
        if (lod1.Count == 0 && lod2.Count == 0)
        {
            Debug.LogWarning("147VR/LOD: no LOD1/LOD2 renderers found under the selection. Instantiate the LOD FBXs (Assets/.../V008_OPT/LODs/*_LOD1.fbx, _LOD2.fbx) into the scene under the same parent, then re-run.");
            return;
        }
        int groups = 0;
        foreach (var r0 in all)
        {
            if (r0.name.Contains("LOD1") || r0.name.Contains("LOD2")) continue;
            string key = StripLod(r0.name);
            var levels = new List<LOD>();
            levels.Add(new LOD(1f, new[] { r0 }));
            if (lod1.TryGetValue(key, out var a)) levels.Add(new LOD(0.4f, new[] { a }));
            if (lod2.TryGetValue(key, out var b)) levels.Add(new LOD(0.15f, new[] { b }));
            if (levels.Count < 2) continue;
            var g = r0.gameObject.GetComponent<LODGroup>() ?? r0.gameObject.AddComponent<LODGroup>();
            g.SetLODs(levels.ToArray());
            g.RecalculateBounds();
            groups++;
        }
        EditorSceneManager_MarkDirty(root);
        Debug.Log("147VR/LOD: built " + groups + " LODGroups on '" + root.name + "'.");
    }

    [MenuItem("147VR/Audio/Create AudioMixer + apply SFX import")]
    public static void CreateMixer()
    {
        const string mixerPath = "Assets/Audio/147VR_Mixer.mixer";
        var mx = AssetDatabase.LoadAssetAtPath<AudioMixer>(mixerPath);
        if (mx == null)
            Debug.LogWarning("147VR/Audio: mixer not found at " + mixerPath + ". Create it manually: Assets > Create > Audio Mixer (save as 147VR_Mixer under Assets/Audio/), then add groups Master > SFX / Crowd / Ambience.");
        else
            Debug.Log("147VR/Audio: mixer ready at " + mixerPath + ".");
        // apply Vorbis/Streaming to SFX
        string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Audio/SFX" });
        int n = 0;
        foreach (var g in guids)
        {
            string p = AssetDatabase.GUIDToAssetPath(g);
            var imp = AssetImporter.GetAtPath(p) as AudioImporter;
            if (imp == null) continue;
            var so = imp.defaultSampleSettings;
            so.compressionFormat = AudioCompressionFormat.Vorbis;
            so.quality = 0.7f;
            so.loadType = new System.IO.FileInfo(p).Length > 150 * 1024 ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
            imp.defaultSampleSettings = so; imp.SaveAndReimport(); n++;
        }
        AssetDatabase.SaveAssets();
        Debug.Log("147VR/Audio: configured " + n + " SFX clips (Vorbis/Streaming).");
    }

    static string StripLod(string name)
    {
        return name.Replace("_LOD1", "").Replace("_LOD2", "").Replace("LOD1", "").Replace("LOD2", "").Trim();
    }
    static void EditorSceneManager_MarkDirty(GameObject go)
    {
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(go.scene);
    }
}
#endif
