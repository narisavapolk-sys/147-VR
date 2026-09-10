using UnityEditor;
using UnityEngine;
using VR147.AAA.Physics;

namespace VR147.AAA.Editor
{
    public static class Validate147VRPhysics
    {
        [MenuItem("147VR/AAA/Validate Physics Profiles")]
        public static void Validate()
        {
            int errors = 0;
            int warnings = 0;
            errors += ValidateTableProfiles(ref warnings);
            errors += ValidatePocketProfiles(ref warnings);
            Debug.Log($"[147VR AAA] Physics validation complete. Errors={errors}, Warnings={warnings}");
        }

        private static int ValidateTableProfiles(ref int warnings)
        {
            int errors = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:TableSurfaceProfile"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var profile = AssetDatabase.LoadAssetAtPath<TableSurfaceProfile>(path);
                if (profile == null) continue;
                if (profile.rollingFriction < 0f || profile.slidingFriction < 0f ||
                    profile.spinFriction < 0f) errors++;
                if (profile.rollingFriction >= profile.slidingFriction) warnings++;
            }
            return errors;
        }

        private static int ValidatePocketProfiles(ref int warnings)
        {
            int errors = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:PocketProfile"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var profile = AssetDatabase.LoadAssetAtPath<PocketProfile>(path);
                if (profile == null) continue;
                if (profile.captureRadius <= 0f || profile.captureDepth <= 0f ||
                    profile.captureSpeed < 0f) errors++;
                if (profile.rollInAssist > 0.5f) warnings++;
            }
            return errors;
        }
    }
}
