using System;
using UnityEngine;
using CGD.BehaviorTrees;

namespace CGD.Enemies
{
    // Succeeds once its faction's overseer has escalated this far (each target that got away
    // from a search escalates it): for behaviour a faction only brings out once its prey has
    // proved slippery. Without an overseer it never succeeds, unless the level is 0.
    [Serializable]
    [BtNodeInfo("Enemy/Pacing/Overseer Allows", "Succeeds once its faction's overseer has escalated to this level and isn't holding it back.")]
    public sealed class OverseerAllows : EnemyCondition
    {
        [SerializeField, Range(0, 5)] private int _escalation = 1;
        [Tooltip("Also fails while the overseer is holding the faction back")]
        [SerializeField] private bool _notWhileBackingOff = true;

        public override string Summary => $"Escalation {_escalation}+";

        protected override bool Check(BtContext context)
        {
            if (!Overseers.TryGet(Ai.Doctrine, Ai.Health.Team, out FactionOverseer overseer)) return _escalation <= 0;
            if (_notWhileBackingOff && overseer.IsBackingOff) return false;
            return overseer.Escalation >= _escalation;
        }
    }
}
