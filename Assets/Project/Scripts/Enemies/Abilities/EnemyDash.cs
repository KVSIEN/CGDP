using System;
using UnityEngine;
using UnityEngine.AI;
using CGD.Abilities;

namespace CGD.Enemies
{
    // An enemy dashing in a straight line (Thruster Dash, Smoke Screen, Venom Bite): it drops its
    // path and is driven along the NavMesh from where it stands to the end of the dash, so it stops
    // short of walls and ledges instead of passing through them. Its tree waits while it dashes (EnemyAI skips the tick),
    // and a stun cuts the dash short. Afterwards its tree sends it on again from wherever it ended up.
    internal sealed class EnemyDash : IDasher
    {
        private readonly EnemyAI _ai;

        private Vector3 _from;
        private Vector3 _to;
        private float   _length;
        private float   _elapsed;
        private float   _duration;
        private float   _spacing;
        private float   _nextDropAt;
        private Action<Vector3> _passed;
        private Action<Vector3> _arrived;

        public EnemyDash(EnemyAI ai) => _ai = ai;

        public bool IsDashing { get; private set; }

        public float Reach(Vector3 direction, float distance)
        {
            NavMeshAgent agent = _ai.Agent;
            if (!agent.isActiveAndEnabled || !agent.isOnNavMesh || !TryFlatten(direction, out Vector3 way)) return 0f;

            Vector3 from = agent.nextPosition;
            return Vector3.Distance(from, EndOf(from, way, distance));
        }

        public void Begin(Vector3 direction, float distance, float seconds, float spacing,
                          Action<Vector3> passed, Action<Vector3> arrived = null)
        {
            NavMeshAgent agent = _ai.Agent;
            if (IsDashing || !agent.isActiveAndEnabled || !agent.isOnNavMesh) return;
            if (!TryFlatten(direction, out Vector3 way)) return;

            _from = agent.nextPosition;
            _to   = EndOf(_from, way, distance);

            _length     = Vector3.Distance(_from, _to);
            _elapsed    = 0f;
            _duration   = seconds;
            _spacing    = Mathf.Max(0.1f, spacing);
            _nextDropAt = 0f;
            _passed     = passed;
            _arrived    = arrived;
            IsDashing   = true;

            _ai.Routing.Stop();
            agent.isStopped = true;
            Drop(0f);
        }

        public void Tick(float deltaTime)
        {
            if (!IsDashing) return;
            if (_ai.IsStunned || !_ai.Agent.isOnNavMesh) { End(); return; }

            _elapsed += deltaTime;
            float t = Mathf.Clamp01(_elapsed / _duration);
            Vector3 next = Vector3.Lerp(_from, _to, t);
            _ai.Agent.Move(next - _ai.Agent.nextPosition);
            Drop(t * _length);

            if (t < 1f) return;

            Action<Vector3> arrived = _arrived;
            End();
            arrived?.Invoke(_to);
        }

        // Dying or being pooled mid-dash: nothing left to finish.
        public void Reset()
        {
            IsDashing = false;
            _passed   = null;
            _arrived  = null;
        }

        private static bool TryFlatten(Vector3 direction, out Vector3 way)
        {
            direction.y = 0f;
            way = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.zero;
            return way != Vector3.zero;
        }

        // Where a dash from `from` along `way` stops: its full distance, or the wall or ledge in between.
        private static Vector3 EndOf(Vector3 from, Vector3 way, float distance)
        {
            Vector3 to = from + way * distance;
            return NavMesh.Raycast(from, to, out NavMeshHit blocked, NavMesh.AllAreas) ? blocked.position : to;
        }

        // Every point `spacing` apart up to how far it has come.
        private void Drop(float travelled)
        {
            while (_nextDropAt <= travelled)
            {
                _passed?.Invoke(_length > 0f ? Vector3.Lerp(_from, _to, _nextDropAt / _length) : _from);
                _nextDropAt += _spacing;
            }
        }

        private void End()
        {
            Reset();
            NavMeshAgent agent = _ai.Agent;
            if (!agent.isOnNavMesh) return;
            agent.isStopped = false;
            _ai.Routing.Stop();
        }
    }
}
