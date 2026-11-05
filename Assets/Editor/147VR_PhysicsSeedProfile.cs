#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using VR147.AAA.Physics;

// Non-destructive: reads PhysicMaterial_seed.json and creates a SEPARATE candidate
// CushionProfile asset. It does NOT touch the live/certified table. The owner assigns it
// to a CushionResponder and validates with the EXISTING GOLDEN regression runner.
public static class _147VR_PhysicsSeedProfile
{
    const string SEED = "Assets/AAA/Physics/PhysicMaterial_seed.json";
    const string OUT = "Assets/AAA/Physics/CushionProfile_SEED.asset";

    [System.Serializable] class Cand { public float restitution; public float tangentialFriction; public float spinTransfer; public float minimumImpactSpeed; }
    [System.Serializable] class SeedRoot { public Cand candidate_cushion_profile; }

    [MenuItem("147VR/Physics/Create candidate CushionProfile from seed (non-destructive)")]
    public static void CreateProfile()
    {
        if (!File.Exists(SEED)) { Debug.LogError("147VR/Seed: " + SEED + " not found."); return; }
        SeedRoot root = JsonUtility.FromJson<SeedRoot>(File.ReadAllText(SEED));
        if (root == null || root.candidate_cushion_profile == null)
        {
            Debug.LogError("147VR/Seed: 'candidate_cushion_profile' missing in seed JSON.");
            return;
        }
        Cand c = root.candidate_cushion_profile;

        if (!AssetDatabase.IsValidFolder("Assets/AAA/Physics"))
            AssetDatabase.CreateFolder("Assets/AAA", "Physics");

        CushionProfile p = AssetDatabase.LoadAssetAtPath<CushionProfile>(OUT);
        if (p == null)
        {
            p = ScriptableObject.CreateInstance<CushionProfile>();
            AssetDatabase.CreateAsset(p, OUT);
        }
        p.restitution = c.restitution;
        p.tangentialFriction = c.tangentialFriction;
        p.spinTransfer = c.spinTransfer;
        p.minimumImpactSpeed = c.minimumImpactSpeed;
        EditorUtility.SetDirty(p);
        AssetDatabase.SaveAssets();

        Debug.Log("147VR/Seed: candidate CushionProfile -> " + OUT +
                  " (restitution=" + c.restitution + ", tangFric=" + c.tangentialFriction +
                  ", spinTransfer=" + c.spinTransfer + "). NOT applied to the live table. " +
                  "Assign it to a CushionResponder and run the EXISTING '147VR/AAA/Physics/Run Golden Regression' to validate vs tolerance.");
    }
}
#endif
