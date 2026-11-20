namespace CGD.Editor
{
    // What a sweep can plot a room by (RoomMeasures, ExpressiveRange).
    public enum RoomMeasure
    {
        Concealed,   // share of the floor under concealment
        Narrow,      // share of the floor at most a couple of tiles across
        Lane,        // longest open lane, against twice the High band
        Revealed,    // share of the open floor seen from the way in
        Filled,      // share of the floor props stand on
    }
}
