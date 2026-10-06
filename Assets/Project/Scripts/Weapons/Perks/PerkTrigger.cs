namespace CGD.Weapons
{
    // What sets a weapon perk off. Perks only answer for the weapon in hand, and kills only
    // count when that weapon dealt the killing hit.
    public enum PerkTrigger
    {
        Kill  = 0,
        Parry = 1,
        Dodge = 2,
    }
}
