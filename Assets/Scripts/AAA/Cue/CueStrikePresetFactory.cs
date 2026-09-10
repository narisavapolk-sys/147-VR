using UnityEngine;

namespace VR147.AAA.Cue
{
    public static class CueStrikePresetFactory
    {
        public static CueStrikeTestCase CreateRuntime(string name, float side, float vertical, float power)
        {
            var test = ScriptableObject.CreateInstance<CueStrikeTestCase>();
            test.testName = name;
            test.side = Mathf.Clamp(side, -1f, 1f);
            test.vertical = Mathf.Clamp(vertical, -1f, 1f);
            test.power = Mathf.Clamp01(power);
            return test;
        }
    }
}
