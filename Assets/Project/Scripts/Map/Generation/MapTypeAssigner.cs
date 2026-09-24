using System.Collections.Generic;
using UnityEngine;

namespace CGD.Map
{
    // Decides what each room is, in three passes:
    //   1. pins — locked nodes from the previous map claim the closest matching room
    //   2. minimums — each rule gets its Min rooms, dead ends first when it prefers them,
    //      most constrained rule first
    //   3. fill — remaining rooms roll a weighted rule that still has room under its Max
    // A rule is only offered rooms matching its placement, depth and adjacency limits.
    internal class MapTypeAssigner
    {
        // Weight multiplier for PreferDeadEnds rules when filling a dead end.
        private const float DeadEndWeightMultiplier = 4f;

        private readonly MapGenerationContext          _context;
        private readonly HashSet<int>                  _assigned  = new();
        private readonly Dictionary<MapNodeType, int>  _counts    = new();
        private readonly List<int>                     _neighbors = new();
        private readonly List<MapSlot>                 _candidates = new();

        public MapTypeAssigner(MapGenerationContext context) => _context = context;

        // Node id each pin landed on, or MapGenerationResult.PinNotPlaced.
        public List<int> PinnedNodeIds { get; } = new();

        private MapGraph              Graph    => _context.Graph;
        private MapGenerationSettings Settings => _context.Settings;

        public void Assign(IReadOnlyList<MapNodePin> pins)
        {
            foreach (MapSlot slot in _context.Slots)
                if (slot.IsStructural)
                    _assigned.Add(slot.NodeId);

            foreach (MapNodePin pin in pins)
                PinnedNodeIds.Add(PlacePin(pin));

            PlaceMinimums();

            FillRemaining();
        }

        private int PlacePin(MapNodePin pin)
        {
            if (MapGenerationSettings.IsStructural(pin.Type))
                return Graph.FindFirst(pin.Type)?.Id ?? MapGenerationResult.PinNotPlaced;

            MapSlot best = ClosestFreeSlot(pin, requireSamePath: true) ?? ClosestFreeSlot(pin, requireSamePath: false);
            if (best == null)
            {
                _context.Warnings.Add($"No room left for locked {pin.Type} node.");
                return MapGenerationResult.PinNotPlaced;
            }

            SetType(best, pin.Type);
            return best.NodeId;
        }

        private MapSlot ClosestFreeSlot(MapNodePin pin, bool requireSamePath)
        {
            MapSlot best = null;
            float bestDistance = float.MaxValue;

            foreach (MapSlot slot in _context.Slots)
            {
                if (_assigned.Contains(slot.NodeId)) continue;
                if (requireSamePath && slot.OnMainPath != pin.OnMainPath) continue;

                float distance = Mathf.Abs(slot.Progress - pin.Progress);
                if (distance >= bestDistance) continue;

                best = slot;
                bestDistance = distance;
            }
            return best;
        }

        // Most-constrained first: each step serves the rule with the fewest free rooms
        // left, so broad rules like Combat can't take the only rooms a narrow rule (a
        // branch-only Treasure) could use.
        private void PlaceMinimums()
        {
            var pending = new List<MapNodeTypeRule>();
            foreach (MapNodeTypeRule rule in Settings.NodeRules)
                if (!MapGenerationSettings.IsStructural(rule.Type))
                    pending.Add(rule);

            while (true)
            {
                pending.RemoveAll(r => CountOf(r.Type) >= r.Min);
                if (pending.Count == 0) return;

                MapNodeTypeRule rule = MostConstrained(pending, out int candidateCount);
                if (candidateCount == 0)
                {
                    _context.Warnings.Add($"{rule.Type}: placed {CountOf(rule.Type)} of minimum {rule.Min} — no room matches its placement rules.");
                    pending.Remove(rule);
                    continue;
                }

                CollectCandidates(rule, deadEndsOnly: rule.PreferDeadEnds);
                if (_candidates.Count == 0)
                    CollectCandidates(rule, deadEndsOnly: false);

                SetType(_context.Pick(_candidates), rule.Type);
            }
        }

        private MapNodeTypeRule MostConstrained(List<MapNodeTypeRule> rules, out int candidateCount)
        {
            MapNodeTypeRule best = null;
            candidateCount = int.MaxValue;

            foreach (MapNodeTypeRule rule in rules)
            {
                CollectCandidates(rule, deadEndsOnly: false);
                if (_candidates.Count >= candidateCount) continue;

                best = rule;
                candidateCount = _candidates.Count;
            }
            return best;
        }

        private void CollectCandidates(MapNodeTypeRule rule, bool deadEndsOnly)
        {
            _candidates.Clear();
            foreach (MapSlot slot in _context.Slots)
            {
                if (deadEndsOnly && !slot.IsDeadEnd) continue;
                if (CanPlace(rule, slot)) _candidates.Add(slot);
            }
        }

        // Visits free rooms in random order so earlier ids don't soak up the rarer types.
        private void FillRemaining()
        {
            _candidates.Clear();
            foreach (MapSlot slot in _context.Slots)
                if (!_assigned.Contains(slot.NodeId))
                    _candidates.Add(slot);

            Shuffle(_candidates);

            foreach (MapSlot slot in _candidates)
                SetType(slot, RollType(slot));
        }

        private MapNodeType RollType(MapSlot slot)
        {
            float total = 0f;
            foreach (MapNodeTypeRule rule in Settings.NodeRules)
                total += FillWeight(rule, slot);

            if (total <= 0f) return Settings.FillType;

            float roll = _context.NextFloat() * total;
            foreach (MapNodeTypeRule rule in Settings.NodeRules)
            {
                roll -= FillWeight(rule, slot);
                if (roll < 0f) return rule.Type;
            }
            return Settings.FillType;
        }

        private float FillWeight(MapNodeTypeRule rule, MapSlot slot)
        {
            if (CountOf(rule.Type) >= rule.Max || !CanPlace(rule, slot)) return 0f;

            bool favoured = rule.PreferDeadEnds && slot.IsDeadEnd;
            return favoured ? rule.Weight * DeadEndWeightMultiplier : rule.Weight;
        }

        private bool CanPlace(MapNodeTypeRule rule, MapSlot slot)
        {
            if (MapGenerationSettings.IsStructural(rule.Type)) return false;
            if (_assigned.Contains(slot.NodeId)) return false;
            if (!rule.Allows(slot.Progress, slot.OnMainPath)) return false;
            return rule.AllowAdjacentSameType || !HasAssignedNeighbor(slot.NodeId, rule.Type);
        }

        private bool HasAssignedNeighbor(int nodeId, MapNodeType type)
        {
            Graph.GetNeighbors(nodeId, _neighbors);
            foreach (int neighbor in _neighbors)
                if (_assigned.Contains(neighbor) && Graph.TryGetNode(neighbor, out MapNode node) && node.Type == type)
                    return true;
            return false;
        }

        private void SetType(MapSlot slot, MapNodeType type)
        {
            if (Graph.TryGetNode(slot.NodeId, out MapNode node))
                node.Type = type;

            _assigned.Add(slot.NodeId);
            _counts[type] = CountOf(type) + 1;
        }

        private int CountOf(MapNodeType type) => _counts.TryGetValue(type, out int count) ? count : 0;

        private void Shuffle(List<MapSlot> slots)
        {
            for (int i = slots.Count - 1; i > 0; i--)
            {
                int j = _context.Random.Next(i + 1);
                (slots[i], slots[j]) = (slots[j], slots[i]);
            }
        }
    }
}
