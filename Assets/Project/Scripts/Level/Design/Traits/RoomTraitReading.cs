using UnityEngine;

namespace CGD.Level
{
    // A room's RoomTraitMeasure scores, and each trait's level by the measure's bands.
    public readonly struct RoomTraitReading
    {
        // How far through a level's band a score reads at most, so a position never reaches the next level's.
        private const float Almost = 0.999f;
        // The lane length at which a lane reads as far into High as it goes.
        private const float HighLaneTiles = 12f;

        public RoomTraitReading(float concealedShare, float narrowShare, int longestLane, RectInt lane, bool laneHasCover)
        {
            ConcealedShare = concealedShare;
            NarrowShare    = narrowShare;
            LongestLane    = longestLane;
            Lane           = lane;
            LaneHasCover   = laneHasCover;
        }

        // Shares of the walkable floor, 0–1.
        public float   ConcealedShare { get; }
        public float   NarrowShare    { get; }
        // Tiles, and the strip they make.
        public int     LongestLane    { get; }
        public RectInt Lane           { get; }
        public bool    LaneHasCover   { get; }

        public TraitPriority LevelOf(RoomTrait trait) => (TraitPriority)(int)Mathf.Floor(PositionOf(trait));

        // Where a trait's score lies on a scale of levels: Low is 0 up to 1, Medium 1 up to 2,
        // High 2 up to 3, with the score's progress through its band as the fraction. The level
        // is the whole part. A position says how close a room is to the next level, which the
        // level alone can't: it is what lets generation tell a nearly-right room from a far-off one.
        public float PositionOf(RoomTrait trait) => trait switch
        {
            RoomTrait.Concealment => ConcealedShare <= 0f ? 0f
                                   : ConcealedShare < RoomTraitMeasure.ConcealmentHighShare
                                       ? 1f + Along(ConcealedShare, 0f, RoomTraitMeasure.ConcealmentHighShare)
                                       : 2f + Along(ConcealedShare, RoomTraitMeasure.ConcealmentHighShare, 2f * RoomTraitMeasure.ConcealmentHighShare),
            RoomTrait.Walls       => NarrowShare < RoomTraitMeasure.WallsMediumShare ? Along(NarrowShare, 0f, RoomTraitMeasure.WallsMediumShare)
                                   : NarrowShare < RoomTraitMeasure.WallsHighShare
                                       ? 1f + Along(NarrowShare, RoomTraitMeasure.WallsMediumShare, RoomTraitMeasure.WallsHighShare)
                                       : 2f + Along(NarrowShare, RoomTraitMeasure.WallsHighShare, 1f),
            _                     => LinesOfFirePosition(),
        };

        // An open lane with nothing beside it counts a level lower: it is a killing field, not a firing line.
        private float LinesOfFirePosition()
        {
            float position = LongestLane < RoomTraitMeasure.LaneMediumTiles ? Along(LongestLane, 0f, RoomTraitMeasure.LaneMediumTiles)
                           : LongestLane < RoomTraitMeasure.LaneHighTiles
                               ? 1f + Along(LongestLane, RoomTraitMeasure.LaneMediumTiles, RoomTraitMeasure.LaneHighTiles)
                               : 2f + Along(LongestLane, RoomTraitMeasure.LaneHighTiles, HighLaneTiles);
            return !LaneHasCover && position >= 1f ? position - 1f : position;
        }

        private static float Along(float value, float from, float to) => Mathf.Min((value - from) / (to - from), Almost);
    }
}
