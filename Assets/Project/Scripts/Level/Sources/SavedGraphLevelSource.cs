using System.Collections.Generic;
using CGD.Core;
using CGD.Map;
using UnityEngine;

namespace CGD.Level
{
    // A hand-checked map graph asset, built the same every time. With no seed typed it uses the
    // asset's own, so its rooms lay out as the Map Graph window's blueprint shows them.
    internal sealed class SavedGraphLevelSource : LevelSource
    {
        private readonly MapGraphAsset _asset;

        public SavedGraphLevelSource(MapGraphAsset asset) => _asset = asset;

        public override MapGraph BuildGraph(LevelBuildSettings settings, Seed seed) => _asset.Graph;
        public override Vector2 NodeSpacing => _asset.NodeSpacing;
        public override MapContentSettings Content => _asset.Content;
        public override IReadOnlyList<MapRunModifier> Modifiers => _asset.Tuning.Modifiers;

        protected override Seed OwnSeed() => _asset.Seed;
    }
}
