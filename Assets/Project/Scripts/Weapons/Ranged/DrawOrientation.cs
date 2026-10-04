namespace CGD.Weapons
{
    // How a bow is held. Toggled with the Weapon Mode key.
    public enum DrawOrientation
    {
        Vertical,     // upright: slower draw, more damage, tighter aim; multishot arrows stack vertically
        Horizontal    // canted: faster draw, less damage, looser aim; multishot arrows fan sideways
    }
}
