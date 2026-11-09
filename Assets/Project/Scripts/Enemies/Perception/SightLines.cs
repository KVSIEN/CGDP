using CGD.Combat;
using UnityEngine;

namespace CGD.Enemies
{
    // Line-of-sight checks that don't let a character hide itself. A line aimed at someone's
    // chest ends inside their own collider, so on a layer the obstacle mask includes (every
    // enemy body) a plain Linecast always reports them as blocked — enemies could never see,
    // hear clearly or shoot each other, nor spot a fallen ally's body.
    internal static class SightLines
    {
        private static readonly RaycastHit[] Hits = new RaycastHit[16];

        // Whether something fired or thrown along from → to gets within `slack` metres of `to`
        // before scenery stops it. Characters in the way don't count as scenery: a shot into
        // one is a hit, not a wasted shot. A wall right in front of the shooter fails this
        // whatever lies behind it.
        public static bool ReachesWithin(Vector3 from, Vector3 to, float slack, LayerMask mask)
        {
            Vector3 line   = to - from;
            float   length = line.magnitude;
            if (length < 0.001f) return true;

            if (!Physics.Raycast(from, line / length, out RaycastHit hit, length, mask, QueryTriggerInteraction.Ignore)) return true;
            if (hit.distance >= length - Mathf.Min(slack, length * 0.5f)) return true;
            return Hitbox.FindDamageable(hit.collider) != null;
        }

        // Nothing on mask blocks from → to, except colliders belonging to `ignore` (and its children).
        public static bool IsClear(Vector3 from, Vector3 to, LayerMask mask, Transform ignore)
        {
            if (!Physics.Linecast(from, to, out RaycastHit first, mask, QueryTriggerInteraction.Ignore)) return true;
            if (ignore == null || !first.collider.transform.IsChildOf(ignore)) return false;

            // Only reached when the line touches the ignored character, so the common case stays one cast.
            Vector3 line   = to - from;
            float   length = line.magnitude;
            int count = Physics.RaycastNonAlloc(from, line / length, Hits, length, mask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
                if (!Hits[i].collider.transform.IsChildOf(ignore)) return false;
            return true;
        }
    }
}
