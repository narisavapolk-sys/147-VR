using UnityEngine;

namespace VR147.AAA.Cue
{
    /// <summary>Immutable-at-runtime description of a committed cue strike.</summary>
    public readonly struct CueShotData
    {
        public readonly Vector3 direction;
        public readonly Vector3 contactPoint;
        public readonly float power01;
        public readonly float speed;

        public CueShotData(Vector3 direction, Vector3 contactPoint, float power01, float speed)
        {
            this.direction = direction.normalized;
            this.contactPoint = contactPoint;
            this.power01 = Mathf.Clamp01(power01);
            this.speed = Mathf.Max(0f, speed);
        }

        public bool IsValid => speed > 0f && direction.sqrMagnitude > 0.99f;
    }
}
