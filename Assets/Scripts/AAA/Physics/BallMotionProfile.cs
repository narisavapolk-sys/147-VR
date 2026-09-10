using UnityEngine;

namespace VR147.AAA.Physics
{
    [CreateAssetMenu(menuName = "147VR/Physics/Ball Motion Profile")]
    public sealed class BallMotionProfile : ScriptableObject
    {
        [Header("Cloth")]
        [Min(0f)] public float rollingFriction = 0.18f;
        [Min(0f)] public float slidingFriction = 0.32f;
        [Min(0f)] public float spinDamping = 0.08f;
        [Min(0f)] public float rollingTransition = 0.045f;
        [Min(0f)] public float settleSpeed = 0.012f;
        [Min(0f)] public float settleSpin = 0.035f;
        [Header("Rolling")]
        [Min(0f)] public float rollSlipTolerance = 0.03f;
        [Min(0f)] public float maxSpinCorrection = 0.75f;
    }
}
