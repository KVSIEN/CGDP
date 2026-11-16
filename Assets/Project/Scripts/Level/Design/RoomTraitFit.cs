using CGD.Core;

namespace CGD.Level
{
    // How generation follows a faction's priorities in its fight rooms: concealment props are
    // scaled by the concealment priority, and the structure and the furnishing are each tried
    // from a few seeds, keeping the try whose measured traits miss the priorities least.
    // Try 0 is the room's own seed and only a strictly better try replaces it, so a room that
    // already fits is built exactly as it always was.
    public static class RoomTraitFit
    {
        // Rooms that hold enemies: the ones a faction's priorities are about.
        public static bool IsFightRoom(RoomContentRule rule) => rule != null && rule.EnemyCount.Max > 0;

        public static bool Steers(RoomContentRule rule, FactionDesign design) => design != null && IsFightRoom(rule);

        // How many tries a room gets: several for a fight room a faction design holds, else one.
        public static int TriesFor(LevelRoom room, RoomContentRule rule, FactionDesign design, LevelBuildSettings settings) =>
            room.Landmark == null && Steers(rule, design) ? System.Math.Max(1, settings.PriorityTries) : 1;

        // The seed of one try: the room's own seed first, then seeds derived from it.
        public static Seed Try(Seed seed, int attempt) => attempt == 0 ? seed : seed.Derive("try").Derive(attempt);

        // Levels off target, summed over the traits: 0 is a perfect fit. Without concealment,
        // for a bare structure, whose concealment only its props will bring.
        public static int Miss(RoomTraitReading reading, RoomTraitPriorities wanted, bool countConcealment = true)
        {
            int miss = Off(reading, wanted, RoomTrait.Walls) + Off(reading, wanted, RoomTrait.LinesOfFire);
            return countConcealment ? miss + Off(reading, wanted, RoomTrait.Concealment) : miss;
        }

        // What concealment placements' counts are multiplied by.
        public static float ConcealmentScale(TraitPriority priority) => priority switch
        {
            TraitPriority.Low  => 0.5f,
            TraitPriority.High => 1.5f,
            _                  => 1f,
        };

        private static int Off(RoomTraitReading reading, RoomTraitPriorities wanted, RoomTrait trait) =>
            System.Math.Abs((int)reading.LevelOf(trait) - (int)wanted.Of(trait));
    }
}
