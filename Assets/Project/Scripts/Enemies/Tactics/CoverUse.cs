namespace CGD.Enemies
{
    // What an enemy wants a cover spot for.
    public enum CoverUse
    {
        // Somewhere to shoot from: low or high cover near its preferred range.
        Fight,
        // Somewhere to get out of the line of fire (hurt, reloading): high cover first.
        Hide,
    }
}
