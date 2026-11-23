namespace CGD.Enemies
{
    // How far from its target an enemy likes to fight. Each faction gives its attackers, tanks and
    // supports a different band so a squad spreads over the whole fight (see FEATURES.md, Range
    // roles): BIO attackers close, tanks mid, supports long; TECH supports close, attackers mid,
    // tanks long; VOID tanks close, supports mid, attackers long.
    public enum CombatRange
    {
        Close,
        Mid,
        Long,
    }
}
