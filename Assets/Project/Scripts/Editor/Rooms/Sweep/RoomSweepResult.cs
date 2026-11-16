using CGD.Level;

namespace CGD.Editor
{
    // One room a sweep built: which place, held by which faction, from which seed, what it
    // measured, and how far that is from what the faction wants.
    public sealed class RoomSweepResult
    {
        public RoomSweepResult(string place, string faction, string seed, RoomTraitReading traits, FactionDesign design,
                               int attempt, int tries)
        {
            Place    = place;
            Faction  = faction;
            Seed     = seed;
            Traits   = traits;
            Design   = design;
            Attempt  = attempt;
            Tries    = tries;
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
        // Levels off the design's priorities, summed over the traits; -1 without a design.
        public int              Miss    { get; }
    }
}
