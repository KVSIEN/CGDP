using System.Collections.Generic;
using CGD.Core;
using UnityEngine;

namespace CGD.Map
{
    // Decides what each room is, in four passes:
    //   1. pins — locked nodes from the previous map claim the closest matching room
    //   2. guaranteed rooms — the room before the Boss, a reward behind every gate and
    //      the early room, when the content asks for them
    //   3. minimums — each rule gets its Min rooms, dead ends first when it prefers them,
    //      most constrained rule first
    //   4. fill — remaining rooms roll a weighted rule that still has room under its Max
    // A rule is only offered rooms matching its placement, depth and adjacency limits.
    // Guaranteed rooms count toward their type's rule and never exceed its Max.
    internal class MapTypeAssigner
    {
        // Weight multiplier for PreferDeadEnds rules when filling a dead end.
        private const float DeadEndWeightMultiplier = 4f;

        private readonly MapGenerationContext          _context;
        private readonly RandomStream                  _random;
        private readonly HashSet<int>                  _assigned  = new();
        private readonly Dictionary<MapNodeType, int>  _counts    = new();
        private readonly List<int>                     _neighbors = new();
        private readonly List<MapSlot>                 _candidates = new();

        public MapTypeAssigner(MapGenerationContext context)
        {
            _context = context;
            _random  = context.StreamFor(MapGenerator.TypesLayer);
        }

        // Node id each pin landed on, or MapGenerationResult.PinNotPlaced.
        public List<int> PinnedNodeIds { get; } = new();

        private MapGraph              Graph    => _context.Graph;
        private MapContentSettings    Content  => _context.Content;

        public void Assign(IReadOnlyList<MapNodePin> pins)
        {
            foreach (MapSlot slot in _context.Slots)
                if (slot.IsStructural)
                    _assigned.Add(slot.NodeId);

            foreach (MapNodePin pin in pins)
                PinnedNodeIds.Add(PlacePin(pin));

            PlaceBossApproach();
            PlaceGatedRewards();
            PlaceEarlyRoom();

            PlaceMinimums();

            FillRemaining();
        }

