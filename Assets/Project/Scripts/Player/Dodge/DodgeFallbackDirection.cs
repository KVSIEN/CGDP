namespace CGD.Player
{
    // Where a dodge goes when started without movement input.
    public enum DodgeFallbackDirection
    {
        Backward,   // away from where the camera looks — a backstep
        Forward,    // where the camera looks
        None,       // no dodge without a direction
    }
}
