using System.Collections.Generic;
using CGD.Core;
using UnityEngine;

namespace CGD.Map
{
    // Intensity = the depth curve, plus the type's bonus, plus a little jitter so
    // neighbouring rooms of the same type don't read identically. Pinned rooms then take their
    // pin's intensity, and the main path's peaks are raised where the pacing asks for one
    // (MapPacing.Raise), never a pinned room's: the map comes out with the sawtooth the map
    // director builds, instead of the director making peaks of its own afterwards.
    internal class MapIntensityPainter
    {
        private readonly MapGenerationContext _context;
        private readonly RandomStream         _random;

        public MapIntensityPainter(MapGenerationContext context)
        {
            _context = context;
            _random  = context.StreamFor(MapGenerator.IntensityLayer);
        }

        // `pins` and `pinnedIds` pair up: the pinned rooms and the nodes they were given.
        public void Paint(IReadOnlyList<MapNodePin> pins, IReadOnlyList<int> pinnedIds)
        {
            foreach (MapSlot slot in _context.Slots)
                if (_context.Graph.TryGetNode(slot.NodeId, out MapNode node))
                    node.Intensity = Mathf.Clamp01(IntensityFor(node.Type, slot.Progress) + OffsetFor(node.Type));

            var pinned = new HashSet<int>();
            for (int i = 0; i < pins.Count; i++)
            {
                if (!_context.Graph.TryGetNode(pinnedIds[i], out MapNode node)) continue;
                node.Intensity = pins[i].Intensity;
                pinned.Add(node.Id);
            }
            MapPacing.Raise(_context.Graph, _context.Content.Pacing, pinned);
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
