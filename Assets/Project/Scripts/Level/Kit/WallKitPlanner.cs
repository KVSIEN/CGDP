using System;
using System.Collections.Generic;
using UnityEngine;

namespace CGD.Level
{
    // Turns a LevelWallPlan into kit pieces: straight runs get the straight piece repeated
    // (as many whole pieces as fit, stretched to close the gap), walls off the grid
    // (bevels, curves) one angled piece each, a post wherever walls meet at a real corner
    // or a wall ends (a doorway's jambs), and a frame on every doorway. Each wall uses its
    // room's kit (`kitFor`; corridors pass null); walls whose kit is null get nothing.
    public static class WallKitPlanner
    {
        public static List<WallKitPlacement> Plan(LevelLayout layout, LevelWallPlan walls, Func<LevelRoom, WallKit> kitFor)
        {
            var placements = new List<WallKitPlacement>();
            float tile = layout.TileSize;

            foreach (LevelWallPlan.Segment segment in walls.Segments)
            {
                WallKit kit = kitFor(segment.Room);
                if (kit != null) AddWall(placements, kit, segment, tile);
            }

            AddPosts(placements, walls, kitFor, tile);

            foreach (LevelDoorway doorway in layout.Doorways)
            {
                WallKit kit = kitFor(doorway.Room);
                if (kit == null || kit.DoorFrame == null) continue;

                Vector3 position = (layout.TileToLocal(doorway.RoomTile) + layout.TileToLocal(doorway.OutsideTile)) * 0.5f;
                Vector3 outward  = new(doorway.Outward.x, 0f, doorway.Outward.y);
                placements.Add(new WallKitPlacement(kit.DoorFrame, position, Quaternion.LookRotation(outward), Vector3.one));
            }
            return placements;
        }

        private static void AddWall(List<WallKitPlacement> placements, WallKit kit, LevelWallPlan.Segment segment, float tile)
        {
            Vector3 from = ToLocal(segment.From, tile), to = ToLocal(segment.To, tile);
            Vector3 along = to - from;
            float length = along.magnitude;
            if (length < 0.01f) return;

            bool onGrid = Mathf.Abs(along.x) < 0.001f || Mathf.Abs(along.z) < 0.001f;
            GameObject prefab = onGrid ? kit.Straight : kit.Angled;
            if (prefab == null) return;

            int count = onGrid ? Mathf.Max(1, Mathf.RoundToInt(length / kit.PieceLength)) : 1;
            Vector3 dir = along / length;
            Quaternion rotation = Facing(dir);
            var scale = new Vector3(length / count / kit.PieceLength, segment.Height / kit.PieceHeight, 1f);
            for (int i = 0; i < count; i++)
                placements.Add(new WallKitPlacement(prefab, from + along * ((float)i / count), rotation, scale));
        }

        // A post where a wall ends (nothing continues from it) or turns by at least the
        // kit's PostMinAngle. Points are matched on a millimetre grid.
        private static void AddPosts(List<WallKitPlacement> placements, LevelWallPlan walls, Func<LevelRoom, WallKit> kitFor, float tile)
        {
            var ends = new Dictionary<Vector2Int, List<(LevelWallPlan.Segment segment, bool atStart)>>();
            foreach (LevelWallPlan.Segment segment in walls.Segments)
            {
                Add(ends, segment.From, (segment, true));
                Add(ends, segment.To, (segment, false));
            }

            foreach (var (_, meeting) in ends)
            {
                LevelWallPlan.Segment first = meeting[0].segment;
                WallKit kit = kitFor(first.Room);
                if (kit == null || kit.Post == null) continue;
                if (meeting.Count == 2 && Turn(meeting) < kit.PostMinAngle) continue;

                Vector2 point = meeting[0].atStart ? first.From : first.To;
                float height = 0f;
                foreach (var (segment, _) in meeting) height = Mathf.Max(height, segment.Height);
                placements.Add(new WallKitPlacement(kit.Post, ToLocal(point, tile), Facing(Direction(first, tile)),
                                                    new Vector3(1f, height / kit.PieceHeight, 1f)));
            }
        }

        private static void Add(Dictionary<Vector2Int, List<(LevelWallPlan.Segment, bool)>> ends, Vector2 point,
                                (LevelWallPlan.Segment, bool) end)
        {
            var key = Vector2Int.RoundToInt(point * 1000f);
            if (!ends.TryGetValue(key, out var list)) ends[key] = list = new List<(LevelWallPlan.Segment, bool)>(2);
            list.Add(end);
        }

        // Degrees between the wall arriving at a point and the one leaving it.
        private static float Turn(List<(LevelWallPlan.Segment segment, bool atStart)> meeting)
        {
            Vector2 a = meeting[0].segment.To - meeting[0].segment.From;
            Vector2 b = meeting[1].segment.To - meeting[1].segment.From;
            return Vector2.Angle(a, b);
        }

        private static Vector3 Direction(LevelWallPlan.Segment segment, float tile) =>
            (ToLocal(segment.To, tile) - ToLocal(segment.From, tile)).normalized;

        // +X along the wall, +Z toward the floor (the wall plan keeps the floor on the left).
        private static Quaternion Facing(Vector3 along) =>
            Quaternion.LookRotation(new Vector3(-along.z, 0f, along.x), Vector3.up);

        private static Vector3 ToLocal(Vector2 tilePoint, float tile) => new(tilePoint.x * tile, 0f, tilePoint.y * tile);
    }
}