        private int PlacePin(MapNodePin pin)
        {
            if (pin.Type.IsStructural())
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

        // The main path room the Boss is entered from. Asking for it is explicit, so it
        // overrides the type rule's depth and placement — but not its maximum, and not a pin.
        private void PlaceBossApproach()
        {
            MapRoomGuarantee guarantee = Content.BossApproach;
            if (!guarantee.Enabled) return;

            MapSlot slot = BossApproachSlot();
            if (slot == null || _assigned.Contains(slot.NodeId)) return;

            if (!HasRoomFor(guarantee.Type))
            {
                _context.Warnings.Add($"No {guarantee.Type} before the Boss — its rule's maximum is already reached.");
                return;
            }
            SetType(slot, guarantee.Type);
        }

        private MapSlot BossApproachSlot()
        {
            MapNode boss = Graph.FindFirst(MapNodeType.Boss);
            if (boss == null) return null;

            Graph.GetNeighbors(boss.Id, _neighbors);
            foreach (int id in _neighbors)
            {
                MapSlot slot = _context.GetSlot(id);
                if (slot != null && slot.OnMainPath && !_context.IsBossOrExit(id)) return slot;
            }
            return null;
        }

        // One reward per gated area, unless a pin already put one there. Dead ends first,
        // so the reward sits at the far end of the area; the rule's placement and depth
        // are followed when any room in the area allows it.
        private void PlaceGatedRewards()
        {
            MapRoomGuarantee guarantee = Content.GatedAreaReward;
            if (!guarantee.Enabled) return;

            MapNodeTypeRule rule = Content.GetRule(guarantee.Type);
            foreach (MapGatedArea area in _context.GatedAreas)
            {
                if (AreaHasType(area, guarantee.Type)) continue;
                if (!HasRoomFor(guarantee.Type))
                {
                    _context.Warnings.Add($"Not every gated area holds a {guarantee.Type} — its rule's maximum is already reached.");
                    return;
                }

                CollectFreeSlots(area.Rooms, rule);
                if (_candidates.Count == 0) CollectFreeSlots(area.Rooms, null);
                if (_candidates.Count == 0)
                {
                    _context.Warnings.Add($"The area behind the gate at #{area.OutsideId} has no free room for its {guarantee.Type}.");
                    continue;
                }

                if (_candidates.Exists(s => s.IsDeadEnd))
                    _candidates.RemoveAll(s => !s.IsDeadEnd);
                SetType(_random.Pick(_candidates), guarantee.Type);
            }
        }

        private bool AreaHasType(MapGatedArea area, MapNodeType type)
        {
            foreach (int id in area.Rooms)
                if (_assigned.Contains(id) && Graph.TryGetNode(id, out MapNode node) && node.Type == type)
                    return true;
            return false;
        }

        // Free rooms among `ids` that `rule` allows (any free room when rule is null).
        private void CollectFreeSlots(IReadOnlyList<int> ids, MapNodeTypeRule rule)
        {
            _candidates.Clear();
            foreach (int id in ids)
            {
                MapSlot slot = _context.GetSlot(id);
                if (slot == null || _assigned.Contains(id)) continue;
                if (rule == null || CanPlace(rule, slot)) _candidates.Add(slot);
            }
        }

        // Follows the type's rule for placement and adjacency (the window stands in for its
        // depth limits), and prefers a branch room so reaching it can mean leaving the
        // main path. A pinned or gated-reward room of the type already in the window counts.
        private void PlaceEarlyRoom()
        {
            MapRoomGuarantee guarantee = Content.EarlyRoom;
            IntRange         window    = Content.EarlyRoomDepth;
            if (!guarantee.Enabled || HasAssignedWithinDepth(guarantee.Type, window)) return;
            if (!HasRoomFor(guarantee.Type)) return;

            MapNodeTypeRule rule = Content.GetRule(guarantee.Type);
            _candidates.Clear();
            foreach (MapSlot slot in _context.Slots)
                if (slot.Depth >= window.Min && slot.Depth <= window.Max && CanPlaceIgnoringDepth(rule, guarantee.Type, slot))
                    _candidates.Add(slot);

            if (_candidates.Exists(s => !s.OnMainPath))
                _candidates.RemoveAll(s => s.OnMainPath);

            if (_candidates.Count == 0)
            {
                _context.Warnings.Add($"No room {window.Min}–{window.Max} deep can take the early {guarantee.Type}.");
                return;
            }

            SetType(_random.Pick(_candidates), guarantee.Type);
        }

        private bool HasAssignedWithinDepth(MapNodeType type, IntRange window)
        {
            foreach (MapSlot slot in _context.Slots)
                if (_assigned.Contains(slot.NodeId) && slot.Depth >= window.Min && slot.Depth <= window.Max
                    && Graph.TryGetNode(slot.NodeId, out MapNode node) && node.Type == type)
                    return true;
            return false;
        }

        // Most-constrained first: each step serves the rule with the fewest free rooms
        // left, so broad rules like Combat can't take the only rooms a narrow rule (a
        // branch-only Treasure) could use.
        private void PlaceMinimums()
        {
            var pending = new List<MapNodeTypeRule>();
            foreach (MapNodeTypeRule rule in Content.NodeRules)
                if (!rule.Type.IsStructural())
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

                SetType(_random.Pick(_candidates), rule.Type);
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

            _random.Shuffle(_candidates);

            foreach (MapSlot slot in _candidates)
                SetType(slot, RollType(slot));
        }

        private MapNodeType RollType(MapSlot slot)
        {
            float total = 0f;
            foreach (MapNodeTypeRule rule in Content.NodeRules)
                total += FillWeight(rule, slot);

            if (total <= 0f) return Content.FillType;

            float roll = _random.Value * total;
            foreach (MapNodeTypeRule rule in Content.NodeRules)
            {
                roll -= FillWeight(rule, slot);
                if (roll < 0f) return rule.Type;
            }
            return Content.FillType;
        }

        private float FillWeight(MapNodeTypeRule rule, MapSlot slot)
        {
            if (CountOf(rule.Type) >= rule.Max || !CanPlace(rule, slot)) return 0f;

            bool favoured = rule.PreferDeadEnds && slot.IsDeadEnd;
            return favoured ? rule.Weight * DeadEndWeightMultiplier : rule.Weight;
        }

        private bool CanPlace(MapNodeTypeRule rule, MapSlot slot) =>
            rule.AllowsDepth(slot.Progress) && CanPlaceIgnoringDepth(rule, rule.Type, slot);

        // `rule` may be null for a type without one: then only the room being free matters.
        private bool CanPlaceIgnoringDepth(MapNodeTypeRule rule, MapNodeType type, MapSlot slot)
        {
            if (type.IsStructural() || _assigned.Contains(slot.NodeId)) return false;
            if (rule == null) return true;
            if (!rule.AllowsPlacement(slot.OnMainPath)) return false;
            return rule.AllowAdjacentSameType || !HasAssignedNeighbor(slot.NodeId, type);
        }

        private bool HasRoomFor(MapNodeType type) => Content.HasRoomFor(type, CountOf(type));

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
    }
}
