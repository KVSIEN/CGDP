using System;
using System.Collections.Generic;
using UnityEngine;
using CGD.Core;

namespace CGD.Level
{
    // How much structure a room has, counted and weighted, and what is added when it has too
    // little. Every wall piece, building, pillar and half wall in the room counts for its kind's
    // weight (walls most, then buildings, pillars and half walls); a room is built up when that
    // score reaches its floor tiles divided by TilesPerPoint. A bigger room needs more, and one
    // that asks for nothing (a hall with corner pillars) is open floor to fight across.
    //
    // A room short of its budget gets structure added until it is met, at most MaxAdditions
    // times: each time one option of the heaviest kind the room has none of (walls first), else
    // of the heaviest kind. So an open hall gets walls to run along and fight round before
    // pillars, and a walled one a pillar or half wall to fill in. Options are drawn from a
    // stream of their own, so no other draw in the room moves.
    [Serializable]
    public sealed class StructureBudget
    {
        [Serializable]
        public sealed class Option
        {
            public RoomStructureRule Rule;
            public StructureKind Kind;
        }

        // Heaviest kind first.
        private static readonly StructureKind[] ByWeight =
            { StructureKind.Wall, StructureKind.Building, StructureKind.Pillar, StructureKind.HalfWall };

        [Tooltip("What can be added, each tagged with the kind of structure it builds. Empty = nothing is added")]
        [SerializeField] private List<Option> _options = new();
        [Tooltip("Rooms with fewer floor tiles than this are left as they are")]
        [SerializeField, Min(1)] private int _minTiles = 90;
        [Tooltip("A room is built up at one point of structure for this many floor tiles")]
        [SerializeField, Min(1)] private int _tilesPerPoint = 8;
        [Tooltip("The most rules added to one room")]
        [SerializeField, Range(1, 4)] private int _maxAdditions = 2;

        [Header("Points")]
        [Tooltip("For each full-height wall piece (one tile of wall)")]
        [SerializeField, Min(0f)] private float _wall = 1f;
        [Tooltip("For each building or hut standing in the room")]
        [SerializeField, Min(0f)] private float _building = 4f;
        [Tooltip("For each pillar")]
        [SerializeField, Min(0f)] private float _pillar = 0.75f;
        [Tooltip("For each piece of low wall, one you can shoot over")]
        [SerializeField, Min(0f)] private float _halfWall = 0.5f;

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

        public bool Needs(RoomStructure structure) => Score(structure) < Needed(structure);

        public void Apply(RoomStructure structure, RandomStream random)
        {
            RandomStream own = random.Fork("structure budget");
            float needed = Needed(structure);
            for (int added = 0; added < _maxAdditions && Score(structure) < needed; added++)
                if (!AddOne(structure, own)) return;
        }

        // Adds one option of the heaviest kind the room has none of (else the heaviest), trying
        // lighter kinds when a rule found no room for itself. False when nothing changed.
        private bool AddOne(RoomStructure structure, RandomStream random)
        {
            foreach (StructureKind kind in Preference(structure))
            {
                List<Option> choices = OptionsOf(kind);
                if (choices.Count == 0) continue;

                float before = Score(structure);
                choices[random.Range(0, choices.Count)].Rule.Apply(structure, random);
                if (Score(structure) > before) return true;
            }
            return false;
        }

        // Kinds the room lacks come first, each group heaviest first.
        private IEnumerable<StructureKind> Preference(RoomStructure structure)
        {
            foreach (StructureKind kind in ByWeight)
                if (CountOf(structure, kind) == 0) yield return kind;
            foreach (StructureKind kind in ByWeight)
                if (CountOf(structure, kind) > 0) yield return kind;
        }

        private List<Option> OptionsOf(StructureKind kind)
        {
            var choices = new List<Option>();
            foreach (Option option in _options)
                if (option != null && option.Rule != null && option.Kind == kind) choices.Add(option);
            return choices;
        }
    }
}
