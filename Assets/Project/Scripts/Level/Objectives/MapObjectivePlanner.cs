using System;
using System.Collections.Generic;
using CGD.Core;
using CGD.Map;

namespace CGD.Level
{
    // Picks a map's objectives and the rooms their steps happen in. Only rooms the player can
    // reach without the Boss count (secret passages don't), Start, Exit, Boss and Shop rooms
    // are never used, and no room hosts two steps. `canClear` says whether a room will have
    // enemies to clear. Templates that can't be fitted are skipped; each is used once per map.
    public class MapObjectivePlanner
    {
        private const int AttemptsPerObjective = 8;

        private readonly MapGraph _graph;
        private readonly RandomStream _random;
        private readonly Func<MapNode, bool> _canClear;
        private readonly List<MapNode> _candidates = new();
        private readonly HashSet<int> _used = new();

        public MapObjectivePlanner(MapGraph graph, RandomStream random, Func<MapNode, bool> canClear)
        {
            _graph    = graph;
            _random   = random;
            _canClear = canClear;
            CollectCandidates();
        }

        public List<PlannedObjective> Plan(IReadOnlyList<MapObjectiveTemplate> templates, int mainCount, int sideCount)
        {
            var planned = new List<PlannedObjective>();
            var unused  = new List<MapObjectiveTemplate>();
            foreach (MapObjectiveTemplate template in templates)
                if (template != null && template.Steps.Length > 0 && !unused.Contains(template)) unused.Add(template);

            PlanSome(true, mainCount, unused, planned);
            PlanSome(false, sideCount, unused, planned);
            return planned;
        }

        private void PlanSome(bool main, int count, List<MapObjectiveTemplate> unused, List<PlannedObjective> planned)
        {
            for (int i = 0; i < count; i++)
            {
                for (int attempt = 0; attempt < AttemptsPerObjective; attempt++)
                {
                    var options = unused.FindAll(t => t.CanBe(main) && t.Weight > 0f);
                    if (options.Count == 0) return;

                    MapObjectiveTemplate template = _random.PickWeighted(options, t => t.Weight);
                    if (!TryAssign(template, out List<int> rooms, out int topTier)) continue;

                    unused.Remove(template);
                    foreach (int id in rooms) _used.Add(id);
                    planned.Add(new PlannedObjective(template, main, rooms, topTier));
                    break;
                }
            }
        }

        private bool TryAssign(MapObjectiveTemplate template, out List<int> rooms, out int topTier)
        {
            rooms   = new List<int>();
            topTier = 1;
            int faction = template.SameFaction ? PickFaction(template.Steps[0]) : MapNode.NoFaction;
            var taken = new HashSet<int>();
            int previousTier = 0;

            foreach (ObjectiveStepTemplate step in template.Steps)
            {
                int after = previousTier;
                var options = _candidates.FindAll(n => !_used.Contains(n.Id) && !taken.Contains(n.Id) && Fits(step, n)
                                                       && FitsOrder(template.TierOrder, after, n.EffectiveTier)
                                                       && (!template.SameFaction || n.Faction == faction));
                if (options.Count == 0) return false;

                MapNode room = _random.Pick(options);
                taken.Add(room.Id);
                rooms.Add(room.Id);
                previousTier = room.EffectiveTier;
                topTier = Math.Max(topTier, room.EffectiveTier);
            }
            return true;
        }

        // A faction with a room that fits the first step, so the rest have somewhere to go.
        private int PickFaction(ObjectiveStepTemplate first)
        {
            var options = _candidates.FindAll(n => !_used.Contains(n.Id) && Fits(first, n));
            return options.Count > 0 ? _random.Pick(options).Faction : MapNode.NoFaction;
        }

        // previousTier 0 = the first step, which is free.
        private static bool FitsOrder(ObjectiveTierOrder order, int previousTier, int tier) => order switch
        {
            ObjectiveTierOrder.NeverDown => tier >= previousTier,
            ObjectiveTierOrder.Same      => previousTier == 0 || tier == previousTier,
            _                            => true,
        };

        private bool Fits(ObjectiveStepTemplate step, MapNode room) =>
            step.AcceptsTier(room.EffectiveTier) && (step.Action != ObjectiveAction.Clear || _canClear(room));

        // Rooms reachable from Start without passing the Boss or a secret passage.
        private void CollectCandidates()
        {
            MapNode start = _graph.FindFirst(MapNodeType.Start);
            MapNode boss  = _graph.FindFirst(MapNodeType.Boss);
            if (start == null) return;

            var seen  = new HashSet<int> { start.Id };
            var queue = new Queue<int>();
            queue.Enqueue(start.Id);
            while (queue.Count > 0)
            {
                int id = queue.Dequeue();
                foreach (MapConnection connection in _graph.Connections)
                {
                    if (!connection.Connects(id) || connection.Type == ConnectionType.Secret) continue;
                    int other = connection.Other(id);
                    if ((boss != null && other == boss.Id) || !seen.Add(other)) continue;
                    queue.Enqueue(other);
                }
            }

            foreach (int id in seen)
                if (_graph.TryGetNode(id, out MapNode node) && IsEligible(node.Type)) _candidates.Add(node);
            _candidates.Sort((a, b) => a.Id.CompareTo(b.Id)); // stable order, so a seed always plans the same
        }

        private static bool IsEligible(MapNodeType type) => !type.IsStructural() && type != MapNodeType.Shop;
    }
}
