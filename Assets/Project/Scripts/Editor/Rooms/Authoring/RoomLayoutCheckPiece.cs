using CGD.Level;

namespace CGD.Editor
{
    // How one placement of a layout fared over a layout check's rooms.
    public sealed class RoomLayoutCheckPiece
    {
        public RoomLayoutCheckPiece(RoomPropPlacement placement, string source)
        {
            Placement = placement;
            Source    = source;
        }

        public RoomPropPlacement Placement { get; }
        // Where it comes from: Anchor, Set piece, a set's name, or Place.
        public string Source { get; }
        public string Name { get; private set; } = "(none)";
        public int Rooms { get; private set; }
        public int RoomsWithNone { get; private set; }
        public int Wanted { get; private set; }
        public int Placed { get; private set; }
        // The most recent reason it fell short, for the check's tooltip.
        public string Shortfall { get; private set; }

        // Share of the rooms it went down in at all.
        public float FitRate => Rooms > 0 ? 1f - (float)RoomsWithNone / Rooms : 0f;

        public void Add(PropPlacementOutcome outcome)
        {
            Name = outcome.Name;
            Rooms++;
            Wanted += outcome.Wanted;
            Placed += outcome.Placed;
            if (outcome.Placed == 0) RoomsWithNone++;
            if (outcome.IsShort || outcome.Relaxed) Shortfall = outcome.Explanation;
        }
    }
}
