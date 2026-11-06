using System;
using UnityEngine;

namespace CGD.BehaviorTrees
{
    [Serializable]
    [BtNodeInfo("Blackboard/Clear Key", "Removes a value from the blackboard, then succeeds.")]
    public sealed class ClearKey : BtAction
    {
        [SerializeField] private string _key = "Key";

        public override string Summary => _key;

        protected override BtStatus OnTick(BtContext context)
        {
            context.Blackboard.Remove(_key);
            return BtStatus.Success;
        }
    }
}
