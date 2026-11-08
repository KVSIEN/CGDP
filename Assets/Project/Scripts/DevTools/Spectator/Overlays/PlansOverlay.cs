using UnityEngine;
using UnityEngine.AI;
using CGD.Enemies;

namespace CGD.DevTools
{
    // Where agents are going and why: each agent's path to its destination in its team
    // colour, and the inspected agent's latest cover, flank and vanish searches — the spots
    // each looked at, coloured by what became of them (OverlayPalette.ForVerdict), the
    // threat it searched against, and a line to the spot it picked — fading out as the
    // search grows old. Paths are recomputed a few times a second, into reused buffers.
    internal sealed class PlansOverlay
    {
        private const float GroundLift   = 0.08f;
        private const float PathRefresh  = 0.2f;
        private const float SearchFade   = 8f;
        private const int   MaxCorners   = 32;

        private readonly EnemyAI[]      _agents;
        private readonly NavMeshAgent[] _navigation;
        private readonly NavMeshPath[]  _paths;
        private readonly Vector3[][]    _corners;
        private readonly int[]          _cornerCounts;
        private float _nextRefresh;

        public PlansOverlay(EnemyAI[] agents)
        {
            _agents       = agents;
            _navigation   = new NavMeshAgent[agents.Length];
            _paths        = new NavMeshPath[agents.Length];
            _corners      = new Vector3[agents.Length][];
            _cornerCounts = new int[agents.Length];
            for (int i = 0; i < agents.Length; i++)
            {
                if (agents[i] != null) agents[i].TryGetComponent(out _navigation[i]);
                _paths[i]   = new NavMeshPath();
                _corners[i] = new Vector3[MaxCorners];
            }
        }

        // Refreshes the paths (on real time, so they stay right while paused and stepping).
        public void Refresh(float now)
        {
            if (now < _nextRefresh) return;
            _nextRefresh = now + PathRefresh;

            for (int i = 0; i < _agents.Length; i++)
            {
                _cornerCounts[i] = 0;
                if (_agents[i] == null) continue;
                var agent = new EnemyInsight(_agents[i]);
                NavMeshAgent navigation = _navigation[i];
                if (!agent.IsValid || agent.IsDead || navigation == null || !navigation.isOnNavMesh || !agent.HasDestination) continue;
                if (!navigation.CalculatePath(agent.Destination, _paths[i])) continue;
                _cornerCounts[i] = _paths[i].GetCornersNonAlloc(_corners[i]);
            }
        }

        public void DrawPath(OverlayMesh mesh, int index, bool detailed)
        {
            int count = _cornerCounts[index];
            if (count < 2) return;

            Color color = OverlayPalette.Faded(OverlayPalette.ForTeam(_agents[index].Health.Team), detailed ? 1f : 0.6f);
            Vector3[] corners = _corners[index];
            for (int i = 1; i < count; i++)
                mesh.Line(corners[i - 1] + Vector3.up * GroundLift, corners[i] + Vector3.up * GroundLift, color, detailed ? 3f : 2f);
            mesh.Cross(corners[count - 1] + Vector3.up * GroundLift, 0.8f, color, 3f);
        }

        public static void DrawSearches(OverlayMesh mesh, EnemyInsight agent, float now)
        {
            Search(mesh, agent, agent.Tactics.Cover, now);
            Search(mesh, agent, agent.Tactics.Flank, now);
            Search(mesh, agent, agent.Tactics.Vanish, now);
        }

        private static void Search(OverlayMesh mesh, EnemyInsight agent, TacticalDecision search, float now)
        {
            if (!search.HasRun) return;
            float fade = 1f - (now - search.Time) / SearchFade;
            if (fade <= 0f) return;

            foreach (TacticalCandidate candidate in search.Candidates)
            {
                Vector3 foot  = candidate.Position + Vector3.up * GroundLift;
                Color   color = OverlayPalette.Faded(OverlayPalette.ForVerdict(candidate.Verdict), fade);
                if (candidate.Verdict == CandidateVerdict.Picked) mesh.Circle(foot, 0.55f, color, 3f);
                else mesh.Cross(foot, 0.5f, color, 2f);
            }

            if (search.Plan != TacticalPlan.Vanish)
                mesh.Cross(search.Threat + Vector3.up * GroundLift, 1.2f, OverlayPalette.Faded(OverlayPalette.Threat, fade), 3f);
            if (search.Succeeded)
                mesh.Dashed(agent.Ai.transform.position + Vector3.up * GroundLift, search.Chosen + Vector3.up * GroundLift,
                            OverlayPalette.Faded(OverlayPalette.ForPlan(search.Plan), fade), 2.5f, 0.35f);
        }
    }
}
