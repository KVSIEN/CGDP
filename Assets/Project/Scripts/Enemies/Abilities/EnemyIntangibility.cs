using UnityEngine;
using UnityEngine.AI;
using CGD.Abilities;

namespace CGD.Enemies
{
    // An enemy turned intangible (IntangibleAbility): nothing damages it and it goes straight
    // for where its tree is sending it, through walls, instead of along the NavMesh. It keeps
    // its pace (slows still bite) and still attacks and casts.
    //
    // The NavMeshAgent stays on and keeps the tree's calls working, but no longer moves the
    // body: the body walks the straight line and the agent is kept at the nearest NavMesh point
    // to it, so arrival checks come true when the body really gets there. When the time is up
    // it carries on out of any wall it is in (up to ExitGrace) before the agent takes over again.
    internal sealed class EnemyIntangibility : IIntangibility
    {
        private const float ArrivedWithin = 0.35f;  // metres from the goal it stops
        private const float OnMeshWithin  = 0.6f;   // close enough to the NavMesh to stand on it
        private const float MeshSearch    = 6f;     // how far it looks for the NavMesh to step back onto
        private const float FloorReach    = 1.5f;   // the floor is taken from the NavMesh only when it is this close in height
        private const float ExitGrace     = 4f;     // seconds past its time it may still be looking for the way out

        private readonly EnemyAI _ai;

        private float _until;

        public EnemyIntangibility(EnemyAI ai) => _ai = ai;

        public bool IsIntangible { get; private set; }

        public void Begin(float seconds)
        {
            _until = Mathf.Max(_until, Time.time + seconds);
            if (IsIntangible) return;

            IsIntangible = true;
            _ai.Health.AddInvulnerability();
            _ai.Agent.updatePosition = false;
        }

        public void Tick(float deltaTime)
        {
            if (!IsIntangible) return;

            NavMeshAgent agent = _ai.Agent;
            Vector3 position = _ai.transform.position;
            bool timeUp = Time.time >= _until;

            if (timeUp && IsStandable(position)) { End(position); return; }
            if (timeUp && Time.time >= _until + ExitGrace) { End(position); return; }

            if (TryChooseGoal(timeUp, position, out Vector3 goal) && !agent.isStopped && !_ai.IsStunned)
            {
                position = StepTowards(position, goal, agent.speed * deltaTime);
                _ai.transform.position = position;
            }
            if (NavMesh.SamplePosition(position, out NavMeshHit onMesh, OnMeshWithin, NavMesh.AllAreas))
                agent.nextPosition = onMesh.position;
        }

        // Dying or being pooled while intangible: nothing left to hold or give back.
        public void Reset()
        {
            if (!IsIntangible) return;

            IsIntangible = false;
            _until = 0f;
            _ai.Health.RemoveInvulnerability();
            _ai.Agent.updatePosition = true;
        }

        // Where it is heading: the tree's destination, or — once its time is up — the nearest
        // floor, to get out of the wall.
        private bool TryChooseGoal(bool timeUp, Vector3 position, out Vector3 goal)
        {
            if (timeUp)
                return SampleMesh(position, out goal);
            return _ai.Routing.TryGetGoal(out goal);
        }

        private static bool SampleMesh(Vector3 position, out Vector3 spot)
        {
            bool found = NavMesh.SamplePosition(position, out NavMeshHit hit, MeshSearch, NavMesh.AllAreas);
            spot = hit.position;
            return found;
        }

        private static bool IsStandable(Vector3 position) =>
            NavMesh.SamplePosition(position, out NavMeshHit hit, OnMeshWithin, NavMesh.AllAreas);

        private static Vector3 StepTowards(Vector3 from, Vector3 goal, float distance)
        {
            Vector3 flat = goal - from;
            flat.y = 0f;
            float remaining = flat.magnitude;
            if (remaining <= ArrivedWithin) return from;

            Vector3 next = from + flat / remaining * Mathf.Min(distance, remaining);
            // Off the NavMesh inside a wall it keeps the height it had, otherwise it follows the floor.
            if (NavMesh.SamplePosition(next, out NavMeshHit floor, MeshSearch, NavMesh.AllAreas) && Mathf.Abs(floor.position.y - from.y) < FloorReach)
                next.y = floor.position.y;
            return next;
        }

        private void End(Vector3 position)
        {
            IsIntangible = false;
            _until = 0f;
            _ai.Health.RemoveInvulnerability();

            NavMeshAgent agent = _ai.Agent;
            agent.updatePosition = true;
            if (!agent.enabled) return;

            // Back onto the NavMesh where it is, or as near as there is. The path starts over: its tree sends it again.
            agent.Warp(NavMesh.SamplePosition(position, out NavMeshHit hit, MeshSearch, NavMesh.AllAreas) ? hit.position : position);
            _ai.Routing.Stop();
        }
    }
}
