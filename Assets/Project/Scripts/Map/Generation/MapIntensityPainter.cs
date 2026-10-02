using CGD.Core;
using UnityEngine;

namespace CGD.Map
{
    // Intensity = the depth curve, plus the type's bonus, plus a little jitter so
    // neighbouring rooms of the same type don't read identically.
    internal class MapIntensityPainter
    {
        private readonly MapGenerationContext _context;
        private readonly RandomStream         _random;

        public MapIntensityPainter(MapGenerationContext context)
        {
            _context = context;
            _random  = context.StreamFor(MapGenerator.IntensityLayer);
        }

        public void Paint()
        {
            foreach (MapSlot slot in _context.Slots)
                if (_context.Graph.TryGetNode(slot.NodeId, out MapNode node))
                    node.Intensity = Mathf.Clamp01(IntensityFor(node.Type, slot.Progress) + OffsetFor(node.Type));
        }

        // Run modifiers shift every room but the calm structural ones.
        private float OffsetFor(MapNodeType type) =>
            type == MapNodeType.Start || type == MapNodeType.Exit ? 0f : _context.Tuning.IntensityOffset;

        private float IntensityFor(MapNodeType type, float progress)
        {
            MapContentSettings content = _context.Content;

            switch (type)
            {
                case MapNodeType.Start:
                case MapNodeType.Exit:
                    return 0f;
                case MapNodeType.Boss:
                    return content.BossIntensity;
            }

            float bonus  = content.GetRule(type)?.IntensityBonus ?? 0f;
            float jitter = _random.Range(-1f, 1f) * content.IntensityJitter;
            return content.BaseIntensity(progress) + bonus + jitter;
        }
    }
}
