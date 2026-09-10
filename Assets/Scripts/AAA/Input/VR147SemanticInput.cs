using UnityEngine;

namespace VR147.Input
{
    /// <summary>Hardware-agnostic input vocabulary for 147 VR.</summary>
    public enum VR147Action
    {
        Move, Aim, GrabCue, Strike, CancelShot,
        OpenTablet, ResetFrame, Recenter, CalibrateHeight, CycleRest
    }

    public readonly struct VR147InputState
    {
        public readonly Vector2 Move;
        public readonly Vector2 Aim;
        public readonly bool GrabCue;
        public readonly bool Strike;
        public readonly bool CancelShot;
        public readonly bool OpenTablet;
        public readonly bool ResetFrame;
        public readonly bool Recenter;
        public readonly bool CalibrateHeight;
        public readonly bool CycleRest;

        public VR147InputState(Vector2 move, Vector2 aim, bool grabCue,
            bool strike, bool cancelShot, bool openTablet, bool resetFrame,
            bool recenter, bool calibrateHeight, bool cycleRest)
        {
            Move = move;
            Aim = aim;
            GrabCue = grabCue;
            Strike = strike;
            CancelShot = cancelShot;
            OpenTablet = openTablet;
            ResetFrame = resetFrame;
            Recenter = recenter;
            CalibrateHeight = calibrateHeight;
            CycleRest = cycleRest;
        }
    }

    public interface IVR147InputSource
    {
        VR147InputState ReadState();
    }
}
