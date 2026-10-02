namespace CGD.Settings
{
    // Whether the player's field of view setting changes how wide the view is while aiming.
    public enum AdsFovMode
    {
        Independent,   // each weapon zooms to its own fixed FOV, whatever the hip FOV
        Affected,      // each weapon keeps its magnification, so a wider hip FOV aims wider too
    }
}
