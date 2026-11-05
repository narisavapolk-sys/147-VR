using UnityEngine;

namespace VR147.AAA.Physics
{
    [RequireComponent(typeof(Collider))]
    public sealed class TableSurfaceController : MonoBehaviour
    {
        [SerializeField] private TableSurfaceProfile profile;
        [SerializeField] private float minVelocity = 0.01f;

        public TableSurfaceProfile Profile => profile;

        private void OnCollisionStay(Collision collision)
        {
            Rigidbody body = collision.rigidbody;
            if (body == null || profile == null) return;

            Vector3 planar = new Vector3(body.linearVelocity.x, 0f, body.linearVelocity.z);
            float speed = planar.magnitude;
            if (speed < minVelocity) return;

            float friction = speed > 0.045f
                ? profile.rollingFriction
                : profile.slidingFriction;
            body.AddForce(-planar.normalized * friction * body.mass, ForceMode.Force);

            float spinDamping = Mathf.Clamp01(profile.spinFriction * Time.fixedDeltaTime);
            body.angularVelocity = Vector3.Lerp(
                body.angularVelocity, Vector3.zero, spinDamping);
        }
    }
}
