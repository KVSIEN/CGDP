namespace CGD.Settings
{
    // How the view zooms while aiming down sights.
    public enum AdsZoomMode
    {
        Gradual,   // the FOV narrows along with the aim-in (Battlefield)
        Snap,      // the FOV stays put until fully aimed in, then snaps to the zoom (Modern Warfare)
    }
}
