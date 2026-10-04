namespace CGD.Map
{
    public static class MapNodeTypeExtensions
    {
        // Start, Boss and Exit are placed by the generator's layout pass, never by type
        // rules or guaranteed rooms.
        public static bool IsStructural(this MapNodeType type) =>
            type == MapNodeType.Start || type == MapNodeType.Boss || type == MapNodeType.Exit;

        // Breach and Rift rooms are held by two realities at once.
        public static bool HasSecondFaction(this MapNodeType type) =>
            type == MapNodeType.Breach || type == MapNodeType.Rift;

        // Rooms that play out as an encounter (sealing, waves, a puzzle, a leak). Every other
        // room is a normal one: its doors never lock, and its enemies just patrol.
        public static bool IsEncounter(this MapNodeType type) => type switch
        {
            MapNodeType.Lockdown or MapNodeType.Holdout or MapNodeType.Ambush or MapNodeType.Stealth
                or MapNodeType.Rift or MapNodeType.Puzzle or MapNodeType.Hazard => true,
            _ => false,
        };

        // What a door sign shows for the room beyond: an Ambush passes for a Treasure room.
        public static MapNodeType SignType(this MapNodeType type) =>
            type == MapNodeType.Ambush ? MapNodeType.Treasure : type;
    }
}
