using System;
using System.Collections.Generic;
using CGD.Core;
using CGD.Map;
using UnityEngine;

namespace CGD.Level
{
    // A map generated on the spot from a MapGenerationSettings style and the level's seed.
    internal sealed class GeneratedMapLevelSource : LevelSource
    {
        private readonly MapGenerationSettings _generation;
        private IReadOnlyList<MapRunModifier> _modifiers = Array.Empty<MapRunModifier>();

        // `generation` may be null: then there is nothing to build.
        public GeneratedMapLevelSource(MapGenerationSettings generation) => _generation = generation;

        public override MapGraph BuildGraph(LevelBuildSettings settings, Seed seed)
        {
            if (_generation == null || !_generation.CanGenerate) return null;

            MapGenerationResult result = new MapGenerator(_generation).Generate(LevelSeeds.Map(seed));
            _modifiers = result.Modifiers;
            return result.Graph;
        }

        public override Vector2 NodeSpacing => _generation != null ? _generation.NodeSpacing : MapGenerationSettings.DefaultNodeSpacing;
        public override MapContentSettings Content => _generation != null ? _generation.Content : null;
        public override IReadOnlyList<MapRunModifier> Modifiers => _modifiers;
    }
}
