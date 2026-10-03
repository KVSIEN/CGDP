using System.Collections.Generic;
using UnityEngine;
using CGD.Core;
using CGD.Factions;
using CGD.Map;

namespace CGD.Level
{
    // Turns map nodes into rooms:
    //   1. Each node gets a grid cell from its editor position (the generator already lays
    //      nodes out on a column/lane grid; hand-moved nodes snap to the nearest free cell).
    //   2. It picks a function (lobby, park…) for its type, favouring functions that prefer
    //      the room's ship section. A function spanning several cells
    //      claims free neighbouring cells; with none free it stays in one.
    //   3. The room fills its cells minus a gap for corridors — a single-cell room may be
    //      smaller and centred — and gets a hand-built landmark or a generated floor plan.
    public class RoomPlacer
    {
        // Smallest room a function may ask for: room for a doorway on each side.
        private const int MinRoomTiles = 4;

        private readonly LevelBuildSettings _settings;
        private readonly RoomShapeSelector _shapes;

        public RoomPlacer(LevelBuildSettings settings)
        {
            _settings = settings;
            _shapes   = new RoomShapeSelector(settings.MinRoomWidthTiles);
        }

        public void Place(MapGraph graph, Vector2 nodeSpacing, Seed seed, LevelLayout layout, MapContentSettings content)
        {
            Dictionary<int, Vector2Int> cells = AssignCells(graph, nodeSpacing);
            Dictionary<int, int> connections = CountConnections(graph);
            var claimed = new HashSet<Vector2Int>(cells.Values);

            foreach (MapNode node in graph.Nodes)
            {
                RandomStream random = seed.Derive(node.Id).Stream();
                MapSectionDefinition section = content != null && node.HasSection ? content.GetSection(node.Section) : null;
                RoomFunction function = random.PickWeighted(_settings.FunctionsFor(node.Type), f => f != null ? f.WeightIn(section) : 0f);
                RectInt block = ClaimBlock(cells[node.Id], function != null ? function.Cells : Vector2Int.one, claimed, random);
                connections.TryGetValue(node.Id, out int count);

                float height = function != null && function.WallHeight > 0f ? function.WallHeight : _settings.WallHeight;
                FactionDefinition faction = content != null && node.HasFaction ? content.GetFaction(node.Faction) : null;
                FactionDefinition breach  = content != null && node.HasBreachFaction ? content.GetFaction(node.BreachFaction) : null;
                LandmarkRoomDefinition landmark = function != null ? function.Landmark : null;
                if (landmark != null && TryPlaceLandmark(landmark, BlockArea(block), count, out RoomFootprint landmarkFootprint))
                {
                    layout.AddRoom(new LevelRoom(node, landmarkFootprint, function, height, landmark, faction, breach));
                    continue;
                }
                if (landmark != null)
                    layout.Warnings.Add($"Landmark '{landmark.name}' doesn't fit {node.Type} #{node.Id} (too big, or fewer doorways than its {count} connections); generated instead.");

                RectInt area = RoomArea(block, function, random);
                IReadOnlyList<RoomShape> shapeList = function != null && function.Shapes.Count > 0 ? function.Shapes : _settings.DefaultShapes;
                layout.AddRoom(new LevelRoom(node, _shapes.Build(shapeList, count, area, random), function, height, faction: faction, breachFaction: breach,
                                             chamfer: function != null ? function.ChamferOr(_settings.RoomChamfer) : _settings.RoomChamfer));
            }
        }

        // --- Cells ---------------------------------------------------------------------

        private static Dictionary<int, Vector2Int> AssignCells(MapGraph graph, Vector2 nodeSpacing)
        {
            var cells = new Dictionary<int, Vector2Int>();
            var taken = new HashSet<Vector2Int>();
            Vector2 spacing = new(Mathf.Max(1f, nodeSpacing.x), Mathf.Max(1f, nodeSpacing.y));

            foreach (MapNode node in graph.Nodes)
            {
                // The editor's y grows downward; the level's tile y is world +z.
                var preferred = new Vector2Int(
                    Mathf.RoundToInt(node.Position.x / spacing.x),
                    -Mathf.RoundToInt(node.Position.y / spacing.y));
                Vector2Int coords = NearestFreeCell(preferred, taken);
                taken.Add(coords);
                cells[node.Id] = coords;
            }
            return cells;
        }

