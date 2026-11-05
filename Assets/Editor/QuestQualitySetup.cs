using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace _147VR.Quality
{
    /// <summary>
    /// Creates the 3-tier Quest render setup (STAGING tool — run menu once, review assets):
    ///   Assets/AAA/Settings/Quality/Resources/URP_Quest_Low.asset      no MSAA, no shadows, renderScale 0.85
    ///   Assets/AAA/Settings/Quality/Resources/URP_Quest_Medium.asset   MSAA 2x, main-light shadow only
    ///   Assets/AAA/Settings/Quality/Resources/URP_Quest_High.asset     MSAA 4x, single 1024 shadow, HDR off (VR+MSAA)
    /// The runtime QuestRenderBootstrap picks a tier (PlayerPrefs "vr_quality" 0/1/2) and applies it
    /// plus foveation — no per-scene wiring needed.
    /// NOTE: written against URP 17 (Unity 6000.x) public properties; if a setter name moved in the
    /// project's exact URP version, fix at the marked lines — values, not structure, are what matter.
    /// </summary>
    public static class QuestQualitySetup
    {
        const string Dir  = "Assets/AAA/Settings/Quality/Resources";

        [MenuItem("147VR/Quality/Create 3-Tier Quest URP Assets", false, 30)]
        public static void Create()
        {
            EnsureDir();
            Build(Dir + "/URP_Quest_Low.asset",    msaa: 1, shadows: false, shadowRes: 512,  scale: 0.85f);
            Build(Dir + "/URP_Quest_Medium.asset", msaa: 2, shadows: true,  shadowRes: 1024, scale: 1.0f);
            Build(Dir + "/URP_Quest_High.asset",   msaa: 4, shadows: true,  shadowRes: 1024, scale: 1.0f);
            AssetDatabase.SaveAssets();
            Debug.Log("[Quality] 3 URP tiers created under " + Dir + " — runtime selector reads PlayerPrefs 'vr_quality' (0/1/2).");
            EditorUtility.DisplayDialog("Quest Quality", "Created:\nURP_Quest_Low / Medium / High\n\n" +
                "QuestRenderBootstrap applies them at launch (default Medium).\n" +
                "Change tier from in-game menu or:\nPlayerPrefs.SetInt(\"vr_quality\", 2)", "OK");
        }

        static void Build(string path, int msaa, bool shadows, int shadowRes, float scale)
        {
            var a = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
            bool isNew = a == null;
            if (isNew) a = UniversalRenderPipelineAsset.Create();

            // ---- marked lines: URP-version-sensitive setters ----
            a.msaaSampleCount            = msaa;
            a.renderScale                = scale;      // URP-level scale (XR uses eyeTextureResolutionScale on top)
            a.supportsHDR                = false;      // HDR breaks MSAA on mobile VR — deliberate
            a.mainLightShadowmapResolution = shadowRes;
            a.shadowDistance             = shadows ? 8f : 0f;
            a.supportsDynamicBatching    = true;
            // shadows/mode are READ-ONLY properties in URP 17 — write serialized fields instead
            // (field names verified against UniversalRenderPipelineAsset @17.4.0 source)
            var sr = new SerializedObject(a);
            SetBool(sr, "m_MainLightShadowsSupported", shadows);
            SetBool(sr, "m_AdditionalLightShadowsSupported", false);
            SetInt(sr,  "m_AdditionalLightsRenderingMode", 1);        // 1 = PerVertex: cheap on XR2
            sr.ApplyModifiedProperties();
            // NOTE: depth-priming moved to UniversalRenderPipelineGlobalSettings in URP 17
            // (verified absent from UniversalRenderPipelineAsset @17.4.0 source) — leave default.
            // ------------------------------------------------------

            if (isNew) AssetDatabase.CreateAsset(a, path);
            else EditorUtility.SetDirty(a);
        }

        static void SetBool(SerializedObject so, string prop, bool v)
        { var p = so.FindProperty(prop); if (p != null) p.boolValue = v; else Debug.LogWarning("[Quality] missing field " + prop); }
        static void SetInt(SerializedObject so, string prop, int v)
        { var p = so.FindProperty(prop); if (p != null) p.intValue = v; else Debug.LogWarning("[Quality] missing field " + prop); }

        static void EnsureDir()
        {
            if (AssetDatabase.IsValidFolder("Assets/AAA/Settings"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/AAA/Settings/Quality"))
                    AssetDatabase.CreateFolder("Assets/AAA/Settings", "Quality");
                if (!AssetDatabase.IsValidFolder(Dir))
                    AssetDatabase.CreateFolder("Assets/AAA/Settings/Quality", "Resources");
            }
            else Debug.LogError("[Quality] Create Assets/AAA/Settings first (or edit Dir).");
        }
    }
}
