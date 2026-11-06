using System;
using UnityEngine;
using CGD.Abilities;
using CGD.BehaviorTrees;

namespace CGD.Enemies
{
    // Casts one of the project's Ability assets — the same ones the player uses — aimed at
    // where it believes its target is. The ability's cast time is a wind-up spent facing
    // the target; its cooldown is kept per enemy. Fails straight away if it's cooling down,
    // out of range, or needs something enemies don't have (dash, reflect, stealth).
    [Serializable]
    [BtNodeInfo("Enemy/Combat/Use Ability", "Casts an Ability asset at the (believed) target, with its wind-up and cooldown. Fails if not ready.")]
    public sealed class UseAbility : EnemyAction
    {
        [SerializeField] private Ability    _ability;
        [SerializeField] private AbilityAim _aim = AbilityAim.Target;
        [Tooltip("Metres. Only casts with the target this close. 0 = any distance")]
        [SerializeField, Min(0f)] private float _maxRange;

        [NonSerialized] private bool _casting;

        public override string Summary => _ability != null ? _ability.name : "(no ability)";

        protected override void OnEnter(BtContext context)
        {
            _casting = TryAimPoint(out Vector3 aimAt) && InRange(aimAt) && Ai.Abilities.TryBegin(_ability, aimAt);
        }

        protected override BtStatus OnTick(BtContext context)
        {
            if (!_casting) return BtStatus.Failure;

            Ai.ReportStep(EngageStep.Cast);
            Ai.Agent.isStopped = true;
            if (!TryAimPoint(out Vector3 aimAt)) aimAt = Ai.transform.position + Ai.transform.forward * 5f;
            return Ai.Abilities.TickCast(aimAt, context.DeltaTime) ? BtStatus.Success : BtStatus.Running;
        }

        protected override void OnAbort(BtContext context) => Ai.Abilities.Cancel();

        protected override void OnExit(BtContext context) => Ai.Agent.isStopped = false;

        private bool TryAimPoint(out Vector3 point)
        {
            if (_aim == AbilityAim.Forward)
            {
                point = Ai.transform.position + Vector3.up * 1.5f + Ai.transform.forward * 5f;
                return true;
            }

            point = Ai.Perception.TargetPosition + Vector3.up;
            return Ai.Perception.Target != null;
        }

        private bool InRange(Vector3 point) =>
            _maxRange <= 0f || (point - Ai.transform.position).sqrMagnitude <= _maxRange * _maxRange;
    }
}
