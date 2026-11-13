using UnityEngine;
using UnityEngine.AI;
using CGD.Combat;
using CGD.Items;
using CGD.Stats;

namespace CGD.Enemies
{
    // How an enemy's body moves once its route is set (EnemyRouting): how fast it goes, which
    // way it faces, and being shoved. Facing is ours rather than the NavMeshAgent's: it follows
    // the walk by default, but a node can hold the gaze elsewhere, and the turn is spread over
    // the frames so it stays smooth when a calm enemy's tree ticks only a few times a second.
    internal sealed class EnemyMotion
    {
        private const float TurnSpeed = 540f;
        // Even a slow walker stops and turns crisply.
        private const float MinAcceleration = 20f;
        // A partial path ending this close means it is as near the target as it can walk.
        private const float BlockedDistance = 0.6f;

        private readonly EnemyAI        _ai;
        private readonly Transform      _self;
        private readonly NavMeshAgent   _agent;
        private readonly Stunnable      _stunnable;
        private readonly CharacterStats _stats;

        // Turning toward a point, spread over the frames until the tree's next tick.
        private Vector3 _facePoint;
        private float   _faceTimeLeft;

        public EnemyMotion(EnemyAI ai, NavMeshAgent agent, Stunnable stunnable, CharacterStats stats)
        {
            _ai        = ai;
            _self      = ai.transform;
            _agent     = agent;
            _stunnable = stunnable;
            _stats     = stats;
            _agent.updateRotation = false;
        }

        // Stuck at the end of a partial path: the target is somewhere it can't walk to.
        public bool IsPathBlocked =>
            _agent.isOnNavMesh && !_agent.pathPending && _agent.pathStatus == NavMeshPathStatus.PathPartial &&
            _agent.remainingDistance < BlockedDistance;

        public void Tick(float deltaTime)
        {
            TickKnockback(deltaTime);
            TickFacing(deltaTime);
        }

        // Slowed by status effects, and sped up or slowed by buffs (MovementSpeed stat, base 1).
        // Acceleration scales with the pace so it reaches, turns at and sheds any speed within
        // MoveResponseTime: a fixed acceleration leaves a fast mover drifting wide round corners
        // and sliding past where it meant to stop.
        public void SetSpeed(float speed)
        {
            _agent.speed        = speed * _stunnable.SpeedMultiplier * SpeedFromStats;
            _agent.acceleration = Mathf.Max(MinAcceleration, _agent.speed / _ai.Data.MoveResponseTime);

            // Out of a burst (a dash, a dodge) it drops straight to the new pace instead of coasting on.
            if (!_agent.isOnNavMesh) return;
            Vector3 velocity = _agent.velocity;
            if (velocity.sqrMagnitude > _agent.speed * _agent.speed) _agent.velocity = velocity.normalized * _agent.speed;
        }

        // Turns at once, with nothing in between: a head that is simply somewhere else now.
        public void SnapFacing(Vector3 position)
        {
            Vector3 flat = position - _self.position;
            flat.y = 0f;
            if (flat.sqrMagnitude < 0.0001f) return;
            _self.rotation = Quaternion.LookRotation(flat);
        }

        // Turns as far as deltaTime allows; the turn itself is applied frame by frame (TickFacing).
        public void FaceTowards(Vector3 position, float deltaTime)
        {
            _facePoint    = position;
            _faceTimeLeft = deltaTime;
        }

        // Repositioning without losing the objective: keeps the target (or where it was last known)
        // in view while walking, so the agent sidesteps and backpedals instead of turning its back.
        public void KeepEyesOnTarget(float deltaTime)
        {
            EnemyPerception perception = _ai.Perception;
            if (perception.Target != null) FaceTowards(perception.TargetPosition, deltaTime);
        }

        // Drops any turn still in progress (death, respawn).
        public void StopTurning() => _faceTimeLeft = 0f;

        private float SpeedFromStats => _stats != null ? Mathf.Max(0.1f, _stats.Apply(ItemStat.MovementSpeed, 1f)) : 1f;

        // Shoved (Stunnable.Knockback): carried along the navmesh, so it can't be pushed through walls or off it.
        private void TickKnockback(float deltaTime)
        {
            if (!_stunnable.IsKnockedBack || !_agent.enabled || !_agent.isOnNavMesh) return;
            _agent.Move(_stunnable.KnockbackVelocity * deltaTime);
        }

        private void TickFacing(float deltaTime)
        {
            Vector3 flat;
            float   step = deltaTime;
            if (_faceTimeLeft > 0f)
            {
                step = Mathf.Min(deltaTime, _faceTimeLeft);
                _faceTimeLeft -= step;
                flat = _facePoint - _self.position;
            }
            else if (_agent.enabled && _agent.isOnNavMesh && !_agent.isStopped)
            {
                flat = _agent.velocity;   // nothing to watch: look where it is going
            }
            else return;

            flat.y = 0f;
            if (flat.sqrMagnitude < 0.0001f) return;

            _self.rotation = Quaternion.RotateTowards(_self.rotation, Quaternion.LookRotation(flat), TurnSpeed * step);
        }
    }
}
