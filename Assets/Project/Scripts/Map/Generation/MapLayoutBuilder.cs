using System.Collections.Generic;
using UnityEngine;

namespace CGD.Map
{
    // Builds the connection structure: a main path from Start to Exit, side branches
    // that dead-end or rejoin further ahead, and shortcuts that skip along the path.
    // Every room is created with the fill type; MapTypeAssigner decides the real types.
    //
    // Positions are a simple column/lane grid for the editor: column = steps along the
    // main path, lane = rows above (-) or below (+) it.
    internal class MapLayoutBuilder
    {
        private const int PlacementAttempts = 8;

        private readonly MapGenerationContext _context;
        private readonly List<int>            _mainPath = new();
        private readonly List<LaneSpan>       _occupiedLanes = new();

        // Rooms between Start and Boss; Boss sits at column RoomCount + 1.
        private int _roomCount;

        public MapLayoutBuilder(MapGenerationContext context) => _context = context;

        private MapGraph              Graph    => _context.Graph;
        private MapGenerationSettings Settings => _context.Settings;
        private float BossColumn => _roomCount + 1;

        public void Build()
        {
            BuildMainPath();

            int branches = _context.Roll(Settings.BranchCount);
            for (int i = 0; i < branches; i++)
                if (!TryAddBranch())
                    _context.Warnings.Add($"Only placed {i} of {branches} branches — raise Max Connections Per Node or the main path length.");

            int shortcuts = _context.Roll(Settings.ShortcutCount);
            for (int i = 0; i < shortcuts; i++)
                if (!TryAddShortcut())
                    _context.Warnings.Add($"Only placed {i} of {shortcuts} shortcuts.");
        }

        private void BuildMainPath()
        {
            _roomCount = Mathf.Max(1, _context.Roll(Settings.MainPathLength));

            AddMainNode(MapNodeType.Start, 0);
            for (int column = 1; column <= _roomCount; column++)
                AddMainNode(Settings.FillType, column);
            AddMainNode(MapNodeType.Boss, _roomCount + 1);
            AddMainNode(MapNodeType.Exit, _roomCount + 2);
        }

        private void AddMainNode(MapNodeType type, int column)
        {
            MapNode node = Graph.AddNode(type, Position(column, 0));
            bool structural = MapGenerationSettings.IsStructural(type);
            _context.Slots.Add(new MapSlot(node.Id, Progress(column), onMainPath: true, structural));

            if (_mainPath.Count > 0)
                Graph.Connect(_mainPath[^1], node.Id);
            _mainPath.Add(node.Id);
        }

        // Branches attach anywhere from Start up to the last room before the boss, so
        // the boss keeps a single entrance.
        private bool TryAddBranch()
        {
            for (int attempt = 0; attempt < PlacementAttempts; attempt++)
            {
                int attachColumn = _context.Random.Next(0, _roomCount + 1);
                if (IsFull(_mainPath[attachColumn])) continue;

                AddBranch(attachColumn, Mathf.Max(1, _context.Roll(Settings.BranchLength)));
                return true;
            }
            return false;
        }

        private void AddBranch(int attachColumn, int length)
        {
            int firstColumn = attachColumn + 1;
            int lastColumn  = attachColumn + length;

            // Rejoining at the branch's last column keeps the branch one step longer than
            // the main path segment it runs beside, so the main path stays the shortest
            // route and branch rooms keep the depth of their column.
            int rejoinColumn = lastColumn;

            bool rejoins = rejoinColumn <= _roomCount
                        && !IsFull(_mainPath[rejoinColumn])
                        && _context.Chance(Settings.BranchRejoinChance);

            int lane     = ClaimLane(firstColumn, lastColumn);
            int previous = _mainPath[attachColumn];

            for (int column = firstColumn; column <= lastColumn; column++)
            {
                MapNode node = Graph.AddNode(Settings.FillType, Position(column, lane));
                _context.Slots.Add(new MapSlot(node.Id, Progress(column), onMainPath: false, isStructural: false));

                ConnectionType link = column == firstColumn ? RollEntranceType() : ConnectionType.Normal;
                Graph.Connect(previous, node.Id, link);
                previous = node.Id;
            }

            if (rejoins)
                Graph.Connect(previous, _mainPath[rejoinColumn]);
            else
                _context.GetSlot(previous).IsDeadEnd = true;
        }

        private ConnectionType RollEntranceType()
        {
            if (_context.Chance(Settings.LockedBranchChance)) return ConnectionType.Locked;
            if (_context.Chance(Settings.SecretBranchChance)) return ConnectionType.Secret;
            return ConnectionType.Normal;
        }

        // Skips one or two rooms ahead, never past the last room before the boss.
        private bool TryAddShortcut()
        {
            for (int attempt = 0; attempt < PlacementAttempts; attempt++)
            {
                int from = _context.Random.Next(0, _roomCount + 1);
                int to   = from + _context.Random.Next(2, 4);
                if (to > _roomCount) continue;

                int a = _mainPath[from];
                int b = _mainPath[to];
                if (IsFull(a) || IsFull(b)) continue;

                if (Graph.Connect(a, b, ConnectionType.Shortcut)) return true;
            }
            return false;
        }

        private bool IsFull(int nodeId) => Graph.Degree(nodeId) >= Settings.MaxConnectionsPerNode;

        // Picks a random side, then the lane closest to the main path whose columns
        // aren't already used by another branch on that side.
        private int ClaimLane(int firstColumn, int lastColumn)
        {
            int side = _context.Chance(0.5f) ? -1 : 1;
            int lane = side;

            while (_occupiedLanes.Exists(s => s.Lane == lane && s.Overlaps(firstColumn, lastColumn)))
                lane += side;

            _occupiedLanes.Add(new LaneSpan(lane, firstColumn, lastColumn));
            return lane;
        }

        private float Progress(int column) => Mathf.Clamp01(column / BossColumn);

        private Vector2 Position(int column, int lane) =>
            new(column * Settings.NodeSpacing.x, lane * Settings.NodeSpacing.y);

        private readonly struct LaneSpan
        {
            public LaneSpan(int lane, int firstColumn, int lastColumn)
            {
                Lane        = lane;
                FirstColumn = firstColumn;
                LastColumn  = lastColumn;
            }

            public int Lane        { get; }
            public int FirstColumn { get; }
            public int LastColumn  { get; }

            public bool Overlaps(int first, int last) => first <= LastColumn && last >= FirstColumn;
        }
    }
}
