using UnityEngine;
using CGD.Enemies;

namespace CGD.DevTools
{
    // What the inspected agent thinks about where an unseen target went: the last place it saw it
    // and the way it was heading, and a ring on every spot it is weighing, sized and brightened by
    // how likely it is — green for a bush, which it must walk up to before it can rule it out.
    // Spots it has looked at and found empty show as small grey crosses.
    internal static class BeliefOverlay
    {
        private const float GroundLift = 0.1f;
        private const float MinShown   = 0.04f;

        public static void Draw(OverlayMesh mesh, EnemyInsight agent)
        {
            if (!agent.BeliefActive) return;

            float confidence = agent.BeliefConfidence;
            Vector3 seen = agent.BeliefLastSeen + Vector3.up * GroundLift;
            mesh.Cross(seen, 1.2f, OverlayPalette.Faded(OverlayPalette.Threat, confidence), 3f);
            if (agent.BeliefHeading != Vector3.zero)
                mesh.Arrow(seen, seen + agent.BeliefHeading * 3f, OverlayPalette.Faded(OverlayPalette.Threat, confidence), 2.5f);

            foreach (BeliefSpot spot in agent.BeliefSpots)
            {
                Vector3 foot = spot.Position + Vector3.up * GroundLift;
                if (spot.Checked) mesh.Cross(foot, 0.25f, OverlayPalette.Truth, 1.5f);
                else if (spot.Weight >= MinShown)
                {
                    Color colour = spot.Concealed ? OverlayPalette.Concealed : OverlayPalette.Belief;
                    mesh.Circle(foot, 0.3f + 0.9f * spot.Weight, OverlayPalette.Faded(colour, Mathf.Clamp01(0.3f + spot.Weight)), 2f);
                }
            }
        }
    }
}
