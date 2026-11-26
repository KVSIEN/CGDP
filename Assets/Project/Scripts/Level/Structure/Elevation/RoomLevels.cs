using System.Collections.Generic;
using CGD.Algorithms;
using UnityEngine;

namespace CGD.Level
{
    // The floor levels of one room: the level each tile stands at, the flights of stairs and
    // slopes between levels, the raised and sunken areas and the decks over the floor, and how
    // walking crosses from one level to the next. RoomStructure owns one; it keeps the claims
    // (what is free, what stays reachable) and checks a new area before it is set here.
    internal sealed class RoomLevels
    {
        private readonly TileGrid _grid;
        private readonly float _wallHeight;
        private readonly List<ElevatedArea> _elevations = new();
        private readonly List<Overpass> _overpasses = new();
        // Per tile, as flat arrays over the grid (read on every step of the reachability search):
        // the floor's level (0 = the doorways' level), the flight on it (0 = none, else 1 + its
        // Up's index in GridNeighbours.Four, plus 4 for a slope) and the elevated area it belongs
        // to (0 = none, else 1 + its index).
        private readonly sbyte[] _level;
        private readonly sbyte[] _stairUp;
        private readonly short[] _areaOf;

        public RoomLevels(TileGrid grid, float wallHeight)
        {
            _grid       = grid;
            _wallHeight = wallHeight;
            _level      = new sbyte[grid.Count];
            _stairUp    = new sbyte[grid.Count];
            _areaOf     = new short[grid.Count];
        }

        public int LowestLevel  { get; private set; }
        public int HighestLevel { get; private set; }
        public IReadOnlyList<ElevatedArea> Elevations => _elevations;
        public IReadOnlyList<Overpass> Overpasses => _overpasses;

        // Makes `copy` (of a room of the same footprint) hold these levels.
        public void CopyTo(RoomLevels copy)
        {
            copy._elevations.AddRange(_elevations);
            copy._overpasses.AddRange(_overpasses);
            System.Array.Copy(_level, copy._level, _level.Length);
            System.Array.Copy(_stairUp, copy._stairUp, _stairUp.Length);
            System.Array.Copy(_areaOf, copy._areaOf, _areaOf.Length);
            copy.LowestLevel  = LowestLevel;
            copy.HighestLevel = HighestLevel;
        }

        // --- Which levels the room takes ------------------------------------------------

        public void Allow(int lowest, int highest)
        {
            LowestLevel  = Mathf.Clamp(lowest, RoomElevation.Lowest, 0);
            HighestLevel = Mathf.Clamp(highest, 0, RoomElevation.Highest);
        }

        public bool AllowsFloor(int level) => level >= LowestLevel && level <= HighestLevel && HasHeadroom(level);

        public bool AllowsDeck(int level) => level >= RoomElevation.Lowest && level <= RoomElevation.Highest && HasHeadroom(level);

        public int TiersAllowed(int direction, int wanted)
        {
            int tiers = 0;
            while (tiers < wanted && AllowsFloor(direction * (tiers + 1))) tiers++;
            return tiers;
        }

        private bool HasHeadroom(int level) => RoomElevation.HeightOf(level) + RoomElevation.Headroom <= _wallHeight;

        // --- What stands where ------------------------------------------------------------

        public int LevelOf(Vector2Int tile) => _grid.Contains(tile) ? _level[_grid.IndexOf(tile)] : 0;

        public bool HasStairs(Vector2Int tile) => _grid.Contains(tile) && _stairUp[_grid.IndexOf(tile)] != 0;

        public bool TryGetStairs(Vector2Int tile, out StairFlight stairs)
        {
            stairs = default;
            if (!HasStairs(tile)) return false;
            int index = _grid.IndexOf(tile);
            stairs = new StairFlight(tile, UpAt(index), _stairUp[index] > GridNeighbours.Four.Length);
            return true;
        }

        public ElevatedArea AreaOf(Vector2Int tile) =>
            _grid.Contains(tile) && _areaOf[_grid.IndexOf(tile)] > 0 ? _elevations[_areaOf[_grid.IndexOf(tile)] - 1] : null;

