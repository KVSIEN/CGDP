using System;
using UnityEngine;
using UnityEngine.AI;
using CGD.Abilities;

namespace CGD.Enemies
{
    // An enemy dashing straight ahead (Thruster Dash): it drops its path and is driven along the
    // NavMesh from where it stands to the end of the dash, so it stops short of walls and ledges
    // instead of passing through them. Its tree waits while it dashes (EnemyAI skips the tick),
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

        public EnemyDash(EnemyAI ai) => _ai = ai;

        public bool IsDashing { get; private set; }

        public void Begin(float distance, float seconds, float spacing, Action<Vector3> passed)
        {
            NavMeshAgent agent = _ai.Agent;
            if (IsDashing || !agent.isActiveAndEnabled || !agent.isOnNavMesh) return;

            Vector3 facing = _ai.transform.forward;
            facing.y = 0f;
            if (facing.sqrMagnitude < 0.0001f) return;

            _from = agent.nextPosition;
            _to   = _from + facing.normalized * distance;
            if (NavMesh.Raycast(_from, _to, out NavMeshHit blocked, NavMesh.AllAreas)) _to = blocked.position;

            _length     = Vector3.Distance(_from, _to);
            _elapsed    = 0f;
            _duration   = seconds;
            _spacing    = Mathf.Max(0.1f, spacing);
            _nextDropAt = 0f;
            _passed     = passed;
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

            if (t >= 1f) End();
        }

        // Dying or being pooled mid-dash: nothing left to finish.
        public void Reset()
        {
            IsDashing = false;
            _passed   = null;
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
