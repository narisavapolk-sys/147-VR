using UnityEngine;

namespace VR147.AAA.Physics
{
    public static class CushionResponse
    {
        public static Vector3 Solve(Vector3 velocity, Vector3 normal, CushionProfile profile)
        {
            if (profile == null) return velocity;
            Vector3 n = normal.normalized;
            float normalSpeed = Vector3.Dot(velocity, n);
            if (normalSpeed >= -profile.minimumImpactSpeed) return velocity;

            Vector3 normalVelocity = n * normalSpeed;
            Vector3 tangentVelocity = velocity - normalVelocity;
            normalVelocity *= -profile.restitution;
            tangentVelocity *= Mathf.Max(0f, 1f - profile.tangentialFriction);
            return normalVelocity + tangentVelocity;
        }
    }
}
