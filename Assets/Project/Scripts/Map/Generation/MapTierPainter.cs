using System.Collections.Generic;
using CGD.Core;

namespace CGD.Map
{
    // Room tier 1–3 from how deep each room lies between Start and the Boss (plus jitter, so
    // tiers rise with depth without following it exactly), so the map gets harder as the
    // player pushes on: tougher enemies, rarer resources and slightly better loot higher up.
    // Start, Exit, Boss and Shop rooms stay tier 1. Run modifiers that raise intensity push
    // tiers up too and can add tier-3 rooms, and a tier-3 fight on the main path always has
    // a breather after it (otherwise it stays tier 2).
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
            float depth  = slot.Progress + _context.Tuning.IntensityOffset + _random.Range(-jitter, jitter);
            if (depth >= Content.Tier3Depth && CanBeTopTier(node, slot)) return 3;
            return depth >= Content.Tier2Depth ? 2 : 1;
        }

        // The deepest tier-2 rooms that can take it go up first.
        private static void PromoteExtra(List<(MapNode node, MapSlot slot)> candidates, int count)
        {
            if (count <= 0) return;
            candidates.Sort((a, b) => b.slot.Progress.CompareTo(a.slot.Progress));
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
