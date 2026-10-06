using System.Collections.Generic;
using CGD.Core;

namespace CGD.Map
{
    // Room tier 1–3 from each room's intensity (plus jitter, so tiers don't follow depth
    // exactly): tougher enemies, rarer resources and slightly better loot higher up. Start,
    // Exit, Boss and Shop rooms stay tier 1. Run modifiers can raise extra rooms to tier 3,
    // and a tier-3 fight on the main path always has a breather after it (otherwise it
    // stays tier 2).
    internal class MapTierPainter
    {
        private readonly MapGenerationContext _context;
        private readonly RandomStream         _random;

        public MapTierPainter(MapGenerationContext context)
        {
            _context = context;
            _random  = context.StreamFor(MapGenerator.TiersLayer);
        }

        private MapContentSettings Content => _context.Content;

        public void Paint()
        {
            var promotable = new List<(MapNode node, MapSlot slot)>();
            foreach (MapSlot slot in _context.Slots)
            {
                if (!_context.Graph.TryGetNode(slot.NodeId, out MapNode node)) continue;
                node.Tier = IsTiered(node.Type) ? TierFor(node, slot) : 1;
                if (node.Tier == 2 && CanBeTopTier(node, slot)) promotable.Add((node, slot));
            }
            PromoteExtra(promotable, _context.Tuning.ExtraTopTierRooms);
        }

        public static bool IsTiered(MapNodeType type) => !type.IsStructural() && type != MapNodeType.Shop;

        private int TierFor(MapNode node, MapSlot slot)
        {
            float jitter = Content.TierJitter;
            float roll   = node.Intensity + _random.Range(-jitter, jitter);
            if (roll >= Content.Tier3Intensity && CanBeTopTier(node, slot)) return 3;
            return roll >= Content.Tier2Intensity ? 2 : 1;
        }

        // The most intense tier-2 rooms that can take it go up first.
        private static void PromoteExtra(List<(MapNode node, MapSlot slot)> candidates, int count)
        {
            if (count <= 0) return;
            candidates.Sort((a, b) => b.node.Intensity.CompareTo(a.node.Intensity));
            for (int i = 0; i < count && i < candidates.Count; i++)
                candidates[i].node.Tier = 3;
        }

        // A tier-3 fight on the main path needs a room after it that isn't a fight.
        private bool CanBeTopTier(MapNode node, MapSlot slot)
        {
            MapPacingSettings pacing = Content.Pacing;
            if (!pacing.RestAfterTopTier || slot.MainPathIndex < 0 || !pacing.IsCombat(node.Type)) return true;

            List<int> path = _context.MainPath;
            int next = slot.MainPathIndex + 1;
            return next >= path.Count
                || !_context.Graph.TryGetNode(path[next], out MapNode after)
                || !pacing.IsCombat(after.Type);
        }
    }
}