        private static Vector2Int NearestFreeCell(Vector2Int preferred, HashSet<Vector2Int> taken)
        {
            if (!taken.Contains(preferred)) return preferred;

            for (int radius = 1; ; radius++)
                for (int dx = -radius; dx <= radius; dx++)
                    for (int dy = -radius; dy <= radius; dy++)
                    {
                        if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != radius) continue;
                        var candidate = preferred + new Vector2Int(dx, dy);
                        if (!taken.Contains(candidate)) return candidate;
                    }
        }

        // A block of `size` cells (either way round) containing `home` whose other cells are
        // unclaimed — every node's own cell is claimed from the start, so a big room never
        // pushes a neighbour out. Tried in random order; one cell when nothing fits.
        private static RectInt ClaimBlock(Vector2Int home, Vector2Int size, HashSet<Vector2Int> claimed, RandomStream random)
        {
            var single = new RectInt(home, Vector2Int.one);
            if (size == Vector2Int.one) return single;

            var candidates = new List<RectInt>();
            foreach (Vector2Int s in size.x == size.y ? new[] { size } : new[] { size, new Vector2Int(size.y, size.x) })
                for (int ox = home.x - s.x + 1; ox <= home.x; ox++)
                    for (int oy = home.y - s.y + 1; oy <= home.y; oy++)
                        candidates.Add(new RectInt(new Vector2Int(ox, oy), s));
            random.Shuffle(candidates);

            foreach (RectInt block in candidates)
            {
                if (!IsFree(block, home, claimed)) continue;
                foreach (Vector2Int cell in block.allPositionsWithin) claimed.Add(cell);
                return block;
            }
            return single;
        }

        private static bool IsFree(RectInt block, Vector2Int home, HashSet<Vector2Int> claimed)
        {
            foreach (Vector2Int cell in block.allPositionsWithin)
                if (cell != home && claimed.Contains(cell)) return false;
            return true;
        }

        // --- Areas -----------------------------------------------------------------------

        // The tiles a block of cells offers: all of it but half a gap on each side.
        private RectInt BlockArea(RectInt block)
        {
            int cell = _settings.CellTiles, inset = _settings.GapTiles / 2;
            return new RectInt(block.xMin * cell + inset, block.yMin * cell + inset,
                block.width * cell - _settings.GapTiles, block.height * cell - _settings.GapTiles);
        }

        // Multi-cell rooms fill their block; single-cell rooms may be smaller, centred.
        private RectInt RoomArea(RectInt block, RoomFunction function, RandomStream random)
        {
            RectInt area = BlockArea(block);
            if (function == null || block.width * block.height > 1) return area;

            int size = function.RollSize(random, MinRoomTiles, _settings.RoomTiles);
            int inset = (_settings.RoomTiles - size) / 2;
            return new RectInt(area.xMin + inset, area.yMin + inset, size, size);
        }

        private static bool TryPlaceLandmark(LandmarkRoomDefinition landmark, RectInt area, int connections, out RoomFootprint footprint)
        {
            footprint = null;
            Vector2Int size = landmark.SizeTiles;
            if (size.x < 1 || size.y < 1 || size.x > area.width || size.y > area.height) return false;

            var origin = new Vector2Int(area.xMin + (area.width - size.x) / 2, area.yMin + (area.height - size.y) / 2);
            var sockets = new List<DoorSocket>();
            foreach (LandmarkDoorway doorway in landmark.Doorways)
                sockets.Add(new DoorSocket(origin + doorway.Tile, doorway.Outward));

            footprint = RoomFootprint.Rectangle(new RectInt(origin, size), sockets);
            return footprint.Sockets.Count >= connections;
        }

        private static Dictionary<int, int> CountConnections(MapGraph graph)
        {
            var counts = new Dictionary<int, int>();
            foreach (MapConnection connection in graph.Connections)
            {
                counts.TryGetValue(connection.A, out int a);
                counts.TryGetValue(connection.B, out int b);
                counts[connection.A] = a + 1;
                counts[connection.B] = b + 1;
            }
            return counts;
        }
    }
}
