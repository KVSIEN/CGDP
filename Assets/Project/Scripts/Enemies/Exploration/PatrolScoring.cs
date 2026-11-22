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
    // costs a little so it prefers what is near. On top of that each faction has its own taste in
    // where to look (ExploreStyle, StyleBonus).
    internal static class PatrolScoring
    {
        public const float DangerReach     = PlaceMemory.CellSize * 2f;
        private const float DistanceWeight = 0.25f;

        // ExploreStyle weights and distances (metres).
        private const float ExpandHopCost = 0.6f;
        public const float ExpandStart    = 8f;
        private const float ExpandLead    = 1.3f;
        private const float OutskirtsEdge = 0.5f;
        private const float OutskirtsWall = 0.4f;
        public const float WallReach      = 2.5f;
        private const float InsideMiddle  = 0.35f;
        private const float InsideOpen    = 0.3f;
        private const float OpenGround    = 4f;
        private const float InsideCover   = 0.45f;
        public const float CoverReach     = 6f;

        // appeal: 0–1, what the place is worth to the faction times how familiar it is (0 for ground it knows nothing of).
        public static float Score(float staleness, float appeal, float danger, float distanceShare, PatrolDoctrine patrol, GroundDoctrine ground)
        {
            float draw = staleness * (patrol.Curiosity + (1f - patrol.Curiosity) * Mathf.Clamp01(appeal));
            return draw - ground.DangerAversion * Mathf.Clamp01(danger) - DistanceWeight * Mathf.Clamp01(distanceShare);
        }

        // How much a stop suits the faction's way of exploring (ExploreStyle), added to Score.
        // anchorShare: 0 at its post, 1 at the edge of its range. hopShare: how far the stop is from
        // where it stands, as a share of its range. wallDistance: metres to the nearest wall (NavMesh
        // edge). coverDistance: metres to the nearest concealment it knows (infinity for none).
        public static float StyleBonus(ExploreStyle style, float anchorShare, float hopShare, float wallDistance, float coverDistance) => style switch
        {
            ExploreStyle.Expand    => -ExpandHopCost * Mathf.Clamp01(hopShare),
            ExploreStyle.Outskirts => OutskirtsEdge * Mathf.Clamp01(anchorShare) + OutskirtsWall * Near(wallDistance, WallReach),
            ExploreStyle.Inside    => InsideMiddle * (1f - Mathf.Clamp01(anchorShare)) + InsideOpen * Mathf.Clamp01(wallDistance / OpenGround)
                                      + InsideCover * Near(coverDistance, CoverReach),
            _                      => 0f
        };

        // An expanding faction's range: a little round its post at first, growing with the ground it has
        // covered (as if that ground were a disc round the post), never beyond its full range.
        public static float ExpandedRange(float range, int coveredCells)
        {
            float coveredArea = coveredCells * PatrolCoverage.CellSize * PatrolCoverage.CellSize;
            return Mathf.Min(range, ExpandStart + ExpandLead * Mathf.Sqrt(coveredArea / Mathf.PI));
        }

        private static float Near(float distance, float reach) => Mathf.Clamp01(1f - distance / reach);

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
