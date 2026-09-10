using System;

namespace VR147.AAA.Physics.Golden
{
    public readonly struct PhysicsGoldenResult
    {
        public readonly string caseId;
        public readonly int revision;
        public readonly bool passed;
        public readonly bool valid;
        public readonly float distanceError;
        public readonly float peakSpeedError;
        public readonly ShotCalibrationResult calibration;

        public PhysicsGoldenResult(PhysicsGoldenCase goldenCase, ShotCalibrationResult calibration)
        {
            caseId = goldenCase != null ? goldenCase.CaseId : string.Empty;
            revision = goldenCase != null ? goldenCase.Revision : 0;
            this.calibration = calibration;
            valid = goldenCase != null && goldenCase.IsValid() &&
                    !float.IsNaN(calibration.error) && !float.IsInfinity(calibration.error);
            distanceError = calibration.error;
            peakSpeedError = calibration.peakSpeedError;
            passed = valid && (!goldenCase.RequirePass || calibration.passed);
        }

        public override string ToString()
        {
            return string.Format("{0} r{1}: {2} (distanceError={3:0.####}, peakSpeedError={4:0.####})",
                caseId, revision, passed ? "PASS" : "FAIL", distanceError, peakSpeedError);
        }
    }
}
