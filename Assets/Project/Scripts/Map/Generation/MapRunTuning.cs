using System.Collections.Generic;
using UnityEngine;

namespace CGD.Map
{
    // The layout and content numbers as one run's modifiers change them. The generator's
    // passes ask this instead of reading the rules directly, so a modifier never has to
    // copy or edit the settings assets.
    public class MapRunTuning
    {
        public static readonly MapRunTuning None = new(System.Array.Empty<MapRunModifier>());

        private readonly IReadOnlyList<MapRunModifier> _modifiers;

        public MapRunTuning(IReadOnlyList<MapRunModifier> modifiers)
        {
            _modifiers = modifiers ?? System.Array.Empty<MapRunModifier>();
            foreach (MapRunModifier modifier in _modifiers)
            {
                if (modifier == null) continue;
                ExtraOptionalRooms += modifier.ExtraOptionalRooms;
                ExtraLoops         += modifier.ExtraLoops;
                ExtraGates         += modifier.ExtraGates;
                LockedMultiplier   *= modifier.LockedChanceMultiplier;
                SecretMultiplier   *= modifier.SecretChanceMultiplier;
                IntensityOffset    += modifier.IntensityOffset;
                if (modifier.FactionMix != null) FactionMixOverride = modifier.FactionMix;
            }
        }

        public IReadOnlyList<MapRunModifier> Modifiers => _modifiers;

        public int   ExtraOptionalRooms { get; }
        public int   ExtraLoops         { get; }
        public int   ExtraGates         { get; }
        public float LockedMultiplier   { get; } = 1f;
        public float SecretMultiplier   { get; } = 1f;
        public float IntensityOffset    { get; }
        // Null = the content rolls its own mix. With several, the last modifier's wins.
        public MapFactionMix FactionMixOverride { get; }

        public int Min(MapNodeTypeRule rule) => Mathf.Max(0, rule.Min + Sum(rule.Type, a => a.ExtraMin));

        public int Max(MapNodeTypeRule rule) => Mathf.Max(Min(rule), rule.Max + Sum(rule.Type, a => a.ExtraMax));

        public float Weight(MapNodeTypeRule rule)
        {
            float weight = rule.Weight;
            foreach (MapRunModifier modifier in _modifiers)
            {
                if (modifier == null) continue;
                foreach (MapTypeAdjustment adjustment in modifier.Types)
                    if (adjustment.Type == rule.Type) weight *= adjustment.WeightMultiplier;
            }
            return weight;
        }

        // False once the type's rule (as modified) has placed its maximum. Types without a rule are unlimited.
        public bool HasRoomFor(MapContentSettings content, MapNodeType type, int placed)
        {
            MapNodeTypeRule rule = content.GetRule(type);
            return rule == null || placed < Max(rule);
        }

        private int Sum(MapNodeType type, System.Func<MapTypeAdjustment, int> value)
        {
            int total = 0;
            foreach (MapRunModifier modifier in _modifiers)
            {
                if (modifier == null) continue;
                foreach (MapTypeAdjustment adjustment in modifier.Types)
                    if (adjustment.Type == type) total += value(adjustment);
            }
            return total;
        }
    }
}
