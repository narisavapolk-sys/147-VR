#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace _147VR.Wiring
{
    /// <summary>
    /// Pass B (headless): disable legacy scene art, decide XR rig state, run
    /// BatchSceneOps.RunAll (old-HDRI removal + Auto-Wire + Validate + save),
    /// then dump a post-run root report to Logs/passB_report.txt.
    /// </summary>
    public static class SceneSwapPassB
    {
        const string ScenePath = "Assets/Scenes/147VR_MainScene.unity";
        static readonly string[] DisableNames = { "Prefab_WPBSA_12Foot_Snooker", "Bed_Collider" };

        public static void Run()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid()) { Debug.LogError("[PassB] cannot open scene"); return; }
            var rep = new StringBuilder();

            // 1) disable legacy art
            foreach (var n in DisableNames)
            {
                var go = GameObject.Find(n);
                if (go != null) { go.SetActive(false); rep.AppendLine("disabled: " + n); }
                else rep.AppendLine("not found (already off?): " + n);
            }

            // 1.5 FBX balls carry no components — give every ball a kinematic Rigidbody +
            //     SphereCollider sized from world bounds (runner.Build reads these).
            int prepared = 0;
            var room = GameObject.Find("NarisVintage_Room");
            if (room != null)
            {
                foreach (var t in room.GetComponentsInChildren<Transform>(true))
                {
                    string n = t.name;
                    bool isBall = n.StartsWith("BP_Ball_") || n.Contains("_Ball_") || n.Contains("CueBall") ||
                                  n.StartsWith("SM_Red") || n.StartsWith("SM_Black") || n.StartsWith("SM_White") ||
                                  (n.Contains("Cue") && !n.Contains("Stick") && !n.Contains("Physics"));
                    if (!isBall) continue;
                    if (n.Contains("Table") || n.Contains("carpet") || n.Contains("Chalk")) continue;
                    // NOTE: Unity fake-null breaks C# '??' — must use == null (overloaded operator)
                    var b = t.GetComponent<SphereCollider>();
                    if (b == null) b = t.gameObject.AddComponent<SphereCollider>();
                    var br = t.GetComponent<Rigidbody>();
                    if (br == null) br = t.gameObject.AddComponent<Rigidbody>();
                    // world radius from renderer bounds (FBX scales vary)
                    float r = 0.028575f;
                    var rend = t.GetComponent<Renderer>();
                    if (rend != null) r = Mathf.Max(rend.bounds.extents.x, 0.005f);
                    float ls = Mathf.Max(t.lossyScale.x, 1e-4f);
                    b.radius = r / ls; b.center = Vector3.zero;
                    br.isKinematic = true; br.useGravity = false;
                    br.mass = 0.16f;
                    prepared++;
                }
            }
            rep.AppendLine($"ball components prepared: {prepared}");

            // 2) XR rig decision: if OFF, keep OFF (camera clash with new room's rig).
            var rig = GameObject.Find("OVRCameraRig");
            if (rig != null && !rig.activeSelf)
                rep.AppendLine("OVRCameraRig stays OFF (was off before; enabling would clash cameras)");
            else if (rig != null)
                rep.AppendLine("OVRCameraRig is ON — left as-is, check cameras manually");

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            rep.AppendLine("passB pre-steps saved");

            // 3) full wiring: remove old HDRI -> Auto-Wire -> Validate -> save (BatchSceneOps)
            BatchSceneOps.RunAll();

            // 4) post report
            try
            {
                rep.AppendLine("POST ROOTS:");
                foreach (var go in scene.GetRootGameObjects())
                    rep.AppendLine($"{(go.activeSelf ? "ON " : "OFF")} {go.name} | children={go.transform.childCount}");
                var runners = Object.FindObjectsByType<PoolPhysics.Unity.PoolPhysicsEventRunner>(FindObjectsSortMode.None);
                rep.AppendLine($"runners in scene: {runners.Length}");
                foreach (var r in runners)
                {
                    var so = new SerializedObject(r);
                    rep.AppendLine($"  runner on '{r.name}': balls={so.FindProperty("ballTransforms").arraySize} " +
                                   $"table={(so.FindProperty("tableTransform").objectReferenceValue as Transform)?.name} " +
                                   $"spec={(so.FindProperty("tableSpec").objectReferenceValue != null)} " +
                                   $"profile={(so.FindProperty("profileSO").objectReferenceValue != null)}");
                }
            }
            catch (System.Exception e) { rep.AppendLine("post-report err: " + e.Message); }

            File.WriteAllText("Logs/passB_report.txt", rep.ToString());
            Debug.Log("[PassB] DONE\n" + rep);
        }
    }
}
#endif
