namespace CGD.Weapons
{
    // What sets a grenade off.
    public enum FuseMode
    {
        // Goes off a fixed time after the throw, wherever it is.
        Timed,
        // Goes off the first time it hits something after arming; the fuse time is only a
        // safety limit for one that never lands.
        OnImpact,
    }
}
