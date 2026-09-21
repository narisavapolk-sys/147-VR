using UnityEngine;
using UnityEngine.XR;
using VR147.AAA.Diagnostics;

namespace VR147.Input
{
    /// <summary>
    /// Hardware-facing cue-hand pose source. It exposes hand pose/velocity only;
    /// gameplay and physics remain outside this component.
    /// </summary>
    public sealed class VR147CueHandSource : MonoBehaviour
    {
        [SerializeField] private M7_4RuntimeLatencySampler latencySampler;
        [SerializeField] private VR147DominantHand dominantHand;
        [SerializeField] private Transform fallbackAnchor;
        [SerializeField] private float velocitySmoothing = 18f;

        public bool IsValid { get; private set; }
        public Vector3 Position { get; private set; }
        public Quaternion Rotation { get; private set; } = Quaternion.identity;
        public Vector3 LinearVelocity { get; private set; }
        public XRNode ActiveNode => dominantHand != null && dominantHand.Current == VR147Hand.Left
            ? XRNode.LeftHand : XRNode.RightHand;

        private Vector3 previousPosition;
        private bool initialized;

        private void Awake()
        {
            if (dominantHand == null)
                dominantHand = FindFirstObjectByType<VR147DominantHand>();
        }

        private void Update()
        {
            latencySampler?.MarkPoseSample();
            InputDevice device = InputDevices.GetDeviceAtXRNode(ActiveNode);
            Vector3 position = Vector3.zero;
            Quaternion rotation = Quaternion.identity;
            bool valid = device.isValid &&
                         device.TryGetFeatureValue(CommonUsages.devicePosition, out position) &&
                         device.TryGetFeatureValue(CommonUsages.deviceRotation, out rotation);

            if (!valid && fallbackAnchor != null)
            {
                position = fallbackAnchor.position;
                rotation = fallbackAnchor.rotation;
                valid = true;
            }

            if (!valid)
            {
                IsValid = false;
                LinearVelocity = Vector3.zero;
                return;
            }

            Position = position;
            Rotation = rotation;
            Vector3 rawVelocity = initialized && Time.deltaTime > 0f
                ? (Position - previousPosition) / Time.deltaTime
                : Vector3.zero;
            float blend = 1f - Mathf.Exp(-Mathf.Max(0.01f, velocitySmoothing) * Time.deltaTime);
            LinearVelocity = Vector3.Lerp(LinearVelocity, rawVelocity, blend);
            previousPosition = Position;
            initialized = true;
            IsValid = true;
        }
    }
}








