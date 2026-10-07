using UnityEngine;
using CGD.Combat;
using CGD.Meters;

namespace CGD.Artifacts
{
    // Hold: keeps a ReflectProfile up on the player's Reflector — a mirror that throws shots back,
    // a talisman that negates hits — draining a meter for as long as it is held. Needs a Reflector
    // on the player.
    [CreateAssetMenu(fileName = "WardOffhand", menuName = "CGD/Artifacts/Ward (Hold Reflect)")]
    public class WardOffhandBehavior : OffhandBehavior
    {
        [Tooltip("What the ward catches and what caught damage becomes. Its Duration is only how long each raise lasts before it is raised again, so keep it short")]
        [SerializeField] private ReflectProfile _profile;
        [Tooltip("Resource drained per second while held (Amount is per second). Out of it, the ward drops")]
        [SerializeField] private MeterCost _drainPerSecond;

        public override OffhandUse CreateUse() => new WardUse(_profile, _drainPerSecond);

        private sealed class WardUse : DrainingHoldUse
        {
            private readonly ReflectProfile _profile;

            public WardUse(ReflectProfile profile, MeterCost drain) : base(drain) => _profile = profile;

            protected override bool OnBegin(OffhandContext ctx)
            {
                if (_profile == null || ctx.Reflector == null) return false;
                ctx.Reflector.Open(_profile);
                return true;
            }

            // A catch-limited profile closes after its catches; raising it again keeps the ward up.
            protected override void OnHold(OffhandContext ctx)
            {
                if (!ctx.Reflector.IsOpen) ctx.Reflector.Open(_profile);
            }

            protected override void OnEnd(OffhandContext ctx) => ctx.Reflector.Close();
        }
    }
}
