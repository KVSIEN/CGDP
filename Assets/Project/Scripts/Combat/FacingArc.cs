using UnityEngine;

namespace CGD.Combat
{
    // Whether a direction falls inside an arc in front of a character, measured flat on the
    // horizontal plane. Shared by melee guards and reflects.
    public static class FacingArc
    {
        // A direction that can't be told (attacker on top of the defender) counts as inside.
        public static bool Contains(Vector3 facing, Vector3 toTarget, float arcDeg)
        {
            if (arcDeg >= 360f) return true;
            facing.y = 0f; toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 0.0001f || facing.sqrMagnitude < 0.0001f) return true;
            return Vector3.Angle(facing, toTarget) <= arcDeg * 0.5f;
        }
    }
}
