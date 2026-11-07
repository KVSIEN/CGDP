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
