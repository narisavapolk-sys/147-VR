using UnityEngine;

namespace VR147.AAA.Physics
{
    [CreateAssetMenu(menuName = "147VR/Physics/Cushion Profile")]
    public sealed class CushionProfile : ScriptableObject
    {
        [Range(0f, 1f)] public float restitution = 0.82f;
        [Min(0f)] public float tangentialFriction = 0.035f;
        [Min(0f)] public float spinTransfer = 0.35f;
        [Min(0f)] public float minimumImpactSpeed = 0.02f;
    }
}
