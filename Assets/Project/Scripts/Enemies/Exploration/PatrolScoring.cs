using System.Collections.Generic;
using UnityEngine;

namespace CGD.Enemies
{
    // How worth walking to a candidate stop is, from what the enemy knows of it. Pure
    // arithmetic, so the rule can be tested and tuned without a level.
    //
    // A stop is drawn by how long since it was seen (staleness) — and, the more curious the
    // faction, by nothing else: unexplored ground is as good as a favourite place. Less curious
    // factions are also drawn by places they know are good for something they value, but only
    // once those have gone stale too, so it does not stand at its favourite spot forever.
    // A place where blood was spilt puts it off for as long as the memory lasts, and distance
    // costs a little so it prefers what is near.
    internal static class PatrolScoring
    {
        public const float DangerReach     = PlaceMemory.CellSize * 2f;
        private const float DistanceWeight = 0.25f;

        // appeal: 0–1, what the place is worth to the faction times how familiar it is (0 for ground it knows nothing of).
        public static float Score(float staleness, float appeal, float danger, float distanceShare, PatrolDoctrine patrol, GroundDoctrine ground)
        {
            float draw = staleness * (patrol.Curiosity + (1f - patrol.Curiosity) * Mathf.Clamp01(appeal));
            return draw - ground.DangerAversion * Mathf.Clamp01(danger) - DistanceWeight * Mathf.Clamp01(distanceShare);
        }

        // What a known place is worth to the faction, 0–1: its value times how well it knows it.
        public static float AppealOf(GroundPlace place, GroundDoctrine ground) =>
            Mathf.Clamp01(ground.ValueOf(place.Traits)) * (0.5f + 0.5f * place.Familiarity);

        // The strongest fresh danger within reach of the point, fading out towards the edge of it.
        public static float DangerNear(Vector3 point, IReadOnlyList<GroundPlace> places)
        {
            float worst = 0f;
            foreach (GroundPlace place in places)
            {
                if (place.Danger <= 0f) continue;
                Vector3 offset = place.Position - point;
                offset.y = 0f;
                float falloff = 1f - offset.magnitude / DangerReach;
                if (falloff > 0f) worst = Mathf.Max(worst, place.Danger * falloff);
            }
            return worst;
        }
    }
}
