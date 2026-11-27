using System;
using System.Collections.Generic;
using UnityEngine;
using CGD.Core;

namespace CGD.Level
{
    // How much structure a room has, counted and weighted, and what is added where it has too
    // little. Every wall piece, building, pillar and half wall counts for its kind's weight (walls
    // most, then buildings, half walls and pillars); a room is built up when that score reaches
    // its floor tiles divided by TilesPerPoint.
    //
    // A room is judged in areas too, AreaTiles across, by what a fight there feels: how far a
    // person standing in it sees along its rows and columns (SightlineMap: full walls and columns
    // stop sight, low walls are seen over), and its floor's share of points. A big room with walls
    // down one side and nothing but columns down the other is open on that side, whatever the
    // whole adds up to. A room short of points as a whole, or with an area that sees further than
    // OpenRun on average or has under AreaShare of its points, gets structure added, at most
    // MaxAdditions times. Each time every option that can be aimed (IFocusedStructureRule) is tried
    // on a copy at the shortest area, and a few others drawn at random on copies of their own; the
    // copy that closes the areas'
    // shortfall most is kept, sight first and points after, so walls go where the room is open
    // and low walls still count as cover. A copy that cuts floor off from the doorways is never
    // kept. Options are drawn from a stream of their own, so no other draw in the room moves.
    [Serializable]
    public sealed class StructureBudget
    {
        [Serializable]
        public sealed class Option
        {
            public RoomStructureRule Rule;
        }

        // Heaviest kind first.
        private static readonly StructureKind[] ByWeight =
            { StructureKind.Wall, StructureKind.Building, StructureKind.Pillar, StructureKind.HalfWall };

        [Tooltip("What can be added. Empty = nothing is added")]
        [SerializeField] private List<Option> _options = new();
        [Tooltip("Rooms with fewer floor tiles than this are left as they are")]
        [SerializeField, Min(1)] private int _minTiles = 90;
        [Tooltip("A room is built up at one point of structure for this many floor tiles")]
        [SerializeField, Min(1)] private int _tilesPerPoint = 8;
        [Tooltip("The most rules added to one room")]
        [SerializeField, Range(1, 6)] private int _maxAdditions = 2;

        [Header("Areas")]
        [Tooltip("Tiles across each area a room is judged in")]
        [SerializeField, Min(4)] private int _areaTiles = 8;
        [Tooltip("An area with less than this share of its own floor's points is short, however much the room has")]
        [SerializeField, Range(0f, 1f)] private float _areaShare = 0.5f;
        [Tooltip("Tiles an area's floor may see along its rows and columns on average, standing, before it counts as open")]
        [SerializeField, Min(2f)] private float _openRun = 8f;
        [Tooltip("How much an area's missing points count against it, beside how far it sees")]
        [SerializeField, Range(0f, 1f)] private float _pointsWeight = 0.5f;
        [Tooltip("Options drawn at random and tried for each addition, beside every option that can be aimed at an area: the one that fills the short areas most is kept")]
        [SerializeField, Range(1, 8)] private int _choices = 3;
        [Tooltip("Times each drawn option is tried")]
        [SerializeField, Range(1, 4)] private int _tries = 2;

        [Header("Points")]
        [Tooltip("For each full-height wall piece (one tile of wall)")]
        [SerializeField, Min(0f)] private float _wall = 1f;
        [Tooltip("For each building or hut standing in the room")]
        [SerializeField, Min(0f)] private float _building = 4f;
        [Tooltip("For each pillar: much less than a wall piece, since a column hides little and splits nothing")]
        [SerializeField, Min(0f)] private float _pillar = 0.3f;
        [Tooltip("For each piece of low wall, one you can shoot over: cover, but the room is seen across it standing")]
        [SerializeField, Min(0f)] private float _halfWall = 0.35f;

        public float WeightOf(StructureKind kind) => kind switch
        {
            StructureKind.Wall     => _wall,
            StructureKind.Building => _building,
            StructureKind.Pillar   => _pillar,
            _                      => _halfWall,
        };

        // The pieces of each kind in a room.
        public int CountOf(RoomStructure structure, StructureKind kind)
        {
            if (kind == StructureKind.Building) return structure.Spaces.Count;
            if (kind == StructureKind.Pillar) return structure.Pillars.Count;

            int walls = 0, low = 0;
            foreach (PartitionEdge edge in structure.Partitions)
            {
                if (CoverHeights.IsLow(edge.Height)) low++;
                else walls++;
            }
            foreach (DiagonalWall wall in structure.Diagonals)
            {
                if (CoverHeights.IsLow(wall.Height)) low++;
                else walls++;
            }
            return kind == StructureKind.Wall ? walls : low;
        }

        public float Score(RoomStructure structure)
        {
            float score = 0f;
            foreach (StructureKind kind in ByWeight) score += WeightOf(kind) * CountOf(structure, kind);
            return score;
        }

        // The score a room of this many floor tiles should reach; 0 for a room too small to ask.
        public float Needed(RoomStructure structure)
        {
            int tiles = structure.Footprint.Count;
            return _options.Count == 0 || tiles < _minTiles ? 0f : (float)tiles / _tilesPerPoint;
        }

        // Short as a whole, or in any one area.
        public bool Needs(RoomStructure structure)
        {
            float needed = Needed(structure);
            if (needed <= 0f) return false;
            if (Score(structure) < needed) return true;
            foreach (Area area in AreasOf(structure))
                if (area.Score < area.Needed * _areaShare || area.MeanRun > _openRun) return true;
            return false;
        }

        // The room with structure added: `structure` itself when nothing was, else a copy.
        public RoomStructure Apply(RoomStructure structure, RandomStream random)
        {
            RandomStream own = random.Fork("structure budget");
            for (int added = 0; added < _maxAdditions && Needs(structure); added++)
            {
                RoomStructure built = AddOne(structure, own, added);
                if (built == null) break;
                structure = built;
            }
            return structure;
        }

