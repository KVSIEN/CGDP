using System.Collections.Generic;
using CGD.Algorithms;
using UnityEngine;

namespace CGD.Level
{
    // One way a terraced room could sit (TerracesStructureRule): its lanes across the room, which
    // way they run, and the shift that turns the profile's levels into the room's. Works out the
    // raised and sunken areas and the stairs between them; RoomStructure.TryAddElevations has the
    // last word on whether they stand.
    internal sealed class TerraceLayout
    {
        private const RoomTileTags NotOnStairs = RoomTileTags.NearDoor | RoomTileTags.Centre;

        private readonly RoomStructure _structure;
        private readonly int[] _starts;        // where each lane starts across the room; one more for the far wall
        private readonly int[] _levels;        // each lane's level, shifted
        private readonly bool _stackedAlongY;  // lanes run along x, one above the other in y
        // The tiles each raised or sunken run of lanes takes (null for a run on level 0).
        private readonly List<(int first, int last, int level)> _runs = new();
        private readonly Dictionary<Vector2Int, int> _levelAfter = new();

        public TerraceLayout(RoomStructure structure, TerraceLane[] lanes, int[] starts, bool stackedAlongY, int shift)
        {
            _structure     = structure;
            _starts        = starts;
            _stackedAlongY = stackedAlongY;
            _levels        = new int[lanes.Length];
            for (int i = 0; i < lanes.Length; i++) _levels[i] = lanes[i].Level + shift;

            // Lanes side by side on one level are one run: one piece of floor, no ledge between.
            for (int i = 0; i < _levels.Length; i++)
            {
                if (_runs.Count > 0 && _runs[^1].level == _levels[i]) _runs[^1] = (_runs[^1].first, i, _levels[i]);
                else _runs.Add((i, i, _levels[i]));
            }
        }

        // Every lane's level is one the room allows (under its ceiling too), and every doorway opens onto level 0.
        public bool Fits()
        {
            foreach (int level in _levels)
                if (!_structure.AllowsFloor(level)) return false;
            foreach (Vector2Int entrance in _structure.Entrances)
                if (_levels[LaneOf(entrance)] != 0) return false;
            return true;
        }

        // The runs off level 0, each with the flights onto or off it. Floor just in front of a
        // doorway, and floor something already stands on, stays on level 0, and so does each of
        // `bridges` strips straight across a sunken run that has floor on both sides of it.
        public List<ElevatedArea> Areas(int stairSpacing, int minGap, bool railed, int bridges = 0)
        {
            HashSet<Vector2Int> bridged = Bridges(bridges);
            var tilesOf = new List<Vector2Int>[_runs.Count];
            var stairsOf = new List<StairFlight>[_runs.Count];
            for (int r = 0; r < _runs.Count; r++)
            {
                stairsOf[r] = new List<StairFlight>();
                if (_runs[r].level == 0) continue;

                tilesOf[r] = new List<Vector2Int>();
                foreach (Vector2Int tile in _structure.Footprint.Tiles)
                {
                    int lane = LaneOf(tile);
                    if (lane < _runs[r].first || lane > _runs[r].last) continue;
                    if (!_structure.IsFree(tile) || _structure.Has(tile, RoomTileTags.NearDoor) || bridged.Contains(tile)) continue;
                    tilesOf[r].Add(tile);
                    _levelAfter[tile] = _runs[r].level;
                }
            }

            for (int r = 1; r < _runs.Count; r++)
                foreach (StairFlight stairs in AlongLedge(_starts[_runs[r].first], stairSpacing, minGap))
                {
                    // A flight belongs to the run off level 0 it leads onto or off.
                    int owner = LevelAfter(stairs.Tile) != 0 ? r - (Across(stairs.Up) > 0 ? 1 : 0) : r - (Across(stairs.Up) > 0 ? 0 : 1);
                    stairsOf[owner].Add(stairs);
                }

            // Floor kept level with a doorway inside a raised or sunken lane: where the route from
            // the doorway steps off it into the lane, a flight carries it on.
            for (int r = 0; r < _runs.Count; r++)
            {
                if (tilesOf[r] == null) continue;
                foreach (Vector2Int tile in tilesOf[r])
                    foreach (Vector2Int side in GridNeighbours.Four)
                    {
                        Vector2Int landing = tile + side;
                        if (!_structure.Contains(landing) || LevelAfter(landing) != 0 || LaneRun(landing) != r) continue;
                        if (!_structure.Has(tile, RoomTileTags.Walkway) || !_structure.Has(landing, RoomTileTags.Walkway)) continue;

                        StairFlight stairs = _runs[r].level < 0 ? new StairFlight(tile, side) : new StairFlight(landing, -side);
                        if (CanStand(stairs) && !Uses(stairsOf, stairs)) stairsOf[r].Add(stairs);
                    }
            }

            var areas = new List<ElevatedArea>();
            for (int r = 0; r < _runs.Count; r++)
                if (tilesOf[r] != null && tilesOf[r].Count > 0)
                    areas.Add(new ElevatedArea(ElevationKind.Terrace, _runs[r].level, tilesOf[r], stairsOf[r], railed));
            return areas;
        }

