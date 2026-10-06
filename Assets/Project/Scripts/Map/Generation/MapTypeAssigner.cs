using System.Collections.Generic;
using CGD.Core;
using UnityEngine;

namespace CGD.Map
{
    // Decides what each room is, in four passes:
    //   1. pins — locked nodes from the previous map claim the closest matching room
    //   2. guaranteed rooms — before the Boss, behind every gate, then depth windows, as
    //      the content lists them
    //   3. minimums — each rule gets its Min rooms, dead ends first when it prefers them,
    //      most constrained rule first
    //   4. fill — remaining rooms roll a weighted rule that still has room under its Max
    // A rule is only offered rooms matching its placement, depth, adjacency and spacing
    // limits and the content's pacing; rules that want space prefer rooms with an empty
    // grid cell beside them. Counts, weights and limits come through the run's modifiers
    // (MapRunTuning). Guaranteed rooms count toward their type's rule and never exceed its Max.
    internal class MapTypeAssigner
    {
        // Weight multipliers when filling: PreferDeadEnds rules at a dead end, WantsSpace
        // rules at a room with free cells around it.
        private const float DeadEndWeightMultiplier = 4f;
        private const float SpaceWeightMultiplier   = 3f;

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

        private MapGraph           Graph   => _context.Graph;
        private MapContentSettings Content => _context.Content;
        private MapRunTuning       Tuning  => _context.Tuning;
        private MapPacingSettings  Pacing  => Content.Pacing;

