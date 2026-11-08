using System.Collections.Generic;
using UnityEngine;
using CGD.Enemies;

namespace CGD.DevTools
{
    // The CoverMap's spots around the inspected agent (or around where the camera looks):
    // a post per spot, tall for full cover and short for low, with a tick pointing at its
    // obstacle. Spots someone holds take that agent's team colour; with an agent inspected,
    // the spots it knows are blue, the ones it hasn't learned faint, ticks turn green where
    // the obstacle stands between the spot and its threat, and its own spot is ringed.
    internal sealed class CoverOverlay
    {
        private const float Radius      = 22f;
        private const float GroundLift  = 0.06f;
        private const float HighPost    = 1.6f;
        private const float LowPost     = 0.8f;
        private const float TickLength  = 0.6f;
        private const float FacingDot   = 0.35f;   // as EnemyTactics counts a spot as facing a threat

        private readonly List<int> _spots = new();

        public void Draw(OverlayMesh mesh, EnemyInsight? inspected, Vector3 around)
        {
            CoverMap map = CoverMap.Active;
            if (map == null || map.Count == 0) return;

            EnemyInsight agent = inspected.GetValueOrDefault();
            bool    hasAgent  = inspected.HasValue;
            bool    hasThreat = hasAgent && agent.Target != null;
            Vector3 threat    = hasThreat ? agent.TargetPosition : default;
            int     claimed   = hasAgent ? agent.ClaimedCover : -1;

            map.Query(hasAgent ? agent.Ai.transform.position : around, Radius, _spots);
            foreach (int index in _spots)
            {
                CoverPoint spot   = map[index];
                EnemyAI    holder = map.ClaimedBy(index);
                bool       known  = !hasAgent || agent.KnowsCover(index);

                Color color = holder != null ? OverlayPalette.ForTeam(holder.Health.Team)
                            : known          ? OverlayPalette.KnownCover
                            :                  OverlayPalette.Unknown;
                Vector3 foot = spot.Position + Vector3.up * GroundLift;
                mesh.Post(foot, spot.IsHigh ? HighPost : LowPost, color, spot.IsHigh ? 3f : 2f);

                bool shields = hasThreat && known && spot.Faces(threat, FacingDot);
                mesh.Line(foot, foot + spot.Facing * TickLength, shields ? OverlayPalette.Facing : OverlayPalette.Faded(color, 0.7f), 2f);
            }

            if (claimed < 0) return;
            Vector3 mine = map[claimed].Position + Vector3.up * GroundLift;
            mesh.Circle(mine, 0.7f, OverlayPalette.Highlight, 3f);
            mesh.Dashed(agent.Ai.transform.position + Vector3.up * GroundLift, mine, OverlayPalette.Highlight, 2f);
        }
    }
}
