using UnityEngine;

namespace VR147.AAA.Physics
{
    public static class BallCollisionResponse
    {
        public static bool TrySolve(
            Vector3 normal,
            Vector3 relativeVelocity,
            BallCollisionProfile profile,
            out Vector3 impulse)
        {
            impulse = Vector3.zero;
            if (profile == null) return false;
            if (relativeVelocity.sqrMagnitude <
                profile.minimumImpactSpeed * profile.minimumImpactSpeed)
                return false;

            float approachSpeed = Vector3.Dot(relativeVelocity, normal);
            if (approachSpeed >= 0f) return false;

            float normalImpulse = -approachSpeed * (1f + profile.restitution);
            Vector3 tangentVelocity = relativeVelocity - normal * approachSpeed;
            Vector3 tangentImpulse = -tangentVelocity * profile.tangentialFriction;
            impulse = normal * normalImpulse + tangentImpulse;
            return true;
        }
    }
}