        public void Assign(IReadOnlyList<MapNodePin> pins)
        {
            foreach (MapSlot slot in _context.Slots)
                if (slot.IsStructural)
                    _assigned.Add(slot.NodeId);

            foreach (MapNodePin pin in pins)
                PinnedNodeIds.Add(PlacePin(pin));

            PlaceGuarantees(MapGuaranteeSpot.BeforeBoss);
            PlaceGuarantees(MapGuaranteeSpot.BehindEveryGate);
            PlaceGuarantees(MapGuaranteeSpot.WithinDepth);

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

        // --- Guaranteed rooms ----------------------------------------------------------

        private void PlaceGuarantees(MapGuaranteeSpot spot)
        {
            foreach (MapGuarantee guarantee in Content.Guarantees)
            {
                if (guarantee == null || !guarantee.IsValid || guarantee.Spot != spot) continue;
                switch (spot)
                {
                    case MapGuaranteeSpot.BeforeBoss:      PlaceBossApproach(guarantee); break;
                    case MapGuaranteeSpot.BehindEveryGate: PlaceGatedRewards(guarantee); break;
                    case MapGuaranteeSpot.WithinDepth:     PlaceWithinDepth(guarantee);  break;
                }
            }
        }

        // The main path room the Boss is entered from. Asking for it is explicit, so it
        // overrides the type rule's depth and placement — but not its maximum, and not a pin.
        private void PlaceBossApproach(MapGuarantee guarantee)
        {
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

        // One per gated area, unless a pin already put one there. Dead ends first, so the
        // reward sits at the far end of the area; the rule's limits are followed when any
        // room in the area allows it.
        private void PlaceGatedRewards(MapGuarantee guarantee)
        {
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

        // Count rooms in the window, minus any already there (pinned or gated rewards).
        // Follows the type's rule apart from depth (the window replaces it) and prefers
        // branch rooms, so reaching them can mean leaving the main path.
        private void PlaceWithinDepth(MapGuarantee guarantee)
        {
            IntRange window = guarantee.Depth;
            MapNodeTypeRule rule = Content.GetRule(guarantee.Type);

            for (int placed = CountWithinDepth(guarantee.Type, window); placed < guarantee.Count; placed++)
            {
                if (!HasRoomFor(guarantee.Type)) return;

                _candidates.Clear();
                foreach (MapSlot slot in _context.Slots)
                    if (slot.Depth >= window.Min && slot.Depth <= window.Max && CanPlaceIgnoringDepth(rule, guarantee.Type, slot))
                        _candidates.Add(slot);

                if (_candidates.Exists(s => !s.OnMainPath))
                    _candidates.RemoveAll(s => s.OnMainPath);

                if (_candidates.Count == 0)
                {
                    _context.Warnings.Add($"No room {window.Min}–{window.Max} deep can take guaranteed {guarantee.Type} {placed + 1} of {guarantee.Count}.");
                    return;
                }

                SetType(_random.Pick(_candidates), guarantee.Type);
            }
        }

        private int CountWithinDepth(MapNodeType type, IntRange window)
        {
            int count = 0;
            foreach (MapSlot slot in _context.Slots)
                if (_assigned.Contains(slot.NodeId) && slot.Depth >= window.Min && slot.Depth <= window.Max
                    && Graph.TryGetNode(slot.NodeId, out MapNode node) && node.Type == type)
                    count++;
            return count;
        }

        // --- Minimums and fill ----------------------------------------------------------

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
                pending.RemoveAll(r => CountOf(r.Type) >= Tuning.Min(r));
                if (pending.Count == 0) return;

                MapNodeTypeRule rule = MostConstrained(pending, out int candidateCount);
                if (candidateCount == 0)
                {
                    _context.Warnings.Add($"{rule.Type}: placed {CountOf(rule.Type)} of minimum {Tuning.Min(rule)} — no room matches its placement rules.");
                    pending.Remove(rule);
                    continue;
                }

                CollectCandidates(rule, deadEndsOnly: rule.PreferDeadEnds);
                if (_candidates.Count == 0)
                    CollectCandidates(rule, deadEndsOnly: false);
                if (rule.WantsSpace && _candidates.Exists(s => s.FreeNeighbors > 0))
                    _candidates.RemoveAll(s => s.FreeNeighbors == 0);

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

            if (total <= 0f) return FallbackType(slot);

            float roll = _random.Value * total;
            foreach (MapNodeTypeRule rule in Content.NodeRules)
            {
                roll -= FillWeight(rule, slot);
                if (roll < 0f) return rule.Type;
            }
            return FallbackType(slot);
        }

        // No rule can take the room: the fill type, or a calm type when a fight here would
        // break the pacing — the rest type first, then any calm rule that allows this room
        // and has room under its Max. Only when none does is the rest type forced in.
        private MapNodeType FallbackType(MapSlot slot)
        {
            if (PacingAllows(Content.FillType, slot)) return Content.FillType;
            if (CanFallBackTo(Pacing.RestType, slot)) return Pacing.RestType;

            foreach (MapNodeTypeRule rule in Content.NodeRules)
                if (!Pacing.IsCombat(rule.Type) && CanFallBackTo(rule.Type, slot))
                    return rule.Type;

            _context.Warnings.Add($"#{slot.NodeId} is a {Pacing.RestType} over its rule's limits: no calm room fits there and a fight would break the pacing.");
            return Pacing.RestType;
        }

        private bool CanFallBackTo(MapNodeType type, MapSlot slot)
        {
            if (type.IsStructural() || !HasRoomFor(type)) return false;
            MapNodeTypeRule rule = Content.GetRule(type);
            return rule == null || CanPlace(rule, slot);
        }

        private float FillWeight(MapNodeTypeRule rule, MapSlot slot)
        {
            if (CountOf(rule.Type) >= Tuning.Max(rule) || !CanPlace(rule, slot)) return 0f;

            float weight = Tuning.Weight(rule);
            if (rule.PreferDeadEnds && slot.IsDeadEnd)       weight *= DeadEndWeightMultiplier;
            if (rule.WantsSpace && slot.FreeNeighbors > 0)   weight *= SpaceWeightMultiplier;
            return weight;
        }

        // --- Limits ---------------------------------------------------------------------

        private bool CanPlace(MapNodeTypeRule rule, MapSlot slot) =>
            rule.AllowsDepth(slot.Progress) && CanPlaceIgnoringDepth(rule, rule.Type, slot);

        // `rule` may be null for a type without one: then only the room being free and the
        // pacing matter.
        private bool CanPlaceIgnoringDepth(MapNodeTypeRule rule, MapNodeType type, MapSlot slot)
        {
            if (type.IsStructural() || _assigned.Contains(slot.NodeId)) return false;
            if (!PacingAllows(type, slot)) return false;
            if (rule == null) return true;
            if (!rule.AllowsPlacement(slot.OnMainPath)) return false;
            if (!rule.AllowAdjacentSameType && HasAssignedNeighbor(slot.NodeId, type)) return false;
            return rule.MinSpacing <= 1 || !HasAssignedWithin(slot.NodeId, type, rule.MinSpacing - 1);
        }

        // On the main path: no more fights in a row than allowed (the breather after a tier-3
        // fight is kept by MapTierPainter, which runs once the types are set).
        private bool PacingAllows(MapNodeType type, MapSlot slot)
        {
            if (slot.MainPathIndex < 0 || !Pacing.IsCombat(type)) return true;

            List<int> path = _context.MainPath;
            int i = slot.MainPathIndex;

            if (Pacing.MaxCombatInARow <= 0) return true;
            int run = 1;
            for (int j = i - 1; j >= 0 && IsAssignedCombat(path[j]); j--) run++;
            for (int j = i + 1; j < path.Count && IsAssignedCombat(path[j]); j++) run++;
            return run <= Pacing.MaxCombatInARow;
        }

        private bool IsAssignedCombat(int nodeId)
        {
            MapNodeType? type = AssignedTypeAt(nodeId);
            return type.HasValue && !type.Value.IsStructural() && Pacing.IsCombat(type.Value);
        }

        private MapNodeType? AssignedTypeAt(int nodeId) =>
            _assigned.Contains(nodeId) && Graph.TryGetNode(nodeId, out MapNode node) ? node.Type : null;

        // An assigned room of `type` within `distance` connections.
        private bool HasAssignedWithin(int nodeId, MapNodeType type, int distance)
        {
            foreach (var (id, steps) in MapGraphSearch.Distances(Graph, nodeId, includeShortcuts: true))
                if (steps > 0 && steps <= distance && AssignedTypeAt(id) == type) return true;
            return false;
        }

        private bool HasRoomFor(MapNodeType type) => Tuning.HasRoomFor(Content, type, CountOf(type));

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
