#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace _147VR.Wiring
{
    /// <summary>
    /// Experiment for the golden FAILs: force every bay-4 ball SphereCollider radius to the
    /// baseline value 0.028575 (FBX-derived colliders currently read 0.02860 — +0.025 mm,
    /// enough to pre-overlap the "touching" pair and shift grazing contacts).
    /// Menu: 147VR/Physics/Normalize Bay4 Ball Radii — then re-run the golden.
    /// </summary>
    public static class Bay4RadiusFix
    {
        [MenuItem("147VR/Physics/Normalize Bay4 Ball Radii", false, 12)]
        public static void Run()
        {
            int n = 0;
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                string nm = t.name;
                if (!(nm.StartsWith("SM_147VR_Bay4_Ball_") || nm.StartsWith("BP_Ball_"))) continue;
                var sc = t.GetComponent<SphereCollider>();
                if (sc == null) continue;
                float ls = Mathf.Max(t.lossyScale.x, 1e-4f);
                sc.radius = 0.028575f / ls;
                n++;
            }
            var root = GameObject.Find("Bay4_PhysicsRoot");
            if (root != null)
            {
                var r = root.GetComponent<PoolPhysics.Unity.PoolPhysicsEventRunner>();
                var so = new SerializedObject(r);
                so.FindProperty("defaultBallRadius").floatValue = 0.028575f;
                so.ApplyModifiedProperties();
            }
            Debug.Log($"[RadiusFix] normalized {n} ball colliders to r=0.028575 — STOP/PLAY and re-run the golden");
            EditorUtility.DisplayDialog("Bay4 Ball Radii", $"Normalized {n} colliders to 0.028575 m.\nNow: stop Play, Play again, run In-Editor Golden.", "OK");
        }
    }
}
#endif
