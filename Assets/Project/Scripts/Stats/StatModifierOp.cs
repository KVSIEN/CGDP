namespace CGD.Stats
{
    // How a modifier combines with a stat. Final value:
    //   Override present → the most recent Override
    //   otherwise        → (base + ΣAdditive) × (1 + ΣMultiplicative) × Π(1 + Compound)
    // Values are explicit because assets store them.
    public enum StatModifierOp
    {
        // Flat delta applied to the base value.
        Additive = 0,
        // Fraction of the base value, summed with other Multiplicative modifiers:
        // two +10% make +20%.
        Multiplicative = 1,
        // Fraction applied on top of everything else, compounding with other Compound
        // modifiers: two +10% make +21%. For global multipliers like difficulty.
        Compound = 2,
        // Replaces the value outright (a "fixed at 0" debuff, a god-mode cheat).
        Override = 3,
    }
}
