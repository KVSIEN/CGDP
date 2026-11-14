using UnityEngine;

namespace CGD.Level
{
    // A room's RoomTraitMeasure scores, and each trait's level by the measure's bands.
    public readonly struct RoomTraitReading
    {
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

        public TraitPriority LevelOf(RoomTrait trait) => trait switch
        {
            RoomTrait.Concealment => ConcealedShare <= 0f ? TraitPriority.Low
                                   : ConcealedShare < RoomTraitMeasure.ConcealmentHighShare ? TraitPriority.Medium : TraitPriority.High,
            RoomTrait.Walls       => NarrowShare < RoomTraitMeasure.WallsMediumShare ? TraitPriority.Low
                                   : NarrowShare < RoomTraitMeasure.WallsHighShare ? TraitPriority.Medium : TraitPriority.High,
            _                     => LinesOfFire(),
        };

        private TraitPriority LinesOfFire()
        {
            TraitPriority level = LongestLane < RoomTraitMeasure.LaneMediumTiles ? TraitPriority.Low
                                : LongestLane < RoomTraitMeasure.LaneHighTiles ? TraitPriority.Medium : TraitPriority.High;
            return !LaneHasCover && level > TraitPriority.Low ? level - 1 : level;
        }
    }
}
