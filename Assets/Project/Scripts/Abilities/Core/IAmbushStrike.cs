using CGD.Combat;

namespace CGD.Abilities
{
    // Lets an ability make its caster's next attack hit harder (Ambush). Enemies only: the
    // attack that uses it up, and ends the cloak, is the enemy's own.
    public interface IAmbushStrike
    {
        // The next attack within `seconds` deals `damageMultiplier` times its damage and also
        // applies `onHitEffects` (null for none) to whatever it hits.
        void Arm(float damageMultiplier, float seconds, StatusEffectApplication[] onHitEffects);
    }
}
