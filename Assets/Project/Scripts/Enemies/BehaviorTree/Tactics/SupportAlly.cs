using System;
using UnityEngine;
using CGD.Abilities;
using CGD.BehaviorTrees;
using CGD.Combat;

namespace CGD.Enemies
{
    // A field engineer's or medic's job: finds the ally of its own side close by that most needs
    // it — the least left of what its ability restores (health, shield or both), counting double what it is losing right now, so an
    // ally under fire comes before one merely worn down — goes to it and uses its Support ability
    // (EnemyData › Kit › Support: Field Repair heals, Shield Projector tops up shields, Mending
    // Spores heals and cleanses) once in reach. While its ally faces a threat it comes in on the
    // ally's far side, using the ally as cover rather than walking into the line of fire.
    // Succeeds when the ability went off; fails when nobody needs it, the ability isn't ready,
    // or it can't get there in time.
    [Serializable]
    [BtNodeInfo("Enemy/Tactics/Support Ally", "Goes to the most damaged ally nearby and uses its Support ability on it. Fails when nobody needs help or it isn't ready.")]
    public sealed class SupportAlly : EnemyAction
    {
        [Tooltip("Metres it looks for allies in")]
        [SerializeField, Min(1f)] private float _searchRadius = 20f;
        [Tooltip("Helps allies with at most this share left of what its ability restores (health, shield, or both together)")]
        [SerializeField, Range(0.05f, 1f)] private float _threshold = 0.7f;
        [Tooltip("How much more it favours an ally taking a beating right now: its recent losses (as a share of its maximum) " +
                 "count this many times over against what it has left. 0 = only what it has left matters")]
        [SerializeField, Range(0f, 3f)] private float _underFireWeight = 1f;
        [Tooltip("Metres from the ally at which it uses the ability (keep within the ability's reach)")]
        [SerializeField, Min(0.5f)] private float _castRange = 4f;
        [Tooltip("Speed getting there, as a share of chase speed")]
        [SerializeField, Range(0.2f, 1.5f)] private float _speed = 1f;
        [Tooltip("Seconds to reach the ally before giving up")]
        [SerializeField, Min(0.5f)] private float _giveUpAfter = 6f;

        [NonSerialized] private Ability _support;
        // What its ability gives back, so it only goes to allies short of that.
        [NonSerialized] private bool    _countsHealth;
        [NonSerialized] private bool    _countsShield;
        [NonSerialized] private EnemyAI _ally;
        [NonSerialized] private bool    _casting;
        [NonSerialized] private float   _givesUpAt;

        public override string Summary => $"Below {_threshold:P0} within {_searchRadius:0} m";

        protected override void OnInitialize(BtContext context)
        {
            base.OnInitialize(context);
            _support = Ai.Data.Support;
            Ai.Abilities.Register(_support);
            // A Targeted ability says what it restores; anything else (a Rally) helps either way.
            var targeted  = _support as TargetedAbility;
            _countsShield = targeted == null || targeted.Shield > 0f;
            _countsHealth = targeted == null || targeted.Heal > 0f || !_countsShield;
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
                Ai.Routing.SetDestination(Approach(at));
                return BtStatus.Running;
            }

            Ai.Routing.Stop();
            _casting = Ai.Abilities.TryBegin(_support, at);
            return _casting ? BtStatus.Running : BtStatus.Failure;
        }

        // Where to stand to reach the ally: on its far side from the threat, if there is one, so
        // the ally is between them; the ally itself otherwise.
        private Vector3 Approach(Vector3 ally)
        {
            if (Ai.Perception.Target == null) return ally;

            Vector3 away = ally - Ai.Perception.TargetPosition;
            away.y = 0f;
            if (away.sqrMagnitude < 0.01f) return ally;
            return ally + away.normalized * Mathf.Min(_castRange * 0.6f, 2.5f);
        }

        // The ally of its own team within reach that needs it most, if under the threshold: the
        // least health and shield left, less what it has been losing lately.
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

                EnemyHealth health = other.Health;
                float have = 0f, max = 0f;
                if (_countsHealth) { have += health.Health; max += health.MaxHealth; }
                if (_countsShield) { have += health.Shield; max += health.MaxShield; }
                float share = max > 0f ? have / max : 1f;
                share -= health.RecentDamageShare * _underFireWeight;
                if (share > lowest) continue;

                lowest = share;
                best   = other;
            }
            return best;
        }
    }
}
