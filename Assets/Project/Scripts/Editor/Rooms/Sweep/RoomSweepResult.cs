using CGD.Level;

namespace CGD.Editor
{
    // One room a sweep built: which place, held by which faction, from which seed, what it
    // measured (its traits and how it plays from the way in), and how far that is from what
    // the faction wants.
    public sealed class RoomSweepResult
    {
        public RoomSweepResult(string place, string faction, string seed, RoomTraitReading traits, FactionDesign design,
                               int attempt, int tries, RoomFlowReading flow, bool? showcaseSeen)
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
            Miss     = design != null ? RoomTraitFit.Miss(traits, design.Priorities) : -1;
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
        // Whether the place's defining piece (a Showcase placement) can be seen from the way
        // in; null when the place has none or the room has no way in.
        public bool?            ShowcaseSeen { get; }
        // Levels off the design's priorities, summed over the traits; -1 without a design.
        public int              Miss    { get; }
    }
}
