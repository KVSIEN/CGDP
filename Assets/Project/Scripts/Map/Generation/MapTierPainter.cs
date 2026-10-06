using System.Collections.Generic;
using CGD.Core;

namespace CGD.Map
{
    // Room tier 1–3, guided by how deep each room lies between Start and the Boss, so the map
    // gets harder as the player pushes on: tougher enemies, rarer resources and slightly
    // better loot higher up. Depth is a guideline, not a rule: jitter blurs the thresholds and
    // now and then a room lands a tier above or below (a tough room early, a calm one late).
    // Rooms right next to Start never go above FirstRoomsMaxTier, and Start, Exit, Boss and
    // Shop rooms stay tier 1. Run modifiers that raise intensity push tiers up too and can add
    // tier-3 rooms. Tier-3 fights may come back to back; the rhythm of fights and calm rooms
    // is the pacing's job (MaxCombatInARow), not the tiers'.
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
                node.Tier = IsTiered(node.Type) ? TierFor(slot) : 1;
                if (node.Tier == 2) promotable.Add((node, slot));
            }
            PromoteExtra(promotable, _context.Tuning.ExtraTopTierRooms);
        }

        public static bool IsTiered(MapNodeType type) => !type.IsStructural() && type != MapNodeType.Shop;

        private int TierFor(MapSlot slot)
        {
            float jitter = Content.TierJitter;
            float depth  = slot.Progress + _context.Tuning.IntensityOffset + _random.Range(-jitter, jitter);
            int tier = depth >= Content.Tier3Depth ? 3 : depth >= Content.Tier2Depth ? 2 : 1;

            if (_random.Value < Content.TierOutlierChance)
                tier += _random.Value < 0.5f ? -1 : 1;

            if (slot.Depth <= 1) tier = System.Math.Min(tier, Content.FirstRoomsMaxTier);
            return System.Math.Clamp(tier, 1, 3);
        }

        // The deepest tier-2 rooms that can take it go up first (never one next to Start).
        private void PromoteExtra(List<(MapNode node, MapSlot slot)> candidates, int count)
        {
            if (count <= 0) return;
            candidates.RemoveAll(c => c.slot.Depth <= 1 && Content.FirstRoomsMaxTier < 3);
            candidates.Sort((a, b) => b.slot.Progress.CompareTo(a.slot.Progress));
            for (int i = 0; i < count && i < candidates.Count; i++)
                candidates[i].node.Tier = 3;
        }

    }
}
