using System;
using System.Collections.Generic;
using UnityEngine;
using CGD.Core;

namespace CGD.Level
{
    // A miniature town: open streets and plazas with walled buildings standing among them. The
    // room is cut into a grid of lots by streets; each lot gets a building from the lot list
    // (BuildingPlan), walled all round with one front door onto a street. The streets stay
    // open — long sight lines and room to move — while everything inside a building is close
    // quarters, a doorway at a time.
    //
    // Lots that would swallow a doorway of the room, its centre spot or an earlier structure are
    // left open as plazas, and so are a share of the rest (Empty Lot Chance). Streets are
    // furnished by Street Function. Use it on a large room with an open ceiling.
    [CreateAssetMenu(fileName = "TownStructureRule", menuName = "CGD/Level/Structure/Town")]
    public class TownStructureRule : RoomStructureRule
    {
        private const RoomTileTags KeepOut = RoomTileTags.NearDoor | RoomTileTags.Centre | RoomTileTags.Structure;

        [Tooltip("The buildings the town is made of, by weight. A building only goes on a lot its plan fits")]
        [SerializeField] private TownLot[] _lots = Array.Empty<TownLot>();
        [Tooltip("Tiles along each side of a lot (the building's footprint)")]
        [SerializeField] private IntRange _lotSize = new(7, 10);
        [Tooltip("Tiles wide the streets between lots are (1 = 3 m with the default tiles)")]
        [SerializeField, Min(1)] private int _streetTiles = 2;
        [Tooltip("Tiles of street kept between the outer lots and the room's walls, so the way in from a corridor is a street")]
        [SerializeField, Min(1)] private int _margin = 2;
        [Tooltip("Chance a lot is left as an open plaza instead of getting a building")]
        [SerializeField, Range(0f, 1f)] private float _emptyLotChance = 0.15f;
        [Tooltip("What furnishes the streets and plazas (benches, lamp posts, parked carts). Empty = bare streets")]
        [SerializeField] private RoomFunction _streetFunction;

        public override void Apply(RoomStructure structure, RandomStream random)
        {
            RectInt bounds = structure.Footprint.Bounds;
            List<(int start, int size)> columns = Spans(bounds.xMin + _margin, bounds.xMax - _margin, random);
            List<(int start, int size)> rows    = Spans(bounds.yMin + _margin, bounds.yMax - _margin, random);

            var built = new List<RectInt>();
            foreach ((int x, int width) in columns)
                foreach ((int y, int depth) in rows)
                {
                    var lot = new RectInt(x, y, width, depth);
                    if (random.Chance(_emptyLotChance) || !IsClear(structure, lot)) continue;
                    if (TryBuild(structure, lot, random)) built.Add(lot);
                }

            if (_streetFunction != null && TryFirstStreet(structure, built, out Vector2Int street))
                structure.AddSpace(street, _streetFunction, "Street");
        }

        // Lots along one axis: spans of the lot size with a street between each, starting and
        // ending a margin in.
        private List<(int start, int size)> Spans(int from, int to, RandomStream random)
        {
            var spans = new List<(int, int)>();
            int least = Mathf.Max(BuildingPlan.MinRoomTiles * 2, _lotSize.Min);
            for (int at = from; to - at >= least;)
            {
                int remaining = to - at;
                int size = Mathf.Clamp(_lotSize.Evaluate(random), least, remaining);
                // What is left after this lot is too little for another: the lot takes it if that
                // keeps it within the lot size, otherwise it stays open ground.
                if (remaining - size - _streetTiles < least && remaining <= Mathf.Max(_lotSize.Max, least)) size = remaining;
                spans.Add((at, size));
                at += size + _streetTiles;
            }
            return spans;
        }

        private static bool IsClear(RoomStructure structure, RectInt lot)
        {
            foreach (Vector2Int tile in lot.allPositionsWithin)
                if (!structure.Contains(tile) || structure.Has(tile, KeepOut)) return false;
            return true;
        }

        // A building from the lot list, its front door on a side that opens onto the street.
        private bool TryBuild(RoomStructure structure, RectInt lot, RandomStream random)
        {
            BuildingPlan plan = PickPlan(lot, random);
            if (plan == null) return false;

            var planner = new BuildingLayoutPlanner(plan);
            var fronts = new List<Vector2Int>(BuildingLayoutPlanner.Fronts);
            random.Shuffle(fronts);
            foreach (Vector2Int front in fronts)
            {
                BuildingBlueprint blueprint = planner.Plan(lot, front, random);
                if (blueprint == null || !structure.Contains(blueprint.FrontDoor.B)) continue;
                if (BuildingBlueprintWriter.Write(structure, blueprint, shell: true, keepRoutesOpen: false)) return true;
            }
            return false;
        }

        private BuildingPlan PickPlan(RectInt lot, RandomStream random)
        {
            var fitting = new List<TownLot>();
            foreach (TownLot candidate in _lots)
                if (candidate != null && candidate.Plan != null && candidate.Weight > 0f
                    && (candidate.Plan.Fits(lot.height, lot.width) || candidate.Plan.Fits(lot.width, lot.height)))
                    fitting.Add(candidate);
            return fitting.Count > 0 ? random.PickWeighted(fitting, l => l.Weight).Plan : null;
        }

        private static bool TryFirstStreet(RoomStructure structure, List<RectInt> built, out Vector2Int street)
        {
            foreach (Vector2Int tile in structure.Footprint.Tiles)
            {
                if (built.Exists(lot => lot.Contains(tile))) continue;
                street = tile;
                return true;
            }
            street = default;
            return false;
        }
    }
}
