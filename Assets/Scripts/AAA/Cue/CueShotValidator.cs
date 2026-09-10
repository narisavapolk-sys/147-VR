using UnityEngine;

namespace VR147.AAA.Cue
{
    /// <summary>Validates a proposed strike before gameplay/physics receives it.</summary>
    public static class CueShotValidator
    {
        public static bool TryCreate(
            Vector3 cueBallPosition,
            Vector3 aimPoint,
            float power01,
            CueStrokeModel stroke,
            out CueShotData shot)
        {
            shot = default;
            Vector3 direction = aimPoint - cueBallPosition;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.000001f)
                return false;

            power01 = Mathf.Clamp01(power01);
            float speed = stroke != null ? stroke.EvaluateSpeed(power01) : 0f;
            if (speed <= 0f)
                return false;

            shot = new CueShotData(
                direction.normalized,
                cueBallPosition,
                power01,
                speed);
            return shot.IsValid;
        }
    }
}
