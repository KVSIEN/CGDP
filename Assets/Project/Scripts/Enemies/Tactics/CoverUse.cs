namespace CGD.Enemies
{
    // What an enemy wants a cover spot for.
    public enum CoverUse
    {
        // Somewhere to shoot from: low or high cover near its preferred range.
        Fight,
        // Somewhere out of sight of the threat (hurt, recharging, breaking contact): high cover only.
        Hide,
        // The next spot on the way in: cover clearly closer to the threat than it stands now,
        // so a close-range fighter can cross open ground in short dashes.
        Advance,
    }
}
