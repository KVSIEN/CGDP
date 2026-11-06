using System;
using UnityEngine;
using CGD.BehaviorTrees;

namespace CGD.Enemies
{
    // Says something from the enemy's bark set (a taunt before a charge, "hold!" at a door).
    // Always succeeds — a bark that was skipped (no line, too soon after another) never
    // holds up the tree. Most barks happen on their own; use this for extra moments.
    [Serializable]
    [BtNodeInfo("Enemy/Voice/Bark", "Says a line from the enemy's bark set, if the level isn't too noisy. Always succeeds.")]
    public sealed class Bark : EnemyAction
    {
        [SerializeField] private BarkKind _kind = BarkKind.Pushing;

        public override string Summary => _kind.ToString();

        protected override BtStatus OnTick(BtContext context)
        {
            Ai.Voice.Say(_kind);
            return BtStatus.Success;
        }
    }
}
