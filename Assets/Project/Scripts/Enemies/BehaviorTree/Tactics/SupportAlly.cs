using System;
using UnityEngine;
using CGD.Abilities;
using CGD.BehaviorTrees;
using CGD.Combat;

namespace CGD.Enemies
{
    // A field engineer's job: finds the most damaged ally of its own side close by (health below
    // the threshold), goes to it — staying out of the fight's way — and
    // uses its Support ability (EnemyData › Kit › Support, e.g. Repair: heals allies around it)
    // once in reach. Succeeds when the ability went off; fails when nobody needs it, the
    // ability isn't ready, or it can't get there in time.
    [Serializable]
    [BtNodeInfo("Enemy/Tactics/Support Ally", "Goes to the most damaged ally nearby and uses its Support ability on it. Fails when nobody needs help or it isn't ready.")]
    public sealed class SupportAlly : EnemyAction
    {
        [Tooltip("Metres it looks for allies in")]
        [SerializeField, Min(1f)] private float _searchRadius = 20f;
        [Tooltip("Helps allies whose health is at or below this share of its maximum")]
        [SerializeField, Range(0.05f, 1f)] private float _threshold = 0.7f;
        [Tooltip("Metres from the ally at which it uses the ability (keep within the ability's reach)")]
        [SerializeField, Min(0.5f)] private float _castRange = 4f;
        [Tooltip("Speed getting there, as a share of chase speed")]
        [SerializeField, Range(0.2f, 1.5f)] private float _speed = 1f;
        [Tooltip("Seconds to reach the ally before giving up")]
        [SerializeField, Min(0.5f)] private float _giveUpAfter = 6f;

        [NonSerialized] private Ability _support;
        [NonSerialized] private EnemyAI _ally;
        [NonSerialized] private bool    _casting;
        [NonSerialized] private float   _givesUpAt;

        public override string Summary => $"Below {_threshold:P0} within {_searchRadius:0} m";

        protected override void OnInitialize(BtContext context)
        {
            base.OnInitialize(context);
            _support = Ai.Data.Support;
            Ai.Abilities.Register(_support);
        }

        protected override void OnEnter(BtContext context)
        {
            _casting   = false;
            _givesUpAt = context.Time + _giveUpAfter;
            _ally      = Ai.Abilities.IsReady(_support) ? MostInNeed() : null;
            if (_ally != null) Ai.Agent.isStopped = false;
        }

        protected override void OnExit(BtContext context) => Ai.Agent.isStopped = false;

        protected override void OnAbort(BtContext context) => Ai.Abilities.Cancel();

        protected override BtStatus OnTick(BtContext context)
        {
            if (_ally == null || _ally.Health.IsDead) return BtStatus.Failure;

            Vector3 at = _ally.transform.position;
            if (_casting)
            {
                Ai.ReportStep(EngageStep.Cast);
                Ai.Agent.isStopped = true;
                return Ai.Abilities.TickCast(at, context.DeltaTime) ? BtStatus.Success : BtStatus.Running;
            }

            if (context.Time >= _givesUpAt) return BtStatus.Failure;
            Ai.Motion.SetSpeed(Ai.Data.ChaseSpeed * _speed);
            Vector3 offset = at - Ai.transform.position;
            offset.y = 0f;
            if (offset.sqrMagnitude > _castRange * _castRange)
            {
                Ai.Routing.SetDestination(at);
                return BtStatus.Running;
            }

            Ai.Routing.Stop();
            _casting = Ai.Abilities.TryBegin(_support, at);
            return _casting ? BtStatus.Running : BtStatus.Failure;
        }

        // The ally of its own team within reach with the least health and shield left, if under the threshold.
        private EnemyAI MostInNeed()
        {
            EnemyAI best = null;
            float lowest = _threshold;
            Team team = Ai.Health.Team;
            var all = EnemyRoster.All;
            for (int i = 0; i < all.Count; i++)
            {
                EnemyAI other = all[i];
                if (other == Ai || other.Health == null || other.Health.IsDead || other.Health.Team != team) continue;
                if ((other.transform.position - Ai.transform.position).sqrMagnitude > _searchRadius * _searchRadius) continue;

                HealthManager health = other.Health;
                float share = health.MaxHealth > 0f ? health.Health / health.MaxHealth : 1f;
                if (share > lowest) continue;

                lowest = share;
                best   = other;
            }
            return best;
        }
    }
}
