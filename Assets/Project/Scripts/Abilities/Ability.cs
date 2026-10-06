using UnityEngine;
using CGD.Combat;
using CGD.Meters;

namespace CGD.Abilities
{
    // Base class for all abilities. Create a new ability by inheriting from this
    // and implementing Execute. Add the CreateAssetMenu attribute so it appears
    // in the Project right-click menu.
    public abstract class Ability : ScriptableObject
    {
        [Header("Info")]
        public string DisplayName = "Ability";
        public Color  SlotColor   = new Color(0.3f, 0.55f, 1f, 1f);

        [Header("Cooldown")]
        [Tooltip("Seconds to recharge one charge")]
        public float Cooldown = 5f;
        [Tooltip("Uses stored at once; spent charges recharge one after another")]
        [Min(1)] public int MaxCharges = 1;
        [Tooltip("What refills charges: the cooldown, resource earned in combat (Charge Meter), or both")]
        public ChargeSource ChargeSource = ChargeSource.Cooldown;
        [Tooltip("Earned charges: the meter whose gains refill them (e.g. Rage). Spending a charge doesn't spend the meter")]
        public MeterDefinition ChargeMeter;
        [Tooltip("Earned charges: how much of the meter must be gained per charge")]
        [Min(1f)] public float EarnedPerCharge = 25f;

        [Header("Cost")]
        [Tooltip("Resource spent on each use (mana, energy...). Leave Meter empty for none.")]
        public MeterCost Cost;
        [Tooltip("Optional: spend more than the cost for a stronger cast")]
        public ResourceScaling Scaling = new();

        [Header("Casting")]
        [Tooltip("Delay between pressing the key and the ability firing (0 = instant). Cancelled if the player is stunned, mantling or rolling.")]
        [Min(0f)] public float CastTime = 0f;

        // Whether using the ability now would do anything. Returning false keeps the
        // charge (e.g. Heal at full health). Checked when pressed and again after casting.
        public virtual bool CanExecute(AbilityContext ctx) => true;

        public abstract void Execute(AbilityContext ctx);

        // Damage and on-hit effects after resource scaling (ctx.Power and ctx.BonusEffects).
        protected static float Scaled(float damage, AbilityContext ctx) => damage * ctx.Power;

        protected static StatusEffectApplication[] WithBonusEffects(StatusEffectApplication[] own, AbilityContext ctx)
        {
            if (ctx.BonusEffects == null || ctx.BonusEffects.Length == 0) return own;
            if (own == null || own.Length == 0) return ctx.BonusEffects;

            var all = new StatusEffectApplication[own.Length + ctx.BonusEffects.Length];
            own.CopyTo(all, 0);
            ctx.BonusEffects.CopyTo(all, own.Length);
            return all;
        }
    }
}
