using UnityEngine;
using CGD.Core;
using CGD.Items;
using CGD.Meters;
using CGD.Stats;

namespace CGD.Abilities
{
    // The charges, recharge timer, Surge gauge and price of one ability: everything that
    // decides whether it can be used now, and what using it spends. Input, cast times and
    // who may act stay with the caller (PlayerAbilities for the four slots, an artifact's
    // Cast behavior for a held tome). Unity-independent apart from the meters it pays from.
    public sealed class AbilityState
    {
        private readonly SurgeGauge _surge = new();
        private CooldownTimer _recharge;

        public Ability Ability { get; }
        public int Charges { get; private set; }

        public AbilityState(Ability ability)
        {
            Ability = ability;
            Reset();
        }

        private const float MaxCooldownReduction = 0.75f;

        // Gear and buffs shorten cooldowns by CooldownReduction (0.1 = 10%), capped at 75%.
        public static float CooldownOf(Ability ability, CharacterStats stats)
        {
            float reduction = stats != null ? Mathf.Clamp(stats.Apply(ItemStat.CooldownReduction, 0f), 0f, MaxCooldownReduction) : 0f;
            return ability.Cooldown * (1f - reduction);
        }

        public bool HasCharge => Charges > 0;

        // The Surge gauge (0..1); 0 for abilities without Surge.
        public float Surge => Ability.Surge.IsActive ? _surge.Value : 0f;

        // False while the resource or Surge cost can't be paid.
        public bool CanAfford(MeterSet meters) => Ability.Cost.CanAfford(meters) && _surge.Has(Ability.SurgeCost);

        // 1 when it can be used; otherwise how far the next charge has recharged.
        public float ReadyRatio => Charges > 0 ? 1f : Ability.ChargeSource switch
        {
            ChargeSource.Surge => _surge.Value,
            ChargeSource.Both  => System.Math.Max(_surge.Value, _recharge.Ratio),
            _                  => _recharge.Ratio,
        };

        // Recharges charges and drains the Surge gauge. `cooldown` is the recharge time with
        // cooldown reduction already applied.
        public void Tick(float deltaTime, float now, float cooldown)
        {
            if (Ability.Surge.IsActive) _surge.Tick(deltaTime, now, Ability.Surge);

            if (Ability.ChargeSource == ChargeSource.Surge || Charges >= Ability.MaxCharges) return;

            _recharge.Tick(deltaTime);
            if (!_recharge.IsReady) return;

            Charges++;
            if (Charges < Ability.MaxCharges) _recharge.Start(cooldown);
        }

        // A combat action fills the Surge gauge; a full gauge becomes a charge for abilities
        // charged by Surge.
        public void OnCombatAction(float now)
        {
            if (!Ability.Surge.IsActive) return;

            _surge.Gain(Ability.Surge.GainPerAction, now);
            if (Ability.ChargeSource != ChargeSource.Cooldown && Charges < Ability.MaxCharges && _surge.TrySpend(1f))
                Charges++;
        }

        // Pays the meter cost (mana…) and the Surge cost. A Surge-scaled ability spends all its
        // Surge up to MaxSpend; `surgeSpent` is what it took.
        public bool TryPay(MeterSet meters, out float surgeSpent)
        {
            surgeSpent = Ability.Scaling.IsActive(Ability.SurgeCost)
                ? ResourceSpend.Amount(_surge.Value, Ability.SurgeCost, Ability.Scaling.MaxSpend)
                : Ability.SurgeCost;

            if (!_surge.Has(surgeSpent) || !Ability.Cost.TryPay(meters)) return false;
            _surge.TrySpend(surgeSpent);
            return true;
        }

        // Sets what resource scaling gave this cast (damage multiplier, bonus effects).
        public void Prepare(AbilityContext ctx, float surgeSpent)
        {
            ctx.Power        = ResourceSpend.Power(surgeSpent, Ability.SurgeCost, Ability.Scaling.MaxSpend, Ability.Scaling.MaxPower);
            ctx.BonusEffects = Ability.Scaling.IsActive(Ability.SurgeCost) ? Ability.Scaling.EffectsFor(surgeSpent) : null;
        }

        // A charge was used (after Execute).
        public void Spend(float cooldown)
        {
            bool wasFull = Charges >= Ability.MaxCharges;
            Charges--;
            if (wasFull && Ability.ChargeSource != ChargeSource.Surge) _recharge.Start(cooldown);
        }

        // Surge-only charges start empty, like every Surge gauge: they have to be fought for.
        public void Reset()
        {
            Charges = Ability.ChargeSource == ChargeSource.Surge ? 0 : Ability.MaxCharges;
            _recharge.Reset();
            _surge.Reset();
        }
    }
}
