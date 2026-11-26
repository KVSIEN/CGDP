using System;
using System.Collections.Generic;
using UnityEngine;
using CGD.Core;

namespace CGD.Level
{
    // How much of a room's floor should stand off the doorways' level, and what raises or sinks
    // it when the room has less. A room's own structure rules say what suits the place (a stage,
    // a lounge pit, terraces); this runs after them and tops up whatever is still short, whether
    // the place lists no elevation rule or its rule found no space. Each addition is one weighted
    // option, tried until the share is met or the attempts run out, so a hall gets several
    // features and a small room one. A feature the caller turns down (one that would crowd out
    // the room's required pieces) is left off. Options are drawn from a stream of their own. A
    // room whose levels aren't allowed is left as it is.
    [Serializable]
    public sealed class ElevationBudget
    {
        [Serializable]
        public sealed class Option
        {
            public RoomStructureRule Rule;
            [Min(0f)] public float Weight = 1f;
        }

        [Tooltip("What can be added: Elevation structure rules. Empty = nothing is added")]
        [SerializeField] private List<Option> _options = new();
        [Tooltip("The chance a room short of its share gets features added; the rest stay as their own rules left them, so flat rooms still turn up")]
        [SerializeField, Range(0f, 1f)] private float _chance = 0.9f;
        [Tooltip("Share of the floor off the doorways' level a room is topped up to")]
        [SerializeField, Range(0f, 0.5f)] private float _share = 0.18f;
        [Tooltip("Rooms with fewer floor tiles than this are left as they are")]
        [SerializeField, Min(1)] private int _minTiles = 36;
        [Tooltip("The most options tried on one room")]
        [SerializeField, Range(1, 12)] private int _attempts = 6;

        // Whether the room takes levels, is big enough and has less than its share off level 0.
        public bool Needs(RoomStructure structure) =>
            _options.Count > 0 && (structure.LowestLevel < 0 || structure.HighestLevel > 0)
            && structure.Footprint.Count >= _minTiles && OffLevelShare(structure) < _share;

        // The room with its features added: `structure` itself when none were, else a copy.
        // `accepts`: whether a copy with one more feature may be kept (null = always).
        public RoomStructure Apply(RoomStructure structure, RandomStream random, Predicate<RoomStructure> accepts = null)
        {
            RandomStream own = random.Fork("elevation budget");
            if (!own.Chance(_chance)) return structure;

            for (int attempt = 0; attempt < _attempts && OffLevelShare(structure) < _share; attempt++)
            {
                Option option = own.PickWeighted(_options, o => o != null && o.Rule != null ? o.Weight : 0f);
                if (option == null || option.Rule == null) break;

                RoomStructure trial = structure.Copy();
                option.Rule.Apply(trial, own);
                if (trial.Elevations.Count > structure.Elevations.Count && (accepts == null || accepts(trial)))
                    structure = trial;
            }
            return structure;
        }

        // The share of the room's floor standing above or below the doorways' level.
        public static float OffLevelShare(RoomStructure structure)
        {
            int off = 0;
            foreach (ElevatedArea area in structure.Elevations) off += area.Tiles.Count;
            return (float)off / structure.Footprint.Count;
        }
    }
}