        // Tiles of floor kept level with the room across each sunken run between two others: `count`
        // strips straight across it, spread evenly along it.
        private HashSet<Vector2Int> Bridges(int count)
        {
            var tiles = new HashSet<Vector2Int>();
            if (count <= 0) return tiles;

            RectInt bounds = _structure.Footprint.Bounds;
            int from = _stackedAlongY ? bounds.xMin : bounds.yMin, span = _stackedAlongY ? bounds.width : bounds.height;
            for (int r = 1; r < _runs.Count - 1; r++)
            {
                if (_runs[r].level >= 0) continue;
                for (int k = 0; k < count; k++)
                {
                    int along = from + Mathf.FloorToInt((k + 1) * span / (float)(count + 1));
                    for (int across = _starts[_runs[r].first]; across < _starts[_runs[r].last + 1]; across++)
                        tiles.Add(TileAt(along, across));
                }
            }
            return tiles;
        }

        // Flights across the ledge between the lane ending at `boundary - 1` and the one starting
        // at `boundary`. A bridge or a doorway's landing breaks a ledge into pieces; on each piece,
        // first wherever a route from a doorway crosses it, then spread evenly so there is one
        // every `spacing` tiles and at least one, none nearer another than `minGap`.
        private List<StairFlight> AlongLedge(int boundary, int spacing, int minGap)
        {
            RectInt bounds = _structure.Footprint.Bounds;
            int from = _stackedAlongY ? bounds.xMin : bounds.yMin, to = _stackedAlongY ? bounds.xMax : bounds.yMax;

            var chosen = new List<(int along, StairFlight stairs)>();
            var piece = new List<int>();
            var candidates = new List<(int along, StairFlight stairs)>();
            for (int along = from; along <= to; along++)
            {
                StairFlight stairs = default;
                if (along < to && TryLedge(along, boundary, out stairs))
                {
                    piece.Add(along);
                    if (CanStand(stairs)) candidates.Add((along, stairs));
                    continue;
                }
                if (piece.Count > 0) ChooseOnPiece(piece, candidates, spacing, minGap, chosen);
                piece.Clear();
                candidates.Clear();
            }

            var flights = new List<StairFlight>(chosen.Count);
            foreach ((int along, StairFlight stairs) candidate in chosen) flights.Add(candidate.stairs);
            return flights;
        }

        // Whether the floor steps one level across the boundary here, and the flight that would
        // climb it: standing on the lower side, climbing onto the higher.
        private bool TryLedge(int along, int boundary, out StairFlight stairs)
        {
            stairs = default;
            Vector2Int below = TileAt(along, boundary - 1), above = TileAt(along, boundary);
            if (!_structure.Contains(below) || !_structure.Contains(above)) return false;

            int levelBelow = LevelAfter(below), levelAbove = LevelAfter(above);
            if (Mathf.Abs(levelBelow - levelAbove) != 1) return false;
            stairs = levelBelow < levelAbove ? new StairFlight(below, Step(+1)) : new StairFlight(above, Step(-1));
            return true;
        }

