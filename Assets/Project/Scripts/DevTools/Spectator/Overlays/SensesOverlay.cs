using UnityEngine;
using CGD.Combat;
using CGD.Enemies;

namespace CGD.DevTools
{
    // What an agent senses and believes: its sight cone and hearing range on the ground in
    // its awareness colour, a solid line to a target it senses (dashed to where it only
    // believes one is, with a cross there and a faint line on to where it really is), lines
    // to the other hostiles it weighed (brighter the more its priorities favour them;
    // dashed if only heard), and an arrow toward fire it is under.
    internal static class SensesOverlay
    {
        private const float GroundLift  = 0.06f;
        private const float ChestHeight = 1.2f;

        public static void Draw(OverlayMesh mesh, EnemyInsight agent, bool detailed)
        {
            Vector3 feet  = agent.Ai.transform.position + Vector3.up * GroundLift;
            Color   aware = AwarenessColors.Of(agent.Awareness);

            mesh.Arc(feet, agent.Forward, agent.SightAngle, agent.SightRange,
                     OverlayPalette.Faded(aware, detailed ? 0.95f : 0.55f), detailed ? 2.5f : 1.5f);
            mesh.Circle(feet, agent.HearingRadius, OverlayPalette.Faded(aware, 0.3f), 1f);

            Target(mesh, agent, feet);
            if (detailed) Weighed(mesh, agent);
            if (agent.IsUnderFire)
                mesh.Arrow(feet + Vector3.up, feet + Vector3.up + agent.ThreatDirection * 3f, OverlayPalette.Threat, 3f);
        }

        private static void Target(OverlayMesh mesh, EnemyInsight agent, Vector3 feet)
        {
            HealthManager target = agent.Target;
            if (target == null) return;

            Vector3 eye = agent.Eye;
            if (agent.IsSensingTarget)
            {
                mesh.Line(eye, target.transform.position + Vector3.up * ChestHeight, AwarenessColors.Engaged, 3f);
                return;
            }

            Vector3 belief = agent.TargetPosition;
            mesh.Dashed(eye, belief + Vector3.up * ChestHeight, OverlayPalette.Belief, 2.5f);
            mesh.Cross(belief + Vector3.up * GroundLift, 0.9f, OverlayPalette.Belief, 3f);
            mesh.Dashed(belief + Vector3.up * GroundLift, target.transform.position + Vector3.up * GroundLift,
                        OverlayPalette.Truth, 1f, 0.25f);
        }

        private static void Weighed(OverlayMesh mesh, EnemyInsight agent)
        {
            float heaviest = 0f;
            foreach (WeighedHostile hostile in agent.Weighed) heaviest = Mathf.Max(heaviest, hostile.Weight);
            if (heaviest <= 0f) return;

            Vector3 eye = agent.Eye;
            foreach (WeighedHostile hostile in agent.Weighed)
            {
                if (hostile.Hostile == null || hostile.Hostile == agent.Target || !hostile.Sensed) continue;
                Color   color = OverlayPalette.Faded(OverlayPalette.ForTeam(hostile.Hostile.Team), 0.25f + 0.65f * hostile.Weight / heaviest);
                Vector3 chest = hostile.Hostile.transform.position + Vector3.up * ChestHeight;
                if (hostile.Seen) mesh.Line(eye, chest, color, 1.5f);
                else mesh.Dashed(eye, chest, color, 1.5f);
            }
        }
    }
}
