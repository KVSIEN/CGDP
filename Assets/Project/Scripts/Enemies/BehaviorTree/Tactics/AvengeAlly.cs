using System;
using UnityEngine;
using CGD.BehaviorTrees;

namespace CGD.Enemies
{
    // Swears to get whoever killed the ally it just saw fall (Ally Fell): that hostile becomes its
    // target and stays its preferred one, over anyone else in its senses, for a while; it heads
    // for where the ally fell. Succeeds at once — follow it with a frenzy, a call for help, a charge.
    [Serializable]
    [BtNodeInfo("Enemy/Tactics/Avenge Ally", "Makes the killer of the ally it saw fall its sworn target for a while. Succeeds at once.")]
    public sealed class AvengeAlly : EnemyAction
    {
        [Tooltip("Seconds it stays set on the killer")]
        [SerializeField, Min(1f)] private float _seconds = 15f;

        public override string Summary => $"{_seconds:0} s";

        protected override BtStatus OnTick(BtContext context)
        {
            if (!Ai.Perception.HasUnavengedFall(float.MaxValue, float.MaxValue, out _)) return BtStatus.Failure;
            Ai.Perception.Avenge(_seconds);
            return BtStatus.Success;
        }
    }
}
