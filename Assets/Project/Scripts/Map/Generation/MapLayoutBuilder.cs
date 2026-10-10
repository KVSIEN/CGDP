using System.Collections.Generic;
using CGD.Core;
using UnityEngine;

namespace CGD.Map
{
    // Grows the rooms as a tree on MapGrid: a main path walked out from Start to the Boss
    // and Exit, hub branches off Start, then branches off the path (or off earlier
    // branches) until the map has its optional rooms. Each new room takes a free cell
    // next to the room it hangs off. Branches may rejoin a neighbouring room; every
    // other extra link is MapLinkBuilder's job.
    //
    // Rooms are created with the fill type; MapTypeAssigner decides the real types.
    internal class MapLayoutBuilder
    {
        // Consecutive failed branches before the map stops growing short of its optional rooms.
        private const int PlacementAttempts = 16;
        private const int NoRoom = -1;

        private readonly MapGenerationContext _context;
        private readonly RandomStream         _random;
        // Start and the rooms between it and the Boss: where branches attach.
        private readonly List<int>            _pathRooms   = new();
        private readonly List<int>            _branchRooms = new();
        private readonly List<Vector2Int>     _openCells   = new();
        private readonly List<int>            _neighbors   = new();

        private static readonly Vector2Int[] Directions =
            { Vector2Int.right, Vector2Int.up, Vector2Int.left, Vector2Int.down };

        public MapLayoutBuilder(MapGenerationContext context, RandomStream random)
        {
            _context = context;
            _random  = random;
        }

        private MapGraph              Graph    => _context.Graph;
        private MapGrid               Grid     => _context.Grid;
        private MapLayoutSettings     Layout   => _context.Layout;
        private MapNodeType           FillType => _context.Content.FillType;

        public void Build()
        {
            BuildMainPath();

            int optional = Mathf.Max(0, Layout.OptionalRooms.Evaluate(_random) + _context.Tuning.ExtraOptionalRooms);
            int target   = Graph.Nodes.Count + optional;
            AddHubBranches(target);
            FillWithBranches(target, optional);
        }

        // Never steps back toward Start (see RemoveBackward), so the path can't box itself
        // in and the boss always ends up further from Start than anything on the path.
        private void BuildMainPath()
        {
            MapPathSettings path = Layout.MainPath;
            int length = Layout.MainPathLength(path.Length.Evaluate(_random));

            Vector2Int heading = path.Direction == MapPathDirection.Any ? _random.Pick(Directions) : Vector2Int.right;
            int previous = AddRoom(MapNodeType.Start, Vector2Int.zero, NoRoom);
            Grid.MarkPath(previous);
            _pathRooms.Add(previous);

            for (int i = 0; i < length; i++)
            {
                previous = AddPathRoom(FillType, previous, ref heading, path.Winding);
                _pathRooms.Add(previous);
            }

            int boss = AddPathRoom(MapNodeType.Boss, previous, ref heading, path.Winding);
            AddPathRoom(MapNodeType.Exit, boss, ref heading, path.Winding);
        }

        private int AddPathRoom(MapNodeType type, int previous, ref Vector2Int heading, float winding)
        {
            // A forward cell is always open: nothing lies further from Start than the path's
            // end yet (East: nothing past its last column).
            TryStep(Grid.CellOf(previous), ref heading, winding, allowBackward: false, out Vector2Int cell);
            int id = AddRoom(type, cell, previous);
            Grid.MarkPath(id);
            return id;
        }

        private void AddHubBranches(int target)
        {
            int count = Layout.Branches.HubBranches.Evaluate(_random);
            for (int i = 0; i < count; i++)
                if (!TryAddBranch(_pathRooms[0], target))
                {
                    _context.Warnings.Add($"Only placed {i} of {count} hub branches — Start has no free side left.");
                    return;
                }
        }

        private void FillWithBranches(int target, int optional)
        {
            int failures = 0;
            while (Graph.Nodes.Count < target && failures < PlacementAttempts)
                failures = TryAddBranch(PickAttachRoom(), target) ? 0 : failures + 1;

            if (Graph.Nodes.Count < target)
                _context.Warnings.Add($"Placed {optional - (target - Graph.Nodes.Count)} of {optional} optional rooms — raise Max Spread or Max Connections Per Node.");
        }

        private int PickAttachRoom() =>
            _branchRooms.Count > 0 && _random.Chance(Layout.Branches.ForkChance)
                ? _random.Pick(_branchRooms)
                : _random.Pick(_pathRooms);

        private bool TryAddBranch(int attachId, int target)
        {
            if (_context.IsFull(attachId)) return false;

            MapBranchSettings branches = Layout.Branches;
            // Hub branches still get a room when the path alone already meets the target.
            int roomsLeft = Mathf.Max(1, target - Graph.Nodes.Count);
            int length    = Mathf.Min(Mathf.Max(1, branches.Length.Evaluate(_random)), roomsLeft);
            int previous = attachId;
            Vector2Int heading = Vector2Int.zero;

            for (int i = 0; i < length; i++)
            {
                if (!TryStep(Grid.CellOf(previous), ref heading, branches.Winding, allowBackward: true, out Vector2Int cell))
                    break;

                previous = AddRoom(FillType, cell, previous);
                _branchRooms.Add(previous);
            }

            if (previous == attachId) return false;

            if (_random.Chance(branches.RejoinChance))
                TryRejoin(previous);
            return true;
        }

        // Links a branch's last room to a neighbouring room it isn't already linked to.
        private void TryRejoin(int endId)
        {
            Grid.CollectNeighborRooms(Grid.CellOf(endId), _neighbors);
            _random.Shuffle(_neighbors);

            foreach (int other in _neighbors)
                if (!_context.IsBossOrExit(other) && _context.TryLink(endId, other))
                    return;
        }

        // Keeps going straight unless the winding roll turns (or straight is blocked). A
        // zero heading picks any open direction.
        private bool TryStep(Vector2Int from, ref Vector2Int heading, float winding, bool allowBackward, out Vector2Int cell)
        {
            Grid.CollectOpenNeighbors(from, _openCells);
            if (!allowBackward) RemoveBackward(from);

            cell = from;
            if (_openCells.Count == 0) return false;

            bool straight = heading != Vector2Int.zero && _openCells.Contains(from + heading) && !_random.Chance(winding);
            cell    = straight ? from + heading : _random.Pick(_openCells);
            heading = cell - from;
            return true;
        }

        // East: no step toward Start's column. Any: only steps that end further from Start.
        private void RemoveBackward(Vector2Int from)
        {
            if (Layout.MainPath.Direction == MapPathDirection.East)
            {
                _openCells.Remove(from + Vector2Int.left);
                return;
            }

            int distance = StepsFromStart(from);
            _openCells.RemoveAll(c => StepsFromStart(c) <= distance);
        }

        // Start sits at the origin.
        private static int StepsFromStart(Vector2Int cell) => Mathf.Abs(cell.x) + Mathf.Abs(cell.y);

        private int AddRoom(MapNodeType type, Vector2Int cell, int linkFrom)
        {
            Vector2 spacing = _context.NodeSpacing;
            MapNode node = Graph.AddNode(type, new Vector2(cell.x * spacing.x, cell.y * spacing.y));
            Grid.Place(node.Id, cell);

            if (linkFrom != NoRoom)
                Graph.Connect(linkFrom, node.Id);
            return node.Id;
        }
    }
}
