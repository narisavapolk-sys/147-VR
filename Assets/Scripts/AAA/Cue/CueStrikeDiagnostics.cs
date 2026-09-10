using UnityEngine;

namespace VR147.AAA.Cue
{
    public static class CueStrikeDiagnostics
    {
        public static bool IsPhysicallyPlausible(CueStrikeResult result, float maxLinear, float maxAngular)
        {
            if (!result.IsValid)
                return false;
            if (result.linearImpulse.magnitude > maxLinear)
                return false;
            if (result.angularImpulse.magnitude > maxAngular)
                return false;
            return true;
        }

        public static string Describe(CueStrikeResult result)
        {
            return $"linear={result.linearImpulse.magnitude:F3}, " +
                   $"angular={result.angularImpulse.magnitude:F3}, " +
                   $"offset={result.contactOffset}";
        }
    }
}
