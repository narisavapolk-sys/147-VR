using UnityEngine;

namespace VR147.AAA.Physics
{
    [CreateAssetMenu(menuName = "147VR/Physics/Ball Collision Profile")]
    public sealed class BallCollisionProfile : ScriptableObject
    {
        [Header("Impact")]
        [Range(0f, 1f)] public float restitution = 0.94f;
        [Min(0f)] public float tangentialFriction = 0.055f;
        [Min(0f)] public float maxSpinTransfer = 0.85f;
        [Header("Stability")]
        [Min(0f)] public float minimumImpactSpeed = 0.015f;
        [Min(0f)] public float separationBias = 0.0005f;
    }
}
