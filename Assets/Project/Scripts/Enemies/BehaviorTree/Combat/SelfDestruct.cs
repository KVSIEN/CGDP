using System;
using System.Collections.Generic;
using UnityEngine;
using CGD.BehaviorTrees;
using CGD.Combat;
using CGD.Core;

namespace CGD.Enemies
{
    // A kamikaze's end: once its target is within reach it stops, arms (a short fuse the target
    // can see and hear coming — the Cast step, and it calls a warning), then blows up: everyone
    // in the blast radius with a clear line to it takes the damage (teams decide as for any hit,
    // so its own side is spared), and it is destroyed. Fails while the target is further than
    // the trigger range.
    [Serializable]
    [BtNodeInfo("Enemy/Combat/Self Destruct", "Within reach of the target: arms for a moment, then explodes, hurting everyone around and destroying itself.")]
    public sealed class SelfDestruct : EnemyAction
    {
        private const int MaxHits = 16;

        [Tooltip("Metres from the target within which it arms")]
        [SerializeField, Min(0.5f)] private float _triggerRange = 2.5f;
        [Tooltip("Seconds between arming and the blast — the target's chance to get clear")]
        [SerializeField, Min(0f)] private float _fuse = 0.6f;
        [Tooltip("Metres: blast radius")]
        [SerializeField, Min(0.5f)] private float _radius = 4f;
        [SerializeField, Min(0f)] private float _damage = 45f;
        [Tooltip("Optional: spawned where it explodes (a flash, a puff of smoke)")]
        [SerializeField] private GameObject _blast;
        [Tooltip("Metres the blast is heard from")]
        [SerializeField, Min(0f)] private float _noiseRadius = 25f;

        [NonSerialized] private bool  _armed;
        [NonSerialized] private float _blowsAt;
        [NonSerialized] private Collider[] _hits;
        [NonSerialized] private HashSet<IDamageable> _damaged;

        public override string Summary => $"{_radius:0.#} m, {_damage:0} dmg";

        protected override void OnInitialize(BtContext context)
        {
            base.OnInitialize(context);
            _hits    = new Collider[MaxHits];
            _damaged = new HashSet<IDamageable>();
        }

        protected override void OnEnter(BtContext context) => _armed = false;

        protected override void OnExit(BtContext context) => Ai.Agent.isStopped = false;

        protected override BtStatus OnTick(BtContext context)
        {
            EnemyPerception perception = Ai.Perception;
            if (!_armed)
            {
                if (perception.Target == null || !perception.IsSensing) return BtStatus.Failure;
                Vector3 offset = perception.TargetPosition - Ai.transform.position;
                if (offset.sqrMagnitude > _triggerRange * _triggerRange) return BtStatus.Failure;

                _armed   = true;
                _blowsAt = context.Time + _fuse;
                Ai.Routing.Stop();
                Ai.Bark(BarkKind.Warning);
            }

            Ai.ReportState(EnemyAI.AiState.Chase);
            Ai.ReportStep(EngageStep.Cast);
            Ai.Agent.isStopped = true;
            if (context.Time < _blowsAt) return BtStatus.Running;

            Explode();
            return BtStatus.Success;
        }

        private void Explode()
        {
            Vector3 center = Ai.transform.position + Vector3.up;
            var source = DamageSource.Of(Ai.gameObject);
            if (_blast != null) PrefabPool.Spawn(_blast, center, Quaternion.identity);
            Noise.Emit(center, _noiseRadius, source);

            int count = Physics.OverlapSphereNonAlloc(center, _radius, _hits, Ai.TargetMask, QueryTriggerInteraction.Ignore);
            var hit = new DamageInfo(_damage, source: source);
            _damaged.Clear();
            for (int i = 0; i < count; i++)
            {
                Collider col = _hits[i];
                if (col.transform.IsChildOf(Ai.transform)) continue;

                IDamageable target = Hitbox.FindDamageable(col);
                if (target == null || !_damaged.Add(target)) continue;
                if (Physics.Linecast(center, col.ClosestPoint(center), out RaycastHit block, Ai.ObstacleMask, QueryTriggerInteraction.Ignore)
                    && Hitbox.FindDamageable(block.collider) != target) continue;

                target.TakeDamage(hit);
            }

            // Gone with the blast: a hit nothing can mitigate, from nobody.
            EnemyHealth self = Ai.Health;
            self.TakeDamage(new DamageInfo((self.MaxHealth + self.MaxShield) * 10f + 1000f, type: DamageType.True));
        }
    }
}
