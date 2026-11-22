using System.Collections.Generic;
using UnityEngine;

namespace CGD.Combat
{
    // A shot's straight line: the first thing it hits, flying on through any one-way barrier it
    // was fired from behind (ShieldBarrier). Every hitscan shot goes through here so a shield
    // wall treats the player's and the enemies' fire alike.
    public static class ShotRay
    {
        private static readonly RaycastHit[] Hits = new RaycastHit[32];
        private static readonly IComparer<RaycastHit> ByDistance =
            Comparer<RaycastHit>.Create((a, b) => a.distance.CompareTo(b.distance));

        public static bool Cast(Vector3 origin, Vector3 direction, out RaycastHit hit, float range, int mask)
        {
            hit = default;
            int count = Physics.RaycastNonAlloc(origin, direction, Hits, range, mask, QueryTriggerInteraction.Ignore);
            if (count == 0) return false;

            System.Array.Sort(Hits, 0, count, ByDistance);
            for (int i = 0; i < count; i++)
            {
                if (ShieldBarrier.PassesThrough(Hits[i].collider, direction)) continue;
                hit = Hits[i];
                return true;
            }
            return false;
        }
    }
}
