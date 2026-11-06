using System;
using UnityEngine;

namespace CGD.BehaviorTrees
{
    // Writes a number to the blackboard, or adds to it (a missing key counts as 0) — counters
    // such as "times hit" or "attempts" without writing code.
    [Serializable]
    [BtNodeInfo("Blackboard/Set Number", "Sets a blackboard number, or adds to it (missing = 0), then succeeds.")]
    public sealed class SetNumber : BtAction
    {
        [SerializeField, BlackboardKey(BlackboardKeyAccess.Write, BlackboardValueType.Float)] private string _key = "Count";
        [SerializeField] private NumberOperation _operation = NumberOperation.Set;
        [SerializeField] private float           _value = 1f;

        public override string Summary => _operation == NumberOperation.Add ? $"{_key} += {_value:0.##}" : $"{_key} = {_value:0.##}";

        protected override BtStatus OnTick(BtContext context)
        {
            Blackboard blackboard = context.Blackboard;
            float value = _operation == NumberOperation.Add ? blackboard.GetFloat(_key) + _value : _value;
            blackboard.SetFloat(_key, value);
            return BtStatus.Success;
        }
    }
}
