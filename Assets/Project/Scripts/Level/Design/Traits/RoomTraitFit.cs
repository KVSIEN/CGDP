using UnityEngine;
using CGD.Core;

namespace CGD.Level
{
    // How generation follows a faction's priorities in its fight rooms: concealment props are
    // scaled by the concealment priority, and the structure and the furnishing are each tried
    // from a few seeds, keeping the try whose measured traits miss the priorities least. A
    // furnishing that misses is then refined (RoomPlanRefiner): its cover moved about until it fits.
    // Try 0 is the room's own seed and only a strictly better try replaces it, so a room that
    // already fits is built exactly as it always was.
    public static class RoomTraitFit
    {
        // What a trait off its level costs at least, however close it is to the wanted one.
        private const float LevelOffGap = 0.01f;

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

        // How far a room is from its priorities, as Miss but smooth: a trait off by a level counts
        // between 0 and 1 for how close it is to the wanted level, then 1 more for each further
        // level. It is 0 exactly when Miss is 0, so a search can follow it where Miss, which only
        // changes when a trait crosses a band, gives it nothing to follow.
        public static float Gap(RoomTraitReading reading, RoomTraitPriorities wanted, bool countConcealment = true)
        {
            float gap = TraitGap(reading, wanted, RoomTrait.Walls) + TraitGap(reading, wanted, RoomTrait.LinesOfFire);
            return countConcealment ? gap + TraitGap(reading, wanted, RoomTrait.Concealment) : gap;
        }

        // The stream a try's furnishing is refined with (RoomPlanRefiner): its own, so refining
        // draws nothing from the one the furnishing was planned with.
        public static RandomStream Refinement(Seed attemptSeed) => attemptSeed.Derive("refine").Stream();

        // What concealment placements' counts are multiplied by.
        public static float ConcealmentScale(TraitPriority priority) => priority switch
        {
            TraitPriority.Low  => 0.5f,
            TraitPriority.High => 1.5f,
            _                  => 1f,
        };

        private static int Off(RoomTraitReading reading, RoomTraitPriorities wanted, RoomTrait trait) =>
            System.Math.Abs((int)reading.LevelOf(trait) - (int)wanted.Of(trait));

        // The distance from the room's position on the trait's scale to the wanted level's band,
        // plus a little while the level is still wrong: a position on a band's edge belongs to the
        // next level, and a gap of 0 must mean no miss.
        private static float TraitGap(RoomTraitReading reading, RoomTraitPriorities wanted, RoomTrait trait)
        {
            int target = (int)wanted.Of(trait);
            if ((int)reading.LevelOf(trait) == target) return 0f;

            float position = reading.PositionOf(trait);
            float distance = position < target ? target - position : position - (target + 1);
            return Mathf.Max(distance, 0f) + LevelOffGap;
        }
    }
}
