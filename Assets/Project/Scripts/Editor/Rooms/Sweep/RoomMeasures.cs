using CGD.Level;
using UnityEngine;

namespace CGD.Editor
{
    // A sweep result's value on a measure, from 0 to 1 so every pair plots on the same square.
    public static class RoomMeasures
    {
        // Lanes longer than this all plot at the top.
        public const int LaneCap = RoomTraitMeasure.LaneHighTiles * 2;

        public static readonly RoomMeasure[] All =
            { RoomMeasure.Concealed, RoomMeasure.Narrow, RoomMeasure.Lane, RoomMeasure.Revealed, RoomMeasure.Filled };

        // False when the room has no value on it (a room with no way in reveals nothing).
        public static bool TryGet(RoomSweepResult room, RoomMeasure measure, out float value)
        {
            value = measure switch
            {
                RoomMeasure.Concealed => room.Traits.ConcealedShare,
                RoomMeasure.Narrow    => room.Traits.NarrowShare,
                RoomMeasure.Lane      => Mathf.Clamp01(room.Traits.LongestLane / (float)LaneCap),
                RoomMeasure.Revealed  => room.Flow.Revealed,
                _                     => room.Flow.Filled,
            };
            return measure != RoomMeasure.Revealed || room.Flow.HasArrival;
        }

        public static string Name(RoomMeasure measure) => measure switch
        {
            RoomMeasure.Concealed => "concealed %",
            RoomMeasure.Narrow    => "narrow %",
            RoomMeasure.Lane      => $"lane (0–{LaneCap} tiles)",
            RoomMeasure.Revealed  => "seen from the way in %",
            _                     => "filled %",
        };
    }
}
