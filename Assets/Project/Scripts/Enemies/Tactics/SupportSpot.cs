using UnityEngine;

namespace CGD.Enemies
{
    // How good a spot is for a supporter (a medic, a warden, a shield-bearer): within reach of
    // as many allies as possible, on its side of the line — behind the ally nearest the threat,
    // or in front of it when it holds the line (SupportLine) — behind cover that faces the threat,
    // not standing on an ally's toes, and not a long walk away. Plain logic, so the weighing can
    // be tested and tuned without the editor.
    public static class SupportSpot
    {
        public const float PerAllyInReach  = 10f;
        public const float WrongSideOfLine = 15f;   // per metre on the wrong side of the line
        public const float FacingCover     = 8f;
        public const float HighCover       = 4f;
        public const float Crowding        = 6f;
        public const float PerMetreWalked  = 0.3f;
        public const float PersonalSpace   = 1.5f;
        // Metres on the wrong side of the front ally that still count as in line with it.
        public const float LineSlack       = 1f;

        // Where it would ideally stand: on the line from the threat through its allies' centre,
        // `distance` metres behind the front ally (or in front of it, holding the line), but never
        // past the centre on the wrong side nor further from it than most of its reach. With no
        // threat, the allies' centre.
        public static Vector3 Ideal(in SupportSituation situation, float distance)
        {
            if (!situation.HasThreat) return situation.Centroid;

            Vector3 away = Flat(situation.Centroid - situation.Threat);
            if (away.sqrMagnitude < 0.0001f) return situation.Centroid;
            away.Normalize();

            bool  behind  = situation.Line == SupportLine.Behind;
            float wanted  = situation.FrontDistance + (behind ? distance : -distance);
            float centre  = FlatDistance(situation.Centroid, situation.Threat);
            float most    = situation.Reach * 0.8f;
            float pastMid = Mathf.Clamp(wanted - centre, behind ? 0f : -most, behind ? most : 0f);
            return situation.Centroid + away * pastMid;
        }

        // Higher is better. `facesThreat` and `isHigh` describe cover at the spot (both false for
        // open ground); `walk` is how far it is from where the supporter stands.
        public static float Score(Vector3 spot, bool facesThreat, bool isHigh, float walk, in SupportSituation situation)
        {
            float score = 0f;
            float reachSq = situation.Reach * situation.Reach;
            float spaceSq = PersonalSpace * PersonalSpace;
            for (int i = 0; i < situation.Allies.Count; i++)
            {
                float sq = Flat(situation.Allies[i] - spot).sqrMagnitude;
                if (sq <= reachSq) score += PerAllyInReach;
                if (sq <  spaceSq) score -= Crowding;
            }

            if (situation.HasThreat)
            {
                float toThreat = FlatDistance(spot, situation.Threat);
                float wrongSide = situation.Line == SupportLine.Behind
                    ? situation.FrontDistance + LineSlack - toThreat
                    : toThreat - (situation.FrontDistance + LineSlack);
                if (wrongSide > 0f) score -= wrongSide * WrongSideOfLine;
                if (facesThreat) score += FacingCover + (isHigh ? HighCover : 0f);
            }

            return score - walk * PerMetreWalked;
        }

        public static float FlatDistance(Vector3 a, Vector3 b) => Flat(a - b).magnitude;

        private static Vector3 Flat(Vector3 v) => new(v.x, 0f, v.z);
    }
}
