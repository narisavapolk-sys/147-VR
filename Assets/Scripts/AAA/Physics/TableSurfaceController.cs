using UnityEngine;

namespace VR147.AAA.Physics
{
    [RequireComponent(typeof(Collider))]
    public sealed class TableSurfaceController : MonoBehaviour
    {
        [SerializeField] private TableSurfaceProfile profile;
        [SerializeField] private float minVelocity = 0.01f;

        public TableSurfaceProfile Profile => profile;

        public void Configure(TableSurfaceProfile surfaceProfile)
        {
            profile = surfaceProfile;
            if (profile != null && !profile.measuredTruthCertified)
                Debug.Log($"[147VR Surface] Profile bridged but INACTIVE: measuredTruthCertified=false | asset={profile.name}", this);
        }

        private void OnCollisionStay(Collision collision)
        {
            Rigidbody body = collision.rigidbody;
            if (body == null || profile == null || !profile.measuredTruthCertified) return;

            Vector3 planar = new Vector3(body.linearVelocity.x, 0f, body.linearVelocity.z);
            float speed = planar.magnitude;

            SphereCollider sphere = collision.collider as SphereCollider;
            float radius = sphere != null
                ? sphere.bounds.extents.x
                : 0.02625f;
            Vector3 contactVelocity = Vector3.Cross(
                Vector3.up * radius, body.angularVelocity);
            float slip = (planar - contactVelocity).magnitude;

            if (speed >= minVelocity)
            {
                bool sliding = slip > profile.rollSlipTolerance;
                float friction = sliding
                    ? profile.slidingFriction
                    : profile.rollingFriction;

                if (!sliding)
                {
                    float acceleration = friction + profile.rollingDamping * speed;
                    float resistanceForce = acceleration * body.mass;

                    if (profile.rollingDamping > 0f)
                    {
                        body.AddForce(
                            -planar * (profile.rollingDamping * body.mass),
                            ForceMode.Force);
                    }

                    // Couple the rolling resistance force to angular motion so
                    // v = r*omega remains coherent when the cloth slows the ball.
                    Vector3 rollingAxis = Vector3.Cross(
                        planar.normalized,
                        Vector3.up);
                    float rollingTorque = 0.4f * radius * resistanceForce;
                    body.AddTorque(
                        -rollingAxis * rollingTorque,
                        ForceMode.Force);
                }

                body.AddForce(-planar.normalized * friction * body.mass, ForceMode.Force);
            }

            // Spin friction models vertical-axis spin only. Do not damp
            // the lateral angular components that produce physical rolling.
            float verticalSpin = body.angularVelocity.y;
            if (Mathf.Abs(verticalSpin) > 0.000001f)
            {
                float spinDamping = Mathf.Clamp01(
                    profile.spinFriction * Time.fixedDeltaTime);
                body.angularVelocity = new Vector3(
                    body.angularVelocity.x,
                    Mathf.Lerp(verticalSpin, 0f, spinDamping),
                    body.angularVelocity.z);
            }
        }
    }
}
