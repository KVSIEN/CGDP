using System;
using UnityEngine;

namespace CGD.BehaviorTrees
{
    // Succeeds when there is reason enough to do something now: its weighted reasons (see
    // MotiveReason) plus a growing impatience add up to 1. Several weak reasons together, or
    // one strong one, will do; none at all only once it has been held back for its whole
    // patience. Put it after the readiness check (Ability Ready) so the patience only runs
    // while the action could actually go: being asked again after a pause starts it over.
    [Serializable]
    [BtNodeInfo("Blackboard/Motive", "Succeeds when weighted blackboard reasons, plus impatience that grows while it waits, add up to 1.")]
    public sealed class Motive : BtCondition
    {
        private const float Threshold = 1f;
        private const float AskedGap  = 1f;   // seconds without being asked that start the patience over

        [SerializeField] private MotiveReason[] _reasons = Array.Empty<MotiveReason>();
        [Tooltip("Seconds after which it goes anyway with no reason. 0 = never without one")]
        [SerializeField, Min(0f)] private float _patience = 20f;

        [NonSerialized] private float _askedSince;
        [NonSerialized] private float _lastAsked;

        public override string Summary =>
            $"{Count} reason{(Count == 1 ? "" : "s")}" + (_patience > 0f ? $", patience {_patience:0.#} s" : "");

        private int Count => _reasons != null ? _reasons.Length : 0;

        protected override void OnInitialize(BtContext context) => _lastAsked = float.NegativeInfinity;

        protected override void OnReset() => _lastAsked = float.NegativeInfinity;

        protected override bool Check(BtContext context)
        {
            float now = context.Time;
            if (now - _lastAsked > AskedGap) _askedSince = now;
            _lastAsked = now;

            float want = _patience > 0f ? (now - _askedSince) / _patience : 0f;
            if (_reasons != null)
                foreach (MotiveReason reason in _reasons)
                    if (reason != null) want += reason.Evaluate(context.Blackboard);

            if (want < Threshold) return false;
            _askedSince = now;
            return true;
        }
    }
}
