using CGD.Combat;
using CGD.Stealth;
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
        // whatever lies behind it. `radius` is how wide the shot is.
        public static bool ReachesWithin(Vector3 from, Vector3 to, float slack, LayerMask mask, float radius = 0f)
        {
            Vector3 line   = to - from;
            float   length = line.magnitude;
            if (length < 0.001f) return true;

            // A fat shot (a bolt with a body) is swept as wide as it flies, so it can't clip the edge of a wall or half cover a thin ray would slip past.
            Vector3 direction = line / length;
            bool blocked = radius > 0f
                ? Physics.SphereCast(from, radius, direction, out RaycastHit hit, length, mask, QueryTriggerInteraction.Ignore)
                : Physics.Raycast(from, direction, out hit, length, mask, QueryTriggerInteraction.Ignore);
            if (!blocked) return true;
            if (hit.distance >= length - Mathf.Min(slack, length * 0.5f)) return true;
            return Hitbox.FindDamageable(hit.collider) != null;
        }

        // Whether eyes at `from` see `to`: no concealment (smoke, foliage) in the way and nothing
        // on mask but `subject`, the character the line ends on.
        public static bool CanSee(Vector3 from, Vector3 to, LayerMask mask, Transform subject) =>
            !ConcealmentZone.BlocksLine(from, to) && IsClear(from, to, mask, subject);

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
