namespace CGD.Level
{
    // What a raised or sunken piece of floor is, which decides where it goes and how it is reached.
    public enum ElevationKind
    {
        Platform,     // one level up, set against a wall: a stage, a mezzanine, a loading deck
        SunkenFloor,  // one level down, out in the room with floor all round it: a pit, a lounge well
    }
}
