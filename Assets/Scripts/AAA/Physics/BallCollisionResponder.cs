using UnityEngine;

namespace VR147.AAA.Physics
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class BallCollisionResponder : MonoBehaviour
    {
        [SerializeField] private Rigidbody ball;
        [SerializeField] private BallCollisionProfile profile;

        private void Reset() => ball = GetComponent<Rigidbody>();

        private void OnCollisionEnter(Collision collision)
        {
            if (ball == null || profile == null || collision.contactCount == 0)
                return;

            ContactPoint contact = collision.GetContact(0);
            Vector3 relativeVelocity = collision.relativeVelocity;
            if (BallCollisionResponse.TrySolve(
                contact.normal, relativeVelocity, profile, out Vector3 impulse))
            {
                ball.AddForceAtPosition(impulse, contact.point, ForceMode.Impulse);
            }
        }
    }
}
