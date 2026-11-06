using System;
using CGD.BehaviorTrees;

namespace CGD.Enemies
{
    // Fires one burst (EnemyData's burst count, damage and spread) as soon as the weapon is
    // ready and the target is in sight, then succeeds when the burst is out. Doesn't move —
    // pair it with Hold Range or Chase Target.
    [Serializable]
    [BtNodeInfo("Enemy/Combat/Shoot Burst", "Fires one burst at the target once ready and in sight. Succeeds when the burst is out.")]
    public sealed class ShootBurst : EnemyAction
    {
        [NonSerialized] private bool _fired;

        protected override void OnEnter(BtContext context) => _fired = false;

        protected override BtStatus OnTick(BtContext context)
        {
            var target = Ai.Perception.Target;
            if (target == null) return BtStatus.Failure;

            var combat = Ai.RangedCombat;
            if (Ai.Tactics.IsInCover(Ai.Perception.TargetPosition)) Ai.ReportStep(EngageStep.FireFromCover);
            if (!_fired && combat.HasLineOfSight(Ai.Perception.TargetPosition))
                _fired = combat.TryStartBurst();

            return _fired && !combat.IsBursting ? BtStatus.Success : BtStatus.Running;
        }

        protected override void OnAbort(BtContext context) => Ai.RangedCombat.CancelBurst();
    }
}
