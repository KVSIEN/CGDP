namespace CGD.Combat
{
    public enum SwingAnchor
    {
        // The swing is measured from where the wielder aims.
        Aim,
        // The swing is measured from the wielder's weapon socket, so an animated weapon carries
        // the hitbox with it. Falls back to Aim when the wielder has no socket.
        Socket,
    }
}
