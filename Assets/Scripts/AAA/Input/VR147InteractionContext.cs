using UnityEngine;

namespace VR147.Input
{
    /// <summary>
    /// Runtime context shared by VR interaction systems.
    /// Contains intent/context only; never owns gameplay or physics state.
    /// </summary>
    public sealed class VR147InteractionContext : MonoBehaviour
    {
        [SerializeField] private VR147DominantHand dominantHand;
        [SerializeField] private VR147InputRouter inputRouter;

        public VR147DominantHand DominantHand => dominantHand;
        public VR147InputRouter InputRouter => inputRouter;

        private void Awake()
        {
            if (dominantHand == null)
                dominantHand = GetComponent<VR147DominantHand>();

            if (inputRouter == null)
                inputRouter = GetComponent<VR147InputRouter>();
        }
    }
}
