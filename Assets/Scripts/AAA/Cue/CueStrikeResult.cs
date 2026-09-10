using UnityEngine;

namespace VR147.AAA.Cue
{
    public readonly struct CueStrikeResult
    {
        public readonly Vector3 linearImpulse;
        public readonly Vector3 angularImpulse;
        public readonly Vector3 contactOffset;

        public CueStrikeResult(Vector3 linearImpulse, Vector3 angularImpulse, Vector3 contactOffset)
        {
            this.linearImpulse = linearImpulse;
            this.angularImpulse = angularImpulse;
            this.contactOffset = contactOffset;
        }

        public bool IsValid => linearImpulse.sqrMagnitude > 0.000001f;
    }
}
