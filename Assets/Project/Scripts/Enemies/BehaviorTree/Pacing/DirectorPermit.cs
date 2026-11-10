using System;
using CGD.BehaviorTrees;

namespace CGD.Enemies
{
    // Runs its child only if the director allows one more bold move level-wide right now
    // (see BoldMovePermits): put it above flanks, grenades, charges and suppressing fire. The
    // permit is held while the child runs and handed back when it finishes or is interrupted.
    // A gate, like Cooldown: a branch behind it doesn't interrupt others unless a permit is free.
    [Serializable]
    [BtNodeInfo("Enemy/Pacing/Director Permit", "Runs its child only while the director allows another bold move level-wide (flank, grenade, charge).")]
    public sealed class DirectorPermit : BtDecorator
    {
        [NonSerialized] private EnemyAI _ai;

        public override string Summary => "Bold move";

        internal override bool IsGate => true;
        internal override bool CheckCondition(BtContext context) =>
            BoldMovePermits.IsAvailableTo(_ai) && (Child == null || !Child.IsConditional || Child.CheckCondition(context));

        protected override void OnInitialize(BtContext context)
        {
            _ai = context.Agent as EnemyAI
                  ?? throw new InvalidOperationException($"{nameof(DirectorPermit)} needs an EnemyAI, not {context.Agent?.GetType().Name ?? "nothing"}.");
        }

        protected override BtStatus OnTick(BtContext context)
        {
            if (Child == null || !BoldMovePermits.TryAcquire(_ai)) return BtStatus.Failure;
            return Child.Tick(context);
        }

        protected override void OnExit(BtContext context) => BoldMovePermits.Release(_ai);
    }
}
