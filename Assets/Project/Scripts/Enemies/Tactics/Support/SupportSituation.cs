using System.Collections.Generic;
using UnityEngine;

namespace CGD.Enemies
{
    // What a supporter weighs when it picks where to stand: where its allies are, where the
    // threat is (if it knows one), how far its help reaches and which side of its allies' line it
    // keeps to. Built once per plan; the front is the ally nearest the threat, the line the
    // supporter stays behind (or, holding the line, in front of).
    public readonly struct SupportSituation
    {
        public readonly IReadOnlyList<Vector3> Allies;
        public readonly Vector3     Threat;
        public readonly bool        HasThreat;
        public readonly float       Reach;
        public readonly SupportLine Line;
        public readonly Vector3     Centroid;
        // Flat distance from the threat to the ally nearest it (0 without a threat).
        public readonly float       FrontDistance;

        public SupportSituation(IReadOnlyList<Vector3> allies, Vector3 threat, bool hasThreat, float reach,
                                SupportLine line = SupportLine.Behind)
        {
            Allies    = allies;
            Threat    = threat;
            HasThreat = hasThreat && allies.Count > 0;
            Reach     = reach;
            Line      = line;

            Vector3 sum = Vector3.zero;
            float front = float.MaxValue;
            for (int i = 0; i < allies.Count; i++)
            {
                sum += allies[i];
                if (hasThreat) front = Mathf.Min(front, SupportSpot.FlatDistance(allies[i], threat));
            }
            Centroid      = allies.Count > 0 ? sum / allies.Count : Vector3.zero;
            FrontDistance = HasThreat ? front : 0f;
        }
    }
}
