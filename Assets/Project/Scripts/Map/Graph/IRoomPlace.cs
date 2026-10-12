namespace CGD.Map
{
    // A place a room can be pinned to (a lobby, the bridge). The level builder's RoomFunction
    // implements it; the map graph only needs its name, its category and whether a map may
    // hold more than one, so it doesn't depend on the level builder.
    public interface IRoomPlace
    {
        string       DisplayName { get; }
        RoomCategory Category    { get; }
        bool         IsUnique    { get; }
    }
}
