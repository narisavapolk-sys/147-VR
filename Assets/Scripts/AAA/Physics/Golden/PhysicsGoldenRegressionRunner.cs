using System.Collections.Generic;
using UnityEngine;

namespace VR147.AAA.Physics.Golden
{
    [System.Serializable]
    public struct PhysicsGoldenMeasurement
    {
        public PhysicsGoldenCase goldenCase;
        public float measuredDistance;
        public float measuredPeakSpeed;
    }

    public sealed class PhysicsGoldenRegressionRunner : MonoBehaviour
    {
        [SerializeField] private PhysicsGoldenCatalog catalog;
        [SerializeField] private List<PhysicsGoldenMeasurement> measurements = new();
        private readonly List<PhysicsGoldenResult> results = new();

        public PhysicsGoldenRegressionReport LastReport { get; private set; }
        public bool LastCatalogValid { get; private set; }
        public string LastCatalogError { get; private set; } = string.Empty;
        public IReadOnlyList<PhysicsGoldenResult> Results => results;
        public IReadOnlyList<PhysicsGoldenMeasurement> Measurements => measurements;

        public bool RecordMeasurement(PhysicsGoldenCase goldenCase, float measuredDistance, float measuredPeakSpeed)
        {
            if (!IsRecordable(goldenCase, measuredDistance, measuredPeakSpeed)) return false;
            for (int i = 0; i < measurements.Count; i++)
            {
                if (measurements[i].goldenCase != goldenCase) continue;
                measurements[i] = CreateMeasurement(goldenCase, measuredDistance, measuredPeakSpeed);
                return true;
            }
            measurements.Add(CreateMeasurement(goldenCase, measuredDistance, measuredPeakSpeed));
            return true;
        }

        private bool IsRecordable(PhysicsGoldenCase goldenCase, float distance, float peakSpeed)
        {
            if (goldenCase == null || !goldenCase.IsValid()) return false;
            if (float.IsNaN(distance) || float.IsInfinity(distance)) return false;
            if (float.IsNaN(peakSpeed) || float.IsInfinity(peakSpeed)) return false;
            return catalog == null || ContainsCatalogCase(goldenCase);
        }

        private static PhysicsGoldenMeasurement CreateMeasurement(
            PhysicsGoldenCase goldenCase, float distance, float peakSpeed)
        {
            return new PhysicsGoldenMeasurement
            {
                goldenCase = goldenCase,
                measuredDistance = distance,
                measuredPeakSpeed = peakSpeed
            };
        }

        private bool ContainsCatalogCase(PhysicsGoldenCase goldenCase)
        {
            IReadOnlyList<PhysicsGoldenCase> cases = catalog.Cases;
            for (int i = 0; i < cases.Count; i++)
                if (cases[i] == goldenCase) return true;
            return false;
        }

        public bool HasMeasurementFor(PhysicsGoldenCase goldenCase)
        {
            if (goldenCase == null) return false;
            for (int i = 0; i < measurements.Count; i++)
                if (measurements[i].goldenCase == goldenCase) return true;
            return false;
        }

        public PhysicsGoldenRegressionReport Run()
        {
            results.Clear();
            string catalogError = string.Empty;
            if (catalog == null)
            {
                catalogError = "Golden catalog is not assigned.";
                LastCatalogValid = false;
            }
            else
            {
                LastCatalogValid = catalog.ValidateCatalog(out catalogError);
            }
            LastCatalogError = LastCatalogValid ? string.Empty : (catalogError ?? string.Empty);
            if (!LastCatalogValid)
            {
                LastReport = new PhysicsGoldenRegressionReport(results);
                return LastReport;
            }
            for (int i = 0; i < measurements.Count; i++)
            {
                PhysicsGoldenMeasurement sample = measurements[i];
                if (sample.goldenCase == null || !ContainsCatalogCase(sample.goldenCase)) continue;
                results.Add(PhysicsGoldenEvaluator.Evaluate(
                    sample.goldenCase, sample.measuredDistance, sample.measuredPeakSpeed));
            }
            LastReport = new PhysicsGoldenRegressionReport(results, catalog.Cases.Count);
            return LastReport;
        }
    }
}