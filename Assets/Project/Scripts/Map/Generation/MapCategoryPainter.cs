using System.Collections.Generic;
using CGD.Core;
using UnityEngine;

namespace CGD.Map
{
    // Decides what kind of ship space each room is, in three steps:
    //   1. zones — pockets of Civilian, Operations and Services are seeded (spread apart)
    //      and every room joins the zone whose pocket is nearest, scaled by the zone's
    //      share, so the map has recognisable areas rather than a random mix
    //   2. fixed — pinned rooms keep their category and some node types are always one
    //      (the Exit is a docking bay)
    //   3. the rest — from the Start outwards, each room picks a category by weight: its
    //      zone's categories first, then how well each suits the rooms already beside it
    //      (direct doors count extra), the section, and a nudge toward variety
    internal class MapCategoryPainter
    {
        private const int   OriginCandidates = 3;
        private const float ZoneJitter       = 0.9f;
        private const float OrderJitter      = 0.5f;
        private const int   Unreached        = 99;

        private readonly MapGenerationContext _context;
        private readonly MapCategorySettings  _settings;
        private readonly RandomStream         _random;
        private readonly List<int>            _neighbors = new();
        private readonly List<RoomCategory>   _enabled   = new();
        private readonly Dictionary<RoomCategory, int> _counts = new();

        public MapCategoryPainter(MapGenerationContext context, MapCategorySettings settings)
        {
            _context  = context;
            _settings = settings;
            _random   = context.StreamFor(MapGenerator.CategoriesLayer);
        }

        private MapGraph Graph => _context.Graph;

        public void Paint()
        {
            _settings.CollectEnabled(_enabled);
            if (_enabled.Count == 0) return;

            Dictionary<int, ShipZone> zones = PaintZones();
            AssignFixed();
            AssignRemaining(zones);
        }

        // --- Zones ---------------------------------------------------------------------

        // Empty when no zone has an enabled category: rooms then pick from every category.
        private Dictionary<int, ShipZone> PaintZones()
        {
            var zoneOf = new Dictionary<int, ShipZone>();
            var shares = new Dictionary<ShipZone, float>();
            var pockets = new List<ShipZone>();

            var rules = new List<CategoryZoneRule>();
            foreach (CategoryZoneRule rule in _settings.Zones)
                if (rule != null && rule.Share > 0f && _settings.HasEnabledCategory(rule.Zone) && !shares.ContainsKey(rule.Zone))
                {
                    shares[rule.Zone] = rule.Share;
                    rules.Add(rule);
                }
            if (rules.Count == 0) return zoneOf;

            // One entry per pocket, zones interleaved so each gets one before any gets two.
            for (int round = 0, added = 1; added > 0; round++)
            {
                added = 0;
                foreach (CategoryZoneRule rule in rules)
                    if (round < rule.Pockets) { pockets.Add(rule.Zone); added++; }
            }

            List<int> origins = PickOrigins(Mathf.Min(pockets.Count, Graph.Nodes.Count));
            var reach = new List<Dictionary<int, int>>(origins.Count);
            for (int i = 0; i < origins.Count; i++)
            {
                zoneOf[origins[i]] = pockets[i];
                reach.Add(MapGraphSearch.Distances(Graph, origins[i], includeShortcuts: true));
            }

            foreach (MapNode node in Graph.Nodes)
            {
                if (zoneOf.ContainsKey(node.Id)) continue;

                ShipZone best = pockets[0];
                float bestCost = float.MaxValue;
                for (int i = 0; i < origins.Count; i++)
                {
                    int steps = reach[i].TryGetValue(node.Id, out int known) ? known : Unreached;
                    float cost = (steps + _random.Value * ZoneJitter) / shares[pockets[i]];
                    if (cost >= bestCost) continue;

                    bestCost = cost;
                    best = pockets[i];
                }
                zoneOf[node.Id] = best;
            }
            return zoneOf;
        }

