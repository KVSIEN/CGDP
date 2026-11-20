namespace CGD.Level
{
    // Which part of a room's flow a placement goes in (RoomFlowPlanner). In a room with no
    // arrival (no doorway leads back toward Start) every band is the whole room.
    public enum RoomFlowBand
    {
        Any,
        Threshold,       // just inside the way in: a foothold
        PastThreshold,   // anywhere but the threshold
        Goal,            // the far end: the reward, the landmark at the back
    }
}
