using UnityEngine;

namespace CGD.Enemies
{
    // How good a spot is for a supporter (a medic, a shield projector, a warden): within reach of
    // as many allies as possible, behind the ally nearest the threat rather than in front of the
    // line, behind cover that faces the threat, not standing on an ally's toes, and not a long
    // walk away. Plain logic, so the weighing can be tested and tuned without the editor.
    public static class SupportSpot
    {
        public const float PerAllyInReach  = 10f;
        public const float AheadOfLine     = 15f;   // per metre closer to the threat than the line
        public const float FacingCover     = 8f;
        public const float HighCover       = 4f;
        public const float Crowding        = 6f;
        public const float PerMetreWalked  = 0.3f;
        public const float PersonalSpace   = 1.5f;
        // Metres behind the front ally that still count as in line with it.
        public const float LineSlack       = 1f;

        // Where it would ideally stand: behind its allies' centre, away from the threat, far
        // enough back to be `standBack` metres behind the front ally but never further from the
        // centre than most of its reach. With no threat, the allies' centre.
        public static Vector3 Ideal(in SupportSituation situation, float standBack)
        {
            if (!situation.HasThreat) return situation.Centroid;

            Vector3 away = Flat(situation.Centroid - situation.Threat);
            if (away.sqrMagnitude < 0.0001f) return situation.Centroid;
            away.Normalize();

            float wanted   = situation.FrontDistance + standBack;
            float centre   = FlatDistance(situation.Centroid, situation.Threat);
            float pastMid  = Mathf.Clamp(wanted - centre, 0f, situation.Reach * 0.8f);
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
                float ahead = situation.FrontDistance + LineSlack - FlatDistance(spot, situation.Threat);
                if (ahead > 0f) score -= ahead * AheadOfLine;
                if (facesThreat) score += FacingCover + (isHigh ? HighCover : 0f);
            }

            return score - walk * PerMetreWalked;
        }

        public static float FlatDistance(Vector3 a, Vector3 b) => Flat(a - b).magnitude;

        private static Vector3 Flat(Vector3 v) => new(v.x, 0f, v.z);
    }
}
