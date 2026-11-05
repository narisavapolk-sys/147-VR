using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR;

namespace _147VR.Quality
{
    /// <summary>
    /// Runtime tier + foveation applier for Quest builds (STAGING tool).
    /// Auto-starts before the first scene: loads URP_Quest_{Low|Medium|High} from Resources,
    /// sets QualitySettings.renderPipeline + anamorphic/XR eye scale, and TRIES to enable fixed
    /// foveated rendering through whatever Meta/Unity XR API the installed packages expose
    /// (all reflection-based — this file compiles with or without the Meta SDK, missing
    /// features log a one-line hint instead of crashing).
    /// Tier switch from game UI: QuestRenderBootstrap.SetTier(0..2).
    /// </summary>
    public static class QuestRenderBootstrap
    {
        public static int CurrentTier { get; private set; } = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void AutoStart() => Apply(PlayerPrefs.GetInt("vr_quality", 1));

        public static void Apply(int tier)
        {
            tier = Mathf.Clamp(tier, 0, 2);
            string assetName = tier == 0 ? "URP_Quest_Low" : tier == 1 ? "URP_Quest_Medium" : "URP_Quest_High";
            var urp = Resources.Load<RenderPipelineAsset>(assetName);
            if (urp == null) { Debug.LogWarning($"[Quality] '{assetName}' not found in Resources — run menu 147VR/Quality/Create 3-Tier first."); return; }

            QualitySettings.renderPipeline = urp;
            GraphicsSettings.defaultRenderPipeline = urp;
            PlayerPrefs.SetInt("vr_quality", tier); PlayerPrefs.Save();
            CurrentTier = tier;

            ApplyXR(tier);
            TryEnableFoveation(tier);
            Debug.Log($"[Quality] tier {tier} ({assetName}) applied.");
        }

        static void ApplyXR(int tier)
        {
            // eye texture scale: the XR-native knob (in addition to URP renderScale)
            float eyeScale = tier == 0 ? 0.9f : 1.0f;
            try { if (XRSettings.eyeTextureResolutionScale > 0) XRSettings.eyeTextureResolutionScale = eyeScale; }
            catch (Exception e) { Debug.Log("[Quality] eyeTextureResolutionScale n/a: " + e.Message); }

            // anamorphic (lens) matching on Quest 3 / 3.5 SDK when present
            TryCall("UnityEngine.XR.AnamorphicUtils", "SetRenderMode", 1 /* MatchLens */);
        }

        static void TryEnableFoveation(int tier)
        {
            // Level 0..5: Medium tier -> 3, High -> 2 (center stays crisp, edges give resolution back).
            int foveation = tier == 2 ? 2 : 3;
            // Known entry points across Meta/Unity package generations — first hit wins:
            bool hit = TryCall("Unity.XR.Oculus.OVRPlugin",        "SetEyeFoveation", foveation / 5f)
                    || TryCall("UnityEngine.XR.Oculus.OVRPlugin",  "SetEyeFoveation", foveation / 5f)
                    || TryCall("Meta.xr.OpenXR…tion", "SetFoveationLevel", foveation)
                    || TryCall("UnityEngine.XR.FoveatedRendering", "enabledModifiers", null);
            if (!hit)
                Debug.Log("[Quality] FFR: no known API found — enable via Meta XR Configurator window " +
                          "(Fixed Foveated Rendering = " + (tier == 2 ? "Medium" : "Strong") + ") and rebuild.");
        }

        static bool TryCall(string typeAndAssembly, string member, object arg)
        {
            try
            {
                var comma = typeAndAssembly.IndexOf(',');
                string typeName = comma < 0 ? typeAndAssembly : typeAndAssembly.Substring(0, comma).Trim();
                string asmName  = comma < 0 ? null : typeAndAssembly.Substring(comma + 1).Trim();
                var t = Type.GetType(typeName + (asmName != null ? ", " + asmName : ""), false);
                if (t == null)
                {
                    foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                    { t = asm.GetType(typeName); if (t != null) break; }
                }
                if (t == null) return false;

                var prop = t.GetProperty(member, BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                if (prop != null && arg != null) { prop.SetValue(null, Convert.ChangeType(arg, prop.PropertyType)); return true; }
                var m = t.GetMethod(member, BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                if (m != null)
                {
                    var ps = m.GetParameters();
                    if (ps.Length == 0) { m.Invoke(null, null); return true; }
                    m.Invoke(null, new[] { Convert.ChangeType(arg, ps[0].ParameterType) });
                    return true;
                }
                return false;
            }
            catch { return false; }
        }

        /// <summary>Call from any settings UI: 0=Low 1=Medium 2=High.</summary>
        public static void SetTier(int tier) => Apply(tier);
    }
}
