// 147 VR — WipRoom Reflection Probe helper
// -----------------------------------------------------------------------------
// WHY THIS EXISTS
//   WipRoom has skybox IBL (a NIGHT HDRI) but ZERO reflection probes. In URP a
//   metallic surface (gold / brass / chrome rails, pockets, fixtures) gets its
//   colour from specular reflections sampled from a reflection probe; with no
//   probe it only sees the dark night skybox cubemap, so the gold renders dull
//   instead of gold. Blender avoids this because its HDRI world + the interior
//   lamps all contribute specular. This tool drops a single box probe over the
//   room so the metal reflects the actual lamps / neon / walls.
//
//   Budget note (snooker-vr-pro perf-budgets.md line 16): the Quest build must
//   ship this as a BAKED probe (realtime probe updates = 0). For fast look-dev
//   against the Blender render we render it ONCE on demand (ViaScripting),
//   which costs nothing per-frame and does NOT touch the lightmap pipeline, so
//   it cannot re-trigger the UV2-unwrap freeze.
//
// MENUS
//   147 VR/Lighting/Bake GI/5 Reflection Probe (Preview, realtime)
//       -> creates/updates the room probe, renders it once, leaves mode=Realtime.
//          Use this to SEE the gold reflect the room right now.
//   147 VR/Lighting/Bake GI/6 Reflection Probe (Lock to Baked for ship)
//       -> flips the same probe to mode=Baked. Run a normal GI bake afterwards
//          and the probe is captured once into a texture (0 runtime updates).
//   147 VR/Lighting/Bake GI/7 Reflection Probe (Remove from scene)
//       -> deletes the probe object.
// -----------------------------------------------------------------------------

using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace VR147.EditorTools
{
    public static class WipRoomReflectionProbe
    {
        public const string ProbeName = "147VR_Room_ReflectionProbe";

        [MenuItem("147 VR/Lighting/Bake GI/5 Reflection Probe (Preview, realtime)")]
        public static void AddPreview()
        {
            var log = new StringBuilder();
            var probe = EnsureProbe(log);
            if (probe == null) { Flush(log); return; }

            probe.mode = ReflectionProbeMode.Realtime;
            probe.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
            probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.AllFacesAtOnce;
            log.AppendLine("mode=Realtime, refresh=ViaScripting (one-shot, no per-frame cost)");

            Flush(log);

            // RenderProbe must run outside the menu's own stack; defer one tick.
            EditorApplication.delayCall += () =>
            {
                var p = GameObject.Find(ProbeName)?.GetComponent<ReflectionProbe>();
                if (p == null) { Debug.LogWarning("[147VR Probe] probe vanished before render."); return; }
                p.RenderProbe();
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                Debug.Log("[147VR Probe] RenderProbe() done - gold now reflects the room. Save the scene when happy.");
            };
        }

        [MenuItem("147 VR/Lighting/Bake GI/6 Reflection Probe (Lock to Baked for ship)")]
        public static void LockToBaked()
        {
            var log = new StringBuilder();
            var probe = EnsureProbe(log);
            if (probe == null) { Flush(log); return; }

            probe.mode = ReflectionProbeMode.Baked;
            // refreshMode is ignored for Baked; reset to a sane default.
            probe.refreshMode = ReflectionProbeRefreshMode.OnAwake;
            log.AppendLine("mode=Baked -> run a normal GI bake; probe captured once (0 runtime updates).");
            Flush(log);
        }

        [MenuItem("147 VR/Lighting/Bake GI/7 Reflection Probe (Remove from scene)")]
        public static void Remove()
        {
            var go = GameObject.Find(ProbeName);
            if (go == null) { Debug.Log("[147VR Probe] no probe to remove."); return; }
            Object.DestroyImmediate(go);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("[147VR Probe] removed " + ProbeName);
        }

        // Creates or updates the probe, sized to the union of all enabled
        // renderers (the room). Returns null if nothing to measure.
        private static ReflectionProbe EnsureProbe(StringBuilder log)
        {
            var go = GameObject.Find(ProbeName);
            if (go == null)
            {
                go = new GameObject(ProbeName);
                log.AppendLine($"[NEW] created {ProbeName}");
            }
            else
            {
                log.AppendLine($"[OK] updating existing {ProbeName}");
            }

            var probe = go.GetComponent<ReflectionProbe>();
            if (probe == null) probe = go.AddComponent<ReflectionProbe>();

            if (!TryRoomBounds(out var bounds))
            {
                log.AppendLine("[ABORT] no enabled renderers found - is the WipRoom scene open?");
                return null;
            }

            // Centre the probe in the room, pad it a little so the box fully
            // contains the metal the player looks at.
            probe.transform.position = bounds.center;
            probe.size = bounds.size + new Vector3(1.0f, 1.0f, 1.0f);
            probe.center = Vector3.zero;
            probe.boxProjection = true;
            probe.resolution = 128;
            probe.hdr = true;
            probe.intensity = 1.0f;
            probe.nearClipPlane = 0.1f;
            probe.farClipPlane = 200f;
            probe.cullingMask = ~0;                 // capture everything in the room
            probe.clearFlags = ReflectionProbeClearFlags.Skybox; // gaps fall back to skybox
            probe.importance = 100;

            log.AppendLine($"[OK] bounds center={bounds.center} size={bounds.size} (padded +1m)");
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            return probe;
        }

        // Union of all enabled renderer bounds. For a single interior scene this
        // is effectively the room volume.
        private static bool TryRoomBounds(out Bounds bounds)
        {
            bounds = default;
            var rs = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            bool found = false;
            foreach (var r in rs)
            {
                if (r == null || !r.enabled) continue;
                // Skip absurdly large volumes (skyboxes / giant VFX bounds) so the
                // probe hugs the actual room.
                var b = r.bounds;
                if (b.size.x > 200f || b.size.y > 200f || b.size.z > 200f) continue;
                if (!found) { bounds = b; found = true; }
                else bounds.Encapsulate(b);
            }
            return found;
        }

        private static void Flush(StringBuilder log)
        {
            Debug.Log("[147VR Probe]\n" + log);
        }
    }
}
