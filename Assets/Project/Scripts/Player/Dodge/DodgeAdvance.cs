namespace CGD.Player
{
    // What happens when a dodge stage's movement ends.
    public enum DodgeAdvance
    {
        Automatic,    // straight into the next stage (or the dodge ends)
        OnDodgePress, // the next stage only if Dodge is pressed again in time; otherwise the dodge ends
    }
}
