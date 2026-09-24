using System.Collections.Generic;
using UnityEngine;

namespace CGD.Map
{
    // Each faction claims a random room as its origin; influence fades by
    // FactionFalloff per connection away from it. A node belongs to whichever faction
    // is strongest there, if any is above FactionThreshold.
    internal class MapFactionPainter
    {
        private readonly MapGenerationContext _context;
        private readonly List<int>            _neighbors = new();

        public MapFactionPainter(MapGenerationContext context) => _context = context;

        public void Paint()
        {
            IReadOnlyList<string> factions = _context.Settings.Factions;
            if (factions.Count == 0) return;

            var origins = new List<MapSlot>(_context.Slots);
            origins.RemoveAll(s => s.IsStructural);

            for (int faction = 0; faction < factions.Count; faction++)
            {
                if (origins.Count == 0)
                {
                    _context.Warnings.Add($"No room left to seed faction '{factions[faction]}'.");
                    return;
                }

                MapSlot origin = _context.Pick(origins);
                origins.Remove(origin);
                Spread(faction, origin.NodeId);
            }
        }

        private void Spread(int faction, int originId)
        {
            MapGraph graph     = _context.Graph;
            float    falloff   = _context.Settings.FactionFalloff;
            float    threshold = _context.Settings.FactionThreshold;

            var hops     = new Dictionary<int, int> { [originId] = 0 };
            var frontier = new Queue<int>();
            frontier.Enqueue(originId);

            while (frontier.Count > 0)
            {
                int   current   = frontier.Dequeue();
                float influence = Mathf.Pow(falloff, hops[current]);
                if (influence < threshold) continue;

                if (graph.TryGetNode(current, out MapNode node) && influence > node.FactionInfluence)
                    node.SetFaction(faction, influence);

                graph.GetNeighbors(current, _neighbors);
                foreach (int next in _neighbors)
                {
                    if (hops.ContainsKey(next)) continue;
                    hops[next] = hops[current] + 1;
                    frontier.Enqueue(next);
                }
            }
        }
    }
}
