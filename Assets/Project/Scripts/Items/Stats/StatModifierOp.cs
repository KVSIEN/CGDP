namespace CGD.Items
{
    public enum StatModifierOp
    {
        // Flat delta applied to the base value.
        Additive = 0,
        // Fraction of the base value: 0.15 = +15%, -0.10 = -10%.
        Multiplicative = 1,
    }
}
