using UnityEngine;
using CGD.Stealth;

namespace CGD.Enemies
{
    // How an enemy goes about a bush it knows (BushMemory), given its whole footprint instead of a
    // point: where it passes through on the way elsewhere, where it settles to wait, and — against
    // a threat — which side to go in by and where to lie so the bush stands between them. Flat
    // geometry only; callers snap the spots to the NavMesh.
    internal static class BushApproach
    {
        // Metres in from the edge it keeps to: far enough in that it is really inside.
        public const float Margin = 0.7f;

        private const float AroundMargin  = 1f;     // how far outside the edge it walks to go round
        private const float ExposedAbove  = 0.3f;   // going in by a side facing the threat this squarely is exposed
        private const float ExposedCost   = 8f;     // metres of walking it will spend to avoid going in exposed
        private const float DeepShare     = 0.85f;  // how far toward the far corner it lies, from the middle
        private const int   Samples       = 8;

        // The spot to head for to pass through `area` on the way from → goal: out the far side if
        // the walk crosses it, else the nearest part of it inside to the walk.
        public static Vector3 PassPoint(Vector3 from, Vector3 goal, BushFootprint area)
        {
            if (area.TryChord(from, goal, out _, out float leaves))
                return Ground(area.Inset(Vector3.Lerp(from, goal, leaves), Margin), area);

            Vector3 nearest = from;
            float closest = float.MaxValue;
            for (int i = 0; i <= Samples; i++)
            {
                Vector3 along = Vector3.Lerp(from, goal, i / (float)Samples);
                float distance = area.DistanceTo(along);
                if (distance >= closest) continue;
                closest = distance;
                nearest = along;
            }
            return Ground(area.Inset(nearest, Margin), area);
        }

        // Where to stop in `area` coming from `from` with nothing to hide from: a little way in
        // from where it enters, not the middle.
        public static Vector3 RestSpot(Vector3 from, BushFootprint area)
        {
            Vector3 centre = area.Centre;
            Vector3 entry  = area.TryChord(from, centre, out float enters, out _) ? Vector3.Lerp(from, centre, enters) : from;
            return Ground(area.Inset(Vector3.Lerp(entry, centre, 0.5f), Margin), area);
        }

        // Where to lie to be hidden from `threat`: well in on the side away from it.
        public static Vector3 HideSpot(Vector3 threat, BushFootprint area)
        {
            Vector3 away = Flat(area.Centre - threat);
            if (away.sqrMagnitude < 0.01f) return Ground(area.Centre, area);

            Vector3 farCorner = area.Inset(area.Centre + away.normalized * 100f, Margin);
            return Ground(Vector3.Lerp(area.Centre, farCorner, DeepShare), area);
        }

        // The way into `area` to lie hidden from `threat`: straight to the spot when going in
        // that way keeps the bush between them, otherwise round to a point outside it, on the
        // side that does, first. Walking further to go in by the covered side is worth it.
        public static BushRoute PlanHide(Vector3 from, Vector3 threat, BushFootprint area)
        {
            Vector3 spot = HideSpot(threat, area);
            Vector3 toThreat = Flat(threat - area.Centre).normalized;

            float best = Vector3.Distance(from, spot) + ExposedCost * Exposure(from, spot, toThreat, area);
            Vector3 via = default;
            bool hasVia = false;

            Vector2 half = area.HalfSize + Vector2.one * AroundMargin;
            for (int i = 0; i < Samples; i++)
            {
                Vector3 candidate = Around(area, half, i);
                candidate.y = from.y;

                float length = Vector3.Distance(from, candidate) + Vector3.Distance(candidate, spot);
                float cost   = length + ExposedCost * Exposure(candidate, spot, toThreat, area)
                                      + ExposedCost * 0.5f * Mathf.Max(0f, Vector3.Dot(area.OutwardAt(candidate), toThreat) - ExposedAbove);
                if (cost >= best) continue;

                best   = cost;
                via    = candidate;
                hasVia = true;
            }
            return new BushRoute(hasVia, via, spot);
        }

        // 0 when the walk from → spot goes in by a side not facing the threat (or starts inside),
        // up to 1 for going in on the side facing it squarely.
        private static float Exposure(Vector3 from, Vector3 spot, Vector3 toThreat, BushFootprint area)
        {
            if (!area.TryChord(from, spot, out float enters, out _) || enters <= 0f) return 0f;

            Vector3 entry = Vector3.Lerp(from, spot, enters);
            float facing = Vector3.Dot(area.OutwardAt(entry), toThreat);
            return facing > ExposedAbove ? facing : 0f;
        }

        // The i-th of eight points round the outside of the patch: its four corners and four side middles.
        private static Vector3 Around(BushFootprint area, Vector2 half, int i)
        {
            float x = i switch { 0 or 1 or 7 => half.x, 3 or 4 or 5 => -half.x, _ => 0f };
            float z = i switch { 1 or 2 or 3 => half.y, 5 or 6 or 7 => -half.y, _ => 0f };
            return area.PointAt(x, z);
        }

        private static Vector3 Ground(Vector3 point, BushFootprint area)
        {
            point.y = area.Centre.y;
            return point;
        }

        private static Vector3 Flat(Vector3 vector) => new(vector.x, 0f, vector.z);
    }
}
