namespace CGD.Map
{
    public static class MapNodeTypeExtensions
    {
        // Start, Boss and Exit are placed by the generator's layout pass, never by type
        // rules or guaranteed rooms.
        public static bool IsStructural(this MapNodeType type) =>
            type == MapNodeType.Start || type == MapNodeType.Boss || type == MapNodeType.Exit;

        // Rooms with a fight planned in them: enemies placed, waves, a guarded vault, a leak to
        // shut under fire. Every other type but the Boss is calm. The one list the generator's
        // pacing, the validator, the style learner and the map director all read.
        public static bool IsFight(this MapNodeType type) => type switch
        {
            MapNodeType.Combat or MapNodeType.Elite or MapNodeType.Breach or MapNodeType.Lockdown or MapNodeType.Holdout
                or MapNodeType.Ambush or MapNodeType.Stealth or MapNodeType.Rift or MapNodeType.Hazard or MapNodeType.Spawner
                or MapNodeType.Siege or MapNodeType.Gauntlet or MapNodeType.Haunt => true,
            _ => false,
        };

        // Breach and Rift rooms are held by two realities at once.
        public static bool HasSecondFaction(this MapNodeType type) =>
            type == MapNodeType.Breach || type == MapNodeType.Rift;

        // Rooms that play out as an encounter (sealing, waves, a puzzle, a leak). Every other
        // room is a normal one: its doors never lock, and its enemies just patrol.
        public static bool IsEncounter(this MapNodeType type) => type switch
        {
            MapNodeType.Lockdown or MapNodeType.Holdout or MapNodeType.Ambush or MapNodeType.Stealth
                or MapNodeType.Rift or MapNodeType.Puzzle or MapNodeType.Hazard or MapNodeType.Spawner or MapNodeType.Shrine or MapNodeType.Siege
                or MapNodeType.Gauntlet or MapNodeType.Haunt or MapNodeType.Boss => true,
            _ => false,
        };

        // What a door sign shows for the room beyond: an Ambush passes for a Treasure room.
        public static MapNodeType SignType(this MapNodeType type) =>
            type == MapNodeType.Ambush ? MapNodeType.Treasure : type;
    }
}
