using System.Collections.Generic;
using UnityEngine;

namespace VR147.AAA.Physics.Golden
{
    [CreateAssetMenu(menuName = "147VR/Physics/Golden Case Catalog")]
    public sealed class PhysicsGoldenCatalog : ScriptableObject
    {
        [SerializeField] private List<PhysicsGoldenCase> cases = new();

        public IReadOnlyList<PhysicsGoldenCase> Cases => cases;

        public bool TryGetCase(string caseId, out PhysicsGoldenCase goldenCase)
        {
            goldenCase = null;
            if (string.IsNullOrWhiteSpace(caseId) || cases == null) return false;
            for (int i = 0; i < cases.Count; i++)
            {
                PhysicsGoldenCase item = cases[i];
                if (item != null && item.CaseId == caseId)
                {
                    goldenCase = item;
                    return true;
                }
            }
            return false;
        }

        public bool ValidateCatalog(out string error)
        {
            if (cases == null || cases.Count == 0)
            {
                error = "Golden catalog contains no cases.";
                return false;
            }

            var ids = new HashSet<string>();
            for (int i = 0; i < cases.Count; i++)
            {
                PhysicsGoldenCase item = cases[i];
                if (item == null)
                {
                    error = $"Golden case index {i} is null.";
                    return false;
                }
                if (!item.IsValid())
                {
                    error = $"Golden case '{item.CaseId}' is invalid.";
                    return false;
                }
                if (!ids.Add(item.CaseId))
                {
                    error = $"Duplicate golden case id '{item.CaseId}'.";
                    return false;
                }
            }

            error = string.Empty;
            return true;
        }
    }
}