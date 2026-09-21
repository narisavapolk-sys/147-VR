using System;
using UnityEngine;

namespace VR147.Input
{
    /// <summary>Converts an input source into semantic 147 VR actions.</summary>
    public sealed class VR147InputRouter : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour inputSourceComponent;
        private IVR147InputSource inputSource;
        private VR147InputState state;
        private VR147InputState previousState;

        public event Action<VR147Action> ActionPerformed;
        public VR147InputState State => state;

        private void Awake()
        {
            inputSource = inputSourceComponent as IVR147InputSource;
            if (inputSourceComponent != null && inputSource == null)
                Debug.LogError("VR147 Input source must implement IVR147InputSource.", this);
        }

        private void Update()
        {
            if (inputSource == null) return;
            previousState = state;
            state = inputSource.ReadState();
            PublishEdgeActions();
        }

        private void PublishEdgeActions()
        {
            if (state.Strike && !previousState.Strike) Publish(VR147Action.Strike);
            if (state.CancelShot && !previousState.CancelShot) Publish(VR147Action.CancelShot);
            if (state.OpenTablet && !previousState.OpenTablet) Publish(VR147Action.OpenTablet);
            if (state.ResetFrame && !previousState.ResetFrame) Publish(VR147Action.ResetFrame);
            if (state.Recenter && !previousState.Recenter) Publish(VR147Action.Recenter);
            if (state.CalibrateHeight && !previousState.CalibrateHeight) Publish(VR147Action.CalibrateHeight);
            if (state.CycleRest && !previousState.CycleRest) Publish(VR147Action.CycleRest);
        }

        private void Publish(VR147Action action)
        {
            ActionPerformed?.Invoke(action);
        }
    }
}
