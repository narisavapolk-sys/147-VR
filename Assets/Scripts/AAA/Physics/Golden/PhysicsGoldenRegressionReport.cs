using System.Collections.Generic;

namespace VR147.AAA.Physics.Golden
{
    public readonly struct PhysicsGoldenRegressionReport
    {
        public readonly int catalogCount;
        public readonly int total;
        public readonly int passed;
        public readonly int failed;
        public readonly int invalid;
        public readonly int missing;
        public readonly float passRate;
        public readonly float coverageRate;
        public readonly bool allCasesCovered;
        public readonly IReadOnlyList<PhysicsGoldenResult> results;

        public PhysicsGoldenRegressionReport(
            IReadOnlyList<PhysicsGoldenResult> results,
            int catalogCount = 0)
        {
            this.results = results;
            this.catalogCount = catalogCount;
            total = results?.Count ?? 0;
            int pass = 0;
            int invalidCount = 0;
            if (results != null)
            {
                for (int i = 0; i < results.Count; i++)
                {
                    if (results[i].passed) pass++;
                    if (!results[i].valid) invalidCount++;
                }
            }
            passed = pass;
            invalid = invalidCount;
            failed = total - passed;
            missing = catalogCount > total ? catalogCount - total : 0;
            passRate = total > 0 ? (float)passed / total : 0f;
            coverageRate = catalogCount > 0 ? (float)total / catalogCount : 0f;
            allCasesCovered = catalogCount > 0 && total == catalogCount && invalid == 0;
        }
    }
}
