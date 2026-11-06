using System;
using UnityEngine;

namespace CGD.BehaviorTrees
{
    // Sets a trigger on the agent's Animator (a taunt, a point, a shrug), then succeeds. A
    // model without that trigger (or without an Animator) is left alone — it's cosmetic.
    [Serializable]
    [BtNodeInfo("Actions/Trigger Animation", "Sets a trigger on the agent's Animator (taunt, point, shrug), then succeeds.")]
    public sealed class TriggerAnimation : BtAction
    {
        [SerializeField] private string _trigger = "Taunt";

        [NonSerialized] private Animator _animator;
        [NonSerialized] private int      _hash;
        [NonSerialized] private bool     _hasTrigger;

        public override string Summary => _trigger;

        protected override void OnInitialize(BtContext context)
        {
            _animator = context.Owner != null ? context.Owner.GetComponentInChildren<Animator>() : null;
            _hash     = Animator.StringToHash(_trigger);
            if (_animator == null || _animator.runtimeAnimatorController == null) return;

            foreach (AnimatorControllerParameter parameter in _animator.parameters)
                if (parameter.nameHash == _hash && parameter.type == AnimatorControllerParameterType.Trigger) _hasTrigger = true;
        }

        protected override BtStatus OnTick(BtContext context)
        {
            if (_hasTrigger) _animator.SetTrigger(_hash);
            return BtStatus.Success;
        }
    }
}
