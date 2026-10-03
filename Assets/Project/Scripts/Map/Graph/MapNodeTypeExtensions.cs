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

        // What a door sign shows for the room beyond: an Ambush passes for a Treasure room.
        public static MapNodeType SignType(this MapNodeType type) =>
            type == MapNodeType.Ambush ? MapNodeType.Treasure : type;
    }
}
