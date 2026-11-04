using UnityEngine;

namespace CGD.UI
{
    // Which way an attacker is, seen from the viewer, on the ground plane. Height is ignored:
    // a shooter on a ledge above still reads as ahead, behind, left or right.
    public static class DamageDirection
    {
        private const float MinFlatDistance = 0.1f;

        // Degrees clockwise from straight ahead: 0 ahead, 90 to the right, 180 behind, -90 to
        // the left. False when the attacker is straight above or below (or the viewer looks
        // straight up or down), where there is no direction to show.
        public static bool TryAngle(Vector3 viewForward, Vector3 viewPosition, Vector3 attackerPosition, out float degrees)
        {
            Vector3 ahead = viewForward;
            ahead.y = 0f;
            Vector3 toAttacker = attackerPosition - viewPosition;
            toAttacker.y = 0f;

            if (ahead.sqrMagnitude < 0.0001f || toAttacker.magnitude < MinFlatDistance)
            {
                degrees = 0f;
                return false;
            }

            degrees = Vector3.SignedAngle(ahead, toAttacker, Vector3.up);
            return true;
        }
    }
}
