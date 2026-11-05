using UnityEngine;

namespace VR147.AAA.Physics
{
    [CreateAssetMenu(menuName = "147VR/Physics/Table Surface Profile")]
    public sealed class TableSurfaceProfile : ScriptableObject
    {
        [Header("Cloth")]
        [Min(0f)] public float rollingFriction = 0.18f;
        [Min(0f)] public float rollingDamping = 0f;
        [Min(0f)] public float slidingFriction = 0.32f;
        [Min(0f)] public float spinFriction = 0.08f;
        [Header("Rest")]
        [Min(0f)] public float settleSpeed = 0.012f;
        [Min(0f)] public float settleSpin = 0.035f;

        [Header("Motion State")]
        [Min(0f)] public float rollSlipTolerance = 0.03f;

        [Header("Certification")]
        [Tooltip("Only enable after the cloth coefficients are derived from measured runtime truth.")]
        public bool measuredTruthCertified = false;
    }
}
