using System;
using UnityEngine;
using CGD.BehaviorTrees;

namespace CGD.Enemies
{
    // Succeeds while its squad has given it this role in the current fight (see EnemySquad).
    [Serializable]
    [BtNodeInfo("Enemy/Squad/Has Squad Role", "Succeeds while its squad has given it this role (assault, flank, suppress).")]
    public sealed class HasSquadRole : EnemyCondition
    {
        [SerializeField] private SquadRole _role = SquadRole.Flank;

        public override string Summary => _role.ToString();

        protected override bool Check(BtContext context) => Ai.SquadRole == _role;
    }
}
