using System;
using UnityEngine;

namespace VR147.Input
{
    public enum VR147Hand
    {
        Left,
        Right
    }

    /// <summary>Single source of truth for which hand owns cue interaction.</summary>
    public sealed class VR147DominantHand : MonoBehaviour
    {
        [SerializeField] private VR147Hand dominantHand = VR147Hand.Right;

        public VR147Hand Current => dominantHand;
        public event Action<VR147Hand> Changed;

        public bool IsDominant(VR147Hand hand) => hand == dominantHand;

        public void SetDominantHand(VR147Hand hand)
        {
            if (dominantHand == hand)
                return;

            dominantHand = hand;
            Changed?.Invoke(dominantHand);
        }
    }
}
