using UnityEngine;

namespace VR147.AAA.Physics
{
    [CreateAssetMenu(menuName = "147VR/Physics/Pocket Profile")]
    public sealed class PocketProfile : ScriptableObject
    {
        [Min(0f)] public float captureRadius = 0.055f;
        [Min(0f)] public float captureDepth = 0.06f;
        [Min(0f)] public float captureSpeed = 2.5f;
        [Min(0f)] public float rollInAssist = 0.15f;
    }
}
