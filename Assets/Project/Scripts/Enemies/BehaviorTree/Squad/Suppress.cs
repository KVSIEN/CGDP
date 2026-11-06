using System;
using CGD.BehaviorTrees;

namespace CGD.Enemies
{
    // Fires one burst at where it believes the target is, whether or not it can see it —
    // shots that keep a target's head down behind cover while squadmates flank. Succeeds when
    // the burst is out. Ranged enemies only; respects how many may shoot at once.
    [Serializable]
    [BtNodeInfo("Enemy/Squad/Suppress", "Fires a burst at the (believed) target position, seen or not, to pin it down.")]
    public sealed class Suppress : EnemyAction
    {
        [NonSerialized] private bool _fired;

        protected override void OnEnter(BtContext context) => _fired = false;

        protected override BtStatus OnTick(BtContext context)
        {
            if (Ai.Perception.Target == null) return BtStatus.Failure;

            Ai.ReportStep(EngageStep.Suppress);
            Ai.Agent.isStopped = true;
            Ai.FaceTowards(Ai.Perception.TargetPosition, context.DeltaTime);

            var combat = Ai.RangedCombat;
            if (!_fired) _fired = combat.TryStartBurst();
            return _fired && !combat.IsBursting ? BtStatus.Success : BtStatus.Running;
        }

        protected override void OnAbort(BtContext context) => Ai.RangedCombat.CancelBurst();

        protected override void OnExit(BtContext context) => Ai.Agent.isStopped = false;
    }
}
