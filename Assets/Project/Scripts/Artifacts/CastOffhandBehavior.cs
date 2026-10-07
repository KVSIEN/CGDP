using UnityEngine;
using CGD.Abilities;

namespace CGD.Artifacts
{
    // Tap: casts an ability — a tome's spell, a relic's invocation. The ability's own settings
    // decide how it recharges (cooldown, charges, Surge from combat actions), what it costs and
    // how long it takes to cast, exactly as in an ability slot; the artifact's potency scales its power.
    [CreateAssetMenu(fileName = "CastOffhand", menuName = "CGD/Artifacts/Cast Ability")]
    public class CastOffhandBehavior : OffhandBehavior
    {
        [SerializeField] private Ability _ability;

        public Ability Ability => _ability;

        public override OffhandUse CreateUse() => new CastUse(_ability);

        private sealed class CastUse : OffhandUse
        {
            private readonly AbilityState _state;
            private float _castTimer;
            private bool  _casting;

            public CastUse(Ability ability) => _state = ability != null ? new AbilityState(ability) : null;

            public override OffhandUseMode Mode => OffhandUseMode.Tap;
            public override float Ratio   => _state != null ? _state.ReadyRatio : 0f;
            public override int   Charges => _state != null && _state.Ability.MaxCharges > 1 ? _state.Charges : -1;

            public override void Tick(OffhandContext ctx)
            {
                if (_state == null) return;
                _state.Tick(ctx.DeltaTime, ctx.Now, AbilityState.CooldownOf(_state.Ability, ctx.Stats));

                if (!_casting) return;
                _castTimer -= ctx.DeltaTime;
                if (_castTimer > 0f) return;

                _casting = false;
                if (CanUse(ctx)) Fire(ctx);
            }

            public override bool Begin(OffhandContext ctx)
            {
                if (_state == null || _casting || !CanUse(ctx)) return false;

                ctx.Abilities.ReportStarted(_state.Ability);
                if (_state.Ability.CastTime > 0f)
                {
                    _casting   = true;
                    _castTimer = _state.Ability.CastTime;
                }
                else
                {
                    Fire(ctx);
                }
                return false; // a tap has nothing to hold
            }

            // A cast in progress is lost when the player can no longer act, or the tome is put away.
            public override void End(OffhandContext ctx) => _casting = false;

            public override void OnCombatAction(OffhandContext ctx) => _state?.OnCombatAction(ctx.Now);

            public override void Reset()
            {
                _casting = false;
                _state?.Reset();
            }

            private bool CanUse(OffhandContext ctx) =>
                ctx.Abilities != null && _state.HasCharge && _state.CanAfford(ctx.Meters) && _state.Ability.CanExecute(ctx.Abilities.Context);

            private void Fire(OffhandContext ctx)
            {
                if (!_state.TryPay(ctx.Meters, out float surgeSpent)) return;

                AbilityContext abilityCtx = ctx.Abilities.Context;
                _state.Prepare(abilityCtx, surgeSpent);
                abilityCtx.Power *= ctx.Potency;
                _state.Ability.Execute(abilityCtx);
                ctx.Abilities.ReportUsed(_state.Ability);
                _state.Spend(AbilityState.CooldownOf(_state.Ability, ctx.Stats));
            }
        }
    }
}
