using UnityEngine;
using UnityEngine.XR;

namespace VR147.Input
{
    /// <summary>
    /// Read-only two-hand cue pose source.
    /// Bridge hand = non-dominant hand. Stroke hand = dominant hand.
    /// This component does not own aiming or physics; it only exposes a stable pose contract.
    /// </summary>
    public sealed class VR147TwoHandCuePoseSource : MonoBehaviour
    {
        [SerializeField] private VR147DominantHand dominantHand;
        [SerializeField] private Transform leftFallbackAnchor;
        [SerializeField] private Transform rightFallbackAnchor;
        [SerializeField] private float velocitySmoothing = 18f;

        public bool IsValid { get; private set; }
        public VR147Hand StrokeHand => dominantHand != null ? dominantHand.Current : VR147Hand.Right;
        public VR147Hand BridgeHand => StrokeHand == VR147Hand.Left ? VR147Hand.Right : VR147Hand.Left;
        public Vector3 BridgePosition { get; private set; }
        public Vector3 StrokePosition { get; private set; }
        public Quaternion BridgeRotation { get; private set; } = Quaternion.identity;
        public Quaternion StrokeRotation { get; private set; } = Quaternion.identity;
        public Vector3 BridgeVelocity { get; private set; }
        public Vector3 StrokeVelocity { get; private set; }
        public float HandSeparation { get; private set; }
        public Vector3 CueAxis { get; private set; } = Vector3.forward;

        private Vector3 _previousBridge;
        private Vector3 _previousStroke;
        private bool _initialized;

        private void Awake()
        {
            if (dominantHand == null)
                dominantHand = FindFirstObjectByType<VR147DominantHand>();
        }

        private void Update()
        {
            bool leftValid = TryRead(XRNode.LeftHand, leftFallbackAnchor,
                out Vector3 leftPos, out Quaternion leftRot);
            bool rightValid = TryRead(XRNode.RightHand, rightFallbackAnchor,
                out Vector3 rightPos, out Quaternion rightRot);

            if (!leftValid || !rightValid)
            {
                IsValid = false;
                BridgeVelocity = Vector3.zero;
                StrokeVelocity = Vector3.zero;
                return;
            }

            if (StrokeHand == VR147Hand.Left)
            {
                BridgePosition = rightPos;
                BridgeRotation = rightRot;
                StrokePosition = leftPos;
                StrokeRotation = leftRot;
            }
            else
            {
                BridgePosition = leftPos;
                BridgeRotation = leftRot;
                StrokePosition = rightPos;
                StrokeRotation = rightRot;
            }

            HandSeparation = Vector3.Distance(BridgePosition, StrokePosition);
            // Gameplay shot direction points from the stroke hand toward the bridge/tip hand.
            Vector3 axis = BridgePosition - StrokePosition;
            if (axis.sqrMagnitude > 0.000001f)
                CueAxis = axis.normalized;

            float dt = Time.deltaTime;
            Vector3 rawBridge = _initialized && dt > 0f
                ? (BridgePosition - _previousBridge) / dt
                : Vector3.zero;
            Vector3 rawStroke = _initialized && dt > 0f
                ? (StrokePosition - _previousStroke) / dt
                : Vector3.zero;
            float blend = 1f - Mathf.Exp(
                -Mathf.Max(0.01f, velocitySmoothing) * Mathf.Max(0f, dt));

            BridgeVelocity = Vector3.Lerp(BridgeVelocity, rawBridge, blend);
            StrokeVelocity = Vector3.Lerp(StrokeVelocity, rawStroke, blend);
            _previousBridge = BridgePosition;
            _previousStroke = StrokePosition;
            _initialized = true;
            IsValid = true;
        }
        private static bool TryRead(
            XRNode node,
            Transform fallback,
            out Vector3 position,
            out Quaternion rotation)
        {
            InputDevice device = InputDevices.GetDeviceAtXRNode(node);
            position = Vector3.zero;
            rotation = Quaternion.identity;

            bool valid = device.isValid &&
                         device.TryGetFeatureValue(
                             CommonUsages.devicePosition, out position) &&
                         device.TryGetFeatureValue(
                             CommonUsages.deviceRotation, out rotation);

            if (!valid && fallback != null)
            {
                position = fallback.position;
                rotation = fallback.rotation;
                valid = true;
            }

            return valid;
        }
    }
}