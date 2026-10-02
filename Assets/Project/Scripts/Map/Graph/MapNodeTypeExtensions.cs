namespace CGD.Map
{
    public static class MapNodeTypeExtensions
    {
        // Start, Boss and Exit are placed by the generator's layout pass, never by type
        // rules or guaranteed rooms.
        public static bool IsStructural(this MapNodeType type) =>
            type == MapNodeType.Start || type == MapNodeType.Boss || type == MapNodeType.Exit;
    }
}
