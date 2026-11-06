using System;
using UnityEngine;

namespace CGD.BehaviorTrees
{
    // Succeeds when a blackboard flag has the expected value. A flag never set reads false.
    [Serializable]
    [BtNodeInfo("Blackboard/Check Flag", "Succeeds when the blackboard value matches. Unset reads as false.")]
    public sealed class CheckFlag : BtCondition
    {
        [SerializeField, BlackboardKey(BlackboardKeyAccess.Read, BlackboardValueType.Bool)] private string _key = "Flag";
        [SerializeField] private bool   _expected = true;

        public override string Summary => $"{_key} is {_expected}";

        protected override bool Check(BtContext context) => context.Blackboard.GetBool(_key) == _expected;
    }
}
