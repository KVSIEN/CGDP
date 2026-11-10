using UnityEngine;
using CGD.Combat;
using CGD.Enemies;

namespace CGD.DevTools
{
    // The overlays' colours: one per team (matching the AI test agents' materials), one per
    // tactical search and one per verdict on a searched spot. The HUD's legend uses the same.
    internal static class OverlayPalette
    {
        public static readonly Color Unknown    = new(0.6f, 0.6f, 0.65f, 0.3f);
        public static readonly Color KnownCover = new(0.55f, 0.85f, 1f, 0.9f);
        public static readonly Color Facing     = new(0.45f, 1f, 0.5f, 1f);
        public static readonly Color Highlight  = new(1f, 0.82f, 0.29f, 1f);
        public static readonly Color Threat     = new(1f, 0.25f, 0.2f, 1f);
        public static readonly Color Belief     = new(1f, 0.6f, 0.2f, 0.9f);
        public static readonly Color Concealed  = new(0.45f, 0.95f, 0.45f, 0.9f);
        public static readonly Color Truth      = new(1f, 1f, 1f, 0.35f);

        public static Color ForTeam(Team team) => team switch
        {
            Team.Bio    => new Color(0.45f, 1f, 0.4f),
            Team.Tech   => new Color(1f, 0.85f, 0.25f),
            Team.Void   => new Color(0.78f, 0.55f, 1f),
            Team.Player => Color.white,
            _                  => new Color(1f, 0.35f, 0.3f),
        };

        public static Color ForPlan(TacticalPlan plan) => plan switch
        {
            TacticalPlan.Flank  => new Color(1f, 0.6f, 0.2f),
            TacticalPlan.Vanish => new Color(0.78f, 0.55f, 1f),
            _                   => new Color(0.4f, 0.75f, 1f),
        };

        public static Color ForVerdict(CandidateVerdict verdict) => verdict switch
        {
            CandidateVerdict.Picked      => new Color(0.45f, 1f, 0.5f),
            CandidateVerdict.Passed      => new Color(1f, 1f, 1f, 0.8f),
            CandidateVerdict.Exposed     => new Color(1f, 0.6f, 0.2f),
            CandidateVerdict.Taken       => new Color(0.6f, 0.6f, 0.65f),
            CandidateVerdict.Seen        => new Color(1f, 0.25f, 0.2f),
            CandidateVerdict.TooFar      => new Color(0.85f, 0.4f, 0.85f),
            _                            => new Color(0.35f, 0.35f, 0.4f),
        };

        public static Color Faded(Color color, float alpha) => new(color.r, color.g, color.b, color.a * alpha);

        public static string Hex(Color color) => "#" + ColorUtility.ToHtmlStringRGB(color);
    }
}
