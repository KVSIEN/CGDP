namespace CGD.Combat
{
    // How a DamageZone's tint moves while it lasts; every style fades out over the last second.
    public enum ZoneGlow
    {
        Steady,
        Flicker, // fast and uneven, like flames
        Pulse    // slow breathing
    }
}
