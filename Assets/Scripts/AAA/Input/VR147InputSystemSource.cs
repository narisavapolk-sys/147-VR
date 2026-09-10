using UnityEngine;
using UnityEngine.InputSystem;

namespace VR147.Input
{
    /// <summary>Bridges the existing 147 VR Input System asset into semantic input.</summary>
    public sealed class VR147InputSystemSource : MonoBehaviour, IVR147InputSource
    {
        [SerializeField] private InputActionAsset actions;
        private InputAction move;
        private InputAction look;
        private InputAction attack;
        private InputAction interact;
        private InputAction sprint;
        private InputAction cancelShot;
        private InputAction openTablet;
        private InputAction resetFrame;
        private InputAction recenter;
        private InputAction calibrateHeight;
        private InputAction cycleRest;

        public bool IsConfigured { get; private set; }

        private void Awake()
        {
            if (actions == null)
            {
                Debug.LogError("VR147InputSystemSource requires an InputActionAsset.", this);
                return;
            }

            var map = actions.FindActionMap("Player", false);
            if (map == null)
            {
                Debug.LogError("VR147InputSystemSource requires a Player action map.", this);
                return;
            }

            move = FindOptionalAction(map, "Move");
            look = FindOptionalAction(map, "Aim", "Look");
            attack = FindOptionalAction(map, "Strike", "Attack");
            interact = FindOptionalAction(map, "GrabCue", "Interact");
            sprint = FindOptionalAction(map, "Sprint");
            cancelShot = FindOptionalAction(map, "CancelShot");
            openTablet = FindOptionalAction(map, "OpenTablet");
            resetFrame = FindOptionalAction(map, "ResetFrame");
            recenter = FindOptionalAction(map, "Recenter");
            calibrateHeight = FindOptionalAction(map, "CalibrateHeight");
            cycleRest = FindOptionalAction(map, "CycleRest");

            IsConfigured = move != null && attack != null && interact != null;
        }

        private void OnEnable() => actions?.Enable();
        private void OnDisable() => actions?.Disable();

        public VR147InputState ReadState()
        {
            return new VR147InputState(
                move?.ReadValue<Vector2>() ?? Vector2.zero,
                look?.ReadValue<Vector2>() ?? Vector2.zero,
                interact?.IsPressed() ?? false,
                attack?.WasPressedThisFrame() ?? false,
                cancelShot?.WasPressedThisFrame() ?? false,
                openTablet?.WasPressedThisFrame() ?? false,
                resetFrame?.WasPressedThisFrame() ?? false,
                recenter?.WasPressedThisFrame() ?? false,
                calibrateHeight?.WasPressedThisFrame() ?? false,
                cycleRest?.WasPressedThisFrame() ?? false);
        }

        private static InputAction FindOptionalAction(InputActionMap map, params string[] names)
        {
            foreach (string name in names)
            {
                InputAction action = map.FindAction(name, false);
                if (action != null)
                    return action;
            }

            return null;
        }
    }
}
