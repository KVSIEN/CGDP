namespace CGD.Level
{
    // What a raised or sunken area is for in play, which decides how it is guarded and reached
    // (ElevationAccess). The kind says what it is built as; the role, how it is fought over.
    public enum ElevationRole
    {
        // The floor just changes height: texture that keeps people moving. Its ledges are open
        // (or railed, by the rule's rail chance) and its stairs stand where the routes cross.
        SplitLevel,
        // High ground that sees a lot, so taking it has to cost something: a guard along the side
        // facing into the room (the side with the most ledge), the other sides left open to
        // climb. One way up when small, two when not, on different sides.
        Perch,
        // A position held on purpose: guarded all round, so its stairs are the only ways on, two
        // at least and on different sides, so it can be flanked.
        Bastion,
        // Low ground: open (or railed) rim, and two ways out at least, apart, so it is never a dead end.
        Pit,
        // A platform the room is arranged to face: against the wall across from the way in,
        // centred on it, reached by one wide flight up the middle of its front or a mirrored pair
        // at its ends (a chancel, a command dais, a lounge deck under the big window). Open or
        // railed like a split level. Platforms only: anything else takes it as a split level.
        Stage,
    }
}
