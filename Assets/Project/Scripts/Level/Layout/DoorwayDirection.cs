namespace CGD.Level
{
    // Which way a doorway leads along the run, from the map's depth (steps from Start) of the
    // two rooms it joins: back toward Start (the way a player comes in), onward away from it,
    // or level (a loop between rooms equally deep, or a map with no Start).
    public enum DoorwayDirection
    {
        Level,
        Back,
        Onward,
    }
}
