using UnityEngine;
using PoolPhysics.Sim;

namespace PoolPhysics.Unity
{
    /// <summary>
    /// Authorable physics profile prefilled with constants RECOVERED from Cue VR
    /// (libcuevr.so). [TUNE] fields must be fitted against the GOLDEN regression
    /// set. Create via Assets > Create > 147VR/Pool/Cue VR Physics Profile.
    /// </summary>
    [CreateAssetMenu(menuName = "147VR/Pool/Cue VR Physics Profile", fileName = "CueVRPhysicsProfile")]
    public sealed class CueVRPhysicsProfile : ScriptableObject
    {
        [Header("Integrator (recovered, HIGH)")]
        public float substep = 0.0005f;
        public int   maxSubstepsPerFrame = 2000;
        public float gravity = 9.806f;

        [Header("Balls (radius HIGH, mass MED)")]
        public float poolBallRadius = 0.028575f;
        public float snookerBallRadius = 0.02625f;
        public float ballMass = 0.160f;

        [Header("Cloth / friction [TUNE vs GOLDEN]")]
        public float slidingFriction = 0.2f;
        public float rollingFriction = 0.01f;
        public float spinDecay = 1.4726f;
        public float restSpeedEpsilon = 0.002f;

        [Header("Ball-ball [TUNE]")]
        public float ballBallRestitution = 0.95f;
        public float ballBallFriction = 0.06f;

        [Header("Cushion [TUNE — seeded from game priors]")]
        public float cushionRestitution = 0.75f;
        public float cushionFriction = 0.2f;
        public float cushionSpinLoss = 0.85f;
        public float cushionThrowCoeff = 0f;
        public float cushionGear = 0f;

        [Header("Nap (directional cloth) [TUNE]")]
        public float napAnisotropy = 0.15f;
        public float napSteer = 0f;

        [Header("Spin gains")]
        public float spinGainTangential = 0.8f;
        public float spinGainVertical = 0.4f;

        public PhysicsProfile ToProfile() => new PhysicsProfile {
            Substep = substep, MaxSubstepsPerFrame = maxSubstepsPerFrame, Gravity = gravity,
            PoolBallRadius = poolBallRadius, SnookerBallRadius = snookerBallRadius, BallMass = ballMass,
            SlidingFriction = slidingFriction, RollingFriction = rollingFriction,
            SpinDecay = spinDecay, RestSpeedEpsilon = restSpeedEpsilon,
            BallBallRestitution = ballBallRestitution, BallBallFriction = ballBallFriction,
            CushionRestitution = cushionRestitution, CushionFriction = cushionFriction,
            CushionSpinLoss = cushionSpinLoss, CushionThrowCoeff = cushionThrowCoeff, CushionGear = cushionGear,
            NapAnisotropy = napAnisotropy, NapSteer = napSteer,
            SpinGainTangential = spinGainTangential, SpinGainVertical = spinGainVertical,
        };
    }
}
