using System.Collections.Generic;
using UnityEngine;
using CGD.Core;

namespace CGD.Level
{
    // Splits the room into lanes at different floor levels, side by side: a hall split in two
    // (1-0), raised walks along both walls (1-0-1), a raised middle (0-1-0), a court sunk below
    // its edges (0 -1 0). Lanes run wall to wall, with flights of stairs wherever a route from
    // a doorway crosses a ledge and at even spacing along every ledge, (when Railed) a railing
    // along the rest of it, and (Bridges) strips of floor straight across a sunken lane.
    //
    // The doorways decide how it sits. Every doorway must open onto level 0, so the profile is
    // turned and shifted (1-0-1 becomes 0 -1 0, the raised walks now level with the doorways and
    // the middle sunk) until each doorway's lane is on level 0; floor just in front of a doorway
    // that falls in another lane stays on level 0 as a landing. A room no turn or shift fits is
    // left flat. Runs early in a layout's structure: pillars and walls placed after it stand on
    // the levels it made.
    //
    // Does nothing, and draws no random number, in a room that allows no level but 0.
    [CreateAssetMenu(fileName = "TerracesStructureRule", menuName = "CGD/Level/Structure/Terraces")]
    public class TerracesStructureRule : RoomStructureRule
    {
        private const int MinLaneTiles = 2;
        // Flights along one ledge keep at least this many tiles apart.
        private const int StairSpacingMin = 3;
        private static readonly int[] Shifts = { 0, -1, 1 };

        [Tooltip("The lanes from one wall to the other. Neighbouring lanes may differ by one level at most")]
        [SerializeField] private TerraceLane[] _lanes = { new(1, 1f), new(0, 2f), new(1, 1f) };
        [SerializeField] private TerraceDirection _direction = TerraceDirection.AlongLength;
        [Tooltip("Turn the whole profile a level up or down when that puts the doorways on level 0. Off: the profile is used as written, or not at all")]
        [SerializeField] private bool _shiftToDoorways = true;
        [Tooltip("Tiles of ledge for each flight of stairs. Every ledge gets one at least, and one wherever a route from a doorway crosses it")]
        [SerializeField, Min(StairSpacingMin)] private int _stairSpacing = 6;
        [Tooltip("A railing along the ledges between the stairs: nobody drops off. Off: open ledges, dropped off one way")]
        [SerializeField] private bool _railed = true;
        [Tooltip("Strips of floor level with the room straight across each sunken lane that has floor on both sides, spread evenly along it: bridges over a sunken court. Each piece of lane they leave gets its own stairs")]
        [SerializeField, Range(0, 3)] private int _bridges;

        public override void Apply(RoomStructure structure, RandomStream random)
        {
            if (structure.LowestLevel == 0 && structure.HighestLevel == 0) return;
            if (!IsStepped(_lanes)) return;

            RectInt bounds = structure.Footprint.Bounds;
            bool longAlongX = bounds.width >= bounds.height;
            // Lanes running along x are stacked up y.
            bool stackedAlongY = _direction switch
            {
                TerraceDirection.AlongLength  => longAlongX,
                TerraceDirection.AcrossLength => !longAlongX,
                _                             => random.Chance(0.5f),
            };

            foreach (bool alongY in _direction == TerraceDirection.Either ? new[] { stackedAlongY, !stackedAlongY } : new[] { stackedAlongY })
            {
                int[] starts = LaneStarts(alongY ? bounds.yMin : bounds.xMin, alongY ? bounds.height : bounds.width);
                if (starts == null) continue;

                foreach (int shift in _shiftToDoorways ? Shifts : new[] { 0 })
                {
                    var layout = new TerraceLayout(structure, _lanes, starts, alongY, shift);
                    if (!layout.Fits()) continue;
                    if (structure.TryAddElevations(layout.Areas(_stairSpacing, StairSpacingMin, _railed, _bridges))) return;
                }
            }
        }

        // Neighbouring lanes no more than a level apart, and some lane off level 0.
        private static bool IsStepped(TerraceLane[] lanes)
        {
            if (lanes == null || lanes.Length < 2) return false;
            for (int i = 1; i < lanes.Length; i++)
                if (Mathf.Abs(lanes[i].Level - lanes[i - 1].Level) > 1) return false;
            foreach (TerraceLane lane in lanes)
                if (lane.Level != lanes[0].Level) return true;
            return false;
        }

        // Where each lane starts across the room, sharing `span` tiles out by weight with every
        // lane at least MinLaneTiles wide; null when the room is too narrow for them.
        private int[] LaneStarts(int from, int span)
        {
            int count = _lanes.Length;
            if (span < count * MinLaneTiles) return null;

            float total = 0f;
            foreach (TerraceLane lane in _lanes) total += Mathf.Max(0.1f, lane.Weight);

            var widths = new int[count];
            int used = 0;
            for (int i = 0; i < count; i++)
            {
                widths[i] = Mathf.Max(MinLaneTiles, Mathf.RoundToInt(span * Mathf.Max(0.1f, _lanes[i].Weight) / total));
                used += widths[i];
            }
            // Rounding leaves a tile or two over or short: the widest lane gives or takes them.
            while (used != span)
            {
                int widest = 0;
                for (int i = 1; i < count; i++) if (widths[i] > widths[widest]) widest = i;
                if (used > span && widths[widest] <= MinLaneTiles) return null;
                widths[widest] += used < span ? 1 : -1;
                used += used < span ? 1 : -1;
            }

            var starts = new int[count + 1];
            starts[0] = from;
            for (int i = 0; i < count; i++) starts[i + 1] = starts[i] + widths[i];
            return starts;
        }
    }
}
