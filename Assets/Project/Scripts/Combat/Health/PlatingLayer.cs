using UnityEngine;

namespace CGD.Combat
{
    // How a hit meets armour plating, the layer between shield and health: the plates resist it
    // with their own Plating Armor on top of the character's armour, so a point of plating stands
    // up to more than a point of health — and armour penetration, which cuts all armour, cracks
    // plates fastest. What breaks through carries on at the character's own armour.
    public static class PlatingLayer
    {
        // The share of a hit, as resolved at `armor`, that is left once the plates' extra armour
        // has had its say too (1 = no extra armour, or a hit armour does nothing against).
        public static float PlatedShare(in DamageInfo info, float armor, float platingArmor, float trueShare)
        {
            if (platingArmor <= 0f) return 1f;

            float bare = info.ResolveDamage(armor, trueShare);
            return bare > 0f ? info.ResolveDamage(armor + platingArmor, trueShare) / bare : 1f;
        }

        // Takes what the plates can out of `amount` (damage at the character's own armour).
        // Returns what is left for the layer below; `absorbed` is the plating lost.
        public static float Absorb(float amount, float plating, float platedShare, out float absorbed)
        {
            absorbed = 0f;
            if (amount <= 0f || plating <= 0f) return amount;

            absorbed = Mathf.Min(plating, amount * platedShare);
            return platedShare > 0f ? Mathf.Max(0f, amount - absorbed / platedShare) : 0f;
        }
    }
}
