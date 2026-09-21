using UnityEngine;

namespace VR147.AAA.Physics.Golden
{
    public enum PhysicsGoldenCategory
    {
        Straight, Stun, Follow, Draw, English, Cushion, Pocket, Integration
    }

    [CreateAssetMenu(menuName = "147VR/Physics/Golden Case")]
    public sealed class PhysicsGoldenCase : ScriptableObject
    {
        [SerializeField] private string caseId = "147VR-PHY-001";
        [SerializeField] private int revision = 1;
        [SerializeField] private PhysicsGoldenCategory category;
        [SerializeField, TextArea] private string intent;
        [SerializeField] private ShotCalibrationCase calibrationCase;
        [SerializeField] private bool requirePass = true;

        [Header("Measured Truth Source")]
        [SerializeField] private string sourceJsonPath;
        [SerializeField] private string sourceTimestampUtc;
        [SerializeField] private string sourceScene;
        [SerializeField] private string sourceUnityVersion;
        [SerializeField] private string sourceShotType;
        [SerializeField] private int sourceRepetitionCount;
        [SerializeField] private float measuredDistanceMean;
        [SerializeField] private float measuredDistanceStdDev;
        [SerializeField] private float measuredPeakSpeedMean;
        [SerializeField] private float measuredPeakSpeedStdDev;
        [SerializeField] private float regressionTolerancePercent = 0.5f;
        [SerializeField] private float[] measuredDistanceSamples = new float[0];
        [SerializeField] private float[] measuredPeakSpeedSamples = new float[0];

        public string CaseId => caseId;
        public int Revision => revision;
        public PhysicsGoldenCategory Category => category;
        public string Intent => intent;
        public ShotCalibrationCase CalibrationCase => calibrationCase;
        public bool RequirePass => requirePass;
        public string SourceJsonPath => sourceJsonPath;
        public string SourceTimestampUtc => sourceTimestampUtc;
        public string SourceScene => sourceScene;
        public string SourceUnityVersion => sourceUnityVersion;
        public string SourceShotType => sourceShotType;
        public int SourceRepetitionCount => sourceRepetitionCount;
        public float MeasuredDistanceMean => measuredDistanceMean;
        public float MeasuredDistanceStdDev => measuredDistanceStdDev;
        public float MeasuredPeakSpeedMean => measuredPeakSpeedMean;
        public float MeasuredPeakSpeedStdDev => measuredPeakSpeedStdDev;
        public float RegressionTolerancePercent => regressionTolerancePercent;
        public float[] MeasuredDistanceSamples => measuredDistanceSamples;
        public float[] MeasuredPeakSpeedSamples => measuredPeakSpeedSamples;

        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(caseId) && revision > 0 && calibrationCase != null &&
                   ShotCalibrationEvaluator.IsValidCase(calibrationCase) && sourceRepetitionCount > 0 &&
                   measuredDistanceSamples != null && measuredDistanceSamples.Length == sourceRepetitionCount &&
                   measuredPeakSpeedSamples != null && measuredPeakSpeedSamples.Length == sourceRepetitionCount &&
                   measuredDistanceMean > 0f && measuredPeakSpeedMean >= 0f && regressionTolerancePercent > 0f;
        }
    }
}