        public Overpass OverpassAbove(Vector2Int tile)
        {
            foreach (Overpass overpass in _overpasses)
                foreach (Vector2Int deck in overpass.Deck)
                    if (deck == tile) return overpass;
            return null;
        }

        // --- Walking across levels (both tiles neighbours inside the grid) --------------

        // Whether walking can cross between two neighbouring tiles as far as their levels go: the
        // same level, entered along any flight either stands on, or one level apart up a flight.
        public bool Join(Vector2Int a, Vector2Int b)
        {
            int ia = _grid.IndexOf(a), ib = _grid.IndexOf(b);
            Vector2Int step = b - a;
            // Across a wide flight: two flights side by side, climbing the same way from the same level.
            if (_stairUp[ia] != 0 && _stairUp[ib] != 0 && UpAt(ia) == UpAt(ib) && _level[ia] == _level[ib] && !Along(UpAt(ia), step))
                return true;
            if (_stairUp[ia] != 0 && !Along(UpAt(ia), step)) return false;
            if (_stairUp[ib] != 0 && !Along(UpAt(ib), step)) return false;

            int la = _level[ia], lb = _level[ib];
            if (la == lb) return true;
            if (la + 1 == lb) return _stairUp[ia] != 0 && UpAt(ia) == step;
            if (lb + 1 == la) return _stairUp[ib] != 0 && UpAt(ib) == -step;
            return false;
        }

        // Whether `from` stands an open ledge's height over `to`: one level up, no stairs on
        // either, and no railing of either's area between them. Walls are the structure's to check.
        public bool IsOpenDrop(Vector2Int from, Vector2Int to)
        {
            int a = _grid.IndexOf(from), b = _grid.IndexOf(to);
            if (_level[a] != _level[b] + 1 || _stairUp[a] != 0 || _stairUp[b] != 0) return false;
            return !IsRailed(a, b);
        }

        private static bool Along(Vector2Int up, Vector2Int step) => step == up || step == -up;

        private Vector2Int UpAt(int index) => GridNeighbours.Four[(_stairUp[index] - 1) % GridNeighbours.Four.Length];

        // A railed area keeps anyone from crossing its ledges: off a platform, or into a pit.
        private bool IsRailed(int a, int b)
        {
            ElevatedArea from = _areaOf[a] > 0 ? _elevations[_areaOf[a] - 1] : null;
            ElevatedArea to   = _areaOf[b] > 0 ? _elevations[_areaOf[b] - 1] : null;
            return (from != null && from != to && from.Railed) || (to != null && to != from && to.Railed);
        }

        // --- Changes (checked by RoomStructure first) -----------------------------------

        // Raises or sinks the areas' tiles and lays their flights.
        public void Add(IReadOnlyList<ElevatedArea> areas)
        {
            int first = _elevations.Count;
            for (int i = 0; i < areas.Count; i++)
            {
                _elevations.Add(areas[i]);
                Set(areas[i], (short)(first + i + 1), (sbyte)areas[i].Level);
            }
        }

        // Takes back areas just added, last first.
        public void Remove(IReadOnlyList<ElevatedArea> areas)
        {
            for (int i = areas.Count - 1; i >= 0; i--) Set(areas[i], 0, 0);
            _elevations.RemoveRange(_elevations.Count - areas.Count, areas.Count);
        }

        public void AddOverpass(Overpass overpass) => _overpasses.Add(overpass);

        private void Set(ElevatedArea area, short index, sbyte level)
        {
            foreach (Vector2Int tile in area.Tiles)
            {
                _level[_grid.IndexOf(tile)]  = level;
                _areaOf[_grid.IndexOf(tile)] = index;
            }
            foreach (StairFlight stairs in area.Stairs)
            {
                int code = 1 + GridNeighbours.IndexOf(stairs.Up) + (stairs.Slope ? GridNeighbours.Four.Length : 0);
                _stairUp[_grid.IndexOf(stairs.Tile)] = index != 0 ? (sbyte)code : (sbyte)0;
            }
        }
    }
}