        // The first at random, then each the farthest from the others of a few random rooms,
        // so pockets start apart without always starting in the same rooms.
        private List<int> PickOrigins(int count)
        {
            var origins = new List<int>(count);
            var nearest = new Dictionary<int, int>();   // room -> connections to the closest origin so far
            var pool    = new List<MapNode>(Graph.Nodes);

            while (origins.Count < count && pool.Count > 0)
            {
                MapNode pick = origins.Count == 0 ? _random.Pick(pool) : FarthestOfSome(pool, nearest);
                pool.Remove(pick);
                origins.Add(pick.Id);

                foreach (KeyValuePair<int, int> reached in MapGraphSearch.Distances(Graph, pick.Id, includeShortcuts: true))
                    if (!nearest.TryGetValue(reached.Key, out int known) || reached.Value < known)
                        nearest[reached.Key] = reached.Value;
            }
            return origins;
        }

        private MapNode FarthestOfSome(List<MapNode> pool, Dictionary<int, int> nearest)
        {
            MapNode best = null;
            int bestSteps = -1;
            for (int i = 0; i < OriginCandidates; i++)
            {
                MapNode candidate = _random.Pick(pool);
                int steps = nearest.TryGetValue(candidate.Id, out int known) ? known : Unreached;
                if (steps <= bestSteps) continue;

                best = candidate;
                bestSteps = steps;
            }
            return best;
        }

        // --- Categories ----------------------------------------------------------------

        private void AssignFixed()
        {
            foreach (MapNode node in Graph.Nodes)
            {
                if (node.Category == RoomCategory.None)
                    node.Category = _settings.FixedFor(node.Type);
                if (node.Category != RoomCategory.None)
                    Count(node.Category);
            }
        }

        private void AssignRemaining(Dictionary<int, ShipZone> zones)
        {
            MapNode start = Graph.FindFirst(MapNodeType.Start);
            Dictionary<int, int> depth = start != null
                ? MapGraphSearch.Distances(Graph, start.Id, includeShortcuts: true)
                : new Dictionary<int, int>();

            // Outwards from Start, so each room can weigh the neighbours already settled.
            var order = new List<(float key, MapNode node)>();
            foreach (MapNode node in Graph.Nodes)
                if (node.Category == RoomCategory.None)
                {
                    int steps = depth.TryGetValue(node.Id, out int known) ? known : Unreached;
                    order.Add((steps + _random.Value * OrderJitter, node));
                }
            order.Sort((x, y) => x.key.CompareTo(y.key));

            foreach ((float key, MapNode node) entry in order)
            {
                zones.TryGetValue(entry.node.Id, out ShipZone zone);
                entry.node.Category = Choose(entry.node, zone);
                Count(entry.node.Category);
            }
        }

        private RoomCategory Choose(MapNode node, ShipZone zone)
        {
            MapSlot slot = _context.GetSlot(node.Id);
            MapContentSettings content = _context.Content;
            MapSectionDefinition section = content.GetSection(content.SectionAt(slot != null ? slot.Progress : 0f));
            Graph.GetNeighbors(node.Id, _neighbors);

            var weights = new float[_enabled.Count];
            float total = 0f;
            for (int i = 0; i < _enabled.Count; i++)
            {
                RoomCategory category = _enabled[i];
                float weight = _settings.WeightOf(category, section) * NeighbourFit(node, category)
                             * Mathf.Pow(_settings.RepeatDecay, CountOf(category));
                if (zone != ShipZone.None && category.Zone() != zone) weight *= _settings.OutOfZoneWeight;

                weights[i] = weight;
                total += weight;
            }
            if (total <= 0f) return _random.Pick(_enabled);

            float roll = _random.Value * total;
            for (int i = 0; i < weights.Length; i++)
            {
                roll -= weights[i];
                if (roll < 0f) return _enabled[i];
            }
            return _enabled[_enabled.Count - 1];
        }

        // Product of the affinities with every categorised neighbour; a direct door counts
        // its strength times over, since those rooms touch.
        private float NeighbourFit(MapNode node, RoomCategory category)
        {
            float fit = 1f;
            foreach (int id in _neighbors)
            {
                if (!Graph.TryGetNode(id, out MapNode other) || other.Category == RoomCategory.None) continue;

                MapConnection link = Graph.GetConnection(node.Id, id);
                float strength = link != null && link.Direct ? _settings.DirectDoorStrength : 1f;
                fit *= Mathf.Pow(_settings.Affinity(category, other.Category), strength);
            }
            return fit;
        }

        private void Count(RoomCategory category) => _counts[category] = CountOf(category) + 1;

        private int CountOf(RoomCategory category) => _counts.TryGetValue(category, out int count) ? count : 0;
    }
}
