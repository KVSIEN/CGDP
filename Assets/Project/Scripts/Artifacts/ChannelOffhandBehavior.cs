using UnityEngine;
using CGD.Items;
using CGD.Meters;

namespace CGD.Artifacts
{
    // Hold: gives the wielder stat modifiers for as long as it is held, draining a meter — a
    // lantern whose light steadies you, a tome you keep open. Modifiers scale with the artifact's
    // potency. Visual and sensory effects (a light, revealing the hidden) hang off the same
    // lifecycle in a subclass.
    [CreateAssetMenu(fileName = "ChannelOffhand", menuName = "CGD/Artifacts/Channel (Hold Buff)")]
    public class ChannelOffhandBehavior : OffhandBehavior
    {
        [Tooltip("Applied while held")]
        [SerializeField] private StatModifier[] _modifiers = System.Array.Empty<StatModifier>();
        [Tooltip("Resource drained per second while held (Amount is per second). Out of it, the channel ends")]
        [SerializeField] private MeterCost _drainPerSecond;

        public override OffhandUse CreateUse() => new ChannelUse(_modifiers, _drainPerSecond);

        private sealed class ChannelUse : DrainingHoldUse
        {
            private readonly StatModifier[] _modifiers;

            public ChannelUse(StatModifier[] modifiers, MeterCost drain) : base(drain) => _modifiers = modifiers;

            protected override bool OnBegin(OffhandContext ctx)
            {
                if (ctx.Stats == null) return false;

                foreach (StatModifier modifier in _modifiers)
                    if (modifier.Stat != ItemStat.None)
                        ctx.Stats.Add(modifier.Stat, modifier.Op, modifier.Value * ctx.Potency, this);
                return true;
            }

            protected override void OnEnd(OffhandContext ctx) => ctx.Stats.RemoveFrom(this);
        }
    }
}
