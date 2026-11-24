using CGD.Level;

namespace CGD.Editor
{
    // One room a sweep built: which place, held by which faction, from which seed, what it
    // measured (its traits, how it plays from the way in and how far it can be seen across),
    // and how far that is from what the faction wants.
    public sealed class RoomSweepResult
    {
        public RoomSweepResult(string place, string faction, string seed, RoomTraitReading traits, FactionDesign design,
                               int attempt, int tries, RoomFlowReading flow, bool? showcaseSeen, RoomSightReading sight = default,
                               RoomTraitReading rows = default, RoomTraitReading everyDirection = default)
        {
            Place    = place;
            Faction  = faction;
            Seed     = seed;
            Traits   = traits;
            Design   = design;
            Attempt  = attempt;
            Tries    = tries;
            Flow     = flow;
            ShowcaseSeen = showcaseSeen;
            Sight    = sight;
            Rows     = rows;
            EveryDirection = everyDirection;
            Miss     = design != null ? RoomTraitFit.Miss(traits, design.Priorities) : -1;
            RowsMiss = design != null ? RoomTraitFit.Miss(rows, design.Priorities) : -1;
            EveryDirectionMiss = design != null ? RoomTraitFit.Miss(everyDirection, design.Priorities) : -1;
        }

        public string           Place   { get; }
        public string           Faction { get; }
        public string           Seed    { get; }
        public RoomTraitReading Traits  { get; }
        // The holding faction's design, or null (nothing to compare with).
        public FactionDesign    Design  { get; }
        // The furnishing try kept, of how many.
        public int              Attempt { get; }
        public int              Tries   { get; }
        public RoomFlowReading  Flow    { get; }
        // How far and how widely a person sees in it, in every direction (RoomVisibility).
        public RoomSightReading Sight   { get; }
        // Whether the place's defining piece (a Showcase placement) can be seen from the way
        // in; null when the place has none or the room has no way in.
        public bool?            ShowcaseSeen { get; }
        // The same finished room's traits read both ways, whichever the build settings steered by
        // (IsovistUse.Traits): along rows and columns, and from what can be seen in every
        // direction, with how far each is from the design's priorities (-1 without a design).
        // To compare the two readings fairly, sweep with Traits off: generation then steers by
        // the row-and-column reading and the other is read off the same rooms.
        public RoomTraitReading Rows    { get; }
        public RoomTraitReading EveryDirection { get; }
        public int              RowsMiss { get; }
        public int              EveryDirectionMiss { get; }
        // Levels off the design's priorities, summed over the traits; -1 without a design.
        public int              Miss    { get; }
    }
}
