namespace CGD.Map
{
    // How a layout grows its rooms.
    public enum MapGrowth
    {
        // A main path walked out from Start to the Boss, with branches hung off it and a
        // set number of loops on top: tight control over path length and pacing.
        PathFirst,
        // Rooms spread out from Start in every direction, linking up wherever they meet;
        // the Boss is placed last, at the far side of what grew. A web rather than a
        // spine, so the way to the Boss is never obvious.
        Organic,
    }
}
