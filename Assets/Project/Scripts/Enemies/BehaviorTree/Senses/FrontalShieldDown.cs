using System;
using CGD.BehaviorTrees;

namespace CGD.Enemies
{
    // For shield-bearers: answers whether its Frontal Shield has been broken and is not back up
    // yet, so a behavior can fall back while it is exposed. Fails without a Frontal Shield.
    [Serializable]
    [BtNodeInfo("Enemy/Senses/Frontal Shield Down", "Succeeds while its Frontal Shield is broken and not back up yet. Fails without one.")]
    public sealed class FrontalShieldDown : EnemyCondition
    {
        [NonSerialized] private FrontalShield _shield;

        protected override void OnInitialize(BtContext context)
        {
            base.OnInitialize(context);
            Ai.TryGetComponent(out _shield);
        }

        protected override bool Check(BtContext context) => _shield != null && !_shield.IsUp;
    }
}
