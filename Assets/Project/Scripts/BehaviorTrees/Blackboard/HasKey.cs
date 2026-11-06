using System;
using UnityEngine;

namespace CGD.BehaviorTrees
{
    [Serializable]
    [BtNodeInfo("Blackboard/Has Key", "Succeeds while the blackboard holds a value under the key (of any kind).")]
    public sealed class HasKey : BtCondition
    {
        [SerializeField, BlackboardKey(BlackboardKeyAccess.Read)] private string _key = "Key";

        public override string Summary => _key;

        protected override bool Check(BtContext context) => context.Blackboard.Has(_key);
    }
}
