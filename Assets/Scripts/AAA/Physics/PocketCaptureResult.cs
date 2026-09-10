using UnityEngine;

namespace VR147.AAA.Physics
{
    public readonly struct PocketCaptureResult
    {
        public readonly bool captured;
        public readonly Vector3 target;
        public readonly float strength;

        public PocketCaptureResult(bool captured, Vector3 target, float strength)
        {
            this.captured = captured;
            this.target = target;
            this.strength = Mathf.Clamp01(strength);
        }
    }
}
