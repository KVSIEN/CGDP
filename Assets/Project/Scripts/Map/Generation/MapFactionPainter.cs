using System.Collections.Generic;
using CGD.Core;
using UnityEngine;

namespace CGD.Map
{
    // Splits the rooms between the factions — the ship's warped realities — as the map's
    // faction mix asks. Every room ends up in one of them; a faction only changes how a
    // room looks and whose enemies it fields, never what the room is.
    //   1. shares — equal, or most of the rooms for one dominant faction picked per map
    //   2. territories — each faction seeds its pockets (spread apart) and grows into
    //      free neighbouring rooms until it holds its share; walled in, it reaches past
    //      the rooms in its way
    //   3. scatter — the anomaly flips rooms to a random faction (weighted by share),
    //      regardless of territory
    // Influence is how firmly a room is held: 1 at a pocket's origin, less with each room
    // away from it, and a flat ScatteredInfluence for rooms the anomaly flipped.
    internal class MapFactionPainter
    {
        private const float InfluenceFalloff   = 0.85f;
        private const float MinInfluence       = 0.35f;
        private const float ScatteredInfluence = 0.5f;
        // Origins are the farthest of this many random rooms from the other origins, so
        // territories start apart without always taking the same rooms.
        private const int OriginCandidates = 3;

        private readonly MapGenerationContext _context;
        private readonly MapFactionMix        _mix;
        private readonly RandomStream         _random;
        private readonly List<int>            _neighbors = new();

        private readonly Dictionary<int, int>   _owner     = new();
        private readonly Dictionary<int, float> _influence = new();
        private float[]   _shares;
        private int[]     _held;
        private List<int> _origins;

        public MapFactionPainter(MapGenerationContext context, MapFactionMix mix)
        {
            _context = context;
            _mix     = mix;
            _random  = context.StreamFor(MapGenerator.FactionsLayer);
        }

        private MapGraph Graph => _context.Graph;

        // Without a mix: an even split, every room held, no scatter.
        private float Dominance => _mix != null ? _mix.Dominance : 0f;
        private float Scatter   => _mix != null ? _mix.Scatter   : 0f;
        private int   Pockets   => _mix != null ? _mix.PocketsPerFaction : 1;

        public void Paint()
        {
            int factions = _context.Content.Factions.Count;
            if (factions == 0) return;

            _shares = Shares(factions, Dominance, _random.Range(0, factions));
            _held   = new int[factions];

            int rooms = Graph.Nodes.Count;
            var quota = new int[factions];
            for (int f = 0; f < factions; f++)
                quota[f] = Mathf.RoundToInt(_shares[f] * rooms);

            SeedOrigins(quota);
            Grow(rooms, quota);
            Grow(rooms, null);
            ClaimLeftovers();
            ScatterRooms();
            Apply();
        }

        // `dominant` takes Lerp(1/count, 1, dominance); the others split what's left evenly.
        private static float[] Shares(int count, float dominance, int dominant)
        {
            var shares = new float[count];
            float even = 1f / count;
            float top  = Mathf.Lerp(even, 1f, dominance);
            float rest = count > 1 ? (1f - top) / (count - 1) : 0f;
            for (int f = 0; f < count; f++)
                shares[f] = f == dominant ? top : rest;
            return shares;
        }

        private void SeedOrigins(int[] quota)
        {
            var candidates = new List<int>();
            foreach (MapSlot slot in _context.Slots)
                if (!slot.IsStructural) candidates.Add(slot.NodeId);

            _origins = new List<int>();
            var distance = new Dictionary<int, int>();

            for (int f = 0; f < quota.Length; f++)
            {
                int pockets = Mathf.Min(Pockets, quota[f]);
                for (int p = 0; p < pockets; p++)
                {
                    if (candidates.Count == 0)
                    {
                        _context.Warnings.Add($"No room left to seed faction '{_context.Content.FactionName(f)}'.");
                        return;
                    }

                    int origin = FarthestOf(candidates, distance);
                    candidates.Remove(origin);
                    Claim(origin, f, 1f);
                    _origins.Add(origin);

                    foreach (var (id, steps) in MapGraphSearch.Distances(Graph, origin, includeShortcuts: true))
                        if (!distance.TryGetValue(id, out int known) || steps < known) distance[id] = steps;
                }
            }
        }

        private int FarthestOf(List<int> candidates, Dictionary<int, int> distance)
        {
            int best = _random.Pick(candidates), bestDistance = DistanceOf(best, distance);
            for (int i = 1; i < OriginCandidates; i++)
            {
                int candidate = _random.Pick(candidates), d = DistanceOf(candidate, distance);
                if (d <= bestDistance) continue;
                best = candidate;
                bestDistance = d;
            }
            return best;
        }

