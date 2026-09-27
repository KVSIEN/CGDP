namespace CGD.Stealth
{
    public enum StealthState
    {
        // In the open: normal detection rules.
        Visible,
        // Concealed, but just attacked or made noise — briefly as visible as in the open.
        Revealed,
        // Concealed: only enemies very close (or sharing the cover) can detect you.
        Hidden
    }
}
