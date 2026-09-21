using UnityEngine;

namespace VR147.AAA.Physics.Golden
{
    [CreateAssetMenu(menuName="147VR/Physics/M4.2 Pocket Golden")]
    public sealed class M4PocketGolden : ScriptableObject
    {
        public string caseId, caseName, outcome, sourceJsonPath, sourceTimestampUtc, sourceUnityVersion;
        public int repetitions;
        public float entrySpeedMean, entryAngleMean, minMouthDistanceMean, finalXMean, finalZMean;
        public int wallContactsMean;
        public bool capturedExpected, rejectedExpected, jawExpected, rattleExpected;
        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(caseId) && repetitions==5 &&
                   !string.IsNullOrWhiteSpace(sourceJsonPath) && !string.IsNullOrWhiteSpace(outcome);
        }
    }
}