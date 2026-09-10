using UnityEngine;

namespace VR147.AAA.Cue
{
    [CreateAssetMenu(menuName = "147VR/Cue/Strike Test Case")]
    public sealed class CueStrikeTestCase : ScriptableObject
    {
        public string testName = "Straight";
        [Range(-1f, 1f)] public float side;
        [Range(-1f, 1f)] public float vertical;
        [Range(0f, 1f)] public float power = 0.5f;

        public Vector2 English => new Vector2(side, vertical);
    }
}