        private void ChooseOnPiece(List<int> piece, List<(int along, StairFlight stairs)> candidates, int spacing, int minGap,
                                   List<(int along, StairFlight stairs)> chosen)
        {
            int before = chosen.Count;
            foreach ((int along, StairFlight stairs) candidate in candidates)
                if (OnARoute(candidate.stairs) && FarFromAll(candidate.along, chosen, minGap)) chosen.Add(candidate);

            int first = piece[0], length = piece.Count;
            int wanted = Mathf.Max(1, Mathf.RoundToInt(length / (float)spacing));
            for (int k = 0; k < wanted && chosen.Count - before < wanted; k++)
            {
                float target = first + (k + 0.5f) * length / wanted;
                (int along, StairFlight stairs)? best = null;
                foreach ((int along, StairFlight stairs) candidate in candidates)
                    if (FarFromAll(candidate.along, chosen, minGap) && (best == null || Mathf.Abs(candidate.along + 0.5f - target) < Mathf.Abs(best.Value.along + 0.5f - target)))
                        best = candidate;
                if (best.HasValue) chosen.Add(best.Value);
            }
        }

        // Free floor for the flight and both landings, the foot on the flight's own level, no wall across it.
        private bool CanStand(StairFlight stairs)
        {
            if (!_structure.IsFree(stairs.Tile) || !_structure.IsFree(stairs.Foot) || !_structure.IsFree(stairs.Head)) return false;
            if (_structure.Has(stairs.Tile, NotOnStairs) || LevelAfter(stairs.Foot) != LevelAfter(stairs.Tile)) return false;
            return !_structure.IsPartitioned(stairs.Foot, stairs.Tile) && !_structure.IsPartitioned(stairs.Tile, stairs.Head);
        }

        private bool OnARoute(StairFlight stairs) =>
            _structure.Has(stairs.Tile, RoomTileTags.Walkway) || _structure.Has(stairs.Head, RoomTileTags.Walkway);

        private static bool FarFromAll(int along, List<(int along, StairFlight stairs)> chosen, int minGap)
        {
            foreach ((int along, StairFlight stairs) other in chosen)
                if (Mathf.Abs(other.along - along) < minGap) return false;
            return true;
        }

        // The run of lanes a tile lies in.
        private int LaneRun(Vector2Int tile)
        {
            int lane = LaneOf(tile);
            for (int r = 0; r < _runs.Count; r++)
                if (lane >= _runs[r].first && lane <= _runs[r].last) return r;
            return _runs.Count - 1;
        }

        // Whether any flight already planned stands on, or lands on, one of this one's tiles.
        private static bool Uses(List<StairFlight>[] stairsOf, StairFlight stairs)
        {
            foreach (List<StairFlight> list in stairsOf)
                foreach (StairFlight other in list)
                    foreach (Vector2Int tile in new[] { other.Foot, other.Tile, other.Head })
                        if (tile == stairs.Foot || tile == stairs.Tile || tile == stairs.Head) return true;
            return false;
        }

        private int LevelAfter(Vector2Int tile) => _levelAfter.TryGetValue(tile, out int level) ? level : 0;

        // Which lane a tile lies in (the first or last for a tile past the room's bounds).
        private int LaneOf(Vector2Int tile)
        {
            int across = _stackedAlongY ? tile.y : tile.x;
            for (int i = 0; i < _levels.Length - 1; i++)
                if (across < _starts[i + 1]) return i;
            return _levels.Length - 1;
        }

        private Vector2Int TileAt(int along, int across) => _stackedAlongY ? new Vector2Int(along, across) : new Vector2Int(across, along);

        // One step across the lanes, toward the later ones (+1) or the earlier (-1).
        private Vector2Int Step(int sign) => _stackedAlongY ? new Vector2Int(0, sign) : new Vector2Int(sign, 0);

        private int Across(Vector2Int step) => _stackedAlongY ? step.y : step.x;
    }
}