        // The best of the options tried, each on a copy, the aimable ones at the shortest area:
        // the copy that most fills the areas' shortfall and keeps the room whole. Null when none helps.
        private RoomStructure AddOne(RoomStructure structure, RandomStream random, int addition)
        {
            var usable = new List<RoomStructureRule>();
            var tried = new List<RoomStructureRule>();
            foreach (Option option in _options)
            {
                if (option == null || option.Rule == null) continue;
                usable.Add(option.Rule);
                if (option.Rule is IFocusedStructureRule && !tried.Contains(option.Rule)) tried.Add(option.Rule);
            }
            if (usable.Count == 0) return null;
            for (int choice = 0; choice < _choices; choice++) tried.Add(random.Pick(usable));

            float before = Shortfall(structure, out RectInt shortest);
            RoomStructure best = null;
            float bestGain = 0f;
            for (int choice = 0; choice < tried.Count; choice++)
            {
                RoomStructureRule rule = tried[choice];
                for (int attempt = 0; attempt < _tries; attempt++)
                {
                    RoomStructure trial = structure.Copy();
                    RandomStream draws = random.Fork($"{addition}:{choice}:{attempt}");
                    if (rule is IFocusedStructureRule focused) focused.Apply(trial, draws, shortest);
                    else rule.Apply(trial, draws);
                    if (!trial.StaysConnected(Array.Empty<Vector2Int>())) continue;

                    float gain = before - Shortfall(trial, out _);
                    if (gain <= bestGain + 1e-4f) continue;
                    best = trial;
                    bestGain = gain;
                }
            }
            return best;
        }

        // How far the areas fall short, together, and the one that falls furthest: how much
        // further than OpenRun each sees, in its share of points, and its missing points.
        private float Shortfall(RoomStructure structure, out RectInt shortest)
        {
            float total = 0f, most = -1f;
            shortest = structure.Footprint.Bounds;
            foreach (Area area in AreasOf(structure))
            {
                float missing = area.Needed * Mathf.Max(0f, area.MeanRun - _openRun) / _openRun
                              + _pointsWeight * Mathf.Max(0f, area.Needed - area.Score);
                total += missing;
                if (missing <= most) continue;
                most = missing;
                shortest = area.Rect;
            }
            return total;
        }

        private readonly struct Area
        {
            public Area(RectInt rect, float needed, float score, float meanRun)
            {
                Rect    = rect;
                Needed  = needed;
                Score   = score;
                MeanRun = meanRun;
            }

            public RectInt Rect    { get; }
            public float   Needed  { get; }
            public float   Score   { get; }
            // How far its floor sees on average, along rows and columns, standing (tiles).
            public float   MeanRun { get; }
        }

        // The room's bounds cut into a grid about AreaTiles across, each area with its floor's
        // share and the points of what stands in it (a building's spread over the floor it covers).
        private List<Area> AreasOf(RoomStructure structure)
        {
            RectInt bounds = structure.Footprint.Bounds;
            int columns = Mathf.Max(1, Mathf.RoundToInt((float)bounds.width / _areaTiles));
            int rows = Mathf.Max(1, Mathf.RoundToInt((float)bounds.height / _areaTiles));
            var floor = new int[columns, rows];
            var score = new float[columns, rows];
            var runs = new float[columns, rows];
            var seeing = new int[columns, rows];
            var sight = new SightlineMap(structure);

            int Column(int x) => Mathf.Clamp((x - bounds.xMin) * columns / bounds.width, 0, columns - 1);
            int Row(int y) => Mathf.Clamp((y - bounds.yMin) * rows / bounds.height, 0, rows - 1);
            void Add(Vector2Int tile, float points) => score[Column(tile.x), Row(tile.y)] += points;

            foreach (Vector2Int tile in structure.Footprint.Tiles)
            {
                int c = Column(tile.x), r = Row(tile.y);
                floor[c, r]++;
                (int alongX, int alongY) = sight.Runs(tile);
                if (alongX + alongY == 0) continue;
                runs[c, r] += (alongX + alongY) * 0.5f;
                seeing[c, r]++;
            }
            foreach (PartitionEdge edge in structure.Partitions)
                Add(edge.Tile, CoverHeights.IsLow(edge.Height) ? _halfWall : _wall);
            foreach (DiagonalWall wall in structure.Diagonals)
                Add(wall.Tile, CoverHeights.IsLow(wall.Height) ? _halfWall : _wall);
            foreach (StructurePillar pillar in structure.Pillars)
                Add(Vector2Int.FloorToInt(pillar.Position), _pillar);
            foreach (StructureSpace space in structure.Spaces)
            {
                if (!space.HasArea)
                {
                    Add(space.Tile, _building);
                    continue;
                }
                float share = _building / (space.Area.width * space.Area.height);
                foreach (Vector2Int tile in space.Area.allPositionsWithin) Add(tile, share);
            }

            var areas = new List<Area>(columns * rows);
            for (int c = 0; c < columns; c++)
                for (int r = 0; r < rows; r++)
                {
                    int xMin = bounds.xMin + c * bounds.width / columns, xMax = bounds.xMin + (c + 1) * bounds.width / columns;
                    int yMin = bounds.yMin + r * bounds.height / rows, yMax = bounds.yMin + (r + 1) * bounds.height / rows;
                    areas.Add(new Area(new RectInt(xMin, yMin, xMax - xMin, yMax - yMin), (float)floor[c, r] / _tilesPerPoint, score[c, r],
                                       seeing[c, r] > 0 ? runs[c, r] / seeing[c, r] : 0f));
                }
            return areas;
        }
    }
}
