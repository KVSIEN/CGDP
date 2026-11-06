using System;
using UnityEngine;
using CGD.Core;

namespace CGD.BehaviorTrees
{
    [Serializable]
    [BtNodeInfo("Actions/Wait", "Does nothing for a while (random within the range), then succeeds.")]
    public sealed class Wait : BtAction
    {
        [SerializeField] private FloatRange _seconds = new(1f, 1f);

        [NonSerialized] private float _endsAt;

        public override string Summary => Mathf.Approximately(_seconds.Min, _seconds.Max)
            ? $"{_seconds.Min:0.##} s"
            : $"{_seconds.Min:0.##}–{_seconds.Max:0.##} s";

        protected override void OnEnter(BtContext context) => _endsAt = context.Time + _seconds.EvaluateClamped();

        protected override BtStatus OnTick(BtContext context) =>
            context.Time >= _endsAt ? BtStatus.Success : BtStatus.Running;
    }
}
