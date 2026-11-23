namespace CGD.Enemies
{
    // Which side of its allies' line a supporter keeps to.
    public enum SupportLine
    {
        // Behind the ally nearest the threat: a medic, a warden, an enchanter.
        Behind,
        // Out in front of it, between the threat and its allies: a shield-bearer holding the line.
        Front,
    }
}
