using System.Collections.Generic;
using UnityEngine;
using CGD.Core;
using CGD.Director;
using CGD.Map;

namespace CGD.Level
{
    // A generated level's route for the director (IRouteSpots): the rooms on the shortest way
    // from the player's room to the Exit are ahead, those on the way back to the Start behind.
    // Only the first few rooms each way count (the caller says how many), so a group arrives
    // near where the player is going or has just been, not across the map. The Start room, the
    // safe hub, never counts, nor any room the map director keeps calm (BeatTuning.NoReinforcements).
    // A walk through such a room still goes on to the rooms past it.
    public sealed class LevelRoute : IRouteSpots
    {
        private const RoomTileTags Taken = RoomTileTags.Structure | RoomTileTags.Prop;

        private readonly LevelLayout _layout;
        private readonly MapGraphAnalysis _analysis;
        private readonly Transform _level;
        private readonly Dictionary<(int, RouteSide), List<int>> _paths = new();
        private readonly List<LevelRoom> _along = new();
        private readonly System.Func<LevelRoom, bool> _keptCalm;

        // `keptCalm`: rooms groups never arrive in (null = only the Start room).
        public LevelRoute(LevelLayout layout, MapGraph graph, Transform level, System.Func<LevelRoom, bool> keptCalm = null)
        {
            _keptCalm = keptCalm;
            _layout   = layout;
            _analysis = new MapGraphAnalysis(graph);
            _level    = level;
        }

        public bool TryPick(Vector3 player, RouteSide side, int rooms, RandomStream random, out Vector3 point)
        {
            point = default;
            if (!TryPickTile(_level.InverseTransformPoint(player), side, rooms, random, out Vector2Int tile)) return false;
            point = _level.TransformPoint(_layout.TileToFloor(tile));
            return true;
        }

        // The same in the level's own space, in tiles: kept apart from the Transform so it can be tested.
        internal bool TryPickTile(Vector3 localPlayer, RouteSide side, int rooms, RandomStream random, out Vector2Int tile)
        {
            tile = default;
            LevelRoom room = _layout.RoomAtLocal(localPlayer);
            if (room == null) return false;

            IReadOnlyList<LevelRoom> along = RoomsAlong(room.Node.Id, side, rooms);
            if (along.Count == 0) return false;
            return TryFloorTile(random.Pick(along), random, out tile);
        }

        // The first `count` rooms after the player's on the shortest way toward the exit or the
        // start. The list is reused by the next call.
        internal IReadOnlyList<LevelRoom> RoomsAlong(int from, RouteSide side, int count)
        {
            _along.Clear();
            foreach (int id in PathFrom(from, side))
            {
                if (_along.Count >= count) break;
                if (id != from && _layout.Rooms.TryGetValue(id, out LevelRoom room) && room.Node.Type != MapNodeType.Start
                    && (_keptCalm == null || !_keptCalm(room)))
                    _along.Add(room);
            }
            return _along;
        }

        // The graph doesn't change during a level, so each room's paths are worked out once.
        private List<int> PathFrom(int from, RouteSide side)
        {
            if (_paths.TryGetValue((from, side), out List<int> path)) return path;

            int to = side == RouteSide.Ahead ? _analysis.ExitId : _analysis.StartId;
            path = to != MapGraphAnalysis.Unreachable && to != from ? _analysis.ShortestPath(from, to) : new List<int>();
            _paths[(from, side)] = path;
            return path;
        }

        // A floor tile nothing stands on, tried a few times at random.
        private static bool TryFloorTile(LevelRoom room, RandomStream random, out Vector2Int tile)
        {
            IReadOnlyList<Vector2Int> tiles = room.Footprint.Tiles;
            for (int attempt = 0; attempt < 8 && tiles.Count > 0; attempt++)
            {
                tile = random.Pick(tiles);
                if (room.Structure == null || !room.Structure.Has(tile, Taken)) return true;
            }
            tile = default;
            return false;
        }
    }
}
