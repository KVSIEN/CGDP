using System;
using CGD.BehaviorTrees;

namespace CGD.Enemies
{
    // Stops, turns to the target and strikes as soon as the attack is off cooldown; the hit
    // lands after EnemyData's wind-up. Succeeds once the strike is done. Pair it with
    // Target In Range in a reactive Sequence so it stops when the target steps away.
    [Serializable]
    [BtNodeInfo("Enemy/Combat/Melee Attack", "Stops, faces the target and strikes when ready. Succeeds after the strike.")]
    public sealed class MeleeAttack : EnemyAction
    {
        [NonSerialized] private bool _struck;

        protected override void OnEnter(BtContext context) => _struck = false;

        protected override BtStatus OnTick(BtContext context)
        {
            var target = Ai.Perception.Target;
            if (target == null) return BtStatus.Failure;

            Ai.ReportState(EnemyAI.AiState.Chase);
            Ai.Agent.isStopped = true;
            Ai.FaceTowards(target.transform.position, context.DeltaTime);

            if (!_struck && Ai.CanAttack)
            {
                Ai.TryStartAttack();
                _struck = true;
            }
            return _struck && !Ai.IsAttacking ? BtStatus.Success : BtStatus.Running;
        }

        protected override void OnExit(BtContext context) => Ai.Agent.isStopped = false;
    }
}
