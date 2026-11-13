using System;
using UnityEngine;
using CGD.BehaviorTrees;
using CGD.Combat;

namespace CGD.Enemies
{
    // A bull's charge: it stops and paws the ground for a moment, facing the target (the WindUp
    // step — the tell), then charges along that line and can no longer turn. Reaching the target
    // it hits and knocks it back; running into scenery it slams to a halt and is stunned — the
    // reward for sidestepping. Ends after the charge distance either way. Fails without a target
    // in sight.
    [Serializable]
    [BtNodeInfo("Enemy/Movement/Charge", "Paws the ground, then charges in a straight line it can't turn from: hits and knocks back the target, or stuns itself on a wall.")]
    public sealed class Charge : EnemyAction
    {
        private const float HitRadius = 1.4f;
        private const float StuckShare = 0.3f;   // moving less than this share of its pace means it hit something

        [Tooltip("Seconds it paws the ground before charging — the warning")]
        [SerializeField, Min(0f)] private float _windUp = 0.8f;
        [Tooltip("Metres the charge runs at most")]
        [SerializeField, Min(1f)] private float _distance = 18f;
        [Tooltip("Speed, as a share of chase speed")]
        [SerializeField, Range(0.5f, 4f)] private float _speed = 2.2f;
        [SerializeField, Min(0f)] private float _damage = 30f;
        [Tooltip("Metres per second it shoves the target")]
        [SerializeField, Min(0f)] private float _knockback = 9f;
        [Tooltip("Seconds it is stunned after slamming into scenery")]
        [SerializeField, Min(0f)] private float _stunOnImpact = 1.5f;

        [NonSerialized] private bool    _charging;
        [NonSerialized] private float   _chargesAt;
        [NonSerialized] private float   _travelled;
        [NonSerialized] private Vector3 _direction;

        public override string Summary => $"{_distance:0} m, {_damage:0} dmg";

        protected override void OnEnter(BtContext context)
        {
            _charging  = false;
            _travelled = 0f;
            _chargesAt = context.Time + _windUp;
        }

        protected override void OnExit(BtContext context) => Ai.Agent.isStopped = false;

        protected override BtStatus OnTick(BtContext context)
        {
            EnemyPerception perception = Ai.Perception;
            Ai.ReportState(EnemyAI.AiState.Chase);
            if (!_charging) return WindUp(perception, context);

            Ai.ReportStep(EngageStep.Charge);
            float pace = Ai.Data.ChaseSpeed * _speed;
            Vector3 before = Ai.transform.position;
            Ai.Agent.Move(_direction * (pace * context.DeltaTime));
            float moved = Flat(Ai.transform.position - before).magnitude;
            _travelled += moved;

            if (perception.Target != null && Flat(perception.Target.transform.position - Ai.transform.position).sqrMagnitude < HitRadius * HitRadius)
            {
                Hit(perception.Target);
                return BtStatus.Success;
            }
            if (context.DeltaTime > 0f && moved < pace * context.DeltaTime * StuckShare)
            {
                Ai.Health.GetComponent<Stunnable>().ApplyStun(_stunOnImpact);   // slammed into something
                return BtStatus.Success;
            }
            return _travelled >= _distance ? BtStatus.Success : BtStatus.Running;
        }

        // Pawing the ground, turning to face it; then the line is fixed.
        private BtStatus WindUp(EnemyPerception perception, BtContext context)
        {
            if (perception.Target == null || !perception.IsSensing) return BtStatus.Failure;

            Ai.ReportStep(EngageStep.WindUp);
            Ai.Routing.Stop();
            Ai.Agent.isStopped = true;
            Ai.Motion.FaceTowards(perception.TargetPosition, context.DeltaTime);
            if (context.Time < _chargesAt) return BtStatus.Running;

            _direction = Flat(perception.TargetPosition - Ai.transform.position).normalized;
            if (_direction.sqrMagnitude < 0.01f) return BtStatus.Failure;
            _charging = true;
            Ai.Motion.SnapFacing(Ai.transform.position + _direction);
            return BtStatus.Running;
        }

        private void Hit(HealthManager target)
        {
            target.TakeDamage(new DamageInfo(_damage, source: DamageSource.Of(Ai.gameObject)));
            if (_knockback > 0f && target.TryGetComponent(out Stunnable stunnable))
                stunnable.Knockback(_direction * _knockback, 0.35f);
        }

        private static Vector3 Flat(Vector3 v) => new(v.x, 0f, v.z);
    }
}
