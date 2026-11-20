using System;
using UnityEngine;
using CGD.BehaviorTrees;

namespace CGD.Enemies
{
    // Succeeds while the enemy itself is hidden: in a bush or smoke (or cloaked) and not given
    // away by an attack or a noise. The stealth system's own verdict, so it holds exactly when
    // hostiles outside the same cover can't see it. The cue for a faction's hiding behaviors.
    [Serializable]
    [BtNodeInfo("Enemy/Senses/Is Hidden", "Succeeds while it is hidden in cover or cloaked, and not given away.")]
    public sealed class IsHidden : EnemyCondition
    {
        [Tooltip("Seconds it must have been hidden, without a break")]
        [SerializeField, Min(0f)] private float _forAtLeast;

        public override string Summary => _forAtLeast > 0f ? $"For {_forAtLeast:0.#} s" : "Now";

        protected override bool Check(BtContext context) =>
            Ai.Concealment.IsHidden && Ai.Concealment.HiddenFor >= _forAtLeast;
    }
}
