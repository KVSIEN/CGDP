using System;
using UnityEngine;

namespace CGD.BehaviorTrees
{
    // Writes a yes/no fact to the blackboard for other branches to read with Check Flag.
    [Serializable]
    [BtNodeInfo("Blackboard/Set Flag", "Writes a true/false value to the blackboard, then succeeds.")]
    public sealed class SetFlag : BtAction
    {
        [SerializeField, BlackboardKey(BlackboardKeyAccess.Write, BlackboardValueType.Bool)] private string _key = "Flag";
        [SerializeField] private bool   _value = true;

        public override string Summary => $"{_key} = {_value}";

        protected override BtStatus OnTick(BtContext context)
        {
            context.Blackboard.SetBool(_key, _value);
            return BtStatus.Success;
        }
    }
}
