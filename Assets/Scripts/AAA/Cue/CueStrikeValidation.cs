using UnityEngine;

namespace VR147.AAA.Cue
{
    public static class CueStrikeValidation
    {
        public static bool TrySolve(
            CueImpactProfile profile,
            CueStrikeTestCase test,
            float radius,
            out CueStrikeResult result)
        {
            if (profile == null || test == null || radius <= 0f)
            {
                result = default;
                return false;
            }

            result = CueStrikeSolver.Solve(
                profile,
                Vector3.forward,
                test.English,
                test.power,
                radius);

            return CueStrikeDiagnostics.IsPhysicallyPlausible(
                result, 10f, 250f);
        }
    }
}