        private static int DistanceOf(int nodeId, Dictionary<int, int> distance) =>
            distance.TryGetValue(nodeId, out int d) ? d : int.MaxValue;

        // Grows one room at a time until `target` rooms are held. With a quota, only
        // factions under theirs grow, weighted by how far under they are; without (the
        // rooms quotas leave over from rounding), every faction does, weighted by share.
        private void Grow(int target, int[] quota)
        {
            var frontier = new List<(int node, int from)>();
            var weights  = new float[_held.Length];

            while (_owner.Count < target)
            {
                float total = 0f;
                for (int f = 0; f < _held.Length; f++)
                {
                    weights[f] = quota != null ? Mathf.Max(0, quota[f] - _held[f]) : _shares[f];
                    if (weights[f] > 0f && !CollectFrontier(f, frontier)) weights[f] = 0f;
                    total += weights[f];
                }
                if (total <= 0f) return;

                int faction = PickIndex(weights, total);
                CollectFrontier(faction, frontier);
                var (node, from) = _random.Pick(frontier);
                Claim(node, faction, Mathf.Max(MinInfluence, _influence[from] * InfluenceFalloff));
            }
        }

        // Free rooms bordering the faction's territory, each paired with the held room
        // beside it. A room bordering several held rooms is listed once per neighbour,
        // which favours filling in over reaching out. A faction walled in by others' rooms
        // reaches past them to the nearest free rooms instead (an enclave), so a small
        // pocket in a corridor can't stop a dominant faction from taking its share.
        private bool CollectFrontier(int faction, List<(int node, int from)> frontier)
        {
            frontier.Clear();
            foreach (var (id, owner) in _owner)
            {
                if (owner != faction) continue;
                Graph.GetNeighbors(id, _neighbors);
                foreach (int next in _neighbors)
                    if (!_owner.ContainsKey(next)) frontier.Add((next, id));
            }
            if (frontier.Count == 0) CollectNearestFree(faction, frontier);
            return frontier.Count > 0;
        }

        // Breadth-first from the whole territory through anyone's rooms; the free rooms at
        // the first distance any are found, paired with the territory room each was reached from.
        private void CollectNearestFree(int faction, List<(int node, int from)> frontier)
        {
            var source = new Dictionary<int, int>();
            var layer  = new List<int>();
            foreach (var (id, owner) in _owner)
            {
                if (owner != faction) continue;
                source[id] = id;
                layer.Add(id);
            }

            var next = new List<int>();
            while (layer.Count > 0 && frontier.Count == 0)
            {
                next.Clear();
                foreach (int id in layer)
                {
                    Graph.GetNeighbors(id, _neighbors);
                    foreach (int neighbor in _neighbors)
                    {
                        if (source.ContainsKey(neighbor)) continue;
                        source[neighbor] = source[id];
                        if (_owner.ContainsKey(neighbor)) next.Add(neighbor);
                        else frontier.Add((neighbor, source[id]));
                    }
                }
                layer.Clear();
                layer.AddRange(next);
            }
        }

        private int PickIndex(float[] weights, float total)
        {
            float roll = _random.Value * total;
            for (int i = 0; i < weights.Length; i++)
            {
                roll -= weights[i];
                if (roll < 0f && weights[i] > 0f) return i;
            }
            for (int i = weights.Length - 1; i >= 0; i--)
                if (weights[i] > 0f) return i;
            return 0;
        }

        // Only when no territory could start (a map with no room to seed one): every
        // room still belongs to some reality.
        private void ClaimLeftovers()
        {
            foreach (MapNode node in Graph.Nodes)
                if (!_owner.ContainsKey(node.Id))
                    Claim(node.Id, PickIndex(_shares, 1f), ScatteredInfluence);
        }

        private void ScatterRooms()
        {
            if (Scatter <= 0f) return;

            foreach (MapNode node in Graph.Nodes)
            {
                if (_origins.Contains(node.Id) || !_random.Chance(Scatter)) continue;

                _held[_owner[node.Id]]--;
                Claim(node.Id, PickIndex(_shares, 1f), ScatteredInfluence);
            }
        }

        private void Claim(int nodeId, int faction, float influence)
        {
            _owner[nodeId]     = faction;
            _influence[nodeId] = influence;
            _held[faction]++;
        }

        private void Apply()
        {
            foreach (MapNode node in Graph.Nodes)
                node.SetFaction(_owner[node.Id], _influence[node.Id]);
        }
    }
}
