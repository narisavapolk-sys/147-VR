using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class Phase4A_MarkingAlignmentAudit_V2_1
{
    static readonly (string name, float longZ, float widthX)[] Spots = {
        ("Yellow", -1.0475f, -0.292f),
        ("Green",  -1.0475f,  0.292f),
        ("Brown",  -1.0475f,  0.000f),
        ("Blue",    0.0000f,  0.000f),
        ("Pink",    0.89225f, 0.000f),
        ("Black",   1.4605f, 0.000f),
    };
    const float Gate = 0.001f;
    const string ScenePath = "Assets/Scenes/147VR_MainScene.unity";
    const string LogPath = "C:/Users/mongo/AppData/Local/Temp/147VR_PHASE4A_V2_1_AUDIT.log";

    [MenuItem("147VR/Phase4/Run 4A Audit V2.1 (Physics Authority)")]
    public static void Run()
    {
        var lines = new List<string>();
        Action<string> L = x => { lines.Add(x); Debug.Log(x); };
        try
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var table = GameObject.Find("Prefab_WPBSA_12Foot_Snooker");
            if (table == null) throw new Exception("Prefab_WPBSA_12Foot_Snooker not found");
            var bedT = FindChild(table.transform, "Bed_Collider");
            if (bedT == null) throw new Exception("Bed_Collider not found");
            var bedCol = bedT.GetComponent<Collider>();
            if (bedCol == null) throw new Exception("Bed_Collider has no Collider");

            Bounds pb = bedCol.bounds;
            Vector3 bc = pb.center;
            L($"[4A-V2.1] ROOT worldScale={table.transform.lossyScale}");
            L($"[4A-V2.1] BED worldPos={bedT.position} worldScale={bedT.lossyScale}");
            L($"[4A-V2.1] PhysicsBed center={bc} size={pb.size}");

            bool bedOk = Approx(pb.size.x, 1.778f) && Approx(pb.size.y, 0.05f) && Approx(pb.size.z, 3.569f);
            bool scaleOk = Vector3.Distance(table.transform.lossyScale, Vector3.one) < 0.0001f;
            L($"[4A-V2.1] BED SIZE {(bedOk ? "PASS" : "FAIL")}: {pb.size.x:F3}×{pb.size.y:F3}×{pb.size.z:F3}");
            L($"[4A-V2.1] ROOT SCALE {(scaleOk ? "PASS" : "FAIL")}");

            // Mirror runtime authority creation: SnookerPhysicsSetup promotes the
            // named ball transforms to dynamic Rigidbody bodies without changing
            // their authored spawn coordinates. This makes the audit test the same
            // objects that receive runtime physics, not decorative fallback meshes.
            var setup = table.GetComponentInChildren<SnookerPhysicsSetup>(true);
            if (setup == null) throw new Exception("SnookerPhysicsSetup not found under table");
            setup.EnsurePhysics();
            Physics.SyncTransforms();

            bool allSpots = true;
            foreach (var spot in Spots)
            {
                Transform ball = FindPhysicsBall(table.transform, spot.name);
                if (ball == null)
                {
                    L($"[4A-V2.1] {spot.name}: PHYSICS BALL NOT FOUND");
                    allSpots = false;
                    continue;
                }

                // Blender XY -> Unity XZ after the table's Y90 integration:
                // width axis maps to world X; long axis maps to world Z with sign flip.
                Vector3 expected = new Vector3(bc.x + spot.widthX, ball.position.y, bc.z - spot.longZ);
                float delta = Vector2.Distance(
                    new Vector2(ball.position.x, ball.position.z),
                    new Vector2(expected.x, expected.z));
                bool pass = delta < Gate;
                if (!pass) allSpots = false;
                L($"[4A-V2.1] {spot.name}: actual=({ball.position.x:F6},{ball.position.z:F6}) expected=({expected.x:F6},{expected.z:F6}) delta={delta * 1000f:F3}mm {(pass ? "PASS" : "FAIL")}");
            }

            bool certify = bedOk && scaleOk && allSpots;
            L(certify ? ">>> [PHASE 4A MARKING ALIGNMENT PASS] <<<" : ">>> [PHASE 4A MARKING ALIGNMENT FAIL] <<<");
            File.WriteAllLines(LogPath, lines);
            EditorSceneManager.SaveScene(scene);
            EditorApplication.Exit(certify ? 0 : 2);
        }
        catch (Exception e)
        {
            File.AppendAllText(LogPath, "[4A-V2.1 ERROR] " + e + "\n");
            EditorApplication.Exit(3);
        }
    }

    static Transform FindPhysicsBall(Transform root, string name)
    {
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            if (!t.name.Equals(name, StringComparison.Ordinal)) continue;
            var rb = t.GetComponent<Rigidbody>();
            if (rb != null && !rb.isKinematic) return t;
        }
        return null;
    }

    static Transform FindChild(Transform root, string name)
    {
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t;
        return null;
    }

    static bool Approx(float a, float b, float tol = 0.01f) => Mathf.Abs(a - b) < tol;
}
