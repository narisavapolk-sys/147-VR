using UnityEngine;

namespace VR147.AAA.Physics
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class CushionResponder : MonoBehaviour
    {
        [SerializeField] private Rigidbody ball;
        [SerializeField] private CushionProfile profile;
        [SerializeField] private float cooldown = 0.01f;
        private float _lastImpactTime = -10f;

        private void Reset() => ball = GetComponent<Rigidbody>();

        private void OnCollisionEnter(Collision collision)
        {
            if (ball == null || profile == null || collision.contactCount == 0)
                return;
            if (Time.time - _lastImpactTime < cooldown) return;

            ContactPoint contact = collision.GetContact(0);
            Vector3 velocity = ball.linearVelocity;
            Vector3 result = CushionResponse.Solve(velocity, contact.normal, profile);
            if ((result - velocity).sqrMagnitude < 0.0000001f) return;

            ball.linearVelocity = result;
            _lastImpactTime = Time.time;
        }
    }
}
